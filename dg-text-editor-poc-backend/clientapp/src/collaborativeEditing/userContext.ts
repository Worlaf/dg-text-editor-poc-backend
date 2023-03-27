import { Range } from "slate";

export type UserContext = {
  userId: string;
  userName: string;
  documentSelection?: Range;
};

export type UserModel = UserContext & { color: string };
