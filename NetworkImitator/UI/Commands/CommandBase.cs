using System.Windows.Input;

namespace NetworkImitator.UI.Commands;

public abstract class CommandBase(MainViewModel viewModel) : ICommand
{
    protected readonly MainViewModel ViewModel = viewModel;

    public event EventHandler? CanExecuteChanged;

    public virtual bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        Execute();
    }

    protected abstract void Execute();
}