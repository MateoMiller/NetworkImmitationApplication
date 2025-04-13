using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NetworkImitator.UI;

public class SelectionToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null && (bool)value ? Strokes.SelectedStroke : Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}