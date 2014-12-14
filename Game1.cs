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
    public class Game1 : Game
    {
        public UI UI;
        public static Game1 Game;

        public Game1()
        {
            UI.Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public bool WizMode;

        public int GameTick;
        public int Seed;

        //LH-101214: Move these two world?
        public List<Level> Levels;
        public Level Level;

        public List<Brain> Brains;

        public bool OpenRolls = false;

        public InventoryManager InvMan;

        public Actor Player;

        public int MessagesLoggedSincePlayerControl;
        public int LogSize;
        public List<ColorString> LogText;

        //LH-021214: Currently casting actor (so that our spell casting system
        //           can use the same Question-system as other commands.
        //           Since there should never possibly pass a tick between an
        //           actor casting and targetting a spell, we shouldn't need to
        //           worry about this being rewritten as we do stuff.
        public Actor Caster;
        public Point Target;
        public Spell TargetedSpell;
        public Action QuestionReaction;
        public Stack<string> QpAnswerStack;

        public int StandardActionLength = 10;

        protected override void Initialize()
        {
            #region engineshit
            //this is starting to look dumb
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

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            UI = new UI();

            UI.ScreenSize = new xnaPoint(80, 25);
            #endregion

            SetupMagic(); //essentially magic defs, but we hardcode magic
            SetupTickingEffects(); //same as magic, cba to add scripting
            SaveIO.ReadActorDefinitionsFromFile("Data/actors.def");
            SaveIO.ReadItemDefinitionsFromFile("Data/items.def");
            SaveIO.ReadTileDefinitionsFromFile("Data/tiles.def");

            Seed = Guid.NewGuid().GetHashCode();
            Util.SetSeed(Seed);

            //todo: probably should go back to using this?
            //IO.Load(); //load entire game (except definitions atm)

            Game.Levels = new List<Level>();

            Player = new Actor(
                new Point(0, 0),
                0,
                Util.ADefByName("Moribund"),
                1
            );

            InvMan = new InventoryManager();

            Levels.Add(Level = new Generator().Generate(null, 1));

            Level.Spawn(Player);
            Player.xy =
                Level.Rooms
                    .SelectMany(r => r.GetTiles())
                    .Where(t => !t.Solid)
                    .Where(t => t.Door == Door.None)
                    .Where(t => Level.At(t.Position).Actor == null)
                    .ToList()
                    .SelectRandom().Position;

            //todo: these should probably not be lastingeffects
            //      they probably should be hardcoded really.
            LastingEffect le = new LastingEffect(
                Player.ID,
                StatusType.None,
                -1,
                Util.TickingEffectDefinitionByName("passive hp regeneration")
            );
            Game.Player.AddEffect(le);
            le = new LastingEffect(
                Player.ID,
                StatusType.None,
                -1,
                Util.TickingEffectDefinitionByName("passive mp regeneration")
            );
            Game.Player.AddEffect(le);

            SetupBrains();

            LogSize = 3;
            LogText = new List<ColorString>();
            Log("#ff0000" + "Welcome!");

            QpAnswerStack = new Stack<string>();

            //wiz
            Wizard.WmCursor = Game.Player.xy;

            KeyBindings.ReadBinds(SaveIO.ReadFromFile("Data/keybindings.kb"));

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
                            MessagesLoggedSincePlayerControl -= LogSize;
                        if (MessagesLoggedSincePlayerControl <= LogSize)
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
                        if (MessagesLoggedSincePlayerControl > LogSize)
                        {
                            IO.IOState = InputType.Splash;
                            break;
                        }
                        MessagesLoggedSincePlayerControl = 0;
                        if (KeyBindings.Pressed(Bind.Inventory) && !WizMode)
                            IO.IOState = InputType.Inventory;
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
                .Where(a => a.LevelID == Level.ID))
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
                .Where(a => a.LevelID == Game.Level.ID))
                //shouldn't be needed, but
                //what did i even mean with that comment
                if (actor.ID == 0) Game.Player = actor;
                else Brains.Add(new Brain(actor));
        }

        public void SwitchLevel(Level newLevel, bool gotoStairs = false)
        {
            //assuming only one connector
            Point target = newLevel.Connectors
                .First(lc => lc.Target == Game.Level).Position;

            Game.Player.LevelID = newLevel.ID;

            Level = newLevel;
            foreach (Item item in Player.Inventory)
                item.MoveTo(newLevel);

            foreach (Actor a in Level.Actors)
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
            new Spell("foo bar")
            {
                CastType = InputType.QuestionPromptSingle,
                Effect = () =>
                {
                    string answer = Game.QpAnswerStack.Pop();
                    Item it = Game.Player.Inventory
                        [IO.Indexes.IndexOf(answer[0])];

                    Game.Log("You suddenly feel very dizzy...");
                    Game.Player.DropItem(it);
                    Game.Log("You drop your " + it.GetName("name") + ".");
                    Game.Player.AddEffect(StatusType.Confusion, 100);
                },
                SetupAcceptedInput = () =>
                {
                    IO.AcceptedInput.Clear();
                    foreach (Item item in Game.Player.Inventory)
                    {
                        IO.AcceptedInput.Add(
                            IO.Indexes[Game.Player.Inventory.IndexOf(item)]
                        );
                    }
                },
                CastDifficulty = 0,
                Cost = 1,
                Range = 0
            };

            //ReSharper disable once ObjectCreationAsStatement
            new Spell("forcebolt")
            {
                CastType = InputType.Targeting,
                Effect = () =>
                {
                    Actor target = Game.Level.ActorOnTile(Game.Target);
                    if (target == null)
                    {
                        Game.Log("The bolt fizzles in the air.");
                        return;
                    }
                    Game.Log(
                        "The forcebolt hits {1}.",
                        target.GetName("the")
                    );
                    target.Damage(Util.Roll("2d4"), Game.Caster);
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
                Effect = () =>
                {
                    string answer = Game.QpAnswerStack.Pop();
                    int index = IO.Indexes.IndexOf(answer[0]);
                    Item item = Game.Caster.Inventory[index];
                    item.Identify();
                },
                CastDifficulty = 15,
                Cost = 7
            };

            //ReSharper disable once ObjectCreationAsStatement
            //LH-011214: registered to the spelldefinition list in constructor.
            new Spell("fiery touch")
            {
                CastType = InputType.None,
                Effect = () => {
                    //I'm guessing monsters will have to use the qpstack as
                    //well as the player, atleast for now?
                    //Shouldn't be a problem as long as we're responsibly
                    //pushing and popping right everywhere.

                    //Should be the /point/ we're doing this attack on,
                    //alternaticely target ID (that's actually a pretty good
                    //idea, huh).
                    //Currently not doing anything with it since we know that
                    //this is a monster attack, so it's always targetting
                    //the player. Game.Caster still refers to the right caster
                    //though, so that's neat.
                    //ReSharper disable once UnusedVariable
                    string answer = Game.QpAnswerStack.Pop();
                    Game.Log(
                        Game.Player.GetName("Name") +
                        " " +
                        Game.Player.Verb("is") +
                        " burned by " +
                        Game.Caster.Definition.Name + "'s touch!"
                    );
                    Game.Player.Damage(Util.Roll("6d2"), Game.Caster);

                    if (!Game.Player.IsAlive) return;
                    if (Util.Roll("1d6") < 5) return;

                    if (Game.Player.HasEffect(StatusType.Bleed)) return;

                    Game.Log(
                        Game.Player.GetName("Name") +
                        Game.Player.Verb("start") + " bleeding!"
                    );
                    Game.Player.AddEffect(
                        new LastingEffect(
                            Game.Player.ID,
                            StatusType.Bleed,
                            -1,
                            Util.TickingEffectDefinitionByName("bleed")
                        )
                    );
                },
                CastDifficulty = 0,
                Range = 1,
                Cost = 0
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

            //ReSharper disable once ObjectCreationAsStatement
            new TickingEffectDefinition(
                "bleed",
                25,
                delegate(Actor holder)
                {
                    if(holder == Game.Player)
                        Game.Log("Your wound bleeds!");
                    else
                        Game.Log(
                            holder.GetName("Name")+"'s wound bleeds!"
                        );
                    //todo: getting killed by this effect does currently
                    //      NOT GRANT EXPERIENCE!
                    //      this since it's not directly from another actor,
                    //      but indirectly via the effect. this has to be
                    //      changed.
                    holder.Damage(Util.Roll("2d3"), null);
                }
            );
        }

        public Point NumpadToDirection(char c)
        {
            Point p;
            switch (c)
            {
                case 'y':
                case (char)Keys.D7: p = new Point(-1, -1); break;
                case 'k':
                case (char)Keys.D8: p = new Point(0, -1); break;
                case 'u':
                case (char)Keys.D9: p = new Point(1, -1); break;
                case 'h':
                case (char)Keys.D4: p = new Point(-1, 0); break;
                case 'l':
                case (char)Keys.D6: p = new Point(1, 0); break;
                case 'b':
                case (char)Keys.D1: p = new Point(-1, 1); break;
                case 'j':
                case (char)Keys.D2: p = new Point(0, 1); break;
                case 'n':
                case (char)Keys.D3: p = new Point(1, 1); break;
                default: throw new Exception(
                        "Bad input (expected numpad keycode, " +
                        "got something weird instead).");
            }
            return p;
        }

        public void Log(string s)
        {
            MessagesLoggedSincePlayerControl++;

            //always reset colouring
            s = s + "#ffffff";

            List<ColorString> rows = new List<ColorString>();
            ColorString cs = new ColorString(s);
            int x = 0;
            while(x < cs.String.Length)
            {
                ColorString row = cs.Clone();
                row.SubString(x, 80);
                rows.Add(row);
                x += 80;
                if (cs.String.Length - x <= 0) continue;

                if (row.ColorPoints.Count > 0)
                {
                    cs.ColorPoints.Add(
                        new Tuple<int, Color>(
                            x, row.ColorPoints.Last().Item2
                        )
                    );
                }
            }
            foreach (ColorString c in rows)
                LogText.Add(c);
        }
        public void Log(params object[] args)
        {
            Game.Log(String.Format((string)args[0], args));
        }

        //ReSharper disable once InconsistentNaming
        //LH-011214: NPC is a perfectly fine acronym thank you
        public void ProcessNPCs()
        {
            while(Player.Cooldown > 0 && Player.IsAlive)
            {
                foreach (Brain b in Brains
                    .Where(b =>
                        b.MeatPuppet.Cooldown <= 0 &&
                        b.MeatPuppet.Awake))
                    b.Tick();

                foreach (Actor a in
                    World.WorldActors
                    .Where(a => a.LevelID == Level.ID))
                    a.Cooldown--;

                //todo: should apply to everyone?
                Game.Player.RemoveFood(1);

                GameTick++;

                foreach (Actor a in World.WorldActors)
                {
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