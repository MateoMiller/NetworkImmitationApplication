using NetworkImitator.NetworkComponents;
using NetworkImitator.UI.Commands;

namespace NetworkImitator.UI;

public class AddLoadBalancerCommand(MainViewModel viewModel, LoadBalancerAlgorithm algorithm) : CommandBase(viewModel)
{
    protected override void Execute()
    {
        var loadBalancer = new LoadBalancer(100, 100, algorithm, ViewModel);
        ViewModel.AddVertex(loadBalancer);
    }
}