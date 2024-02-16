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
        public string FullPath { get; set; } // Store the full path here
        public List<DirectoryNode> Children { get; set; } = new List<DirectoryNode>();
        public bool IsDirectory => !Name.Contains('.');

        public DirectoryNode(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
            Children = new List<DirectoryNode>();
        }

        public void AddChild(DirectoryNode child)
        {
            Children.Add(child);
        }
    }
}
