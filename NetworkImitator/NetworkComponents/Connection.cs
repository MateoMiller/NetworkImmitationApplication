using System.Windows.Media;
using Point = System.Windows.Point;

namespace NetworkImitator.NetworkComponents;

public class Connection
{
    public Component FirstComponent { get; set; }

    public Component SecondComponent { get; set; }

    public Point? TemporaryPosition { get; set; }
    private Message? currentMessage;
    private TimeSpan Elapsed = TimeSpan.Zero;

    public Brush GetBrush()
    {
        return currentMessage == null ? Brushes.Black : Brushes.Yellow;
    }

    public void ProcessTick(TimeSpan elapsed)
    {
        if (currentMessage != null)
        {
            Elapsed += elapsed;
            if (Elapsed > TimeSpan.FromMilliseconds(200))
            {
                var receiver = FirstComponent.IP == currentMessage.ToIP ? FirstComponent : SecondComponent;
                receiver.ReceiveData(this, currentMessage);
                currentMessage = null;
            }
        }
    }

    public Component? GetComponent(string ip)
    {
        return FirstComponent.IP == ip ? FirstComponent 
            : SecondComponent.IP == ip ? SecondComponent 
            : null;
    }

    public Component? GetOppositeComponent(string ip)
    {
        return FirstComponent.IP == ip ? SecondComponent 
            : SecondComponent.IP == ip ? FirstComponent 
            : null;
    }

    public void TransferData(Message message)
    {
        currentMessage = message;
        Elapsed = TimeSpan.Zero;
    }
}