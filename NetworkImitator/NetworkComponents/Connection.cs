using System.Drawing;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace NetworkImitator.NetworkComponents;

public class Connection
{
    public Component FirstComponent { get; set; }

    public Component SecondComponent { get; set; }

    public Point? TemporaryPosition { get; set; }
    private DataTransition? currentTransition;
    private TimeSpan Elapsed = TimeSpan.Zero;

    public Brush GetBrush()
    {
        return currentTransition == null ? Brushes.Black : Brushes.Yellow;
    }

    public void ProcessTick(TimeSpan elapsed)
    {
        if (currentTransition != null)
        {
            Elapsed += elapsed;
            if (Elapsed > TimeSpan.FromMilliseconds(200))
            {
                currentTransition.Reciever.ReceiveData(currentTransition);
                currentTransition = null;
            }
        }
    }

    public void TransferData(DataTransition dataTransition)
    {
        currentTransition = dataTransition;
        Elapsed = TimeSpan.Zero;
    }
}