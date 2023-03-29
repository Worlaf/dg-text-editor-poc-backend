import * as msSignalR from "@microsoft/signalr";
import { Range } from "slate";
import { OperationBatch } from "./operationBatch";
import { UserContext } from "./userContext";

const hubUrl =
  process.env.NODE_ENV === "development"
    ? "http://localhost:5221/editor"
    : "/editor";

const connection = new msSignalR.HubConnectionBuilder().withUrl(hubUrl).build();

type GenericHandler = (...args: any[]) => any;
const createSignalrMethod = (methodName: string) => ({
  on: (handler: GenericHandler) => connection.on(methodName, handler),
  off: (handler: GenericHandler) => connection.off(methodName, handler),
  offAll: () => connection.off(methodName),
});

export const collaborativeConnection = {
  receiveOperations: createSignalrMethod("ReceiveOperations"),
  userConnected: createSignalrMethod("UserConnected"),
  userDisconnected: createSignalrMethod("UserDisconnected"),
  userUpdated: createSignalrMethod("UserUpdated"),
  receiveDocument: createSignalrMethod("ReceiveDocument"),

  sendOperations: (operations: OperationBatch) =>
    connection.send("SendOperations", operations),

  sendUserSelection: (selection: Range) =>
    connection.send("SendUserSelection", selection),

  requestDocument: () => connection.send("GetDocument"),

  setUser: (userContext: UserContext) =>
    connection.send("SetUser", userContext),

  start: () => connection.start().catch((err) => console.error(err)),
};
