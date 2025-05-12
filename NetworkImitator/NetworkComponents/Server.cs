using System.Text;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkImitator.Extensions;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public partial class Server : Component
{
    [ObservableProperty] private int _maxConcurrentPackets;
    [ObservableProperty] private int _timeToProcessMs;

    private readonly List<ProcessingProcess> _processing = [];
    private readonly Queue<Message> _messagesQueue = new();

    public Server(double x, double y, int timeToProcessMs, int maxConcurrentPackets, MainViewModel viewModel) : base(viewModel, x, y)
    {
        TimeToProcessMs = timeToProcessMs;
        _maxConcurrentPackets = maxConcurrentPackets;
    }

    public override BitmapImage Image => new(Images.ServerImageUri);

    public override void ReceiveData(Connection connection, Message message)
    {
        if (_processing.Count < MaxConcurrentPackets)
        {
            _processing.Add(new ProcessingProcess(message, TimeSpan.FromMilliseconds(TimeToProcessMs), connection));
        }
        else
        {
            _messagesQueue.Enqueue(message);
        }
        
        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetQueuedMessagesCount));
        OnPropertyChanged(nameof(GetTotalLoad));
    }
    
    public int GetProcessingLoad => _processing.Count;
    
    public int GetQueuedMessagesCount => _messagesQueue.Count;
    
    public int GetTotalLoad => _processing.Count + _messagesQueue.Count;

    public override void ProcessTick(TimeSpan elapsed)
    {
        var finished = new List<ProcessingProcess>();
        foreach (var process in _processing)
        {
            process.Elapsed += elapsed;
            if (process.Elapsed > process.TimeToProcess)
                finished.Add(process);
        }

        foreach (var process in finished)
        {
            var toIp = process.Message.FromIP;
            var connection = GetActiveConnectionTo(toIp);
            
            if (connection != null)
            {
                var content = RandomExtensions.RandomWord();
                connection.TransferData(new Message(IP, toIp, Encoding.ASCII.GetBytes(content), process.Message.OriginalSenderIp));
            }

            _processing.Remove(process);
        }

        while (_processing.Count < MaxConcurrentPackets && _messagesQueue.Count > 0)
        {
            var nextMessage = _messagesQueue.Dequeue();
            _processing.Add(new ProcessingProcess(nextMessage, TimeSpan.FromMilliseconds(TimeToProcessMs), null));
        }

        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetQueuedMessagesCount));
        OnPropertyChanged(nameof(GetTotalLoad));
    }
    
    public override void OnConnectionDisconnected(Connection connection)
    {
        var processesToRemove = _processing
            .Where(p => p.SourceConnection == connection)
            .ToList();
        
        foreach (var process in processesToRemove)
        {
            _processing.Remove(process);
        }
        
        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetTotalLoad));
    }

    private class ProcessingProcess
    {
        public Message Message { get; set; }
        public TimeSpan TimeToProcess { get; }
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
        public Connection? SourceConnection { get; }
        
        public ProcessingProcess(Message message, TimeSpan timeToProcess, Connection? sourceConnection)
        {
            Message = message;
            TimeToProcess = timeToProcess;
            SourceConnection = sourceConnection;
        }
    }
}