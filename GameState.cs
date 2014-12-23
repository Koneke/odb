using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole;

namespace ODB
{
    public abstract class AppState
    {
        public abstract void Update();
        public abstract void Draw();
        public abstract void SwitchTo();
    }

    public class GameState : AppState
    {
        public List<Brain> Brains;

        public GameState()
        {
            ODBGame.GameState = this;

            World.Instance.WorldContainers = new InventoryManager();
            Game.GeneratedUniques = new List<ActorID>();

            Game.Player = new Actor(
                new Point(0, 0),
                Util.ADefByName("Moribund"), 1
            );

            World.Level = new Generator().Generate(null, 1);
            World.Level.Spawn(Game.Player);
            Game.Player.xy = World.Level.RandomOpenPoint();

            //force a screendraw in the beginning
            Game.Player.HasMoved = true;
        }

        private void Tick()
        {
            foreach (Actor a in World.Instance.WorldActors)
            {
                a.HpRegCooldown--;
                a.MpRegCooldown--;
                if (a.HpRegCooldown == 0)
                {
                    a.HpCurrent = Math.Min(a.HpMax, a.HpCurrent + 1);
                    a.HpRegCooldown = 100;
                }
                if (a.MpRegCooldown == 0)
                {
                    a.MpCurrent = Math.Min(a.MpMax, a.MpCurrent + 1);
                    a.MpRegCooldown = 300 - a.Get(Stat.Intelligence) * 10;
                }
            }

            foreach (Actor a in World.Instance.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
                a.Cooldown = Math.Max(0, a.Cooldown - 1);

            //todo: should apply to everyone?
            Game.Player.Food--;

            Game.GameTick++;

            foreach (Actor a in World.Instance.WorldActors)
            {
                if (!a.IsAlive) continue;
                foreach (LastingEffect effect in a.LastingEffects)
                    effect.Tick();
                a.LastingEffects.RemoveAll(
                    x => x.Life > x.LifeLength && x.LifeLength != -1
                );
            }
            World.Instance.WorldActors.RemoveAll(a => !a.IsAlive);
        }

        //ReSharper disable once InconsistentNaming
        private void ProcessNPCs()
        {
            List<Brain> clone = new List<Brain>(Game.Brains);
            foreach (Brain b in clone)
            {
                if(b.MeatPuppet.CanMove())
                    b.Tick();
                //if we could move, only if we weren't sleeping or whatev,
                //pass.
                else if (b.MeatPuppet.CanMove(true))
                    b.MeatPuppet.Pass();
            }
        }

        public override void Update()
        {
            if (KeyBindings.Pressed(KeyBindings.Bind.Exit))
            {
                if (Game.WizMode)
                {
                    Game.WizMode = false;
                    IO.IOState = InputType.PlayerInput;
                }
                else
                {
                    switch (IO.IOState)
                    {
                        case InputType.PlayerInput:
                            if (!Game.Player.IsAlive)
                            {
                                ODBGame.Exit();
                                SaveIO.KillSave();
                            }

                            IO.SetInput('Y', 'n');
                            IO.AskPlayer(
                                "Really quit? [Yn]",
                                InputType.QuestionPromptSingle,
                                () =>
                                {
                                    if (IO.Answer[0] == 'Y')
                                        ODBGame.SaveQuit();
                                }
                            );
                            break;
                        case InputType.Inventory:
                            //todo
                            //probably actually want to split the container-
                            //data-y stuff, and the interact with the
                            //containers stuff into two separate things.
                            World.Instance.WorldContainers.HandleCancel();
                            break;
                        default:
                            IO.IOState = InputType.PlayerInput;
                            break;
                    }
                }
                Game.UI.FullRedraw();
            }

            Game.UI.Input();

            if (Game.WizMode) Wizard.WmInput();
            else
            {
                switch (IO.IOState)
                {
                    case InputType.Splash:
                        if (KeyBindings.Pressed(KeyBindings.Bind.Accept))
                            Game.UI.LoggedSincePlayerInput -= Game.UI.LogSize;
                        if (Game.UI.LoggedSincePlayerInput <= Game.UI.LogSize)
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
                        if (Game.UI.CheckMorePrompt()) break;

                        if (Game.Player.CanMove())
                            PlayerInput.HandlePlayerInput();
                        //pass when sleeping etc.
                        else Game.Player.Pass();

                        while (!Game.Player.CanMove() && Game.Player.IsAlive)
                        {
                            ProcessNPCs();
                            Tick();
                        }
                        break;

                    case InputType.Inventory:
                        //suppress --more-- in the inventory.
                        Game.UI.CheckMorePrompt();
                        World.Instance.WorldContainers.InventoryInput();
                        break;

                    default: throw new Exception("");
                }
            }

            Game.UI.UpdateCamera();

            //should probably find a better place to tick this
            foreach (Actor a in World.Level.Actors.Where(a => a.HasMoved))
                a.UpdateVision();

            if (KeyBindings.Pressed(KeyBindings.Bind.Dev_ToggleConsole))
            {
                IO.Answer = "";
                if (Game.WizMode)
                {
                    IO.IOState = InputType.PlayerInput;
                    IO.AnswerLimit = IO.AnswerLimitDefault;
                }
                else
                {
                    Wizard.WmCursor = Game.Player.xy;
                    IO.AnswerLimit = 80;
                }
                Game.WizMode = !Game.WizMode;
                Game.UI.FullRedraw();
            }
        }

        public override void Draw()
        {
            Game.UI.RenderConsoles();
        }

        public override void SwitchTo()
        {
            Engine.ConsoleRenderStack = Game.UI.Consoles;
        }
    }
}