using System.Collections.ObjectModel;
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

    private readonly ObservableCollection<MessageInTransit> _messagesInTransit = new();

    public int MessagesInTransitCount => _messagesInTransit.Count;
    
    public string DisplayName => $"Соединение {FirstComponent.DeviceName} → {SecondComponent.DeviceName}";
    
    public Point? TemporaryPosition { get; set; }

    public Brush GetBrush()
    {
        if (IsSelected)
            return Brushes.Blue;

        return new SolidColorBrush(GetColor(_messagesInTransit.Count, 4));
    }

    public void ProcessTick(TimeSpan elapsed)
    {
        var completedMessages = new List<MessageInTransit>();
        
        foreach (var messageInTransit in _messagesInTransit.ToArray())
        {
            messageInTransit.ElapsedTime += elapsed;
            if (messageInTransit.ElapsedTime.TotalMilliseconds > TimeToProcessMs)
            {
                var receiver = FirstComponent.IP == messageInTransit.Message.ToIP ? FirstComponent : SecondComponent;
                receiver.ReceiveData(this, messageInTransit.Message);
                completedMessages.Add(messageInTransit);
            }
        }
        
        foreach (var message in completedMessages)
        {
            _messagesInTransit.Remove(message);
        }
        
        OnPropertyChanged(nameof(MessagesInTransitCount));
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
        _messagesInTransit.Add(new MessageInTransit(message));
    }
    
    private static Color GetColor(int n, int max)
    {
        var ratio = Math.Max(0, Math.Min(1, (double)n / max));

        if (ratio <= 0.5)
        {
            var value = (byte)(ratio / 0.5 * 255);
            return Color.FromRgb(value, value, 0);
        }

        var green = (byte)((1 - (ratio - 0.5) / 0.5) * 255);
        return Color.FromRgb(255, green, 0);
    }
}