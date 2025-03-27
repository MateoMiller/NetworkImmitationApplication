using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NetworkImitator.NetworkComponents;
using Component = NetworkImitator.NetworkComponents.Component;

namespace NetworkImitator.UI;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<Component> Components { get; } = new();
    public ObservableCollection<Connection> Connections { get; } = new();
    public Connection? TempConnection { get; set; }

    private Component? selectedVertex;
    private bool isDragging;

    public void Update(TimeSpan elapsed)
    {
        foreach (var component in Components)
        {
            component.ProcessTick(elapsed);
        }

        foreach (var connection in Connections)
        {
            connection.ProcessTick(elapsed);
        }
    }
    
    public ICommand AddClientCommand { get; }
    public ICommand AddServerCommand { get; }
    public ICommand AddLoadBalancerCommand { get; }
    public ICommand AddConnectionCommand { get; }

    public MainViewModel()
    {
        AddClientCommand = new AddClientCommand(this);
        AddServerCommand = new AddServerCommand(this);
        AddLoadBalancerCommand = new AddLoadBalancerCommand(this, LoadBalancerAlgorithm.RoundRobin);
        AddConnectionCommand = new AddConnectionCommand(this);
    }

    public void AddVertex(Component component)
    {
        Components.Add(component);
    }

    public void AddEdge()
    {
        if (selectedVertex == null)
        {
            MessageBox.Show("Выберите первую вершину для соединения");
        }
        else
        {
            TempConnection = new Connection
            {
                FirstComponent = selectedVertex,
                TemporaryPosition = new Point(selectedVertex.X, selectedVertex.Y)
            };
        }
    }
    
    private Component? selectedComponent;
    public Component? SelectedComponent
    {
        get => selectedComponent;
        set
        {
            selectedComponent = value;
            OnPropertyChanged();
        }
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SelectVertex(Component component)
    {
        UnselectVertex();
        selectedVertex = component;
        selectedVertex.IsSelected = true;
        SelectedComponent = component;
        isDragging = true;
    }

    public void UnselectVertex()
    {
        if (selectedVertex != null)
        {
            selectedVertex.IsSelected = false;
            selectedVertex = null;
        }
        SelectedComponent = null;
    }


    public void UpdateTempLine(Point position)
    {
        if (TempConnection != null)
        {
            TempConnection.TemporaryPosition = position;
        }
    }

    public void EndDragging()
    {
        isDragging = false;
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