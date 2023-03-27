using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeEditing.DocumentSchema
{
    public abstract class NodeBase
    {
        public abstract string Type { get; }

        public IEnumerable<NodeBase> Children { get; } = Enumerable.Empty<NodeBase>();
    }
}
