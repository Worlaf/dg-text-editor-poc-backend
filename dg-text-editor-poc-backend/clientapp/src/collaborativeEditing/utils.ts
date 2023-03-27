import * as msSignalR from "@microsoft/signalr";
import { useCallback, useEffect, useState } from "react";
import { Operation, Range } from "slate";
import { v4 } from "uuid";
import { Document, DocumentContext } from "./documentContext";
import { UserContext, UserModel } from "./userContext";

const hubUrl =
  process.env.NODE_ENV === "development"
    ? "http://localhost:5221/editor"
    : "/editor";

const connection = new msSignalR.HubConnectionBuilder().withUrl(hubUrl).build();

const startConnection = () =>
  connection.start().catch((err) => console.error(err));

const setUser = (userContext: UserContext) =>
  connection.send("SetUser", userContext);

const getDocument = () => connection.send("GetDocument");

export const useCollaborativeContext = () => {
  const [userContext, setUserContext] = useState<UserContext>();
  const [documentContext, setDocumentContext] = useState<DocumentContext>();
  const [otherUsers, setOtherUsers] = useState<ReadonlyArray<UserModel>>([]);

  const handleReceiveDocument = useCallback(
    (
      revision: number,
      json: string,
      connectedUsers: ReadonlyArray<UserContext>
    ) => {
      try {
        const document = JSON.parse(json) as Document;

        setDocumentContext({ document, revision });
        setOtherUsers(
          connectedUsers.map((user, index) => ({
            ...user,
            color: USER_COLORS[index % USER_COLORS.length],
          }))
        );
      } catch (error) {
        console.error(error);
      }
    },
    []
  );

  const handleUserConnected = useCallback(
    (user: UserContext) =>
      setOtherUsers((users) => [
        ...users,
        { ...user, color: getUserColor(users.length) },
      ]),
    []
  );

  const handleUserDisconnected = useCallback(
    (userId: string) =>
      setOtherUsers((users) => users.filter((user) => user.userId !== userId)),
    []
  );

  const handleUserUpdated = useCallback(
    (updatedUser: UserContext) =>
      setOtherUsers((users) =>
        users.map((user) =>
          user.userId === updatedUser.userId
            ? { ...updatedUser, color: user.color }
            : user
        )
      ),
    []
  );

  useEffect(() => {
    connection.on("ReceiveDocument", handleReceiveDocument);

    return () => connection.off("ReceiveDocument", handleReceiveDocument);
  }, [handleReceiveDocument]);

  useEffect(() => {
    connection.on("UserConnected", handleUserConnected);
    connection.on("UserDisconnected", handleUserDisconnected);
    connection.on("UserUpdated", handleUserUpdated);

    return () => {
      connection.off("UserConnected", handleUserConnected);
      connection.off("UserDisconnected", handleUserDisconnected);
      connection.off("UserUpdated", handleUserUpdated);
    };
  }, [handleUserConnected, handleUserDisconnected, handleUserUpdated]);

  const connect = async (userName: string) => {
    const context: UserContext = { userId: v4(), userName: userName };

    await startConnection();
    await setUser(context);

    setUserContext(context);
  };

  return {
    connect,
    getDocument,
    userContext,
    documentContext,
    otherUsers,
  };
};

export type CollaborativeIO = ReturnType<typeof useCollaborativeContext>;

const RECEIVE_OPERATION_METHOD_NAME = "ReceiveOperation";
type ReceiveOperationHandler = (operationJson: string) => void;

export const collaborativeConnection = {
  onReceiveOperations: (handler: ReceiveOperationHandler) =>
    connection.on(RECEIVE_OPERATION_METHOD_NAME, handler),
  offReceiveOperations: () => connection.off(RECEIVE_OPERATION_METHOD_NAME),
  sendOperations: (operations: ReadonlyArray<Operation>) =>
    connection.send("SendOperation", JSON.stringify(operations)),

  sendUserSelection: (selection: Range) =>
    connection.send("SendUserSelection", selection),
};

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
