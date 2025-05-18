using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NetworkImitator.Extensions;
using NetworkImitator.NetworkComponents.Metrics;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public partial class Client : Component
{
    [ObservableProperty] private int _sendingPacketPeriod;
    [ObservableProperty] private int _dataSizeInBytes = 1024;

    [ObservableProperty] private string _filePath;
    [ObservableProperty] private double _fileTransferProgress;
    [ObservableProperty] private string _fileTransferStatus = "Не начато";
    [ObservableProperty] private int _fileSizeBytes;
    [ObservableProperty] private bool _isCompressingEnabled;

    internal byte[] _fileData;
    internal int _currentFilePosition;
    private bool FileTransferCompleted => FileTransferProgress >= 100;
    internal readonly Queue<Message> _messagesQueue = new();
    internal readonly ClientContext _context;

    private ClientMode _clientMode = ClientMode.Ping;
    public ClientMode ClientMode
    {
        get => _clientMode;
        set
        {
            if (_clientMode != value)
            {
                _clientMode = value;
                if (_clientMode == ClientMode.Ping && _context.State == ClientState.WaitingForResponse)
                {
                    _context.ChangeState(ClientState.ProcessingData);
                }
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    private void SelectFileForTransfer()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл для отправки",
            Filter = "Все файлы (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                FilePath = dialog.FileName;
                _fileData = File.ReadAllBytes(FilePath);
                
                _currentFilePosition = 0;
                FileTransferProgress = 0;
                FileSizeBytes = _fileData.Length;
                FileTransferStatus = "Готов к отправке";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public Client(double x, double y, int sendingPacketPeriodInMs, MainViewModel viewModel) : base(viewModel, x, y)
    {
        X = x;
        Y = y;
        SendingPacketPeriod = sendingPacketPeriodInMs;
        _context = new ClientContext(ClientState.ProcessingData)
        {
            TimeToProcessData = TimeSpan.FromMilliseconds(sendingPacketPeriodInMs)
        };
    }

    public override BitmapImage Image => new(Images.PcImageUri);

    internal void UpdateFileTransferProgress()
    {
        FileTransferProgress = (double)_currentFilePosition / FileSizeBytes * 100;

        FileTransferStatus = FileTransferProgress >= 100 
            ? "Передача завершена" 
            : $"Передача: {FileTransferProgress:F1}%";
    }

    public override void ProcessTick(TimeSpan elapsed)
    {
        _context.ProcessTick(elapsed);

        switch (_context.State)
        {
            case ClientState.ProcessedData or ClientState.SendingData:
                foreach (var connection in Connections.Where(c => c.IsActive))
                {
                    var receiver = connection.GetOppositeComponent(IP);
                    if (receiver == null) continue;
                    if (_messagesQueue.Count == 0)
                    {
                        if (ClientMode == ClientMode.FileTransfer)
                            UpdateFileTransferProgress();
                        UpdateMessageQueueForTransfer(receiver);
                    }

                    if (!_messagesQueue.TryDequeue(out var message))
                    {
                        continue;
                    }

                    message.UpdateMessageState(MessageProcessingState.InTransit, "Client", IP);

                    connection.TransferData(message);

                    switch (ClientMode)
                    {
                        case ClientMode.Http or ClientMode.FileTransfer:
                            _context.ChangeState(ClientState.WaitingForResponse);
                            break;
                        case ClientMode.Ping:
                            _context.ChangeState(ClientState.ProcessingData);
                            break;
                    }
                }
                break;
        }

        CollectClientMetrics();
    }

    private void CollectClientMetrics()
    {
        var clientMetrics = new ClientMetrics(
            IP, 
            _context.State, 
            _context.TimeInCurrentState,
            _context.TotalElapsedTime,
            _messagesQueue.Count,
            FileTransferProgress,
            FileTransferStatus
        );
        
        MetricsCollector.Instance.AddClientMetrics(clientMetrics);
    }

    internal void UpdateMessageQueueForTransfer(Component receiver)
    {
        if (ClientMode == ClientMode.FileTransfer)
        {
            if (_fileData == null || FileTransferCompleted)
            {
                return;
            }

            var chunkSize = Math.Min(FileSizeBytes - _currentFilePosition, DataSizeInBytes);
            var fileChunk = _fileData[_currentFilePosition..(_currentFilePosition + chunkSize)];
            QueueContent(receiver, fileChunk);
            _currentFilePosition += chunkSize;

            return;
        }

        var randomContent = RandomExtensions.RandomSentence(DataSizeInBytes);
        var content = Encoding.ASCII.GetBytes(randomContent);
        QueueContent(receiver, content);
    }

    internal void QueueContent(Component receiver, byte[] content)
    {
        if (IsCompressingEnabled)
        {
             (content, var timeToCompress) = content.CompressAndMeasure();
             _context.TimeToCompress = timeToCompress;
             _context.ChangeState(ClientState.CompressingData);
        }

        var messagesToSent = content.Chunk(MaxPacketSize)
            .Select(chunk => new Message(IP, receiver.IP, chunk, IP, false, IsCompressingEnabled))
            .ToArray();
            
        messagesToSent[^1].IsFinalMessage = true;
        foreach (var message in messagesToSent)
        {
            _messagesQueue.Enqueue(message);
        }
    }

    public override void ReceiveData(Connection connection, Message currentMessage)
    {
        currentMessage.UpdateMessageState(MessageProcessingState.Received, "Client", IP);
        
        if (_messagesQueue.Count != 0)
            _context.ChangeState(ClientState.SendingData);
        else
            _context.ChangeState(ClientState.ProcessingData);
    }
    
    public override void OnConnectionDisconnected(Connection connection)
    {
        if (_context.State == ClientState.WaitingForResponse)
        {
            _context.ChangeState(ClientState.ProcessingData);
        }
    }

    internal const int MaxPacketSize = 65535;
    
    internal class ClientContext
    {
        public ClientState State { get; private set; }
        public TimeSpan TimeInCurrentState { get; private set; } = TimeSpan.Zero;
        public TimeSpan TotalElapsedTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan TimeToCompress { get; set; } = TimeSpan.Zero;
        public TimeSpan TimeToProcessData { get; set; } = TimeSpan.Zero;

        public ClientContext(ClientState initialState)
        {
            State = initialState;
        }
        
        public void ChangeState(ClientState newState)
        {
            if (State != newState)
            {
                State = newState;
                TimeInCurrentState = TimeSpan.Zero;
            }
        }
        
        public void ProcessTick(TimeSpan elapsed)
        {
            TimeInCurrentState += elapsed;
            TotalElapsedTime += elapsed;

            if (State == ClientState.CompressingData && TimeInCurrentState >= TimeToCompress)
            {
                State = ClientState.SendingData;
            }

            if (State == ClientState.ProcessingData && TimeInCurrentState >= TimeToProcessData)
            {
                State = ClientState.ProcessedData;
            }
        }
    }
}