using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkImitator.NetworkComponents;

public class Server : Component
{
    public Server(double x, double y, int timeToProcessMs)
    {
        X = x;
        Y = y;
        TimeToProcessMs = timeToProcessMs;
    }
    
    public int TimeToProcessMs { get; set; }

    private List<ProcessingProcess> Processing = new();

    public override BitmapImage Image => new(Images.ServerImageUri);

    public override Brush GetBrush()
    {
        return Processing.Count == 0 ? Brushes.Aquamarine : Brushes.Maroon;
    }

    public override void ReceiveData(DataTransition transition)
    {
        Processing.Add(new(transition, TimeSpan.FromMilliseconds(TimeToProcessMs)));
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
            Console.WriteLine("Server" + TimeToProcessMs);
        }
    }

    private class ProcessingProcess(DataTransition dataTransition, TimeSpan timeToProcess)
    {
        public DataTransition DataTransition { get; set; } = dataTransition;
        public TimeSpan TimeToProcess { get; } = timeToProcess;
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    }
}