using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkViewer
{
    internal class DirectoryNode
    {
        public string Name { get; set; }
        public List<DirectoryNode> Children { get; set; } = new List<DirectoryNode>();
    }
}
