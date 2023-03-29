using CollaborativeEditing;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Nodes;
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

        public void SendOperations(OperationBatchDto operationBatchDto)
        {
            var operationBatch = new OperationBatch()
            {
                DocumentRevision = operationBatchDto.DocumentRevision,
                Operations = new OperationFactory().FromJson(operationBatchDto.Operations).ToArray()
            };

            var operations = operationBatch.Operations;
            var operationHandler = new OperationHandler();
            var document = documentProvider.Get();

            foreach (var operation in operations)
            {
                operationHandler.ApplyOperation(document, operation);
            }

            // appears, SignalR don't serialize properties of derived classes, so it need to be configured
            // via custom serializer/serializer options, or workarounded like following
            Clients.Others.SendAsync("ReceiveOperations", operationBatchDto);
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

    // todo: setup json deserialization with deriving of concrete operation class
    public class OperationBatchDto
    {
        public int DocumentRevision { get; set; }
        public JsonArray Operations { get; set; }
    }
}
