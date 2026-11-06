namespace SpaceBattle.Lib;

public class HwdtechCommandAdapter : ICommand
{
    private readonly Hwdtech.ICommand _command;

    public HwdtechCommandAdapter(Hwdtech.ICommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public void Execute() => _command.Execute();
}

public class GameCommandHwdtechAdapter : Hwdtech.ICommand
{
    private readonly ICommand _command;

    public GameCommandHwdtechAdapter(ICommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public void Execute() => _command.Execute();
}
