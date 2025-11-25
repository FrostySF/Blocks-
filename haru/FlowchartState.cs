using Blocks_.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Blocks_
{
    public class FlowchartState
    {
        public List<BlockItem> Blocks { get; set; }
        public List<ConnectionLine> Connections { get; set; }
        public int BlockCounter { get; set; }

        public FlowchartState Clone()
        {
            return new FlowchartState
            {
                Blocks = Blocks.Select(b => new BlockItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    Icon = b.Icon,
                    Description = b.Description,
                    Type = b.Type,
                    Code = b.Code,
                    CanvasLeft = b.CanvasLeft,
                    CanvasTop = b.CanvasTop,
                    GridPosition = b.GridPosition
                }).ToList(),
                Connections = Connections.Select(c => new ConnectionLine
                {
                    FromBlock = c.FromBlock,
                    ToBlock = c.ToBlock,
                    Type = c.Type
                }).ToList(),
                BlockCounter = BlockCounter
            };
        }
    }
}