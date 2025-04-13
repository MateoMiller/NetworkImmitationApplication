using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkImitator.Extensions;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public abstract partial class Component : ObservableObject
{
    [ObservableProperty] private string _iP;
    [ObservableProperty] private string _deviceName;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private bool _isSelected;

    public List<Connection> Connections { get; } = new();

    public abstract BitmapImage Image { get; }

    public MainViewModel MainViewModel { get; }

    public abstract Brush GetBrush();


    protected Component(MainViewModel mainViewModel, double x, double y)
    {
        MainViewModel = mainViewModel;
        X = x;
        Y = y;
        IP = RandomExtensions.RandomIp();
        DeviceName = RandomExtensions.RandomWord();
    }


    public void ConnectTo(Connection connection)
    {
        if (!Connections.Contains(connection))
            Connections.Add(connection);
    }

    public abstract void ProcessTick(TimeSpan elapsed);

    public abstract void ReceiveData(Message currentMessage);

}