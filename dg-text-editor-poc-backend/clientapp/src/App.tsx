import { useState } from "react";
import "./App.css";
import { useCollaborativeEditingContext } from "./collaborativeEditing/collaborativeEditingContext";
import { Editor } from "./components/Editor/Editor";

import { isNotNil } from "./utils/typeGuards";

function App() {
  const [userName, setUserName] = useState("anonymous");

  const { connect, currentUser, documentContext, requestDocument, otherUsers } =
    useCollaborativeEditingContext();

  const handleConnect = async () => {
    await connect(userName);
    await requestDocument();
  };

  return (
    <div className="main">
      <div className="container">
        <div>
          <input
            type="text"
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            placeholder="userName"
            disabled={isNotNil(currentUser)}
          />
          <button disabled={isNotNil(currentUser)} onClick={handleConnect}>
            Connect
          </button>
          <div className="userList">
            {otherUsers.map((user) => (
              <span style={{ color: user.color }} key={user.userId}>
                <b>{user.userName}</b>
              </span>
            ))}
          </div>
        </div>
        <div>
          {isNotNil(documentContext) ? (
            <Editor
              users={otherUsers}
              document={documentContext.document}
              className="editor"
            />
          ) : (
            <div>Connect to start editing</div>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
