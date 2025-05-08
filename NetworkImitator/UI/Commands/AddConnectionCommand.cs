namespace NetworkImitator.UI.Commands;

public class AddConnectionCommand(MainViewModel viewModel) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        ViewModel.AddEdge();
    }
}