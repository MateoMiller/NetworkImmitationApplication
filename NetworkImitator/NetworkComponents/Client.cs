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
    private TimeSpan _timeSinceLastSendPacket = TimeSpan.Zero;

    [ObservableProperty] private int _sendingPacketPeriod;
    [ObservableProperty] private int _dataSizeInBytes = 1024;

    [ObservableProperty] private string _filePath;
    [ObservableProperty] private double _fileTransferProgress;
    [ObservableProperty] private string _fileTransferStatus = "Не начато";
    [ObservableProperty] private int _fileSizeBytes;

    private byte[] _fileData;
    private int _currentFilePosition;
    private bool FileTransferCompleted => FileTransferProgress >= 100;
    
    private readonly ClientContext _context;

    private ClientMode _clientMode = ClientMode.FileTransfer;
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

    private void UpdateFileTransferProgress()
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
                    if (MessagesQueue.Count == 0)
                    {
                        if (ClientMode == ClientMode.FileTransfer)
                            UpdateFileTransferProgress();
                        GetNewMessageQueueForTransfer(receiver);
                    }

                    if (!MessagesQueue.TryDequeue(out var message))
                    {
                        continue;
                    }

                    message.UpdateMessageState(MessageProcessingState.InTransit, "Client", IP);

                    connection.TransferData(message);

                    if (ClientMode is ClientMode.Http or ClientMode.FileTransfer)
                    {
                        _context.ChangeState(ClientState.WaitingForResponse);
                    }
                }
                break;
            case ClientState.WaitingForResponse:
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
            MessagesQueue.Count,
            FileTransferProgress,
            FileTransferStatus
        );
        
        MetricsCollector.Instance.AddClientMetrics(clientMetrics);
    }

    private void GetNewMessageQueueForTransfer(Component receiver)
    {
        if (ClientMode == ClientMode.FileTransfer)
        {
            if (_fileData == null || FileTransferCompleted)
            {
                return;
            }

            var chunkSize = Math.Min(FileSizeBytes - _currentFilePosition, DataSizeInBytes);
            var chunk = _fileData[_currentFilePosition..(_currentFilePosition + chunkSize)];
            var (compressedChuck, timeToCompress) = chunk.CompressAndMeasure();
            var messagesToSent = compressedChuck.Chunk(MaxPacketSize)
                .Select(x => new Message(IP, receiver.IP, x, IP, false, true))
                .ToArray();
            messagesToSent[^1].IsFinalMessage = true;
            MessagesQueue = new Queue<Message>(messagesToSent);
            _context.TimeToCompress = timeToCompress;
            
            _currentFilePosition += chunkSize;

            return;
        }

        var randomContent = RandomExtensions.RandomSentence(DataSizeInBytes);
        var content = Encoding.ASCII.GetBytes(randomContent);
        MessagesQueue.Enqueue(new Message(IP, receiver.IP, content, IP));
    }

    private Queue<Message> MessagesQueue { get; set; } = new();

    public override void ReceiveData(Connection connection, Message currentMessage)
    {
        // Обновление метрики сообщения
        currentMessage.UpdateMessageState(MessageProcessingState.Received, "Client", IP);
        
        if (MessagesQueue.Count != 0)
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

    private const int MaxPacketSize = 65535;
    
    private class ClientContext
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