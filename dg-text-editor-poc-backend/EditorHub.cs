using CollaborativeEditing;
using Microsoft.AspNetCore.SignalR;
using Range = CollaborativeEditing.Range;

namespace dg_text_editor_poc_backend
{
    public class EditorHub : Hub
    {
        private IDocumentProvider documentProvider;

        // todo: proper authentication
        private static List<ConnectionContext> connections = new List<ConnectionContext>();

        public EditorHub(IDocumentProvider documentProvider)
        {
            this.documentProvider = documentProvider;
        }

        public void SetUser(UserContext userContext)
        {
            var currentConnection = GetCurrentConnection();
            if (currentConnection == null)
            {
                connections.Add(new ConnectionContext(userContext, Context.ConnectionId));
                Clients.Others.SendAsync("UserConnected", userContext);
            }
        }

        public void GetDocument()
        {
            var document = documentProvider.Get();
            Clients.Caller.SendAsync("ReceiveDocument", document.Revision, document.GetJson(), connections.Where(connection => connection.ConnectionId != Context.ConnectionId).Select(connection => connection.User));
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionToRemove = GetCurrentConnection();
            if (connectionToRemove != null)
            {
                connections.Remove(connectionToRemove);
                Clients.Others.SendAsync("UserDisconnected", connectionToRemove.User.UserId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public void SendOperation(string operationJson)
        {
            var operations = new OperationFactory().FromJson(operationJson).ToArray();
            var operationHandler = new OperationHandler();
            var document = documentProvider.Get();

            foreach (var operation in operations)
            {
                operationHandler.ApplyOperation(document, operation);
            }

            Clients.Others.SendAsync("ReceiveOperation", operationJson);
        }

        public void SendUserSelection(Range documentSelection)
        {
            var currentConnection = GetCurrentConnection();
            if (currentConnection == null) { return; }

            currentConnection.User.DocumentSelection = documentSelection;
            Clients.Others.SendAsync("UserUpdated", currentConnection.User);
        }

        private ConnectionContext? GetCurrentConnection() => connections.Find(connection => connection.ConnectionId == Context.ConnectionId);

    }

    public class ConnectionContext
    {
        public UserContext User { get; }
        public string ConnectionId { get; }

        public ConnectionContext(UserContext user, string connectionId)
        {
            User = user;
            ConnectionId = connectionId;
        }
    }
}
