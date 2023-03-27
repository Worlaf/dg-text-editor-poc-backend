import { BaseEditor, Descendant } from "slate";
import { ReactEditor } from "slate-react";

type CustomElement = { type: "paragraph"; children: Descendant[] };

export type ElementType = CustomElement["type"];

type FormattedText = {
  isBold?: boolean;
  isItalic?: boolean;
  isStrikethrough?: boolean;
  backgroundColor?: string;
  selectionBackgroundColor?: string;
  selectionEndLabel?: string;
};

export type CustomText = { text: string } & FormattedText;

declare module "slate" {
  interface CustomTypes {
    Editor: BaseEditor & ReactEditor;
    Element: CustomElement;
    Text: CustomText;
  }
}
