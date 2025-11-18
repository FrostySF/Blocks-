namespace Blocks_.Core.Models
{
    public class GridNode
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public BlockItem OccupiedBy { get; set; }
        public bool IsAvailable => OccupiedBy == null;
    }
}
