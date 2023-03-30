import _ from "lodash";
import { useCallback, useEffect, useRef } from "react";
import { BaseRange, Editor, Operation, Selection } from "slate";
import { isNotNil } from "../utils/typeGuards";
import { useCollaborativeEditingContext } from "./collaborativeEditingContext";
import { OperationBatch } from "./operationBatch";
import { collaborativeConnection } from "./collaborativeConnection";
import { transformBatch, transformOtherUserSelections } from "./utils";

const debouncedSendUserSelection = _.debounce((selection: BaseRange) => {
  collaborativeConnection.sendUserSelection(selection);
}, 300);

type EditorContext = {
  pendingBatches: OperationBatch[];
  sentBatch: OperationBatch | undefined;
};

const createEditorContext = (): EditorContext => ({
  pendingBatches: [],
  sentBatch: undefined,
});

const EDITOR_CONTEXTS = new WeakMap<Editor, EditorContext>();

const getOrCreateEditorContext = (editor: Editor) => {
  const context = EDITOR_CONTEXTS.get(editor);
  if (isNotNil(context)) {
    return context;
  }

  const newContext = createEditorContext();
  EDITOR_CONTEXTS.set(editor, newContext);

  return newContext;
};

const queueOperationBatch = async (editor: Editor, batch: OperationBatch) => {
  const context = getOrCreateEditorContext(editor);
  if (isNotNil(context.sentBatch)) {
    // check if operations can be merged (e.g. multiple successive text insertions)
    context.pendingBatches.push(batch);
  } else {
    await collaborativeConnection.sendOperations(batch);
    context.sentBatch = batch;
  }
};

const processPendingBatchesQueue = async (editor: Editor) => {
  const context = getOrCreateEditorContext(editor);
  if (isNotNil(context.sentBatch)) {
    return;
  } else {
    const batch = context.pendingBatches.shift();
    if (isNotNil(batch)) {
      await queueOperationBatch(editor, batch);
    }
  }
};

const acknowledgeSentBatch = (editor: Editor, newDocumentRevision: number) => {
  const context = getOrCreateEditorContext(editor);
  if (!isNotNil(context.sentBatch)) {
    console.error("Unable to acknowledge sent batch because it is undefined");
  }

  context.sentBatch = undefined;
  if (context.pendingBatches.length > 0) {
    context.pendingBatches = context.pendingBatches.map((batch) => ({
      ...batch,
      documentRevision: newDocumentRevision,
    }));
  }
};

const transformNotAcknowledgedChanges = async (
  editor: Editor,
  against: OperationBatch
) => {
  const context = getOrCreateEditorContext(editor);
  if (isNotNil(context.sentBatch)) {
    context.sentBatch = transformBatch(context.sentBatch, against);
  }

  if (context.pendingBatches.length > 0) {
    context.pendingBatches = context.pendingBatches.map((batch) =>
      transformBatch(batch, against)
    );
  }
};

const applyBatch = (editor: Editor, batch: OperationBatch) => {
  batch.operations.forEach((operation) => {
    editor.apply({ ...operation });
  });
};

const revertBatch = (editor: Editor, batch: OperationBatch) => {
  batch.operations.forEach((operation) => {
    editor.apply(Operation.inverse(operation));
  });
};

const revertNotAcknowledgedChanges = (editor: Editor) => {
  const context = getOrCreateEditorContext(editor);
  if (context.pendingBatches.length > 0) {
    context.pendingBatches
      .slice()
      .reverse()
      .forEach((batch) => revertBatch(editor, batch));
  }

  if (isNotNil(context.sentBatch)) {
    revertBatch(editor, context.sentBatch);
  }
};

const applyNotAcknowledgedChanges = (editor: Editor) => {
  const context = getOrCreateEditorContext(editor);
  if (isNotNil(context.sentBatch)) {
    applyBatch(editor, context.sentBatch);
  }

  if (context.pendingBatches.length > 0) {
    context.pendingBatches.forEach((batch) => applyBatch(editor, batch));
  }
};

export const useCollaborativeEditing = (editor: Editor) => {
  const isRemoteRef = useRef(false);

  const { otherUsers, updateUser, documentContext, setDocumentRevision } =
    useCollaborativeEditingContext();

  const handleChange = useCallback(() => {
    if (!isRemoteRef.current) {
      const notSelectionOperations = editor.operations.filter(
        (operation) => !Operation.isSelectionOperation(operation)
      );

      if (notSelectionOperations.length > 0) {
        queueOperationBatch(editor, {
          documentRevision: documentContext?.revision ?? 0,
          operations: notSelectionOperations,
        });

        transformOtherUserSelections({
          operations: notSelectionOperations,
          otherUsers,
          updateUser,
        });

        if (isNotNil(editor.selection)) {
          debouncedSendUserSelection(editor.selection);
        }
      }

      const selectionOperation = editor.operations.filter(
        Operation.isSelectionOperation
      )[0];

      if (
        isNotNil(selectionOperation) &&
        isNotNil(selectionOperation.newProperties) &&
        isNotNil(editor.selection)
      ) {
        const selection: Selection = {
          anchor:
            selectionOperation.newProperties.anchor ?? editor.selection?.anchor,
          focus:
            selectionOperation.newProperties.focus ?? editor.selection?.focus,
        };

        debouncedSendUserSelection(selection);
      }
    }
  }, [documentContext?.revision, editor, otherUsers, updateUser]);

  const handleReceiveOperations = useCallback(
    (incomingBatch: OperationBatch) => {
      try {
        const currentRevision = documentContext?.revision ?? 0;
        const { operations, documentRevision: incomingDocumentRevision } =
          incomingBatch;

        console.log(
          `Received operations [${incomingBatch.documentRevision} => ${currentRevision}] `,
          {
            batch: incomingBatch,
          }
        );

        if (currentRevision < incomingDocumentRevision) {
          // multiple changes behind
          // unexpected
          console.error(
            "Client-side operation transformation is not yet implemented :(",
            { incomingBatch }
          );
        }

        isRemoteRef.current = true;

        // if there are sent or pending changes
        // revert sent and pending changes (not acknowledged by server changes)
        // apply incoming changes
        // transform sent and pending changes against incoming ones
        // apply transformed sent and pending changes

        Editor.withoutNormalizing(editor, () => {
          revertNotAcknowledgedChanges(editor);
          applyBatch(editor, incomingBatch);
          transformNotAcknowledgedChanges(editor, incomingBatch);
          applyNotAcknowledgedChanges(editor);

          Promise.resolve().then(() => {
            isRemoteRef.current = false;
          });
        });

        transformOtherUserSelections({ operations, otherUsers, updateUser });
        setDocumentRevision(incomingDocumentRevision + 1);
      } catch (error) {
        console.error(error);
      }
    },
    [
      documentContext?.revision,
      editor,
      otherUsers,
      setDocumentRevision,
      updateUser,
    ]
  );

  const handleAcknowledgeChanges = useCallback(
    async (newDocumentRevision: number) => {
      console.log(`Acknowledge changes: ${newDocumentRevision}`);
      setDocumentRevision(newDocumentRevision);
      acknowledgeSentBatch(editor, newDocumentRevision);
      await processPendingBatchesQueue(editor);
    },
    [editor, setDocumentRevision]
  );

  useEffect(() => {
    collaborativeConnection.receiveOperations.on(handleReceiveOperations);

    return () =>
      collaborativeConnection.receiveOperations.off(handleReceiveOperations);
  }, [handleReceiveOperations]);

  useEffect(() => {
    collaborativeConnection.acknowledgeChanges.on(handleAcknowledgeChanges);

    return () =>
      collaborativeConnection.acknowledgeChanges.off(handleAcknowledgeChanges);
  }, [handleAcknowledgeChanges]);

  return { handleChange };
};
