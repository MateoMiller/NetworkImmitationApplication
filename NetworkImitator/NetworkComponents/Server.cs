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

    private readonly Dictionary<string, ClientProcessingContext> _processingContexts = new();

    public Server(double x, double y, int timeToProcessMs, int maxConcurrentPackets, MainViewModel viewModel) : base(viewModel, x, y)
    {
        TimeToProcessMs = timeToProcessMs;
        _maxConcurrentPackets = maxConcurrentPackets;
    }

    public override BitmapImage Image => new(Images.ServerImageUri);

    public override void ReceiveData(Connection connection, Message message)
    {
        if (!_processingContexts.TryGetValue(message.OriginalSenderIp, out var context))
        {
            context = new ClientProcessingContext(message.OriginalSenderIp,  connection, TimeSpan.FromMilliseconds(TimeToProcessMs));
            _processingContexts.Add(message.OriginalSenderIp, context);

        }

        context.AddMessage(message);

        if (message is { IsFinalMessage: false, IsCompressed: true })
        {
            //ACK. Возможно для Ping не надо делать. TODO
            connection.TransferData(new Message(IP, message.FromIP, "ACK"u8.ToArray(), message.OriginalSenderIp));
        }

        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetQueuedMessagesCount));
        OnPropertyChanged(nameof(GetTotalLoad));
    }
    
    public int GetProcessingLoad => _processingContexts.Count(x => x.Value.State != ProcessingState.Idle);
    public int GetQueuedMessagesCount => _processingContexts.Count(x => x.Value.State == ProcessingState.Idle);
    public int GetTotalLoad => _processingContexts.Count;

    public override void ProcessTick(TimeSpan elapsed)
    {
        var finishedContexts = new List<ClientProcessingContext>();

        var precessed = 0;
        foreach (var context in _processingContexts.Values)
        {
            if (context.State != ProcessingState.Idle)
            {
                context.ProcessTick(elapsed);
                precessed++;
            }
            if (context.State == ProcessingState.FinishedProcessing)
            {
                finishedContexts.Add(context);
            }

            if (precessed >= _maxConcurrentPackets)
            {
                break;
            }
        }

        foreach (var context in finishedContexts)
        {
            _processingContexts.Remove(context.ClientIp);
            var connection = context.Connection;
            connection.TransferData(new Message(IP, connection.GetOppositeComponent(IP)!.IP, "Finished processing"u8.ToArray(), context.ClientIp));
        }
    
        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetQueuedMessagesCount));
        OnPropertyChanged(nameof(GetTotalLoad));
    }

    public override void OnConnectionDisconnected(Connection connection)
    {
        var contextsToReset = _processingContexts.Keys
            .Where(clientIp => connection.GetComponent(clientIp) != null)
            .ToList();
        foreach (var clientIp in contextsToReset)
        {
            _processingContexts.Remove(clientIp);
        }
        
        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetTotalLoad));
    }

    private enum ProcessingState
    {
        Idle,
        DecompressingData,
        ProcessingData,
        FinishedProcessing,
        SendingResponse
    }

    private class ClientProcessingContext
    {
        public ProcessingState State { get; set; } = ProcessingState.Idle;
        public string ClientIp { get; }
        public Connection Connection { get; }
        public TimeSpan TimeToProcessOnePocket { get; set; }
        public TimeSpan TimeToDecompress { get; set; }
        public List<Message> ProcessingMessages { get; set; } = new();

        public TimeSpan TotalElapsed { get; set; } = TimeSpan.Zero;
        public TimeSpan CurrentStateElapsedTime { get; set; } = TimeSpan.Zero;
        
        public ClientProcessingContext(string ClientIp, Connection connection, TimeSpan timeToProcess)
        {
            this.ClientIp = ClientIp;
            Connection = connection;
            TimeToProcessOnePocket = timeToProcess;
        }

        public void AddMessage(Message message)
        {
            ProcessingMessages.Add(message);
            if (message.IsFinalMessage)
            {
                if (message.IsCompressed)
                {
                    State = ProcessingState.DecompressingData;
                    var totalContent = ProcessingMessages.SelectMany(x => x.Content).ToArray();
                    (_, TimeToDecompress) = totalContent.DecompressAndMeasure();
                    CurrentStateElapsedTime = TimeSpan.Zero;
                }
                else
                {
                    State = ProcessingState.ProcessingData;
                    CurrentStateElapsedTime = TimeSpan.Zero;
                }
            }
        }

        public void ProcessTick(TimeSpan elapsed)
        {
            TotalElapsed += elapsed;
            CurrentStateElapsedTime += elapsed;

            switch (State)
            {
                case ProcessingState.DecompressingData:
                    if (CurrentStateElapsedTime >= TimeToDecompress)
                    {
                        State = ProcessingState.ProcessingData;
                        CurrentStateElapsedTime = TimeSpan.Zero;
                    }
                    break;
                case ProcessingState.ProcessingData:
                    if (CurrentStateElapsedTime >= TimeToProcessOnePocket)
                    {
                        State = ProcessingState.FinishedProcessing;
                    }
                    break;
            }
        }
    }
}