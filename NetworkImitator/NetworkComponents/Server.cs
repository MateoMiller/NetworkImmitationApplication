using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkImitator.NetworkComponents;

public class Server : Component
{
    public Server(double x, double y, int timeToProcess)
    {
        X = x;
        Y = y;
        TimeToProcess = TimeSpan.FromMilliseconds(timeToProcess);
    }

    private TimeSpan TimeToProcess;
    private List<ProcessingProcess> Processing = new();

    public override BitmapImage Image => new(Images.ServerImageUri);

    public override Brush GetBrush()
    {
        return Processing.Count == 0 ? Brushes.Aquamarine : Brushes.Maroon;
    }

    public override void ReceiveData(DataTransition transition)
    {
        Processing.Add(new(transition, TimeToProcess));
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

        var rnd = new Random();
        foreach (var process in finished)
        {
            var bytes = new byte[rnd.Next(500, 1000)];
            var connection = process.DataTransition.Connection;
            rnd.NextBytes(bytes);
            var receiver = connection.FirstComponent != this ? connection.FirstComponent : connection.SecondComponent;
            var response = new DataTransition()
            {
                Connection = process.DataTransition.Connection,
                Content = bytes,
                Reciever = receiver
            };
            connection.TransferData(response);
            Processing.Remove(process);
        }
    }

    private class ProcessingProcess(DataTransition dataTransition, TimeSpan timeToProcess)
    {
        public DataTransition DataTransition { get; set; } = dataTransition;
        public TimeSpan TimeToProcess { get; } = timeToProcess;
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    }
}