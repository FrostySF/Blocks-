using Blocks_.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        private BlockItem draggedBlockTemplate = null;
        private bool isDraggingFromPanel = false;
        private Border dragPreviewBorder = null;
        private readonly Dictionary<BlockItem, Border> blockVariablePanels = new();
        private const double PanningSensitivity = 3.5;

        private bool isSpaceBarPressed = false;

        public ObservableCollection<BlockItem> Blocks { get; } = new ObservableCollection<BlockItem>();

        private int blockCounter = 0;
        private BlockItem selectedBlock;
        private Point lastMousePosition;

        private List<ConnectionLine> connectionLines = new List<ConnectionLine>();
        private Tree syntaxTreeRoot;
        private BlockItem startBlock;
        private BlockItem endBlock;


        private Dictionary<ConnectionLine, Polygon> connectionArrows = new Dictionary<ConnectionLine, Polygon>();
        private int highlightRadius = 1;
        private List<BlockItem> listofblocks = new List<BlockItem>();
        private Ellipse selectedAnchor = null;
        private Line previewLine = null;
        private BlockItem connectionStartBlock = null;
        private ConnectionType connectionStartType = ConnectionType.Normal;

        private GridNode[,] virtualGrid;
        private const int GRID_STEP = 100;
        private const int GRID_ROWS = 30;
        private const int GRID_COLUMNS = 30;

        private bool isDebugging = false;
        private Tree currentDebugNode = null;
        private List<Tree> executionOrder = new List<Tree>();
        private int currentStepIndex = -1;
        private Storyboard highlightStoryboard;
        private Border highlightedBorder;

        public Polyline VisualPath { get; set; }

        private List<Rectangle> gridHighlights = new();

        private const double MIN_SEGMENT_LENGTH = 10.0;
        private const double OBSTACLE_CLEARANCE = 15.0;

        private readonly SolidColorBrush ErrorLineColor = new SolidColorBrush(Colors.Red);
        private readonly SolidColorBrush NormalLineColor = new SolidColorBrush(Colors.White);

        private bool isPanning = false;
        private Point lastPanPosition;

        private double currentZoom = 1.0;
    }
}
