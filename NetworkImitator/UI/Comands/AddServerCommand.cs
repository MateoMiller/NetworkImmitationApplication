using NetworkImitator.NetworkComponents;

namespace NetworkImitator.UI;

public class AddServerCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        var server = new Server(100, 100, 100, ViewModel);
        ViewModel.AddVertex(server);
    }
}