using System.Collections.Generic;

namespace Blocks_.Core.Models
{
    public class Tree
    {
        public BlockItem Block { get; set; }
        public List<Tree> Children { get; set; } = new List<Tree>();
        public ConnectionType BranchType { get; set; }
        public Tree Parent { get; set; }
        public string BranchValue { get; set; }
    }
}
