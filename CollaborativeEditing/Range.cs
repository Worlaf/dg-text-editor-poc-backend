using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class Range
    {
        public Point Anchor { get; set; } = new Point();
        public Point Focus { get; set; } = new Point(); 
    }
}
