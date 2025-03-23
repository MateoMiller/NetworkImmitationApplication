using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkImitator.NetworkComponents;

public abstract class Component
{
    public string IP { get; set; }

    public string DeviceName { get; set; }
    
    public List<Connection> Connections { get; } = new();

    public abstract BitmapImage Image { get; }
    public abstract Brush GetBrush();
    public bool IsSelected { get; set; }
    public double X { get; set; }
    public double Y { get; set; }

    public void ConnectTo(Connection connection)
    {
        if (!Connections.Contains(connection))
            Connections.Add(connection);
    }

    public abstract void ProcessTick(TimeSpan elapsed);

    public abstract void ReceiveData(Message currentMessage);
}