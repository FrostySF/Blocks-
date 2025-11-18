using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Xml.Serialization;

namespace Blocks_.Core.Models
{
    [XmlRoot("Block")]
    public class BlockItem
    {
        public BlockItem()
        {
            // XmlSerializer требует конструктор без параметров
            Id = Guid.NewGuid();
        }

        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public BlockType Type { get; set; }

        // Сохраняем координаты GridPosition через вспомогательные свойства
        [XmlIgnore]
        public GridNode GridPosition { get; set; }

        [XmlElement("GridRow")]
        public int GridRow
        {
            get => GridPosition?.Row ?? -1;
            set { } // Setter нужен для десериализации, но значение устанавливается в LoadFromFile
        }

        [XmlElement("GridColumn")]
        public int GridColumn
        {
            get => GridPosition?.Column ?? -1;
            set { } // Setter нужен для десериализации
        }

        // UI-свойства не сериализуются
        [XmlIgnore]
        public SolidColorBrush BackgroundColor
        {
            get
            {
                return Type switch
                {
                    BlockType.Start => new SolidColorBrush(Colors.RoyalBlue),
                    BlockType.End => new SolidColorBrush(Colors.Crimson),
                    BlockType.Process => new SolidColorBrush(Colors.ForestGreen),
                    BlockType.Decision => new SolidColorBrush(Colors.Orange),
                    BlockType.InputOutput => new SolidColorBrush(Colors.Purple),
                    BlockType.Input => new SolidColorBrush(Colors.Purple),
                    BlockType.Output => new SolidColorBrush(Colors.Purple),
                    BlockType.LoopConnector => new SolidColorBrush(Colors.White),
                    BlockType.Loop => new SolidColorBrush(Colors.DarkSlateBlue),
                    BlockType.VariableDeclaration => new SolidColorBrush(Colors.Indigo),
                    BlockType.ArrayDeclaration => new SolidColorBrush(Colors.DarkOrchid),
                    BlockType.For => new SolidColorBrush(Colors.DarkRed),
                    BlockType.DoWhile => new SolidColorBrush(Colors.Violet),
                    BlockType.Custom => new SolidColorBrush(Colors.Teal),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        [XmlIgnore]
        public SolidColorBrush BorderColor => new SolidColorBrush(Colors.White);

        public double CanvasLeft { get; set; }
        public double CanvasTop { get; set; }

        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public enum BlockType
    {
        Start,
        End,

        Process,
        Decision,
        Switch,

        InputOutput,
        Input,
        Output,



        Loop,
        While,
        DoWhile,
        For,

        LoopConnector,
        VariableDeclaration,
        ArrayDeclaration,

        Custom
    }
}