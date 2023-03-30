using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class OperationBatch
    {
        public int DocumentRevision { get; set; }
        public OperationBase[] Operations { get; set; }

        public override string ToString() => $"rev [{DocumentRevision}]\r\n{string.Join("\r\n", Operations.Select(o => o.ToString()))}";
    }
}
