using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Component = NetworkImitator.NetworkComponents.Component;

namespace NetworkImitator.UI;

public class ComponentToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Component component ?  new ImageBrush(component.Image) : Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}