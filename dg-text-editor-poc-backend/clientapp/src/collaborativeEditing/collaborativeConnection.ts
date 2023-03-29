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
const createSignalrServerEvent = <THandler extends GenericHandler>(
  methodName: string
) => ({
  on: (handler: THandler) => connection.on(methodName, handler),
  off: (handler: THandler) => connection.off(methodName, handler),
  offAll: () => connection.off(methodName),
});

export const collaborativeConnection = {
  receiveOperations:
    createSignalrServerEvent<(batch: OperationBatch) => void>(
      "ReceiveOperations"
    ),
  userConnected:
    createSignalrServerEvent<(user: UserContext) => void>("UserConnected"),
  userDisconnected:
    createSignalrServerEvent<(userId: string) => void>("UserDisconnected"),
  userUpdated:
    createSignalrServerEvent<(user: UserContext) => void>("UserUpdated"),
  receiveDocument:
    createSignalrServerEvent<
      (
        revision: number,
        json: string,
        connectedUsers: ReadonlyArray<UserContext>
      ) => void
    >("ReceiveDocument"),
  acknowledgeChanges:
    createSignalrServerEvent<(newRevisionId: number) => void>(
      "AcknowledgeChanges"
    ),

  sendOperations: (operations: OperationBatch) =>
    connection.send("SendOperations", operations),

  sendUserSelection: (selection: Range) =>
    connection.send("SendUserSelection", selection),

  requestDocument: () => connection.send("GetDocument"),

  setUser: (userContext: UserContext) =>
    connection.send("SetUser", userContext),

  start: () => connection.start().catch((err) => console.error(err)),
};
