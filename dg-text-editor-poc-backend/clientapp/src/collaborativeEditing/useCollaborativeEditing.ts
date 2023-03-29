import _ from "lodash";
import { useCallback, useEffect, useRef } from "react";
import { BaseRange, Editor, Operation, Selection } from "slate";
import { isNotNil } from "../utils/typeGuards";
import { useCollaborativeEditingContext } from "./collaborativeEditingContext";
import { OperationBatch } from "./operationBatch";
import { collaborativeConnection } from "./collaborativeConnection";
import { transformOtherUserSelections } from "./utils";

const debouncedSendUserSelection = _.debounce((selection: BaseRange) => {
  console.log("Send selection", { selection });
  collaborativeConnection.sendUserSelection(selection);
}, 300);

export const useCollaborativeEditing = (editor: Editor) => {
  const isRemoteRef = useRef(false);

  const { otherUsers, updateUser, documentContext, setDocumentRevision } =
    useCollaborativeEditingContext();

  const handleChange = useCallback(() => {
    if (!isRemoteRef.current) {
      console.log({ operations: editor.operations });

      const notSelectionOperations = editor.operations.filter(
        (operation) => !Operation.isSelectionOperation(operation)
      );

      if (notSelectionOperations.length > 0) {
        collaborativeConnection.sendOperations({
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
        !!selectionOperation &&
        !!selectionOperation.newProperties &&
        editor.selection
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
    (batch: OperationBatch) => {
      try {
        const currentRevision = documentContext?.revision ?? 0;
        const { operations, documentRevision: batchDocumentRevision } = batch;

        console.log("Received operations", { batch });

        if (currentRevision < batchDocumentRevision) {
          // todo:
          console.error(
            "Client-side operation transformation is not yet implemented :("
          );
        }

        isRemoteRef.current = true;

        Editor.withoutNormalizing(editor, () => {
          operations.forEach((operation) => editor.apply({ ...operation }));

          Promise.resolve().then(() => {
            isRemoteRef.current = false;
          });
        });

        transformOtherUserSelections({ operations, otherUsers, updateUser });
        setDocumentRevision(batchDocumentRevision + 1);
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
    (newDocumentRevision: number) => {
      setDocumentRevision(newDocumentRevision);
    },
    [setDocumentRevision]
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
