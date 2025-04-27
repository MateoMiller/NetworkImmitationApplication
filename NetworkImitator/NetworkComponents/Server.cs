using System.Windows.Media.Imaging;
using NetworkImitator.Extensions;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents;

public class Server : Component
{
    public Server(double x, double y, int timeToProcessMs, MainViewModel viewModel) : base(viewModel, x, y)
    {
        TimeToProcessMs = timeToProcessMs;
    }
    
    public int TimeToProcessMs { get; set; }

    private List<ProcessingProcess> Processing = [];

    public override BitmapImage Image => new(Images.ServerImageUri);

    public override void ReceiveData(Connection connection, Message message)
    {
        Processing.Add(new(message, TimeSpan.FromMilliseconds(TimeToProcessMs)));
    }
    
    public int GetProcessingLoad()
    {
        return Processing.Count;
    }

    public override void ProcessTick(TimeSpan elapsed)
    {
        var finished = new List<ProcessingProcess>();
        foreach (var process in Processing)
        {
            process.Elapsed += elapsed;
            if (process.Elapsed > process.TimeToProcess)
                finished.Add(process);
        }

        foreach (var process in finished)
        {
            var toIp = process.Message.FromIP;
            var connection = Connections.First(x => x.GetComponent(toIp) != null);
            var content = RandomExtensions.RandomWord();
            connection.TransferData(new Message(IP, toIp, content));

            Processing.Remove(process);
        }
    }

    private class ProcessingProcess(Message message, TimeSpan timeToProcess)
    {
        public Message Message { get; set; } = message;
        public TimeSpan TimeToProcess { get; } = timeToProcess;
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    }
}