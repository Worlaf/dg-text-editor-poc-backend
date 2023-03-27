using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class UserContext
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public int DocumentRevision { get; set; }
        public Range? DocumentSelection { get; set; }
    }
}
