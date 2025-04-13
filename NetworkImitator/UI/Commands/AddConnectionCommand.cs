using NetworkImitator.UI.Commands;

namespace NetworkImitator.UI;

public class AddConnectionCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        ViewModel.AddEdge();
    }
}