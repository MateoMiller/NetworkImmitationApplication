using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkImitator.NetworkComponents.Metrics;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public partial class Server : Component
{
    [ObservableProperty] private int _maxConcurrentPackets;
    [ObservableProperty] private double _timeToProcessMs = 10;

    private readonly Dictionary<int, ClientProcessingContext> _processingContexts = new();

    public Server(double x, double y, int maxConcurrentPackets, MainViewModel viewModel) : base(viewModel, x, y)
    {
        _maxConcurrentPackets = maxConcurrentPackets;
    }
    public int GetProcessingLoad => Math.Min(MaxConcurrentPackets, _processingContexts.Count(x => x.Value.State != ProcessingState.Idle));
    public int GetQueuedMessagesCount => _processingContexts.Count(x => x.Value.State == ProcessingState.Idle);
    public int GetTotalLoad => _processingContexts.Count;

    public override BitmapImage Image => new(Images.ServerImageUri);

    public override void ReceiveData(Connection connection, Message message)
    {
        if (!_processingContexts.TryGetValue(message.Identification, out var context))
        {
            context = new ClientProcessingContext(message.OriginalSenderIp,  connection, () => TimeSpan.FromMilliseconds(TimeToProcessMs));
            _processingContexts.Add(message.Identification, context);
        }

        context.AddMessage(message);

        if (message is { IsFinalMessage: false })
        {
            //ACK. TODO. Не критично. Для Ping не надо делать. 
            var ackMessage = new Message(Random.Shared.Next(), IP, message.FromIP, "ACK"u8.ToArray(), message.OriginalSenderIp);
            connection.TransferData(ackMessage);
        }

        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetQueuedMessagesCount));
        OnPropertyChanged(nameof(GetTotalLoad));
    }

    public override void ProcessTick(TimeSpan elapsed)
    {
        var finishedContextsIdentification = new List<int>();

        var precessed = 0;
        foreach (var (identification, context) in _processingContexts)
        {
            if (context.State != ProcessingState.Idle)
            {
                context.ProcessTick(elapsed);
                precessed++;
            }
            if (context.State == ProcessingState.FinishedProcessing)
            {
                finishedContextsIdentification.Add(identification);
            }

            if (precessed >= _maxConcurrentPackets)
            {
                break;
            }
        }

        foreach (var identification in finishedContextsIdentification)
        {
            var context = _processingContexts[identification];
            _processingContexts.Remove(identification);
            var connection = context.Connection;
            var responseMessage = new Message(Random.Shared.Next(), IP, connection.GetOppositeComponent(IP)!.IP, "Finished processing"u8.ToArray(), context.ClientIp);
            connection.TransferData(responseMessage);
        }

        CollectServerMetrics();
        CollectMessagesMetrics();
        
        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetQueuedMessagesCount));
        OnPropertyChanged(nameof(GetTotalLoad));
    }

    private void CollectMessagesMetrics()
    {
        foreach (var context in _processingContexts.Values)
        {
            foreach (var message in context.ProcessingMessages)
            {
                var state = MapProcessingStateToMessageState(context.State);
                var metrics = new MessageMetrics(message, state, MessageProcessor.Server, MainViewModel.ElapsedTime);

                MetricsCollector.Instance.AddMessageMetrics(metrics);
            }
        }
    }

    private void CollectServerMetrics()
    {
        var metrics = new ServerMetrics(IP, MainViewModel.ElapsedTime, GetProcessingLoad, GetQueuedMessagesCount, GetTotalLoad);

        MetricsCollector.Instance.AddServerMetrics(metrics);
    }
    
    private MessageProcessingState MapProcessingStateToMessageState(ProcessingState state)
    {
        return state switch
        {
            ProcessingState.Idle => MessageProcessingState.Received,
            ProcessingState.DecompressingData => MessageProcessingState.Decompressing,
            ProcessingState.ProcessingData => MessageProcessingState.Processing,
            ProcessingState.FinishedProcessing => MessageProcessingState.Processed,
            _ => MessageProcessingState.Received
        };
    }

    public override void OnConnectionDisconnected(Connection connection)
    {
        var contextsToReset = _processingContexts
            .Where(kv => connection.GetComponent(kv.Value.ClientIp) != null)
            .ToList();

        foreach (var kv in contextsToReset)
        {
            _processingContexts.Remove(kv.Key);
        }
        
        OnPropertyChanged(nameof(GetProcessingLoad));
        OnPropertyChanged(nameof(GetTotalLoad));
    }

    public enum ProcessingState
    {
        Idle,
        DecompressingData,
        ProcessingData,
        FinishedProcessing
    }

    private class ClientProcessingContext
    {
        public ProcessingState State { get; private set; } = ProcessingState.Idle;
        public string ClientIp { get; }
        public Connection Connection { get; }
        public Func<TimeSpan> TimeToProcessOnePocketProvider { get; set; }
        public TimeSpan TimeToDecompress { get; set; }
        public List<Message> ProcessingMessages { get; set; } = new();

        public TimeSpan TotalElapsed { get; set; } = TimeSpan.Zero;
        public TimeSpan CurrentStateElapsedTime { get; set; } = TimeSpan.Zero;
        
        public ClientProcessingContext(string ClientIp, Connection connection, Func<TimeSpan> timeToProcess)
        {
            this.ClientIp = ClientIp;
            Connection = connection;
            TimeToProcessOnePocketProvider = timeToProcess;
        }

        public void AddMessage(Message message)
        {
            ProcessingMessages.Add(message);
            
            if (message.IsFinalMessage)
            {
                if (message.IsCompressed)
                {
                    var totalContent = ProcessingMessages.SelectMany(x => x.Content).ToArray();
                    (_, TimeToDecompress) = totalContent.DecompressAndMeasure();
                    //TODO Зря зануляем время обработки
                    ChangeState(ProcessingState.DecompressingData);
                }
                else
                {
                    //TODO Зря зануляем время обработки
                    ChangeState(ProcessingState.ProcessingData);
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
                        ChangeState(ProcessingState.ProcessingData);
                    }
                    break;
                case ProcessingState.ProcessingData:
                    if (CurrentStateElapsedTime >= TimeToProcessOnePocketProvider())
                    {
                        ChangeState(ProcessingState.FinishedProcessing);

                    }
                    break;
            }
        }

        private void ChangeState(ProcessingState newState)
        {
            if (State != newState)
            {
                State = newState;
                CurrentStateElapsedTime = TimeSpan.Zero;
            }
        }
    }
}