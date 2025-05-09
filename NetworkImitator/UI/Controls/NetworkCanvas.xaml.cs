using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NetworkImitator.NetworkComponents;
using Component = NetworkImitator.NetworkComponents.Component;

namespace NetworkImitator.UI.Controls
{
    public partial class NetworkCanvas : UserControl
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public NetworkCanvas()
        {
            InitializeComponent();
            ComponentsCanvas.MouseMove += OnCanvasMouseMove;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(ComponentsCanvas);
            ViewModel.UpdateTempLine(position);
            RedrawEverything();
        }

        private void OnTemplineMouseClicked(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.TempConnection == null)
                return;

            var clickPosition = e.GetPosition(ComponentsCanvas);

            VisualTreeHelper.HitTest(ComponentsCanvas, null, hitTestResult =>
            {
                if (hitTestResult.VisualHit is Ellipse { DataContext: Component targetComponent })
                {
                    ViewModel.FinishConnection(targetComponent);
                    return HitTestResultBehavior.Stop;
                }

                return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(clickPosition));

            RedrawEverything();
        }

        private void OnConnectionClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Line { DataContext: Connection connection })
            {
                ViewModel.SelectConnection(connection);
                e.Handled = true;
            }
        }

        public void RedrawEverything()
        {
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

            foreach (var edge in ViewModel.Connections)
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

            if (ViewModel.TempConnection != null)
            {
                var line = new Line
                {
                    X1 = ViewModel.TempConnection.FirstComponent.X + width,
                    Y1 = ViewModel.TempConnection.FirstComponent.Y + width,
                    X2 = ViewModel.TempConnection.TemporaryPosition!.Value.X,
                    Y2 = ViewModel.TempConnection.TemporaryPosition!.Value.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                line.MouseLeftButtonUp += OnTemplineMouseClicked;
                line.DataContext = ViewModel.TempConnection;
                ComponentsCanvas.Children.Add(line);
            }
        }
    }
}
