import * as React from "react";
import {
  createEditor,
  Transforms,
  Range,
  NodeEntry,
  BaseRange,
  Text,
} from "slate";
import { Slate, Editable, withReact, RenderLeafProps } from "slate-react";
import { ToolbarButton } from "./ToolbarButton";

import "./Editor.css";
import { EditorLeaf } from "./EditorLeaf";
import { EDITOR_FEATURES } from "./utils";
import { HoveringToolbar } from "./HoveringToolbar";
import { isUndefined } from "lodash";
import { Document } from "../../collaborativeEditing/documentContext";
import { UserModel } from "../../collaborativeEditing/userContext";
import { isNotNil } from "../../utils/typeGuards";
import { useCollaborativeEditing } from "../../collaborativeEditing/useCollaborativeEditing";

type Props = {
  className?: string;
  document: Document;
  users: ReadonlyArray<UserModel>;
};

export const Editor: React.FC<Props> = ({ className, document, users }) => {
  const editor = React.useMemo(() => withReact(createEditor()), []);

  const renderLeaf = React.useCallback(
    (props: RenderLeafProps) => <EditorLeaf {...props} />,
    []
  );

  const { handleChange } = useCollaborativeEditing(editor);

  const decorate = React.useCallback(
    ([node, path]: NodeEntry): BaseRange[] => {
      if (Text.isText(node)) {
        const ranges = users
          .map((user) => {
            if (!user.documentSelection) {
              return undefined;
            }

            const { documentSelection } = user;
            const nodeRange: Range = {
              anchor: { path, offset: 0 },
              focus: { path, offset: node.text.length },
            };

            const intersection = Range.intersection(
              documentSelection,
              nodeRange
            );

            const [_start, end] = Range.edges(documentSelection);

            return isNotNil(intersection)
              ? {
                  ...intersection,
                  selectionBackgroundColor: user.color,
                  selectionEndLabel: Range.includes(nodeRange, end)
                    ? user.userName
                    : undefined,
                }
              : undefined;
          })
          .filter(isNotNil);

        return ranges;
      }

      return [];
    },
    [users]
  );

  const handleEditableKeyDown: React.KeyboardEventHandler<HTMLInputElement> = (
    event
  ) => {
    const { selection } = editor;

    // Default left/right behavior is unit:'character'.
    // This fails to distinguish between two cursor positions, such as
    // <inline>foo<cursor/></inline> vs <inline>foo</inline><cursor/>.
    // Here we modify the behavior to unit:'offset'.
    // This lets the user step into and out of the inline without stepping over characters.
    // You may wish to customize this further to only use unit:'offset' in specific cases.
    if (selection && Range.isCollapsed(selection)) {
      if (event.key === "left") {
        event.preventDefault();
        Transforms.move(editor, { unit: "offset", reverse: true });
        return;
      }
      if (event.key === "right") {
        event.preventDefault();
        Transforms.move(editor, { unit: "offset" });
        return;
      }
    }

    EDITOR_FEATURES.forEach((feature) => {
      const { hotkey } = feature;
      if (!isUndefined(hotkey)) {
        if (
          !!hotkey.altKey === event.altKey &&
          !!hotkey.ctrlKey === event.ctrlKey &&
          !!hotkey.metaKey === event.metaKey &&
          !!hotkey.shiftKey === event.shiftKey &&
          hotkey.key === event.key
        ) {
          event.preventDefault();
          feature.onActivate(editor);
        }
      }
    });
  };

  return (
    <Slate onChange={handleChange} editor={editor} value={document}>
      <div className="toolbar">
        {EDITOR_FEATURES.map((feature, index) => (
          <ToolbarButton
            icon={feature.icon}
            onClick={() => feature.onActivate(editor)}
            key={index}
            isActive={feature.isActive(editor)}
          />
        ))}
      </div>
      <HoveringToolbar />
      <Editable
        className={className}
        renderLeaf={renderLeaf}
        decorate={decorate}
        onKeyDown={handleEditableKeyDown}
      />
    </Slate>
  );
};
