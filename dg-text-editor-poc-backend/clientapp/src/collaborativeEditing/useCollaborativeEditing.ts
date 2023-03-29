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

  const { otherUsers, updateUser } = useCollaborativeEditingContext();

  const handleChange = useCallback(() => {
    if (!isRemoteRef.current) {
      console.log({ operations: editor.operations });

      const notSelectionOperations = editor.operations.filter(
        (operation) => !Operation.isSelectionOperation(operation)
      );

      if (notSelectionOperations.length > 0) {
        collaborativeConnection.sendOperations({
          documentRevision: 0,
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
  }, [editor, otherUsers, updateUser]);

  const handleReceiveOperations = useCallback(
    (batch: OperationBatch) => {
      try {
        const { operations } = batch;

        console.log("Receive Operation:", { operations });

        isRemoteRef.current = true;

        Editor.withoutNormalizing(editor, () => {
          operations.forEach((operation) => editor.apply({ ...operation }));

          Promise.resolve().then(() => {
            isRemoteRef.current = false;
          });
        });

        transformOtherUserSelections({ operations, otherUsers, updateUser });
      } catch (error) {
        console.error(error);
      }
    },
    [editor, otherUsers, updateUser]
  );

  useEffect(() => {
    collaborativeConnection.receiveOperations.on(handleReceiveOperations);

    return () =>
      collaborativeConnection.receiveOperations.off(handleReceiveOperations);
  }, [handleReceiveOperations]);

  return { handleChange };
};
