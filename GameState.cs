using System.Collections.Generic;
using System.Linq;
using SadConsole;

namespace ODB
{
    public abstract class AppState
    {
        protected ODBGame Game;

        protected AppState(ODBGame game)
        {
            Game = game;
        }

        public abstract void Update();
        public abstract void Draw();
        public abstract void SwitchTo();
    }

    public class GameState : AppState
    {
        public GameState(ODBGame game) : base(game)
        {
            Game.InvMan = new InventoryManager();

            Game.Player = new Actor(
                new Point(0, 0), 0,
                Util.ADefByName("Moribund"), 1
            ) { Awake =  true };

            World.Levels.Add(World.Level = new Generator().Generate(null, 1));
            World.Level.Spawn(Game.Player);
            Game.Player.xy = World.Level.RandomOpenPoint();

            //force a screendraw in the beginning
            Game.Player.HasMoved = true;

            Game.SetupBrains();
        }

        public void SetupBrains() {
            if(Game.Brains == null)
                Game.Brains = new List<Brain>();
            else
                Game.Brains.Clear();

            //note: this means that we only act with actors on the same
            //      floor as the player, might want to change this in the future
            foreach (Actor actor in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
            {
                if (actor.ID == 0)
                    Game.Player = actor;
                else
                    Game.Brains.Add(new Brain(actor));
            }
        }

// ReSharper disable once InconsistentNaming
        private void ProcessNPCs()
        {
            while(!Game.Player.CanMove())
            {
                List<Brain> clone = new List<Brain>(Game.Brains);
                foreach (Brain b in clone.Where(b => b.MeatPuppet.CanMove()))
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
                    if (a.MpRegCooldown == 0)
                    {
                        a.MpCurrent = System.Math.Min(a.MpMax, a.MpCurrent + 1);
                        a.MpRegCooldown = 300 - a.Get(Stat.Intelligence) * 10;
                    }
                }

                //todo: should apply to everyone?
                Game.Player.RemoveFood(1);

                Game.GameTick++;

                foreach (Actor a in World.WorldActors)
                {
                    if (!a.IsAlive) continue;
                    foreach (LastingEffect effect in a.LastingEffects)
                        effect.Tick();
                    a.LastingEffects.RemoveAll(
                        x => x.Life > x.LifeLength && x.LifeLength != -1
                    );
                }
                World.WorldActors.RemoveAll(a => !a.IsAlive);
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
                            ODBGame.Game.Exit();
                            break;
                        case InputType.Inventory:
                            Game.InvMan.HandleCancel();
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
                            Player.PlayerInput();
                        //pass when sleeping etc.
                        else Game.Player.Pass();

                        ProcessNPCs(); //mind: also ticks gameclock
                        break;

                    case InputType.Inventory:
                        Game.InvMan.InventoryInput();
                        break;

                    default: throw new System.Exception("");
                }
            }

            Game.UI.UpdateCamera();

            //should probably find a better place to tick this
            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID)
                .Where(a => a.HasMoved)
            ) {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }


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