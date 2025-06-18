using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkImitator.NetworkComponents.Metrics;
using Point = System.Windows.Point;

namespace NetworkImitator.NetworkComponents;

public partial class Connection : ObservableObject
{
    public Component FirstComponent { get; set; }
    public Component SecondComponent { get; set; }

    [ObservableProperty] private double _timeToProcessMs = 14.491473;
    [ObservableProperty] private double _byteTransferTimeMs = 0.000188;
    [ObservableProperty] private bool _isSelected;

    private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;
                
            switch (value)
            {
                case false:
                    Disconnect();
                    break;
                case true:
                    Reconnect();
                    break;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(MessagesInTransitCount));
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private readonly ObservableCollection<MessageInTransit> _messagesInTransit = new();

    public int MessagesInTransitCount => _messagesInTransit.Count;

    public string DisplayName => $"Соединение {FirstComponent.DeviceName} → {SecondComponent.DeviceName} ({(IsActive ? "Активно" : "Отключено")})";
    
    public Point? TemporaryPosition { get; set; }

    public Brush GetBrush()
    {
        if (IsSelected)
            return Brushes.Blue;
        if (!IsActive)
            return Brushes.Gray;
        return new SolidColorBrush(GetColor(_messagesInTransit.Count, 15));
    }

    private double GetTotalTransferTimeMs(Message message)
    {
        return TimeToProcessMs + message.SizeInBytes * ByteTransferTimeMs;
    }

    public void ProcessTick(TimeSpan elapsed)
    {
        if (!IsActive)
            return;

        var completedMessages = new List<MessageInTransit>();
        
        foreach (var messageInTransit in _messagesInTransit.ToArray())
        {
            messageInTransit.ElapsedTime += elapsed;

            var totalTransferTimeMs = GetTotalTransferTimeMs(messageInTransit.Message);

            if (messageInTransit.ElapsedTime >= TimeSpan.FromMilliseconds(totalTransferTimeMs))
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

        var elapsedTime = FirstComponent.MainViewModel.ElapsedTime;

        foreach (var messageInTransit in _messagesInTransit)
        {
            var metric = new MessageMetrics(messageInTransit.Message, MessageProcessingState.InTransit, MessageProcessor.Connection, elapsedTime);
            
            MetricsCollector.Instance.AddMessageMetrics(metric);
        }
        
        var connectionMetrics = new ConnectionMetrics($"{FirstComponent.IP}->{SecondComponent.IP}", elapsedTime, _messagesInTransit.Count, _messagesInTransit.Sum(x => x.Message.SizeInBytes));
        MetricsCollector.Instance.AddConnectionMetrics(connectionMetrics);
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
        if (!IsActive)
            return;

        _messagesInTransit.Add(new MessageInTransit(message));
    }

    private void Disconnect()
    {
        _isActive = false;

        _messagesInTransit.Clear();

        FirstComponent.OnConnectionDisconnected(this);
        SecondComponent.OnConnectionDisconnected(this);
    }

    private void Reconnect()
    {
        _isActive = true;
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