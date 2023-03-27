import { useState } from "react";
import "./App.css";
import { Editor } from "./components/Editor/Editor";

import { useCollaborativeContext } from "./collaborativeEditing/utils";

function App() {
  const [userName, setUserName] = useState("anonymous");

  const { connect, userContext, getDocument, documentContext, otherUsers } =
    useCollaborativeContext();

  const handleConnect = async () => {
    await connect(userName);
    await getDocument();
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
            disabled={!!userContext}
          />
          <button disabled={!!userContext} onClick={handleConnect}>
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
          {!documentContext ? (
            <div>Connect to start editing</div>
          ) : (
            <Editor
              users={otherUsers}
              document={documentContext.document}
              className="editor"
            />
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
