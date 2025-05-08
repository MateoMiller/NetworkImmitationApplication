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
    [ObservableProperty] private int _maxConcurrentMessages = 3;

    private readonly ObservableCollection<MessageInTransit> _messagesInTransit = new();

    private readonly Queue<Message> _messagesQueue = new();

    public int MessagesInTransitCount => _messagesInTransit.Count;

    public int MessagesInQueueCount => _messagesQueue.Count;
    
    public string DisplayName => $"Соединение {FirstComponent?.DeviceName} → {SecondComponent?.DeviceName}";
    
    public Point? TemporaryPosition { get; set; }

    public Brush GetBrush()
    {
        if (IsSelected)
            return Brushes.Blue;

        if (_messagesInTransit.Count == 0)
            return Brushes.Black;
        if (_messagesInTransit.Count < MaxConcurrentMessages / 2)
            return Brushes.Green;
        if (_messagesInTransit.Count < MaxConcurrentMessages)
            return Brushes.Yellow;
        return Brushes.Red;
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
        
        while (_messagesInTransit.Count < MaxConcurrentMessages && _messagesQueue.Count > 0)
        {
            var message = _messagesQueue.Dequeue();
            _messagesInTransit.Add(new MessageInTransit(message));
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
        if (_messagesInTransit.Count < MaxConcurrentMessages)
        {
            _messagesInTransit.Add(new MessageInTransit(message));
        }
        else
        {
            _messagesQueue.Enqueue(message);
        }
    }
}