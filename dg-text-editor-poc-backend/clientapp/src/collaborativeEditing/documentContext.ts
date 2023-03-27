import { Descendant } from "slate";

export type Document = Descendant[];

export type DocumentContext = {
  revision: number;
  document: Document;
};
