import { noop } from "lodash";
import * as React from "react";
import { useContext } from "react";
import { v4 } from "uuid";
import { Document, DocumentContext } from "./documentContext";
import { UserContext, UserModel } from "./userContext";
import { collaborativeConnection as connection } from "./collaborativeConnection";
import { getUserColor } from "./utils";
import { isNotNil } from "../utils/typeGuards";

type ContextData = {
  documentContext: DocumentContext | undefined;
  currentUser: UserContext | undefined;
  otherUsers: ReadonlyArray<UserModel>;
  requestDocument: () => Promise<void>;
  updateUser: (userModel: UserContext) => void;
  connect: (userName: string) => Promise<void>;
  setDocumentRevision: (newVersion: number) => void;
};

const CollaborativeEditingContext = React.createContext<ContextData>({
  currentUser: undefined,
  documentContext: undefined,
  otherUsers: [],
  requestDocument: () => Promise.resolve(),
  updateUser: noop,
  connect: () => Promise.resolve(),
  setDocumentRevision: noop,
});

export const CollaborativeEditingContextProvider: React.FC<{
  children: React.ReactNode;
}> = ({ children }) => {
  const [userContext, setUserContext] = React.useState<UserContext>();
  const [documentContext, setDocumentContext] =
    React.useState<DocumentContext>();
  const [otherUsers, setOtherUsers] = React.useState<ReadonlyArray<UserModel>>(
    []
  );

  const handleReceiveDocument = React.useCallback(
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
            color: getUserColor(index),
          }))
        );
      } catch (error) {
        console.error(error);
      }
    },
    []
  );

  const handleUserConnected = React.useCallback(
    (user: UserContext) =>
      setOtherUsers((users) => [
        ...users,
        { ...user, color: getUserColor(users.length) },
      ]),
    []
  );

  const handleUserDisconnected = React.useCallback(
    (userId: string) =>
      setOtherUsers((users) => users.filter((user) => user.userId !== userId)),
    []
  );

  const handleUserUpdated = React.useCallback((updatedUser: UserContext) => {
    setOtherUsers((users) =>
      users.map((user) =>
        user.userId === updatedUser.userId
          ? { ...updatedUser, color: user.color }
          : user
      )
    );
  }, []);

  React.useEffect(() => {
    connection.receiveDocument.on(handleReceiveDocument);

    return () => connection.receiveDocument.off(handleReceiveDocument);
  }, [handleReceiveDocument]);

  React.useEffect(() => {
    connection.userConnected.on(handleUserConnected);
    connection.userDisconnected.on(handleUserDisconnected);
    connection.userUpdated.on(handleUserUpdated);

    return () => {
      connection.userConnected.off(handleUserConnected);
      connection.userDisconnected.off(handleUserDisconnected);
      connection.userUpdated.off(handleUserUpdated);
    };
  }, [handleUserConnected, handleUserDisconnected, handleUserUpdated]);

  const connect = async (userName: string) => {
    const context: UserContext = { userId: v4(), userName: userName };

    await connection.start();
    await connection.setUser(context);

    setUserContext(context);
  };

  return (
    <CollaborativeEditingContext.Provider
      value={{
        currentUser: userContext,
        documentContext: documentContext,
        requestDocument: connection.requestDocument,
        otherUsers,
        connect,
        updateUser: handleUserUpdated,
        setDocumentRevision: (revision: number) => {
          console.log(`New document revision: ${revision}`);
          setDocumentContext((context) =>
            isNotNil(context) ? { ...context, revision } : undefined
          );
        },
      }}
    >
      {children}
    </CollaborativeEditingContext.Provider>
  );
};

export const useCollaborativeEditingContext = () =>
  useContext(CollaborativeEditingContext);
