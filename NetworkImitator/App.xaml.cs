using System.Text;
using System.Windows;
using NetworkImitator.Extensions;
using NetworkImitator.NetworkComponents;

namespace NetworkImitator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}