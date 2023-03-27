using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeEditing.DocumentSchema
{
    public class Leaf: NodeBase
    {
        public string Text { get; set; } = "";
        public bool IsBold { get; set; }    
        public bool IsItalic { get; set; }
        public bool IsStrikethrough { get; set; }
        public string BackgroundColor { get; set; } = "";

        public override string Type => "leaf";
    }
}
