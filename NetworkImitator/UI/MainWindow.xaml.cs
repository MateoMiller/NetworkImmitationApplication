using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
        NetworkCanvas.RedrawEverything();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.UnselectVertex(_viewModel.SelectedComponent);
            _viewModel.UnselectConnection(_viewModel.SelectedConnection);
            _viewModel.TempConnection = null;

            NetworkCanvas.RedrawEverything();
        }
    }
}