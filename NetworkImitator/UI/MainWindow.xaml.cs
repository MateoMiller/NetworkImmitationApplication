using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NetworkImitator.NetworkComponents;
using Component = NetworkImitator.NetworkComponents.Component;

namespace NetworkImitator.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    private readonly TimeSpan UpdateUITime = TimeSpan.FromMilliseconds(16);

    public MainWindow()
    {
        _viewModel = new MainViewModel();
        InitializeComponent();
        DataContext = _viewModel;

        _timer = new DispatcherTimer
        {
            Interval = UpdateUITime
        };
        _timer.Tick += TimerTick;
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private void AddPcClick100(object sender, RoutedEventArgs e) => AddPcClick(sender, e, 100);

    private void AddServer100(object sender, RoutedEventArgs e) => AddServerClick(sender, e, 100);

    private void AddPcClick(object sender, RoutedEventArgs e, int sendingPacketPeriodInMs)
    {
        var computer = new Client(100, 100, sendingPacketPeriodInMs, _viewModel);
        _viewModel.AddVertex(computer);
        RedrawEverything();
    }

    private void AddServerClick(object sender, RoutedEventArgs e, int timeToProcess)
    {
        var server = new Server(100, 100, timeToProcess);
        _viewModel.AddVertex(server);
        RedrawEverything();
    }

    private void AddEdgeClick(object sender, RoutedEventArgs e)
    {
        _viewModel.AddEdge();
        RedrawEverything();
    }

    private void TimerTick(object sender, EventArgs e)
    {
        _viewModel.Update(UpdateUITime);
        RedrawEverything();
    }

    private void OnEllipseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is Ellipse ellipse && ellipse.DataContext is Component vertexModel)
        {
            if (vertexModel.IsSelected)
                _viewModel.UnselectVertex();
            else
                _viewModel.SelectVertex(vertexModel);
            RedrawEverything();
        }
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(ComponentsCanvas);
        _viewModel.DragSelectedVerticle(position);
        _viewModel.UpdateTempLine(position);
        RedrawEverything();
    }

    private void OnEllipseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _viewModel.EndDragging();
        RedrawEverything();
    }

    private void OnTemplineMouseClicked(object sender, MouseButtonEventArgs e)
    {
        VisualTreeHelper.HitTest(ComponentsCanvas, null, hitTestResult =>
        {
            if (hitTestResult.VisualHit is Ellipse secondVertex)
            {
                _viewModel.ConnectVertices(secondVertex);
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(e.GetPosition(ComponentsCanvas)));


        RedrawEverything();
    }

    private void RedrawEverything()
    {
        ComponentsCanvas.Children.Clear();

        foreach (var vertex in _viewModel.Components)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = 50,
                Height = 50,
                Fill = new ImageBrush(vertex.Image),
                Stroke = vertex.IsSelected ? Strokes.SelectedStroke : vertex.GetBrush()
            };
            Canvas.SetLeft(ellipse, vertex.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, vertex.Y - ellipse.Width / 2);
            ellipse.DataContext = vertex;
            ellipse.MouseLeftButtonDown += OnEllipseLeftButtonDown;
            ellipse.MouseLeftButtonUp += OnEllipseLeftButtonUp;
            ComponentsCanvas.Children.Add(ellipse);
        }

        foreach (var edge in _viewModel.Connections)
        {
            Line line = new Line
            {
                X1 = edge.FirstComponent.X,
                Y1 = edge.FirstComponent.Y,
                X2 = edge.SecondComponent.X,
                Y2 = edge.SecondComponent.Y,
                Stroke = edge.GetBrush(),
                StrokeThickness = 2,
            };
            line.DataContext = edge;
            ComponentsCanvas.Children.Add(line);
        }

        if (_viewModel.TempConnection != null)
        {
            Line line = new Line
            {
                X1 = _viewModel.TempConnection.FirstComponent.X,
                Y1 = _viewModel.TempConnection.FirstComponent.Y,
                X2 = _viewModel.TempConnection.TemporaryPosition!.Value.X,
                Y2 = _viewModel.TempConnection.TemporaryPosition!.Value.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            line.MouseLeftButtonUp += OnTemplineMouseClicked;
            line.DataContext = _viewModel.TempConnection;
            ComponentsCanvas.Children.Add(line);
        }
    }
}