using System.Windows.Media.Imaging;
using NetworkImitator.Extensions;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public class Client : Component
{
    private ClientState _state = ClientState.ProcessingData;
    private TimeSpan _timeSinceLastSendPacket = TimeSpan.Zero;
    public int SendingPacketPeriod { get; set; }

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

                    foreach (var connection in Connections)
                    {
                        var receiver = connection.GetOppositeComponent(IP);
                        var msg = new Message(IP, receiver!.IP, RandomExtensions.RandomWord(), IP);
                
                        connection.TransferData(msg);
                        
                        if (ClientMode == ClientMode.Http)
                        {
                            _state = ClientState.WaitingForResponse;
                        }
                    }
                }
                break;
        }
    }

    public override void ReceiveData(Connection connection, Message currentMessage)
    {
        _state = ClientState.ProcessingData;
    }
}