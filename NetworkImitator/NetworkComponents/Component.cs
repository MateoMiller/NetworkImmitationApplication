using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkImitator.NetworkComponents;

public abstract class Component
{
    public abstract BitmapImage Image { get; }
    public abstract Brush GetBrush();
    public abstract void ReceiveData(DataTransition transition);
    public abstract void ProcessTick(TimeSpan elapsed);
    public bool IsSelected { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}