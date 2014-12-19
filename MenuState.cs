using System;
using SadConsole;
using SadConsole.Consoles;
using Console = SadConsole.Consoles.Console;

namespace ODB
{
    public class MenuState : AppState
    {
        private enum State
        {
            MainMenu,
            Name
        }

        private enum Choices
        {
            Name,
            Start,
            Load,
            Exit
        }

        private State _state;

        private ConsoleList _consoles;
        private Console _menuConsole;

        private int _choice;
        private readonly int _numChoices;

        public MenuState(ODBGame game) : base(game)
        {
            SetupConsoles();
            _numChoices = Enum.GetNames(typeof (Choices)).Length;
            _state = State.MainMenu;
        }

        public override void Update()
        {
            switch (_state)
            {
                case State.MainMenu:
                    if (KeyBindings.Pressed(KeyBindings.Bind.North))
                        _choice--;

                    if (KeyBindings.Pressed(KeyBindings.Bind.South))
                        _choice++;

                    _choice = _choice < 0 ? _choice + _numChoices : _choice;
                    _choice = _choice % _numChoices;

                    if (KeyBindings.Pressed(KeyBindings.Bind.Accept))
                    {
                        switch ((Choices)_choice)
                        {
                            case Choices.Name:
                                IO.SetInput(IO.Indexes, ' ');
                                IO.QuestionReaction = Submit;
                                IO.Answer =
                                    ActorDefinition.ActorDefinitions[0].Name;
                                _state = State.Name;
                                break;
                            case Choices.Start:
                                //LH-191214
                                //should maybe generate the gamestate here, or
                                //on load, currently we're loading stuff in
                                //on startup, which might not really be ncssary.
                                Game.SwitchState(Game.GameState);
                                break;
                            case Choices.Load:
                                if (SaveIO.SaveExists)
                                {
                                    SaveIO.Load();
                                    Game.SwitchState(Game.GameState);
                                }
                                break;
                            case Choices.Exit:
                                Game.Exit();
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }

                    break;

                case State.Name:
                    IO.QuestionPromptInput();
                    break;
            }
        }

        private void Submit()
        {
            _state = State.MainMenu;
            ActorDefinition.ActorDefinitions[0].Name = IO.Answer;
        }

        public override void Draw()
        {
            _menuConsole.CellData.Clear();

            string nameString = String.Format(
                "{0}Name: {1}",
                _choice == (int)Choices.Name ? ">" : "",
                _state == State.Name
                    ? IO.Answer + "_"
                    : ActorDefinition.ActorDefinitions[0].Name
            );

            string startString = String.Format(
                "{0}Enter dungeon",
                _choice == (int)Choices.Start ? ">" : ""
            );

            string loadString = String.Format(
                "{0}{1}Load game",
                SaveIO.SaveExists ? "" : "#aaaaaa",
                _choice == (int)Choices.Load ? ">" : ""
            );

            string exitString = String.Format(
                "{0}Quit",
                _choice == (int)Choices.Exit ? ">" : ""
            );

            _menuConsole.DrawColorString(2, 1, nameString);
            _menuConsole.DrawColorString(2, 2, startString);
            _menuConsole.DrawColorString(2, 3, loadString);
            _menuConsole.DrawColorString(2, 4, exitString);

            _menuConsole.DrawColorString(
                2, _menuConsole.GetHeight() - 2,
                "Checksum: " + Game.Hash
            );
        }

        public override void SwitchTo()
        {
            Engine.ConsoleRenderStack = _consoles;
        }

        private void SetupConsoles()
        {
            _consoles = new ConsoleList();
            _menuConsole = new Console(80, 25);

            _consoles.Add(_menuConsole);
        }
    }
}
