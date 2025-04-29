using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Point = System.Windows.Point;

namespace NetworkImitator.NetworkComponents;

public partial class Connection : ObservableObject
{
    public Component FirstComponent { get; set; }

    public Component SecondComponent { get; set; }

    [ObservableProperty] private int _timeToProcessMs = 200;
    [ObservableProperty] private bool _isSelected;
    
    public string DisplayName => $"Соединение {FirstComponent?.DeviceName} → {SecondComponent?.DeviceName}";
    
    public Point? TemporaryPosition { get; set; }
    private Message? currentMessage;
    private TimeSpan Elapsed = TimeSpan.Zero;

    public Brush GetBrush()
    {
        if (IsSelected)
            return currentMessage == null ? Brushes.Blue : Brushes.Red;
        return currentMessage == null ? Brushes.Black : Brushes.Yellow;
    }

    public void ProcessTick(TimeSpan elapsed)
    {
        if (currentMessage != null)
        {
            Elapsed += elapsed;
            if (Elapsed > TimeSpan.FromMilliseconds(TimeToProcessMs))
            {
                var receiver = FirstComponent.IP == currentMessage.ToIP ? FirstComponent : SecondComponent;
                receiver.ReceiveData(this, currentMessage);
                currentMessage = null;
            }
        }
    }

    public Component? GetComponent(string ip)
    {
        return FirstComponent.IP == ip ? FirstComponent 
            : SecondComponent.IP == ip ? SecondComponent 
            : null;
    }

    public Component? GetOppositeComponent(string ip)
    {
        return FirstComponent.IP == ip ? SecondComponent 
            : SecondComponent.IP == ip ? FirstComponent 
            : null;
    }

    public void TransferData(Message message)
    {
        currentMessage = message;
        Elapsed = TimeSpan.Zero;
    }
}