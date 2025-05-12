using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NetworkImitator.Extensions;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public partial class Client : Component
{
    private ClientState _state = ClientState.ProcessingData;
    private TimeSpan _timeSinceLastSendPacket = TimeSpan.Zero;
    private TimeSpan _timeSinceDisconnection = TimeSpan.Zero;
    private const int ReconnectAttemptMs = 3000;

    [ObservableProperty] private int _sendingPacketPeriod;
    [ObservableProperty] private int _dataSizeInBytes = 1024;

    [ObservableProperty] private string _filePath;
    [ObservableProperty] private double _fileTransferProgress;
    [ObservableProperty] private string _fileTransferStatus = "Не начато";
    [ObservableProperty] private int _fileSizeBytes;

    private byte[] _fileData;
    private int _currentFilePosition;
    private bool FileTransferCompleted => FileTransferProgress >= 100;

    private ClientMode _clientMode = ClientMode.FileTransfer;
    public ClientMode ClientMode
    {
        get => _clientMode;
        set
        {
            if (_clientMode != value)
            {
                _clientMode = value;
                if (_clientMode == ClientMode.Ping && _state == ClientState.WaitingForResponse)
                {
                    _state = ClientState.ProcessingData;
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
        switch (_state)
        {
            case ClientState.ProcessingData:
                _timeSinceLastSendPacket += elapsed;
                if (_timeSinceLastSendPacket.TotalMilliseconds >= SendingPacketPeriod)
                {
                    _timeSinceLastSendPacket = TimeSpan.Zero;

                    foreach (var connection in Connections.Where(c => c.IsActive))
                    {
                        var receiver = connection.GetOppositeComponent(IP);
                        if (receiver == null) continue;

                        var message = GetMessageForTransfer(receiver);
                        if (message == null)
                            continue;

                        connection.TransferData(message);
                        
                        if (ClientMode == ClientMode.Http)
                        {
                            _state = ClientState.WaitingForResponse;
                        }
                    }
                }
                break;
                
            case ClientState.WaitingForResponse:
                var hasActiveConnections = Connections.Any(c => c.IsActive);
                if (!hasActiveConnections)
                {
                    _timeSinceDisconnection += elapsed;
                    
                    if (_timeSinceDisconnection.TotalMilliseconds >= ReconnectAttemptMs)
                    {
                        _timeSinceDisconnection = TimeSpan.Zero;
                        _state = ClientState.ProcessingData;
                    }
                }
                break;
        }
    }

    private Message GetMessageForTransfer(Component receiver)
    {
        if (ClientMode == ClientMode.FileTransfer)
        {
            if (_fileData == null || FileTransferCompleted)
            {
                return null;
            }

            var chunkSize = Math.Min(FileSizeBytes - _currentFilePosition, DataSizeInBytes);
            var chunk = _fileData[_currentFilePosition..(_currentFilePosition + chunkSize)];
            var message = new Message(IP, receiver.IP, chunk, IP);
            _currentFilePosition += chunkSize;

            UpdateFileTransferProgress();

            return message;
        }

        var randomContent = RandomExtensions.RandomSentence(DataSizeInBytes);
        return new Message(IP, receiver.IP, Encoding.ASCII.GetBytes(randomContent), IP);
    }

    public override void ReceiveData(Connection connection, Message currentMessage)
    {
        _state = ClientState.ProcessingData;
    }
    
    public override void OnConnectionDisconnected(Connection connection)
    {
        if (_state == ClientState.WaitingForResponse)
        {
            _timeSinceDisconnection = TimeSpan.Zero;
        }
    }

}