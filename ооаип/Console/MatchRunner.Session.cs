using Hwdtech;
using SpaceBattle.Lib;

namespace SpaceBattle.ConsoleApp;

public static partial class MatchRunner
{
    private sealed class GameSession
    {
        private readonly ConsoleShip _ship;
        private readonly Queue<Lib.ICommand> _queue;
        private readonly TextWriter _output;
        private readonly Action<string> _log;
        private IMovable? _movable;
        private int _turnNumber;

        public GameSession(ConsoleShip ship, Queue<Lib.ICommand> queue, TextWriter output, Action<string> log)
        {
            _ship = ship;
            _queue = queue;
            _output = output;
            _log = log;
        }

        private IMovable Movable
        {
            get
            {
                if (_movable is null)
                {
                    _movable = (IMovable)IoC.Resolve<object>("Adapter", _ship, typeof(IMovable));
                    _log($"Для команды создан адаптер: {_movable.GetType().Name}.");
                }

                return _movable;
            }
        }

        public void EnqueueMove() => _queue.Enqueue(new MoveCommand(Movable));

        public void EnqueueTeleport(Vector target)
        {
            var movable = Movable;
            _queue.Enqueue(new ActionCommand(() => movable.Position = target));
        }

        public void PlayTurn()
        {
            _turnNumber++;
            var movable = Movable;
            _log($"Ход {_turnNumber} начат. Текущая позиция: {movable.Position}.");

            _queue.Enqueue(new HwdtechCommandAdapter(new LogStepCommand(_log, $"Команды хода {_turnNumber} выполнены.")));

            IoC.Resolve<Lib.ICommand>("ReturnCommandTimeStrategy", _queue).Execute();

            var position = movable.Position;
            _log($"Ход {_turnNumber} завершён. Новая позиция: {position}.");
            _output.WriteLine($"Позиция после хода: {position}");
        }
    }

    private sealed class LogStepCommand : Hwdtech.ICommand
    {
        private readonly Action<string> _log;
        private readonly string _message;

        public LogStepCommand(Action<string> log, string message)
        {
            _log = log;
            _message = message;
        }

        public void Execute() => _log(_message);
    }

    private sealed class ConsoleShip : IUObject
    {
        private readonly Dictionary<string, object> _properties = new();

        public void Set(Vector position, Vector velocity)
        {
            _properties["position"] = position;
            _properties["velocity"] = velocity;
        }

        public void setProperty(string key, object value) => _properties[key] = value;

        public object getProperty(string key) => _properties[key];
    }
}
