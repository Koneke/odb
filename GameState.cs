using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public abstract class AppState
    {
        public abstract void Update();
        public abstract void Draw();
    }

    public class GameState : AppState
    {
        private readonly ODBGame _game;

        public GameState(ODBGame game)
        {
            _game = game;
        }

        public void SetupBrains() {
            if(_game.Brains == null)
                _game.Brains = new List<Brain>();
            else
                _game.Brains.Clear();

            //note: this means that we only act with actors on the same
            //      floor as the player, might want to change this in the future
            foreach (Actor actor in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
            {
                if (actor.ID == 0)
                    _game.Player = actor;
                else
                    _game.Brains.Add(new Brain(actor));
            }
        }

// ReSharper disable once InconsistentNaming
        private void ProcessNPCs()
        {
            while(_game.Player.Cooldown > 0 && _game.Player.IsAlive)
            {
                foreach (Brain b in _game.Brains
                    .Where( b =>
                        b.MeatPuppet.Cooldown <= 0 &&
                        b.MeatPuppet.Awake))
                    b.Tick();

                foreach (Actor a in World.WorldActors
                    .Where(a => a.LevelID == World.Level.ID)
                    .Where(a => a.Awake))
                    a.Cooldown--;

                foreach (Actor a in World.WorldActors)
                {
                    a.HpRegCooldown--;
                    a.MpRegCooldown--;
                    if (a.HpRegCooldown == 0)
                    {
                        a.HpCurrent = System.Math.Min(a.HpMax, a.HpCurrent + 1);
                        a.HpRegCooldown = 100;
                    }
                    // ReSharper disable once InvertIf
                    if (a.MpRegCooldown == 0)
                    {
                        a.MpCurrent = System.Math.Min(a.MpMax, a.MpCurrent + 1);
                        a.MpRegCooldown = 300 -
                            a.Get(Stat.Intelligence) * 10;
                    }
                }

                //todo: should apply to everyone?
                _game.Player.RemoveFood(1);

                _game.GameTick++;

                foreach (Actor a in World.WorldActors)
                {
                    if (!a.IsAlive) continue;
                    foreach (LastingEffect effect in a.LastingEffects)
                        effect.Tick();
                    a.LastingEffects.RemoveAll(
                        x =>
                            x.Life > x.LifeLength &&
                                x.LifeLength != -1);
                }
                World.WorldActors.RemoveAll(a => !a.IsAlive);
            }
        }

        public override void Update()
        {
            if (KeyBindings.Pressed(KeyBindings.Bind.Exit))
            {
                if (_game.WizMode)
                {
                    _game.WizMode = false;
                    IO.IOState = InputType.PlayerInput;
                }
                else
                {
                    switch (IO.IOState)
                    {
                        case InputType.PlayerInput:
                            ODBGame.Game.Exit();
                            break;
                        case InputType.Inventory:
                            _game.InvMan.HandleCancel();
                            break;
                        default:
                            IO.IOState = InputType.PlayerInput;
                            break;
                    }
                }
            }

            _game.UI.Input();

            if (_game.WizMode) Wizard.WmInput();
            else
            {
                switch (IO.IOState)
                {
                    case InputType.Splash:
                        if (KeyBindings.Pressed(KeyBindings.Bind.Accept))
                            _game.UI.LoggedSincePlayerInput -= _game.UI.LogSize;
                        if (_game.UI.LoggedSincePlayerInput <= _game.UI.LogSize)
                            IO.IOState = InputType.PlayerInput;
                        break;

                    case InputType.QuestionPromptSingle:
                    case InputType.QuestionPrompt:
                        IO.QuestionPromptInput();
                        break;

                    case InputType.Targeting:
                        IO.TargetInput();
                        break;

                    case InputType.PlayerInput:
                        if (_game.UI.CheckMorePrompt()) break;
                        if (_game.Player.Cooldown == 0)
                            Player.PlayerInput();
                        else ProcessNPCs(); //mind: also ticks gameclock
                        break;

                    case InputType.Inventory:
                        _game.InvMan.InventoryInput();
                        break;

                    default: throw new System.Exception("");
                }
            }

            _game.UI.UpdateCamera();

            //should probably find a better place to tick this
            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
            {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }

            if (KeyBindings.Pressed(KeyBindings.Bind.Window_Size))
                _game.UI.CycleFont();

            if (KeyBindings.Pressed(KeyBindings.Bind.Dev_ToggleConsole))
            {
                if (_game.WizMode)
                {
                    IO.Answer = "";
                    IO.IOState = InputType.PlayerInput;
                }
                else Wizard.WmCursor = _game.Player.xy;
                _game.WizMode = !_game.WizMode;
            }
        }

        public override void Draw()
        {
            _game.UI.RenderConsoles();
        }
    }
}