namespace SpaceBattle.Lib;

/// <summary>
/// Адаптер Hwdtech.ICommand -> SpaceBattle.Lib.ICommand.
/// Позволяет ставить команды, созданные через IoC (Hwdtech), в игровую очередь.
/// </summary>
public class HwdtechCommandAdapter : ICommand
{
    private readonly Hwdtech.ICommand _command;

    public HwdtechCommandAdapter(Hwdtech.ICommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public void Execute() => _command.Execute();
}

/// <summary>
/// Адаптер SpaceBattle.Lib.ICommand -> Hwdtech.ICommand.
/// Позволяет регистрировать игровые команды в IoC-контейнере.
/// </summary>
public class GameCommandHwdtechAdapter : Hwdtech.ICommand
{
    private readonly ICommand _command;

    public GameCommandHwdtechAdapter(ICommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public void Execute() => _command.Execute();
}
