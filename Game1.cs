using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole;
using Console = SadConsole.Consoles.Console;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using xnaPoint = Microsoft.Xna.Framework.Point;

//~~~ QUEST TRACKER for ?? dec ~~~
// * Item paid-for status.
// * Wizard mode area select.
// * Clean up wizard mode class a bit.
//   * Fairly low prio, since it's not part of the /game/ per se,
//     but it /is/ fairly messy.
// * Inventory stuff currently doesn't make noise.

//~~~ QUEST TRACKER for 09 dec ~~~
// * Write item/character tile number, instead of the actual tile,
//   to the .def files.
//   * Mainly because it just makes things more convenient to edit
//     manually.
// * Switch to human numbers in .def-files?
//   * Original reason for hex was because I was dumb and though
//     "wow can't use variable length things", then I realized what a semicolon
//     was?
//   * More easily manually tweaked that way.

namespace ODB
{
    public class Game1 : Game
    {
        readonly GraphicsDeviceManager _graphics;

        private List<Console> _consoles;
        Console _dfc;
        Console _logConsole;
        Console _inputRowConsole;
        Console _inventoryConsole;
        Console _statRowConsole;

        public static Game1 Game;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public bool WizMode;

        public int GameTick;
        public int Seed;

        public List<Level> Levels;
        public Level Level;
        public List<Brain> Brains;

        public bool OpenRolls = false;

        public InventoryManager InvMan;

        public Actor Player;
        //for now, breaking on of the RL rules a bit,
        //only the player actually has hunger.
        public int Food;

        private Point _camera;
        private Point _cameraOffset;
        public xnaPoint ScreenSize;

        int _logSize;
        List<string> _log;

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

        private Font _standardFont;
        private Font _doubleFont;

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
            Game = this;

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            Engine.Initialize(GraphicsDevice);
            Engine.UseMouse = false;
            Engine.UseKeyboard = true;

            using (var stream = System.IO.File.OpenRead("Fonts/IBM.font"))
                _standardFont = Serializer.Deserialize<Font>(stream);
            using (var stream = System.IO.File.OpenRead("Fonts/IBM2x.font"))
                _doubleFont = Serializer.Deserialize<Font>(stream);

            Engine.DefaultFont = _standardFont;

            Engine.DefaultFont.ResizeGraphicsDeviceManager(
                _graphics, 80, 25, 0, 0
            );
            #endregion

            SetupConsoles();

            _camera = new Point(0, 0);
            _cameraOffset = new Point(0, 0);
            ScreenSize = new xnaPoint(80, 25);

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

            Levels.Add(new Generator().Generate(0));
            Level = Levels[0];

            Level.Spawn(Player);
            Player.xy =
                Level.Rooms
                .SelectMany(r => r.GetTiles())
                .First(t => t.Stairs == Stairs.Up).Position;
                
            Food = 9000;

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

            _logSize = 3;
            _log = new List<string>();
            Log("Welcome!");

            QpAnswerStack = new Stack<string>();

            //wiz
            Wizard.WmCursor = Game.Player.xy;

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            Engine.Update(gameTime, IsActive);
            IO.Update(false);

            #region ui interaction
            if (IO.KeyPressed(Keys.Escape))
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

            if (IO.KeyPressed((Keys)0x6B)) //np+
                _logSize = Math.Min(_logConsole.ViewArea.Height, ++_logSize);
            if (IO.KeyPressed((Keys)0x6D)) //np-
                _logSize = Math.Max(0, --_logSize);
            _logConsole.Position = new xnaPoint(
                0, -_logConsole.ViewArea.Height + _logSize
            );
            #endregion

            if (WizMode) Wizard.WmInput();
            else
            {
                switch (IO.IOState)
                {
                    case InputType.QuestionPromptSingle:
                    case InputType.QuestionPrompt:
                        IO.QuestionPromptInput();
                        break;
                    case InputType.Targeting:
                        IO.TargetInput();
                        break;
                    case InputType.PlayerInput:
                        if (IO.KeyPressed(Keys.I) && !WizMode)
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

            UpdateCamera();

            //should probably find a better place to tick this
            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == Level.ID))
            {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }

            #region f-keys //devstuff
            if (IO.KeyPressed(Keys.F1))
                SaveIO.WriteActorDefinitionsToFile("Data/actors.def");
            if (IO.KeyPressed(Keys.F2))
                SaveIO.WriteItemDefinitionsToFile("Data/items.def");
            if (IO.KeyPressed(Keys.F3))
            {
                Engine.DefaultFont = Engine.DefaultFont == _standardFont
                    ? _doubleFont
                    : _standardFont;
                foreach (Console c in _consoles)
                    c.Font = Engine.DefaultFont;
                Engine.DefaultFont.ResizeGraphicsDeviceManager(
                    _graphics, 80, 25, 0, 0);
            }

            if (IO.KeyPressed(Keys.F5)) SaveIO.Save();
            if (IO.KeyPressed(Keys.F6)) SaveIO.Load();

            if (IO.KeyPressed(Keys.F9))
            {
                int index = Levels.IndexOf(Game.Level);
                Level level = new Generator()
                    .Generate(index);
                Levels.RemoveAt(index);
                Levels.Insert(index, level);
                SwitchLevel(level);
                IO.Answer = "";
            }

            if (IO.KeyPressed(Keys.OemTilde))
            {
                if (WizMode)
                {
                    IO.Answer = "";
                    IO.IOState = InputType.PlayerInput;
                }
                WizMode = !WizMode;
            }
            #endregion

            RenderConsoles();
            IO.Update(true);
            base.Update(gameTime);
        }

        private void UpdateCamera()
        {
            if (IO.KeyPressed(Keys.PageDown)) _cameraOffset.x++;
            if (IO.KeyPressed(Keys.Delete)) _cameraOffset.x--;
            if (IO.KeyPressed(Keys.Home)) _cameraOffset.y++;
            if (IO.KeyPressed(Keys.End)) _cameraOffset.y--;

            _camera.x = Player.xy.x - 40;
            _camera.y = Player.xy.y - 12;
            _camera += _cameraOffset;

            _camera.x = Math.Max(0, _camera.x);
            _camera.x = Math.Min(
                Game.Level.Size.x - ScreenSize.X, _camera.x);
            _camera.y = Math.Max(0, _camera.y);
            _camera.y = Math.Min(
                Game.Level.Size.y - ScreenSize.Y, _camera.y);
        }

        public void SetupConsoles()
        {
            _consoles = new List<Console>();

            _dfc = new Console(80, 25);
            Engine.ActiveConsole = _dfc;

            //22 instead of 25 so inputRow and statRows fit.
            _logConsole = new Console(80, 22) {
                Position = new xnaPoint(0, -19)
            };
            //part of the console is offscreen, so we can resize it downwards

            _inputRowConsole = new Console(80, 1) {
                Position = new xnaPoint(0, 3),
                VirtualCursor = { IsVisible = true }
            };

            _inventoryConsole = new Console(80, 25);
            _inventoryConsole.Position =
                new xnaPoint(
                    _dfc.ViewArea.Width - _inventoryConsole.ViewArea.Width,
                    0
                );

            _statRowConsole = new Console(80, 2) {
                Position = new xnaPoint(0, 23)
            };

            _consoles.Add(_dfc);
            _consoles.Add(_logConsole);
            _consoles.Add(_inputRowConsole);
            _consoles.Add(_statRowConsole);
            _consoles.Add(_inventoryConsole);

            //draw order
            Engine.ConsoleRenderStack.Add(_dfc);
            Engine.ConsoleRenderStack.Add(_logConsole);
            Engine.ConsoleRenderStack.Add(_inputRowConsole);
            Engine.ConsoleRenderStack.Add(_statRowConsole);
            Engine.ConsoleRenderStack.Add(_inventoryConsole);
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
            bool downwards =
                Game.Levels.IndexOf(newLevel) > Game.Levels.IndexOf(Level);

            Game.Player.LevelID = newLevel.ID;

            Level = newLevel;
            foreach (Item item in Player.Inventory)
                item.MoveTo(newLevel);

            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == Level.ID))
            {
                //reset vision, incase the level we moved to is a different size
                a.Vision = null;
                a.ResetVision();
            }

            SetupBrains();

            if (!gotoStairs) return;

            //auto tele to (a) pair of stairs
            //so hint for now: don't have more stairs, it gets weird
            for (int x = 0; x < Level.Size.x; x++)
                for (int y = 0; y < Level.Size.y; y++)
                    if (Level.Map[x, y] != null)
                        if (
                            (Level.Map[x, y].Stairs == Stairs.Up &&
                                downwards) ||
                            (Level.Map[x, y].Stairs == Stairs.Down &&
                                !downwards)
                        )
                            Player.xy = new Point(x, y);
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
                CastDifficulty = 1,
                Cost = 1,
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
                    string prename = item.GetName("the");
                    item.Identify();
                    Game.Log("You identified " +
                        prename + " as " + item.GetName("a") + ".");
                },
                CastDifficulty = 0,
                Cost = 0
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

        void DrawToScreen(Point xy, Color? bg, Color fg, String tile)
        {
            if (bg != null)
                _dfc.CellData.SetBackground(
                    xy.x, xy.y, bg.Value
                );

            _dfc.CellData.SetForeground(xy.x, xy.y, fg);

            _dfc.CellData.Print(xy.x, xy.y, tile);
        }

        void DrawBorder(Console c, Rect r, Color bg, Color fg)
        {
            for (int x = 0; x < r.wh.x; x++)
            {
                c.CellData.Print(r.xy.x + x, 0, (char)205+"", fg, bg);
                c.CellData.Print(r.xy.x + x, r.wh.y-1, (char)205+"", fg, bg);
            }
            for (int y = 0; y < r.wh.y; y++)
            {
                c.CellData.Print(r.xy.x, y, (char)186+"", fg, bg);
                c.CellData.Print(r.xy.x + r.wh.x-1, y, (char)186+"", fg, bg);
            }
            _inventoryConsole.CellData.Print(
                r.xy.x, 0, (char)201 + "");
            _inventoryConsole.CellData.Print(
                r.xy.x + r.wh.x-1, 0, (char)187 + "");
            _inventoryConsole.CellData.Print(
                r.xy.x, r.wh.y-1, (char)200 + "");
            _inventoryConsole.CellData.Print(
                r.xy.x + r.wh.x-1, r.wh.y-1, (char)188 + "");
        }

        public Point NumpadToDirection(Keys k)
        {
            Point p;
            switch (k)
            {
                case Keys.NumPad7: p = new Point(-1, -1); break;
                case Keys.NumPad8: p = new Point(0, -1); break;
                case Keys.NumPad9: p = new Point(1, -1); break;
                case Keys.NumPad4: p = new Point(-1, 0); break;
                case Keys.NumPad6: p = new Point(1, 0); break;
                case Keys.NumPad1: p = new Point(-1, 1); break;
                case Keys.NumPad2: p = new Point(0, 1); break;
                case Keys.NumPad3: p = new Point(1, 1); break;
                default: throw new Exception(
                        "Bad input (expected numpad keycode, " +
                        "got something weird instead).");
            }
            return p;
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
            List<string> rows = new List<string>();
            while (s.Length > 80)
            {
                rows.Add(s.Substring(0, 80));
                s = s.Substring(80, s.Length - 80);
            }
            rows.Add(s);
            foreach (string ss in rows)
                _log.Add(ss);
        }
        public void Log(params object[] args)
        {
            Game.Log(String.Format((string)args[0], args));
        }

        //ReSharper disable once InconsistentNaming
        //LH-011214: NPC is a perfectly fine acronym thank you
        public void ProcessNPCs()
        {
            while(Game.Player.Cooldown > 0)
            {
                foreach (Brain b in Brains
                    .Where(
                        b => b.MeatPuppet.Cooldown <= 0 && b.MeatPuppet.Awake))
                    b.Tick();

                foreach (Brain b in Brains.Where(b => b.MeatPuppet.Awake))
                    b.MeatPuppet.Cooldown--;
                Game.Player.Cooldown--;

                Game.Food--;

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

        public void RenderConsoles()
        {
            RenderMap();
            RenderItems();
            RenderActors();

            RenderLog();
            RenderPrompt();
            RenderInventory();
            RenderStatus();

            RenderTarget();
            RenderWmCursor();
        }

        private void RenderMap()
        {
            _dfc.CellData.Clear();
            //_dfc.CellData.Fill(Color.Gray, Color.Gray, ' ', null);

            for (int x = 0; x < ScreenSize.X; x++)
            for (int y = 0; y < ScreenSize.Y; y++)
            {
                Tile t = Game.Level.Map[x + _camera.x, y + _camera.y];

                if (t == null) continue;
                if (!(Game.Level.Seen[x + _camera.x, y + _camera.y] || WizMode)
                ) continue;

                bool inVision =
                    Game.Player.Vision[x + _camera.x, y + _camera.y] || WizMode;

                Color background = t.Background;
                if (Level.Blood[x, y]) background = Color.DarkRed;

                string tileToDraw = t.Character;
                //doors override the normal tile
                //which shouldn't be a problem
                //if it is a problem, it's not, it's something else
                if (t.Engraving != "") tileToDraw = t.RenderEngraving();
                if (t.Door == Door.Closed) tileToDraw = "+";
                if (t.Door == Door.Open) tileToDraw = "/";
                if (t.Stairs == Stairs.Down) tileToDraw = ">";
                if (t.Stairs == Stairs.Up) tileToDraw = "<";

                DrawToScreen(
                    new Point(x, y),
                    background,
                    t.Foreground * (inVision ? 1f : 0.6f),
                    tileToDraw
                );
            }
        }

        private void RenderItems()
        {
            Rect screen = new Rect(_camera, new Point(80, 25));

            int[,] itemCount = new int[
                Game.Level.Size.x,
                Game.Level.Size.y
            ];

            foreach (Item i in World.WorldItems
                .Where(it => it.LevelID == Game.Level.ID))
                itemCount[i.xy.x, i.xy.y]++;

            foreach (Item i in World.WorldItems
                .Where(i => i.LevelID == Level.ID)
                .Where(i => Game.Player.Vision[i.xy.x, i.xy.y] || WizMode)
                .Where(i => screen.ContainsPoint(i.xy)))
            {
                if (itemCount[i.xy.x, i.xy.y] == 1)
                    DrawToScreen(
                        i.xy - _camera,
                        i.Known ? i.Definition.Background : null,
                        i.Known ? i.Definition.Foreground : Color.Gray,
                        i.Definition.Tile
                    );
                //not sure I like the + for pile, since doors are +
                else DrawToScreen(i.xy, null, Color.White, "+");
            }
        }

        private void RenderActors()
        {
            Rect screen = new Rect(_camera, new Point(80, 25));

            int[,] actorCount = new int[
                Game.Level.Size.x,
                Game.Level.Size.y
            ];

            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == Level.ID))
                actorCount[a.xy.x, a.xy.y]++;

            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == Level.ID)
                .Where(a => Game.Player.Vision[a.xy.x, a.xy.y] || WizMode)
                .Where(a => screen.ContainsPoint(a.xy)))
            {
                if (actorCount[a.xy.x, a.xy.y] == 1)
                    DrawToScreen(
                        a.xy - _camera,
                        a.Definition.Background,
                        a.Definition.Foreground, a.Definition.Tile
                    );
                    //draw a "pile" (shouldn't happen at all atm
                else DrawToScreen(a.xy, null, Color.White, "*");
            }
        }

        private void RenderLog()
        {
            _logConsole.CellData.Clear();
            _logConsole.CellData.Fill(Color.White, Color.Black, ' ', null);
            for (
                int i = _log.Count, n = 0;
                i > 0 && n < _logConsole.ViewArea.Height;
                i--, n++
            ) {
                _logConsole.CellData.Print(
                    0, _logConsole.ViewArea.Height - (n + 1),
                    _log[i - 1]
                );
            }
        }

        private void RenderPrompt()
        {
            _inputRowConsole.IsVisible =
                (IO.IOState != InputType.PlayerInput) || WizMode;

            if (!_inputRowConsole.IsVisible) return;

            _inputRowConsole.Position = new xnaPoint(0, _logSize);
            _inputRowConsole.CellData.Fill(
                //Color.WhiteSmoke,
                Color.Black,
                Color.WhiteSmoke,
                ' ',
                null
            );

            _inputRowConsole.CellData.Print(
                0, 0, (WizMode ? "" : IO.Question + " ") + IO.Answer + "_");
        }

        private void RenderInventory()
        {
            _inventoryConsole.IsVisible = IO.IOState == InputType.Inventory;
            if (IO.IOState != InputType.Inventory) return;

            int inventoryW = _inventoryConsole.ViewArea.Width;
            int inventoryH = _inventoryConsole.ViewArea.Height;

            _inventoryConsole.CellData.Clear();
            _inventoryConsole.CellData.Fill(
                Color.White, Color.Black, ' ', null);

            DrawBorder(
                _inventoryConsole,
                new Rect(
                    new Point(0, 0),
                    new Point(inventoryW, inventoryH)),
                Color.Black,
                Color.DarkGray
            );

            const int offset = 14;

            DrawBorder(
                _inventoryConsole,
                new Rect(
                    new Point(inventoryW - (offset + 3), 0),
                    new Point(offset + 3, inventoryH)),
                Color.Black,
                Color.DarkGray
            );

            int j = 2;

            if (InventoryManager.CurrentContainer != -1)
            {
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<p>ut into", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<t>ake out", Color.White);
            }
            //if (InventoryManager.CurrentContainer == -1)
            else
            {
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<d>rop", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<e>at", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<r>ead", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<R>emove", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<S>heath", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<q>uiver", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<w>ield", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<W>ear", Color.White);
            }

            _inventoryConsole.CellData.Print(
                inventoryW - (offset), j++ + 1, "Free:", Color.White);

            List<DollSlot> emptySlots =
                Game.Player.PaperDoll
                    .Where(bp => bp.Item == null)
                    .OrderBy(bp => bp.Type)
                    .Select(bp => bp.Type)
                    .ToList();


            foreach (DollSlot slot in emptySlots)
            {
                _inventoryConsole.CellData.Print(
                    inventoryW - (offset), j++ + 1,
                    BodyPart.BodyPartNames[slot], Color.White);
            }

            //border texts
            if (InventoryManager.CurrentContainer == -1)
                _inventoryConsole.CellData.Print(
                    2, 0, Player.GetName("Name", true), Color.White);
            else
                _inventoryConsole.CellData.Print(
                    2, 0,
                    Util.GetItemByID(InventoryManager.CurrentContainer)
                        .GetName("Name"),
                    Color.White);

            string weightString =
                Game.Player.GetCarriedWeight() + "/" +
                Game.Player.GetCarryingCapacity() + "dag";

            _inventoryConsole.CellData.Print(
                _inventoryConsole.ViewArea.Width-(2+weightString.Length), 0,
                weightString, Color.White);

            _inventoryConsole.CellData.Print(
                2, _inventoryConsole.ViewArea.Height-1,
                _log[_log.Count-1],
                Color.White);
            //end border texts

            List<Item> items = new List<Item>();
            if (InventoryManager.CurrentContainer != -1)
                items.AddRange(InvMan.CurrentContents);
            else items.AddRange(Player.Inventory);

            int y = 1;
            for(int i = 0; i < items.Count; i++)
            {
                Item item = items[i];

                string name = "";

                name += IO.Indexes[items.IndexOf(item)];
                name += ") ";

                if (item.Stacking) name += item.Count + "x ";
                if ((item.HasComponent("cWearable") ||
                    item.HasComponent("cAttack")) &&
                    item.Known)
                    name += item.Mod.ToString("+#;-#;+0") + " ";
                name += item.GetName("Name");
                if (item.Stacking && item.Count > 1) name += "s";

                //LH-021214: We might actually not know the number of charges..?
                if (item.Charged) name += "[" + item.Count + "]";

                name += " " + item.GetWeight() + "dag";

                if (item.HasComponent("cContainer"))
                {
                    name += " (holding ";
                    name += InventoryManager.Containers[item.ID]
                        .Count + " item";
                    if (InventoryManager.Containers[item.ID].Count == 1)
                        name += "s";
                    name += ")";
                }

                if (Game.Player.IsEquipped(item))
                {
                    if (Game.Player.Quiver == item) name += " (quivered)";
                    else if (Game.Player.PaperDoll.Any(
                        x => x.Type == DollSlot.Hand && x.Item == item))
                        name += " (wielded)";
                    else
                        name += " (worn)";
                }

                Color bg = Color.Black;
                if (i == InventoryManager.Selection)
                {
                    if (InventoryManager.State ==
                            InventoryManager.InventoryState.Inserting ||
                        InventoryManager.State ==
                            InventoryManager.InventoryState.Joining)
                        name = "> " + name;
                    bg = Color.DimGray;
                }

                int yoffs=0;
                const int xoffs = 6;
                while (name.Length > 0)
                {
                    int cap = 58 + (yoffs > 0 ? -xoffs : 0);

                    int len;
                    if (name.Length > cap)
                    {
                        len = cap;
                        //check if we have a better place to break on
                        for (int k = 0; k < 10; k++)
                        {
                            if (name.Substring(cap - k, 1) != " ") continue;

                            len -= k;
                            name = name.Remove(cap - k, 1);
                            break;
                        }
                    }
                    else len = name.Length;

                    _inventoryConsole.CellData.Print(
                        yoffs > 0 ? 2 + xoffs : 2, y + yoffs++,
                        name.Substring(0, len), Color.White, bg
                    );
                    name = name.Substring(len, name.Length - len);
                }
                y += yoffs;
            }
        }

        private void RenderStatus()
        {
            _statRowConsole.CellData.Clear();
            _statRowConsole.CellData.Fill(Color.White, Color.Black, ' ', null);

            if (WizMode)
            {
                string str = "W I Z A R D";
                str += " (" + String.Format("{0:X2}", Wizard.WmCursor.x);
                str += " ; ";
                str += String.Format("{0:X2}", Wizard.WmCursor.y) + ")";
                str += " (" + Wizard.WmCursor.x + " ; " +
                    Wizard.WmCursor.y + ") ";

                str += "D:" + (Game.Levels.IndexOf(Game.Level) + 1) + "0M";
                str += " (" + Game.Level.Name + ")";

                _statRowConsole.CellData.Print(
                    0, 0, str 
                );
                return;
            }

            //Not using GetName() here, simply because that'd yield "You"
            //since it is the player.
            string namerow = Game.Player.GetName("Name", true);
            namerow += "  ";
            namerow += "STR " + Player.Get(Stat.Strength) + "  ";
            namerow += "DEX " + Player.Get(Stat.Dexterity) + "  ";
            namerow += "INT " + Player.Get(Stat.Intelligence) + "  ";
            namerow += "AC " + Player.GetArmor();

            if (Game.Food < 500)
                namerow += "  Starving";
            else if (Game.Food < 1500)
                namerow += "  Hungry";

            namerow = Game.Player.LastingEffects.Aggregate(
                namerow,
                (current, lastingEffect) => current +
                (
                    lastingEffect.Type == StatusType.None
                        ? ""
                        : " " + LastingEffect.EffectNames[lastingEffect.Type]
                )
            );

            string statrow = "";
            statrow += "[";
            statrow += ("" + Player.HpCurrent).PadLeft(3, ' ');
            statrow += "/";
            statrow += ("" + Player.HpMax).PadLeft(3, ' ');
            statrow += "]";

            statrow += " ";
            statrow += "[";
            statrow += ("" + Player.MpCurrent).PadLeft(3, ' ');
            statrow += "/";
            statrow += ("" + Player.MpMax).PadLeft(3, ' ');
            statrow += "]";

            statrow += " ";
            statrow += "XP:";
            statrow += Player.Level + "";

            statrow += " ";
            statrow += "$:";
            statrow +=
                Player.Inventory
                .Where(item => item.Type == 0x8000)
                .Sum(item => item.Count)
            ;

            statrow += " ";
            statrow += "T:";
            statrow += string.Format("{0:F1}", GameTick / 10f);

            statrow += " ";
            statrow += "D:" + (Game.Levels.IndexOf(Game.Level) + 1) + "0M";

            statrow += " (" + Game.Level.Name + ")";

            float playerHealthPcnt =
                Player.HpCurrent /
                (float)Player.HpMax
            ;
            float playerManaPcnt =
                Player.MpCurrent /
                (float)Player.MpMax
            ;
            float colorStrength = 0.6f + 0.4f - (0.4f * playerHealthPcnt);
            float manaColorStrength = 0.6f + 0.4f - (0.4f * playerManaPcnt);

            for (int x = 0; x < 9; x++)
                _statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        colorStrength - colorStrength * playerHealthPcnt,
                        colorStrength * playerHealthPcnt,
                        0
                    )
                );

            for (int x = 10; x < 19; x++)
                _statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        manaColorStrength - manaColorStrength * playerManaPcnt,
                        manaColorStrength * playerManaPcnt / 2,
                        manaColorStrength * playerManaPcnt
                    )
                );

            _statRowConsole.CellData.Print(0, 0, namerow);
            _statRowConsole.CellData.Print(0, 1, statrow);
        }

        private void RenderTarget()
        {
            if (IO.IOState != InputType.Targeting) return;

            Cell cs = _dfc.CellData[Target.x, Target.y];

            bool blink = (DateTime.Now.Millisecond%500 > 250);

            cs.Background = blink
                ? Util.InvertColor(cs.Background)
                : Color.White;

            cs.Foreground = blink
                ? Util.InvertColor(cs.Foreground)
                : Color.White;
        }

        private void RenderWmCursor()
        {
            if (!WizMode) return;

            Cell cs = _dfc.CellData[
                Wizard.WmCursor.x, Wizard.WmCursor.y];

            bool blink = (DateTime.Now.Millisecond%500 > 250);

            cs.Background = blink
                ? Util.InvertColor(cs.Background)
                : Color.White;

            cs.Foreground = blink
                ? Util.InvertColor(cs.Foreground)
                : Color.White;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            Engine.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}