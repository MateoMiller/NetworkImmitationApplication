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
        KeyDown += OnKeyDown;
        Loaded += (_, _) => Keyboard.Focus(this);
        Closed += (_, _) => _timer.Stop();
    }

    private void TimerTick(object sender, EventArgs e)
    {
        _viewModel.Update(UpdateUITime);
        RedrawEverything();
    }

    private void OnEllipseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is Ellipse { DataContext: Component vertexModel })
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
        if (_viewModel.TempConnection == null)
            return;

        var clickPosition = e.GetPosition(ComponentsCanvas);

        VisualTreeHelper.HitTest(ComponentsCanvas, null, hitTestResult =>
        {
            if (hitTestResult.VisualHit is Ellipse { DataContext: Component targetComponent })
            {
                _viewModel.FinishConnection(targetComponent);
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(clickPosition));

        RedrawEverything();
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.UnselectVertex();
            _viewModel.TempConnection = null;
            RedrawEverything();
        }
    }

    private void RedrawEverything()
    {
        ComponentsCanvas.Children.Clear();

        foreach (var vertex in _viewModel.Components)
        {
            var ellipse = new Ellipse
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
            var line = new Line
            {
                X1 = edge.FirstComponent.X,
                Y1 = edge.FirstComponent.Y,
                X2 = edge.SecondComponent.X,
                Y2 = edge.SecondComponent.Y,
                Stroke = edge.GetBrush(),
                StrokeThickness = 2,
                DataContext = edge
            };
            ComponentsCanvas.Children.Add(line);
        }

        if (_viewModel.TempConnection != null)
        {
            var line = new Line
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

public abstract class CommandBase(MainViewModel viewModel) : ICommand
{
    protected readonly MainViewModel ViewModel = viewModel;

    public event EventHandler? CanExecuteChanged;

    public virtual bool CanExecute(object parameter) => true;

    public void Execute(object parameter)
    {
        Execute();
    }

    protected abstract void Execute();
}

public class AddClientCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        var client = new Client(100, 100, 100, ViewModel);
        ViewModel.AddVertex(client);
    }
}

public class AddServerCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        var server = new Server(100, 100, 100);
        ViewModel.AddVertex(server);
    }
}

public class AddLoadBalancerCommand(MainViewModel viewModel, LoadBalancerAlgorithm algorithm) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        var loadBalancer = new LoadBalancer(100, 100, algorithm);
        ViewModel.AddVertex(loadBalancer);
    }
}

public class AddConnectionCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        ViewModel.AddEdge();
    }
}