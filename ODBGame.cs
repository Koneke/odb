using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using Microsoft.Xna.Framework;
using xnaPoint = Microsoft.Xna.Framework.Point;

using Bind = ODB.KeyBindings.Bind;

//~~~ QUEST TRACKER for ?? dec ~~~
// * Item paid-for status.
// * Wizard mode area select.
// * Clean up wizard mode class a bit.
//   * Fairly low prio, since it's not part of the /game/ per se,
//     but it /is/ fairly messy.
// * Inventory stuff currently doesn't make noise.

namespace ODB
{
    public class ODBGame : Game
    {
        public UI UI;
        public static ODBGame Game;

        public ODBGame()
        {
            UI.Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public bool WizMode;

        public int GameTick;
        public int Seed;

        public List<Brain> Brains;

        public bool OpenRolls = false;

        public InventoryManager InvMan;

        public Actor Player;

        //LH-021214: Currently casting actor (so that our spell casting system
        //           can use the same Question-system as other commands.
        //           Since there should never possibly pass a tick between an
        //           actor casting and targetting a spell, we shouldn't need to
        //           worry about this being rewritten as we do stuff.
        public Actor Caster;
        public Point Target;
        public Action QuestionReaction;
        public Stack<string> QpAnswerStack;

        public Command CurrentCommand = new Command("foo");

        public const int StandardActionLength = 10;

        private void GameReferences()
        {
            InventoryManager.Game = 
            PlayerResponses.Game =
            ODB.Player.Game =
            gObject.Game =
            Wizard.Game =
            Brain.Game =
            Util.Game =
            IO.Game =
            UI.Game =
            Game = this;
        }

        private void Load()
        {
            SetupMagic(); //essentially magic defs, but we hardcode magic
            SetupTickingEffects(); //same as magic, cba to add scripting
            SaveIO.ReadActorDefinitionsFromFile("Data/actors.def");
            SaveIO.ReadItemDefinitionsFromFile("Data/items.def");
            SaveIO.ReadTileDefinitionsFromFile("Data/tiles.def");
            KeyBindings.ReadBinds(SaveIO.ReadFromFile("Data/keybindings.kb"));

            //todo: mig this later
            Brains = new List<Brain>();
        }

        private void SetupSeed()
        {
            Seed = Guid.NewGuid().GetHashCode();
            Util.SetSeed(Seed);
        }

        protected override void Initialize()
        {
            #region engineshit
            GameReferences();

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            UI = new UI { ScreenSize = new xnaPoint(80, 25) };
            #endregion

            Load();
            SetupSeed();

            InvMan = new InventoryManager();

            QpAnswerStack = new Stack<string>();

            Player = new Actor(
                new Point(0, 0),
                0, Util.ADefByName("Moribund"), 10)
            { Awake = true };

            World.Levels.Add(World.Level = new Generator().Generate(null, 1));

            Player.xy = World.Level.RandomOpenPoint();

            World.Level.Spawn(Player);

            SetupBrains();

            UI.Log("Welcome!");

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            Engine.Update(gameTime, IsActive);
            IO.Update(false);

            if (KeyBindings.Pressed(Bind.Exit))
            {
                if (WizMode)
                {
                    WizMode = false;
                    IO.IOState = InputType.PlayerInput;
                }
                else
                {
                    switch (IO.IOState)
                    {
                        case InputType.PlayerInput:
                            Exit();
                            break;
                        case InputType.Inventory:
                            InvMan.HandleCancel();
                            break;
                        default:
                            //LH-021214: Temporary hack to make sure you can't
                            //           cancel using an item or casting a spell
                            //           without targeting. This is, well,
                            //           okay-ish for now, if slightly annoying.
                            //           This is mainly because a cancelled item
                            //           use would otherwise spend a charge
                            //           without doing anything. We'll see which
                            //           way turns out the best.
                            if (Caster == null)
                                IO.IOState = InputType.PlayerInput;
                            break;
                    }
                }
            }

            UI.Input();

            if (WizMode) Wizard.WmInput();
            else
            {
                switch (IO.IOState)
                {
                    case InputType.Splash:
                        if (KeyBindings.Pressed(Bind.Accept))
                            UI.LoggedSincePlayerInput -= UI.LogSize;
                        if (UI.LoggedSincePlayerInput <= UI.LogSize)
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
                        if (UI.CheckMorePrompt()) break;
                        if (Player.Cooldown == 0)
                            ODB.Player.PlayerInput();
                        else ProcessNPCs(); //mind: also ticks gameclock
                        break;
                    case InputType.Inventory:
                        InvMan.InventoryInput();
                        break;
                    //if this happens,
                    //you're breaking some kind of weird shit somehow
                    default: throw new Exception("");
                }
            }

            UI.UpdateCamera();

            //should probably find a better place to tick this
            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
            {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }

            #region f-keys //devstuff
            if (KeyBindings.Pressed(Bind.Dev_SaveActors))
                SaveIO.WriteActorDefinitionsToFile("Data/actors.def");
            if (KeyBindings.Pressed(Bind.Dev_SaveItems))
                SaveIO.WriteItemDefinitionsToFile("Data/items.def");
            if (KeyBindings.Pressed(Bind.Window_Size)) UI.CycleFont();

            if (KeyBindings.Pressed(Bind.Dev_ToggleConsole))
            {
                if (WizMode)
                {
                    IO.Answer = "";
                    IO.IOState = InputType.PlayerInput;
                }
                else Wizard.WmCursor = Player.xy;
                WizMode = !WizMode;
            }
            #endregion

            UI.RenderConsoles();
            IO.Update(true);
            base.Update(gameTime);
        }


        public void SetupBrains() {
            if(Brains == null) Brains = new List<Brain>();
            else Brains.Clear();
            foreach (Actor actor in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
                //shouldn't be needed, but
                //what did i even mean with that comment
                if (actor.ID == 0) Game.Player = actor;
                else Brains.Add(new Brain(actor));
        }

        public void SwitchLevel(Level newLevel, bool gotoStairs = false)
        {
            //assuming only one connector
            Point target = newLevel.Connectors
                .First(lc => lc.Target == World.Level).Position;

            Game.Player.LevelID = newLevel.ID;

            World.Level = newLevel;
            foreach (Item item in Player.Inventory)
                item.MoveTo(newLevel);

            foreach (Actor a in World.Level.Actors)
            {
                //reset vision, incase the level we moved to is a different size
                a.Vision = null;
                a.ResetVision();
            }

            SetupBrains();

            if (!gotoStairs) return;
            Game.Player.xy = target;
        }

        private static void SetupMagic()
        {
            //ReSharper disable once ObjectCreationAsStatement
            new Spell("potion of healing")
            {
                CastType = InputType.None,
                Effect = (c, t) =>
                {
                    Game.UI.Log(
                        Game.Caster.GetName("Name") + " " +
                        Game.Caster.Verb("#feel") + " " +
                        "better!"
                    );
                    Util.Game.Caster.Heal(Util.Roll("3d3"));
                }
            };

            //ReSharper disable once ObjectCreationAsStatement
            new Spell("forcebolt")
            {
                CastType = InputType.Targeting,
                Effect = (caster, target) =>
                {
                    //Actor target = World.Level.ActorOnTile(Game.Target);
                    Actor targetActor = World.Level.ActorOnTile(
                        (Point)target
                    );

                    if (targetActor == null)
                    {
                        Game.UI.Log("The bolt fizzles in the air.");
                        return;
                    }
                    Game.UI.Log("The forcebolt hits {1}.",
                        targetActor.GetName("the"));
                    DamageSource ds = new DamageSource
                    {
                        Damage = Util.Roll("2d4"),
                        AttackType = AttackType.Magic,
                        DamageType = DamageType.Physical,
                        Source = Game.Caster,
                        Target = targetActor
                    };
                    targetActor.Damage(ds);
                },
                CastDifficulty = 10,
                Cost = 3,
                Range = 5
            };

            new Spell("identify")
            {
                CastType = InputType.QuestionPromptSingle,
                SetupAcceptedInput = () =>
                {
                    IO.AcceptedInput.Clear();
                    IO.AcceptedInput.AddRange(
                        Game.Caster.Inventory
                            .Where(it => !it.Known)
                            .Select(item => Game.Caster.Inventory.IndexOf(item))
                            .Select(index => IO.Indexes[index])
                    );

                },
                Effect = (caster, target) =>
                {
                    //string answer = Game.QpAnswerStack.Pop();
                    Command cmd = (Command)target;
                    string answer = (string)cmd.Get("answer");
                    int index = IO.Indexes.IndexOf(answer[0]);
                    Item item = Game.Caster.Inventory[index];
                    item.Identify();
                },
                CastDifficulty = 15,
                Cost = 7
            };
        }

        public static void SetupTickingEffects()
        {
            //ReSharper disable once ObjectCreationAsStatement
            //LH-011214: Constructor registers the definition.
            new TickingEffectDefinition(
                "passive hp regeneration",
                100, //trigger once every 100 ticks
                delegate(Actor holder)
                {
                    if (holder.HpCurrent < holder.HpMax)
                        holder.HpCurrent++;
                }
            );

            //ReSharper disable once ObjectCreationAsStatement
            new TickingEffectDefinition(
                "passive mp regeneration",
                100, //trigger once every 100 ticks
                delegate(Actor holder)
                {
                    if (holder.MpCurrent < holder.MpMax)
                        holder.MpCurrent++;
                }
            );
        }

        public Point NumpadToDirection(char c)
        {
            Point p;
            switch (c)
            {
                case 'y': case (char)Keys.D7: p = new Point(-1, -1); break;
                case 'k': case (char)Keys.D8: p = new Point(0, -1); break;
                case 'u': case (char)Keys.D9: p = new Point(1, -1); break;
                case 'h': case (char)Keys.D4: p = new Point(-1, 0); break;
                case 'l': case (char)Keys.D6: p = new Point(1, 0); break;
                case 'b': case (char)Keys.D1: p = new Point(-1, 1); break;
                case 'j': case (char)Keys.D2: p = new Point(0, 1); break;
                case 'n': case (char)Keys.D3: p = new Point(1, 1); break;
                default: throw new Exception(
                        "Bad input (expected numpad keycode, " +
                        "got something weird instead).");
            }
            return p;
        }

        //ReSharper disable once InconsistentNaming
        //LH-011214: NPC is a perfectly fine acronym thank you
        public void ProcessNPCs()
        {
            while(Player.Cooldown > 0 && Player.IsAlive)
            {
                foreach (Brain b in new List<Brain>(Brains)
                    .Where(b =>
                        b.MeatPuppet.Cooldown <= 0 &&
                        b.MeatPuppet.Awake))
                    b.Tick();

                foreach (Actor a in
                    World.WorldActors
                    .Where(a => a.LevelID == World.Level.ID)
                    .Where(a => a.Awake)
                )
                    a.Cooldown--;

                foreach (Actor a in World.WorldActors)
                {
                    a.HpRegCooldown--;
                    a.MpRegCooldown--;
                    if (a.HpRegCooldown == 0)
                    {
                        a.HpCurrent = Math.Min(a.HpMax, a.HpCurrent + 1);
                        a.HpRegCooldown = 100;
                    }
                    // ReSharper disable once InvertIf
                    if (a.MpRegCooldown == 0)
                    {
                        a.MpCurrent = Math.Min(a.MpMax, a.MpCurrent + 1);
                        a.MpRegCooldown = 300 -
                            a.Get(Stat.Intelligence) * 10;
                    }
                }

                //todo: should apply to everyone?
                Game.Player.RemoveFood(1);

                GameTick++;

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

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            Engine.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}