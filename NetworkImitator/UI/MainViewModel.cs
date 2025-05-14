using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NetworkImitator.NetworkComponents;
using NetworkImitator.NetworkComponents.Metrics;
using NetworkImitator.UI.Commands;
using Component = NetworkImitator.NetworkComponents.Component;

namespace NetworkImitator.UI;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<Component> Components { get; } = [];
    public ObservableCollection<Connection> Connections { get; } = [];
    public Connection? TempConnection { get; set; }
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
    
    [ObservableProperty] private Component? _selectedComponent;
    [ObservableProperty] private Connection? _selectedConnection;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private int _stepsPerOneUpdate = 100;
    [ObservableProperty] private double _realtimeSpeedModifier = 1.0;

    public bool CanEditSettings => IsPaused;

    public void Update(TimeSpan totalElapsed)
    {
        if (IsPaused)
            return;

        var stepDuration = RealtimeSpeedModifier * totalElapsed / StepsPerOneUpdate;

        for (var i = 0; i < StepsPerOneUpdate; i++)
        {
            foreach (var component in Components)
            {
                component.ProcessTick(stepDuration);
            }
    
            foreach (var connection in Connections)
            {
                connection.ProcessTick(stepDuration);
            }
            ElapsedTime += stepDuration;
        }
    }
    
    public ICommand AddClientCommand { get; }
    public ICommand AddServerCommand { get; }
    public ICommand AddLoadBalancerCommand { get; }
    public ICommand AddConnectionCommand { get; }
    public ICommand TogglePauseCommand { get; }
    public ICommand SaveMetricsCommand { get; }

    public MainViewModel()
    {
        AddClientCommand = new AddClientCommand(this);
        AddServerCommand = new AddServerCommand(this);
        AddLoadBalancerCommand = new AddLoadBalancerCommand(this, LoadBalancerAlgorithm.RoundRobin);
        AddConnectionCommand = new AddConnectionCommand(this);
        TogglePauseCommand = new RelayCommand(TogglePause);
        SaveMetricsCommand = new RelayCommand(SaveMetrics);
        
        IsPaused = true;
    }
    
    private void SaveMetrics()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Выберите папку для сохранения метрик",
            FileName = "metrics", // Заглушка для имени файла
            Filter = "Все файлы|*.*",
            CheckFileExists = false,
            OverwritePrompt = false
        };

        if (dialog.ShowDialog() == true)
        {
            var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
            
            if (string.IsNullOrEmpty(folderPath))
            {
                MessageBox.Show("Не выбрана папка для сохранения", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (MetricsCollector.Instance.SaveMetricsToFile(folderPath))
            {
                MessageBox.Show($"Метрики успешно сохранены в папку:\n{folderPath}", 
                    "Сохранение метрик", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        OnPropertyChanged(nameof(CanEditSettings));
    }

    public void AddVertex(Component component)
    {
        Components.Add(component);
    }

    public void AddEdge()
    {
        if (SelectedComponent == null)
        {
            MessageBox.Show("Выберите первую вершину для соединения");
        }
        else
        {
            TempConnection = new Connection
            {
                FirstComponent = SelectedComponent,
                TemporaryPosition = new Point(SelectedComponent.X, SelectedComponent.Y)
            };
        }
    }

    public void SelectVertex(Component component)
    {
        UnselectVertex(SelectedComponent);
        UnselectConnection(SelectedConnection);
        SelectedComponent = component;
        SelectedComponent.IsSelected = true;
    }

    public void UnselectVertex(Component? component)
    {
        if (SelectedComponent == component && SelectedComponent != null)
        {
            SelectedComponent.IsSelected = false;
            SelectedComponent = null;
        }
    }

    public void SelectConnection(Connection connection)
    {
        UnselectVertex(SelectedComponent);
        UnselectConnection(SelectedConnection);
        SelectedConnection = connection;
        SelectedConnection.IsSelected = true;
    }

    public void UnselectConnection(Connection? connection)
    {
        if (SelectedConnection == connection && SelectedConnection != null)
        {
            SelectedConnection.IsSelected = false;
            SelectedConnection = null;
        }
    }

    public void UpdateTempLine(Point position)
    {
        if (TempConnection != null)
        {
            TempConnection.TemporaryPosition = position;
        }
    }

    public void FinishConnection(Component targetComponent)
    {
        if (TempConnection == null || targetComponent == TempConnection.FirstComponent)
            return;

        TempConnection.SecondComponent = targetComponent;

        TempConnection.FirstComponent.ConnectTo(TempConnection);
        TempConnection.SecondComponent.ConnectTo(TempConnection);

        Connections.Add(TempConnection);
        TempConnection = null;
    }
}