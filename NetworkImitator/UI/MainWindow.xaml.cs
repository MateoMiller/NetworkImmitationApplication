using System.Windows;
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
    private readonly TimeSpan _updateUiTime = TimeSpan.FromMilliseconds(16);

    private const int UpdatesPerOneUiRedraw = 100;

    public MainWindow()
    {
        _viewModel = new MainViewModel();
        InitializeComponent();
        DataContext = _viewModel;

        _timer = new DispatcherTimer
        {
            Interval = _updateUiTime
        };
        _timer.Tick += TimerTick;
        _timer.Start();
        KeyDown += OnKeyDown;
        Loaded += (_, _) => Keyboard.Focus(this);
        Closed += (_, _) => _timer.Stop();
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        //Можно сделать await Task.Run(() => _viewModel.Update(UpdateUITime));
        _viewModel.Update(_updateUiTime, UpdatesPerOneUiRedraw);
        RedrawEverything();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(ComponentsCanvas);
        _viewModel.UpdateTempLine(position);
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
            _viewModel.UnselectVertex(_viewModel.SelectedComponent);
            _viewModel.UnselectConnection(_viewModel.SelectedConnection);
            _viewModel.TempConnection = null;
            RedrawEverything();
        }
    }
    
    private void OnConnectionClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Line { DataContext: Connection connection })
        {
            _viewModel.SelectConnection(connection);
            e.Handled = true;
        }
    }

    private void RedrawEverything()
    {
        //TODO Потенциально надо будет убрать в xaml
        var enumerator = ComponentsCanvas.Children.GetEnumerator();
        using var enumerator1 = enumerator as IDisposable;
        var toRemove = new List<UIElement>();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is Line line)
            {
                toRemove.Add(line);
            }
        }

        foreach (var remove in toRemove)
        {
            ComponentsCanvas.Children.Remove(remove);
        }

        const int width = 25;

        foreach (var edge in _viewModel.Connections)
        {
            var line = new Line
            {
                X1 = edge.FirstComponent.X + width,
                Y1 = edge.FirstComponent.Y + width,
                X2 = edge.SecondComponent.X + width,
                Y2 = edge.SecondComponent.Y + width,
                Stroke = edge.GetBrush(),
                StrokeThickness = edge.IsSelected ? 4 : 2,
                DataContext = edge
            };
            line.MouseLeftButtonDown += OnConnectionClick;
            ComponentsCanvas.Children.Add(line);
        }

        if (_viewModel.TempConnection != null)
        {
            var line = new Line
            {
                X1 = _viewModel.TempConnection.FirstComponent.X + width / 2,
                Y1 = _viewModel.TempConnection.FirstComponent.Y + width / 2,
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