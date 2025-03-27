using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkImitator.NetworkComponents;

public abstract class Component : INotifyPropertyChanged
{
    public string IP { get; set; }

    public string DeviceName { get; set; }
    
    public List<Connection> Connections { get; } = new();

    public abstract BitmapImage Image { get; }
    public abstract Brush GetBrush();
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private double _x;
    private double _y;

    public double X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = value;
                OnPropertyChanged();
            }
        }
    }
    public double Y { get => _y;
        set
        {
            if (_y != value)
            {
                _y = value;
                OnPropertyChanged();
            }
        }}

    public void ConnectTo(Connection connection)
    {
        if (!Connections.Contains(connection))
            Connections.Add(connection);
    }

    public abstract void ProcessTick(TimeSpan elapsed);

    public abstract void ReceiveData(Message currentMessage);

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}