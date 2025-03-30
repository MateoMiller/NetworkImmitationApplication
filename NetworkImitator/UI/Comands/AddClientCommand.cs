using NetworkImitator.NetworkComponents;

namespace NetworkImitator.UI;

public class AddClientCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        var client = new Client(100, 100, 100, ViewModel);
        ViewModel.AddVertex(client);
    }
}