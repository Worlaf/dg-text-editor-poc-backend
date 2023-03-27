import _ from "lodash";
import { BaseOperation, Editor, Operation, Selection } from "slate";
import { collaborativeConnection } from "./utils";

const debouncedSendUserSelection = _.debounce(
  collaborativeConnection.sendUserSelection,
  300
);

export const withCollaborativeEditing = (editor: Editor): Editor => {
  const { apply, onChange } = editor;

  const isRemoteRef = { value: false };

  editor.onChange = (args) => {
    if (!isRemoteRef.value) {
      console.log({ operations: editor.operations });

      const notSelectionOperations = editor.operations.filter(
        (operation) => !Operation.isSelectionOperation(operation)
      );

      if (notSelectionOperations.length > 0) {
        collaborativeConnection.sendOperations(notSelectionOperations);
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

        console.log("New Selection", { selection });

        debouncedSendUserSelection(selection);
      }
    }

    onChange(args);
  };

  const handleReceiveOperations = (json: string) => {
    try {
      const operations = JSON.parse(json) as BaseOperation[];

      console.log("Receive Operation:", { operations });

      isRemoteRef.value = true;

      Editor.withoutNormalizing(editor, () => {
        operations.forEach((operation) => apply({ ...operation }));

        Promise.resolve().then(() => {
          isRemoteRef.value = false;
        });
      });
    } catch (error) {
      console.error(error);
    }
  };

  collaborativeConnection.offReceiveOperations();
  collaborativeConnection.onReceiveOperations(handleReceiveOperations);

  return editor;
};
