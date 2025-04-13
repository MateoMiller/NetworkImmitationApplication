using System.Windows.Media;
using System.Windows.Media.Imaging;
using NetworkImitator.Extensions;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public class Client : Component
{
    private ClientState state = ClientState.ProcessingData;
    private TimeSpan timeSinceLastSendPacket = TimeSpan.Zero;
    public int SendingPacketPeriod { get; set; }

    public Client(double x, double y, int sendingPacketPeriodInMs, MainViewModel viewModel) : base(viewModel, x, y)
    {
        X = x;
        Y = y;
        SendingPacketPeriod = sendingPacketPeriodInMs;
    }

    public override BitmapImage Image => new(Images.PcImageUri);

    public override Brush GetBrush()
    {
        return state == ClientState.ProcessingData ? Brushes.Chartreuse : Brushes.Fuchsia;
    }

    public override void ProcessTick(TimeSpan elapsed)
    {
        switch (state)
        {
            case ClientState.ProcessingData:
                timeSinceLastSendPacket += elapsed;
                var rnd = new Random();
                if (timeSinceLastSendPacket.TotalMilliseconds >= SendingPacketPeriod)
                {
                    timeSinceLastSendPacket = TimeSpan.Zero;

                    foreach (var connection in Connections)
                    {
                        var receiver = connection.GetOppositeComponent(IP);
                        var msg = new Message(IP, receiver!.IP, RandomExtensions.RandomWord());
                
                        connection.TransferData(msg);
                        
                        state = ClientState.WaitingForResponse;
                    }

                }
                break;
        }
    }

    public override void ReceiveData(Message currentMessage)
    {
        state = ClientState.ProcessingData;
    }
}