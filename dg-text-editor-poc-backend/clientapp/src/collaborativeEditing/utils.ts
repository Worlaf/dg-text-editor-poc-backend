import { BaseRange, Operation, Path, Point } from "slate";
import { isNotNil } from "../utils/typeGuards";
import { OperationBatch } from "./operationBatch";
import { UserModel, UserContext } from "./userContext";

const USER_COLORS = [
  "#e6194B",
  "#3cb44b",
  "#f58231",
  "#911eb4",
  "#f032e6",
  "#9A6324",
  "#808000",
  "#000075",
];

export const getUserColor = (index: number) =>
  USER_COLORS[index % USER_COLORS.length];

export const transformOtherUserSelections = ({
  operations,
  otherUsers,
  updateUser,
}: {
  otherUsers: ReadonlyArray<UserModel>;
  updateUser: (userModel: UserContext) => void;
  operations: ReadonlyArray<Operation>;
}) => {
  otherUsers.forEach((otherUser) => {
    const { documentSelection } = otherUser;
    if (isNotNil(documentSelection)) {
      const updatedRange = operations.reduce((range, operation): BaseRange => {
        return {
          anchor: Point.transform(range.anchor, operation) ?? range.anchor,
          focus: Point.transform(range.focus, operation) ?? range.focus,
        };
      }, documentSelection);

      updateUser({ ...otherUser, documentSelection: updatedRange });
    }
  });
};

export const transformBatch = (
  batch: OperationBatch,
  against: OperationBatch
): OperationBatch => {
  if (batch.documentRevision !== against.documentRevision) {
    console.error(
      `Unexpected document revision difference when transforming operation batch (${batch.documentRevision} against ${against.documentRevision})!`
    );
  }

  const result = {
    documentRevision: batch.documentRevision + 1,
    operations: batch.operations
      .map((operation) => transformOperationAgainstBatch(operation, against))
      .filter(isNotNil),
  };

  return result;
};

const transformOperationAgainstBatch = (
  operation: Operation,
  against: OperationBatch
): Operation | undefined => {
  let result = operation;
  for (let againstOperation of against.operations) {
    const transformedOperation = transformOperation(result, againstOperation);
    if (!isNotNil(transformedOperation)) {
      return undefined;
    }

    result = transformedOperation;
  }

  return result;
};

const transformOperation = (
  operation: Operation,
  against: Operation
): Operation | undefined => {
  if ("path" in operation) {
    if ("offset" in operation) {
      const { path, offset } = operation;
      const result = Point.transform({ path, offset }, against);

      return isNotNil(result)
        ? {
            ...operation,
            path: result.path,
            offset: result.offset,
          }
        : undefined;
    }

    if ("position" in operation) {
      const { path, position } = operation;
      const result = Point.transform({ path, offset: position }, against);

      return isNotNil(result)
        ? {
            ...operation,
            path: result.path,
            position: result.offset,
          }
        : undefined;
    }

    const result = Path.transform(operation.path, against);
    return isNotNil(result) ? { ...operation, path: result } : undefined;
  }
};
