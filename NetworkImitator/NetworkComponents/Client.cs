using System.Windows.Media;
using System.Windows.Media.Imaging;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public class Client : Component
{
    private readonly MainViewModel viewModel;
    private ClientState state = ClientState.ProcessingData;
    private TimeSpan timeSinceLastSendPacket = TimeSpan.Zero;
    public int SendingPacketPeriod { get; set; }

    public Client(double x, double y, int sendingPacketPeriodInMs, MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        X = x;
        Y = y;
        SendingPacketPeriod = sendingPacketPeriodInMs;
    }

    public override BitmapImage Image => new(Images.PcImageUri);

    public override Brush GetBrush()
    {
        return state == ClientState.ProcessingData ? Brushes.Chartreuse : Brushes.Fuchsia;
    }

    public override void ReceiveData(DataTransition transition)
    {
        state = ClientState.ProcessingData;
        timeSinceLastSendPacket = TimeSpan.Zero;
    }

    public override void ProcessTick(TimeSpan elapsed)
    {
        var connection =
            viewModel.Connections.FirstOrDefault(x => x.FirstComponent == this || x.SecondComponent == this);

        if (connection != null && state == ClientState.ProcessingData)
        {
            timeSinceLastSendPacket += elapsed;
            if (timeSinceLastSendPacket.TotalMilliseconds >= SendingPacketPeriod)
            {
                var transaction = new DataTransition()
                {
                    Connection = connection,
                    Content = new byte[1000],
                    Reciever = connection.FirstComponent == this
                        ? connection.SecondComponent
                        : connection.FirstComponent
                };
                connection.TransferData(transaction);
                Console.WriteLine("Server" + SendingPacketPeriod);
                state = ClientState.WaitingForResponse;
            }
        }
    }
}