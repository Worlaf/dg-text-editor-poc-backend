import { BaseRange, Operation, Point } from "slate";
import { isNotNil } from "../utils/typeGuards";
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

      console.log("update other user selection", { otherUser, updatedRange });
      updateUser({ ...otherUser, documentSelection: updatedRange });
    }
  });
};
