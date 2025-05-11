using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
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

    private ClientMode _clientMode = ClientMode.Http;
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

    public Client(double x, double y, int sendingPacketPeriodInMs, MainViewModel viewModel) : base(viewModel, x, y)
    {
        X = x;
        Y = y;
        SendingPacketPeriod = sendingPacketPeriodInMs;
    }

    public override BitmapImage Image => new(Images.PcImageUri);

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
                        var msg = new Message(IP, receiver!.IP, RandomExtensions.RandomSentence(DataSizeInBytes), IP);
                
                        connection.TransferData(msg);
                        
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