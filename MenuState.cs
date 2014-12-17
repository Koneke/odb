using SadConsole;
using SadConsole.Consoles;

namespace ODB
{
    public class MenuState : AppState
    {
        private ConsoleList _consoles;
        private Console _foo;

        private int _choice;
        private const int NumChoices = 2;

        public MenuState(ODBGame game) : base(game)
        {
            SetupConsoles();
        }

        public override void Update()
        {
            if (KeyBindings.Pressed(KeyBindings.Bind.Exit))
                Game.SwitchState(Game.GameState);

            if (KeyBindings.Pressed(KeyBindings.Bind.North))
                _choice--;

            if (KeyBindings.Pressed(KeyBindings.Bind.South))
                _choice++;

            _choice = _choice < 0 ? _choice + NumChoices : _choice;
            _choice = _choice % NumChoices;

            IO.SetInput(IO.Indexes, ' ');
            IO.QuestionReaction = () => { };
            IO.QuestionPromptInput();
        }

        public override void Draw()
        {
            _foo.CellData.Clear();

            _foo.DrawColorString(
                0, 0, _choice == 0
                    ? "#ff0000foo bar"
                    : "foo bar"
            );
            _foo.DrawColorString(
                0, 1, _choice == 1
                    ? "#0000fffoo bar"
                    : "foo bar"
            );

            if((IO.Answer ?? "").Length > 0)
                _foo.DrawColorString(0, 2, IO.Answer);
        }

        public override void SwitchTo()
        {
            Engine.ConsoleRenderStack = _consoles;
        }

        private void SetupConsoles()
        {
            _consoles = new ConsoleList();
            _foo = new Console(80, 25);

            _consoles.Add(_foo);
        }
    }
}
