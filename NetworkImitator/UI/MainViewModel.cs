using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkImitator.NetworkComponents;
using Component = NetworkImitator.NetworkComponents.Component;

namespace NetworkImitator.UI;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<Component> Components { get; } = [];
    public ObservableCollection<Connection> Connections { get; } = [];
    public Connection? TempConnection { get; set; }
    
    [ObservableProperty] private Component? _selectedComponent;

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