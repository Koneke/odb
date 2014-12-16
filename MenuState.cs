using SadConsole;
using SadConsole.Consoles;

namespace ODB
{
    class MenuState : AppState
    {
        private ConsoleList _consoles;
        private Console _foo;

        public MenuState(ODBGame game) : base(game)
        {
            SetupConsoles();
        }

        public override void Update()
        {
            if (KeyBindings.Pressed(KeyBindings.Bind.Exit))
                Game.SwitchState(new GameState(Game));
        }

        public override void Draw()
        {
            _foo.DrawColorString(
                0, 0, new ColorString("#ff0000VIDEO GAMES")
            );
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
