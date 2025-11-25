using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Windows.Foundation;

namespace Blocks_.Core.Models
{
    public enum ConnectionType
    {
        Normal,
        TrueBranch,
        FalseBranch,
        LoopBody,
        Input,
        LoopExit,
        Case,
        DefaultCase
    }

    [XmlRoot("Connection")]
    public class ConnectionLine
    {
        private Guid _fromBlockId;
        private Guid _toBlockId;

        [XmlIgnore]
        public List<Point> Points { get; set; }

        [XmlIgnore]
        public TextBlock VisualLabel { get; set; } 
        public ConnectionLine()
        {
        }

        [XmlIgnore]
        public Polyline VisualPath { get; set; }

        [XmlIgnore]
        public Line VisualLine { get; set; }

        [XmlIgnore]
        public Polygon ArrowHead { get; set; }

        [XmlIgnore]
        public SolidColorBrush Stroke { get; set; }

        [XmlIgnore]
        public Tree SyntaxNode { get; set; }

        [XmlIgnore]
        public BlockItem FromBlock { get; set; }

        [XmlIgnore]
        public BlockItem ToBlock { get; set; }

        [XmlElement("FromBlockId")]
        public Guid FromBlockId
        {
            get => FromBlock?.Id ?? _fromBlockId;
            set => _fromBlockId = value;
        }

        [XmlElement("ToBlockId")]
        public Guid ToBlockId
        {
            get => ToBlock?.Id ?? _toBlockId;
            set => _toBlockId = value; 
        }

        public ConnectionType Type { get; set; }


        [XmlIgnore]
        public bool IsRouted => VisualPath != null;
        
    }
}