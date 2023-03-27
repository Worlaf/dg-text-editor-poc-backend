import { IconDefinition } from "@fortawesome/fontawesome-svg-core";
import { Editor, Transforms, Text } from "slate";
import { CustomText } from "./customTypes";
import { solid } from "@fortawesome/fontawesome-svg-core/import.macro";
import { isUndefined } from "lodash";

type ToggleableTextMarkProp = keyof Pick<
  CustomText,
  "isBold" | "isItalic" | "isStrikethrough"
>;

type StringTextMarkProp = keyof Pick<CustomText, "backgroundColor">;

type HotkeyData = Partial<
  Pick<
    React.KeyboardEvent,
    "altKey" | "ctrlKey" | "shiftKey" | "metaKey" | "key"
  >
>;

type EditorFeature = {
  icon: IconDefinition;
  isAvailableInHoveringToolbar: (editor: Editor) => boolean;
  isActive: (editor: Editor) => boolean;
  onActivate: (editor: Editor) => void;
  hotkey?: HotkeyData;
};

export const EDITOR_FEATURES: readonly EditorFeature[] = [
  {
    icon: solid("bold"),
    isActive: (editor) => isMarkActive(editor, "isBold"),
    isAvailableInHoveringToolbar: () => true,
    onActivate: (editor) => toggleMark(editor, "isBold"),
    hotkey: { ctrlKey: true, key: "b" },
  },
  {
    icon: solid("italic"),
    isActive: (editor) => isMarkActive(editor, "isItalic"),
    isAvailableInHoveringToolbar: () => true,
    onActivate: (editor) => toggleMark(editor, "isItalic"),
    hotkey: { ctrlKey: true, key: "i" },
  },
  {
    icon: solid("droplet"),
    isActive: () => false,
    isAvailableInHoveringToolbar: () => true,
    onActivate: (editor) => setMarkTextValue(editor, "backgroundColor", "red"),
  },
  {
    icon: solid("droplet"),
    isActive: () => false,
    isAvailableInHoveringToolbar: () => true,
    onActivate: (editor) => setMarkTextValue(editor, "backgroundColor", "blue"),
  },
  {
    icon: solid("droplet-slash"),
    isActive: () => false,
    isAvailableInHoveringToolbar: () => true,
    onActivate: (editor) => setMarkTextValue(editor, "backgroundColor", ""),
  },
  {
    icon: solid("strikethrough"),
    isActive: (editor) => isMarkActive(editor, "isStrikethrough"),
    isAvailableInHoveringToolbar: () => true,
    onActivate: (editor) => toggleMark(editor, "isStrikethrough"),
    hotkey: { ctrlKey: true, key: "s" },
  },
];

export const isMarkActive = (
  editor: Editor,
  markProp: ToggleableTextMarkProp
) => {
  const [match] = Array.from(
    Editor.nodes(editor, {
      match: (node) => Text.isText(node) && !!node[markProp],
      universal: true,
    })
  );

  return !isUndefined(match);
};

export const toggleMark = (
  editor: Editor,
  markProp: ToggleableTextMarkProp
) => {
  const isActive = isMarkActive(editor, markProp);
  Transforms.setNodes(
    editor,
    { [markProp]: !isActive },
    { match: (node) => Text.isText(node), split: true }
  );
};

export const setMarkTextValue = (
  editor: Editor,
  markProp: StringTextMarkProp,
  value: string
) => {
  Transforms.setNodes(
    editor,
    { [markProp]: value },
    { match: (node) => Text.isText(node), split: true }
  );
};
