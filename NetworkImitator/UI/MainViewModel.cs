using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Shapes;
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
    
    public void AddLoadBalancer(double x, double y, LoadBalancerAlgorithm algorithm)
    {
        var loadBalancer = new LoadBalancer(x, y, algorithm);
        Components.Add(loadBalancer);
    }

    public void ConnectLoadBalancerToServer(LoadBalancer loadBalancer, Server server)
    {
        loadBalancer.AddServer(server);
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

    public void ConnectVertices(Ellipse secondVertex)
    {
        if (selectedVertex != null && TempConnection != null && secondVertex.DataContext is Component second && second != selectedVertex)
        {
            TempConnection.TemporaryPosition = null;
            TempConnection.SecondComponent = second;
            Connections.Add(TempConnection);
        }

        TempConnection = null;
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

    public void RemoveTempLine(Point position)
    {
        TempConnection = null;
    }

    public void EndDragging()
    {
        isDragging = false;
    }

    public void DragSelectedVerticle(Point position)
    {
        if (isDragging && selectedVertex != null)
        {
            selectedVertex.X = position.X;
            selectedVertex.Y = position.Y;
        }
    }
}