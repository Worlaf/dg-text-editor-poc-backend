import { Operation } from "slate";

export type OperationBatch = {
  documentRevision: number;
  operations: Operation[];
};
