using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole;
using Console = SadConsole.Consoles.Console;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using xnaPoint = Microsoft.Xna.Framework.Point;

//~~~ QUEST TRACKER for ?? nov ~~~
// * Item value and paid-for status
// * Wizard mode area select
// * Clean up wizard mode class a bit
//   * Fairly low prio, since it's not part of the /game/ per se,
//     but it /is/ fairly messy.
// * Switch from numbers in actor def to die?

//~~~ QUEST TRACKER for 2 dec ~~~
// * Containers
//   * Originally planned for 1 dec, but bumped forward.
//   * Should be doable with the new component system though,
//     including container in container, using the magic of
//     blocks :~)

//General note:
// * Currently, items need cWeapon to be wieldable. This is /not/ the way it
//   is supposed to. We need a GenerateHands(int count) or something, so we can
//   generate an equip set on the fly depending on number of hands item needs
//   so we can check them against Actor.CanEquip();

namespace ODB
{
    public class Game1 : Game
    {
        readonly GraphicsDeviceManager _graphics;

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

        public Actor Player;
        //for now, breaking on of the RL rules a bit,
        //only the player actually has hunger.
        public int Food; //not yet saved

        int _camX, _camY;
        int _scrW, _scrH;

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

        protected override void Initialize()
        {
            #region engineshit
            //this is starting to look dumb
            PlayerResponses.Game =
            gObject.Game =
            ODB.Player.Game =
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
                Engine.DefaultFont = Serializer.Deserialize<Font>(stream);

            Engine.DefaultFont.ResizeGraphicsDeviceManager(
                _graphics, 80, 25, 0, 0
            );
            #endregion

            SetupConsoles();

            _camX = _camY = 0;
            _scrW = 80; _scrH = 25;

            SetupMagic(); //essentially magic defs, but we hardcode magic
            SetupTickingEffects(); //same as magic, cba to add scripting
            IO.ReadActorDefinitionsFromFile("Data/actors.def");
            IO.ReadItemDefinitionsFromFile("Data/items.def");
            IO.ReadTileDefinitionsFromFile("Data/tiles.def");

            Seed = Guid.NewGuid().GetHashCode();
            Util.SetSeed(Seed);

            //todo: probably should go back to using this?
            //IO.Load(); //load entire game (except definitions atm)

            Game.Levels = new List<Level>();
            Game.Level = new Level(80, 25);
            Game.Levels.Add(Game.Level);

            Game.Level.WorldActors.Add(
                Player = new Actor(
                    new Point(13, 15),
                    Util.ADefByName("Moribund")
                )
            );
            Game.Food = 9000;

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
                if (WizMode) {
                    WizMode = false; IO.IOState = InputType.PlayerInput; }
                else if (IO.IOState != InputType.PlayerInput)
                {
                    //LH-021214: Temporary hack to make sure you can't cancel
                    //           using an item or casting a spell without
                    //           targeting. This is, well, okay-ish for now,
                    //           if slightly annoying. This is mainly because
                    //           a cancelled item use would otherwise spend a
                    //           charge without doing anything.
                    //           We'll see which way turns out the best.
                    if(Caster == null)
                        IO.IOState = InputType.PlayerInput;
                }
                else if (_inventoryConsole.IsVisible)
                    _inventoryConsole.IsVisible = false;
                else Exit();
            }

            if (IO.KeyPressed(Keys.I) && !WizMode)
                _inventoryConsole.IsVisible =
                    !_inventoryConsole.IsVisible;

            if (IO.KeyPressed((Keys)0x6B)) //np+
                _logSize = Math.Min(_logConsole.ViewArea.Height, ++_logSize);
            if (IO.KeyPressed((Keys)0x6D)) //np-
                _logSize = Math.Max(0, --_logSize);
            _logConsole.Position = new xnaPoint(
                0, -_logConsole.ViewArea.Height + _logSize
            );

            //todo: edge scrolling
            //ReSharper disable once ConvertToConstant.Local
            //LH-011214: Not consting this since it will be adjustable in the
            //           future, the const is only here as a placeholder really.
            int scrollSpeed = 3;
            if (IO.KeyPressed(Keys.Right)) _camX += scrollSpeed;
            if (IO.KeyPressed(Keys.Left)) _camX -= scrollSpeed;
            if (IO.KeyPressed(Keys.Up)) _camY += scrollSpeed;
            if (IO.KeyPressed(Keys.Down)) _camY -= scrollSpeed;

            _camX = Math.Max(0, _camX);
            _camX = Math.Min(Game.Level.LevelSize.x - _scrW, _camX);
            _camY = Math.Max(0, _camY);
            _camY = Math.Min(Game.Level.LevelSize.y - _scrH, _camY);
            #endregion camera

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
                        if (Player.Cooldown == 0)
                            ODB.Player.PlayerInput();
                        else ProcessNPCs(); //mind: also ticks gameclock
                        break;
                    //if this happens,
                    //you're breaking some kind of weird shit somehow
                    default: throw new Exception("");
                }
            }

            //should probably find a better place to tick this
            foreach (Actor a in Game.Level.WorldActors)
            {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }

            #region f-keys //devstuff
            if (IO.KeyPressed(Keys.F1))
                IO.WriteActorDefinitionsToFile("Data/actors.def");
            if (IO.KeyPressed(Keys.F2))
                IO.WriteItemDefinitionsToFile("Data/items.def");

            if (IO.KeyPressed(Keys.F5)) IO.Save();
            if (IO.KeyPressed(Keys.F6)) IO.Load();

            if (IO.KeyPressed(Keys.F9) || IO.KeyPressed(Keys.OemTilde))
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

        public void SetupConsoles()
        {
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
            _inventoryConsole.IsVisible = false;

            _statRowConsole = new Console(80, 2) {
                Position = new xnaPoint(0, 23)
            };

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
            foreach (Actor actor in Level.WorldActors)
                //shouldn't be needed, but
                //what did i even mean with that comment
                if (actor.ID == 0) Game.Player = actor;
                else Brains.Add(new Brain(actor));
        }

        public void SwitchLevel(Level newLevel, bool gotoStairs = true)
        {
            bool downwards =
                Game.Levels.IndexOf(newLevel) > Game.Levels.IndexOf(Level);
            Level.WorldActors.Remove(Player);
            foreach (Item it in Player.Inventory) Level.WorldItems.Remove(it);

            Level = newLevel;
            Level.WorldActors.Add(Player);
            foreach (Item it in Player.Inventory) Level.WorldItems.Add(it);

            foreach (Actor a in Level.WorldActors)
                //reset vision, incase the level we moved to is a different size
                a.Vision = null;

            SetupBrains();

            if (!gotoStairs) return;

            //auto tele to (a) pair of stairs
            //so hint for now: don't have more stairs, it gets weird
            for (int x = 0; x < Level.LevelSize.x; x++)
                for (int y = 0; y < Level.LevelSize.y; y++)
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
                    string answer = Util.Game.QpAnswerStack.Pop();
                    Util.Game.Log(
                        Game.Player.GetName("Name") +
                        " " +
                        Game.Player.Is() +
                        " burned by " +
                        Game.Caster.Definition.Name + "'s touch!"
                    );
                    Game.Player.Damage(Util.Roll("6d2"));

                    if (!Game.Player.IsAlive) return;
                    if (Util.Roll("1d6") < 5) return;

                    TickingEffectDefinition bleed =
                        Util.TickingEffectDefinitionByName("bleed");

                    if (Game.Player.HasEffect(bleed)) return;
                    Game.Log(
                        //todo: Note, should be startS for everyone except you
                        Game.Player.GetName("Name") +
                        " start bleeding!"
                    );
                    Game.Player.AddEffect(
                        Util.TickingEffectDefinitionByName("bleed")
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
                    holder.Damage(Util.Roll("2d3"));
                }
            );
        }

        void DrawToScreen(Point xy, Color? bg, Color fg, String tile)
        {
            if (bg != null)
                _dfc.CellData.SetBackground(
                    xy.x, xy.y, bg.Value
                );
            else
            {
                Tile bgtile = Game.Level.Map[xy.x, xy.y];
                if(bgtile != null)
                    _dfc.CellData.SetBackground(
                        xy.x, xy.y,
                        bgtile.Background
                    );
            }

            _dfc.CellData.SetForeground(xy.x, xy.y, fg);

            _dfc.CellData.Print(xy.x, xy.y, tile);
        }

        void DrawBorder(Console c, Rect r, Color bg, Color fg)
        {
            for (int x = 0; x < r.wh.x; x++)
            {
                c.CellData.Print(x, 0, (char)205+"", fg, bg);
                c.CellData.Print(x, r.wh.y-1, (char)205+"", fg, bg);
            }
            for (int y = 0; y < r.wh.y; y++)
            {
                c.CellData.Print(0, y, (char)186+"", fg, bg);
                c.CellData.Print(r.wh.x-1, y, (char)186+"", fg, bg);
            }
            _inventoryConsole.CellData.Print(0, 0, (char)201 + "");
            _inventoryConsole.CellData.Print(r.wh.x-1, 0, (char)187 + "");
            _inventoryConsole.CellData.Print(0, r.wh.y-1, (char)200 + "");
            _inventoryConsole.CellData.Print(r.wh.x-1, r.wh.y-1, (char)188 + "");
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
                case (char)Keys.D7: p = new Point(-1, -1); break;
                case (char)Keys.D8: p = new Point(0, -1); break;
                case (char)Keys.D9: p = new Point(1, -1); break;
                case (char)Keys.D4: p = new Point(-1, 0); break;
                case (char)Keys.D6: p = new Point(1, 0); break;
                case (char)Keys.D1: p = new Point(-1, 1); break;
                case (char)Keys.D2: p = new Point(0, 1); break;
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

        //ReSharper disable once InconsistentNaming
        //LH-011214: NPC is a perfectly fine acronym thank you
        public void ProcessNPCs()
        {
            while(Game.Player.Cooldown > 0)
            {
                foreach (Brain b in Brains
                    .Where(
                        b => Game.Level.WorldActors.Contains(b.MeatPuppet) &&
                        b.MeatPuppet.Cooldown <= 0 && b.MeatPuppet.Awake))
                    b.Tick();

                foreach (Brain b in Brains.Where(b => b.MeatPuppet.Awake))
                    b.MeatPuppet.Cooldown--;
                Game.Player.Cooldown--;

                Game.Food--;

                GameTick++;

                List<Actor> wa = new List<Actor>(Level.WorldActors);
                foreach (Actor a in wa)
                {
                    foreach (TickingEffect effect in a.TickingEffects)
                        effect.Tick();
                    a.TickingEffects.RemoveAll(x => x.Die);
                }

                foreach (Actor a in wa)
                {
                    foreach (LastingEffect effect in a.LastingEffects)
                        effect.Tick();
                    a.LastingEffects.RemoveAll(x => x.Life <= 0);
                }
                Level.WorldActors = wa;
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

            for (int x = 0; x < _scrW; x++) for (int y = 0; y < _scrH; y++)
            {
                Tile t = Game.Level.Map[x + _camX, y + _camY];

                if (t == null) continue;
                if (
                    !(Game.Level.Seen[x + _camX, y + _camY] || WizMode)
                ) continue;

                bool inVision =
                    Game.Player.Vision[x + _camX, y + _camY] || WizMode;

                string tileToDraw = t.Character;
                //doors override the normal tile
                //which shouldn't be a problem
                //if it is a problem, it's not, it's something else
                if (t.Door == Door.Closed) tileToDraw = "+";
                if (t.Door == Door.Open) tileToDraw = "/";
                if (t.Stairs == Stairs.Down) tileToDraw = ">";
                if (t.Stairs == Stairs.Up) tileToDraw = "<";

                DrawToScreen(
                    new Point(x, y),
                    t.Background,
                    t.Foreground * (inVision ? 1f : 0.6f),
                    tileToDraw
                );
            }
        }

        private void RenderItems()
        {
            Rect screen = new Rect(new Point(_camX, _camY), new Point(80, 25));

            int[,] itemCount = new int[
                Game.Level.LevelSize.x,
                Game.Level.LevelSize.y
            ];

            foreach (Item i in Game.Level.WorldItems)
                itemCount[i.xy.x, i.xy.y]++;

            foreach (Item i in Game.Level.WorldItems
                .Where(i => Game.Player.Vision[i.xy.x, i.xy.y] || WizMode)
                .Where(i => screen.ContainsPoint(i.xy)))
            {
                if (itemCount[i.xy.x, i.xy.y] == 1)
                    DrawToScreen(
                        i.xy,
                        i.Identified ? i.Definition.Background : null,
                        i.Identified ? i.Definition.Foreground : Color.Gray,
                        i.Definition.Tile
                        );
                    //not sure I like the + for pile, since doors are +
                else DrawToScreen(i.xy, null, Color.White, "+");
            }
        }

        private void RenderActors()
        {
            Rect screen = new Rect(new Point(_camX, _camY), new Point(80, 25));

            int[,] actorCount = new int[
                Game.Level.LevelSize.x,
                Game.Level.LevelSize.y
            ];

            foreach (Actor a in Game.Level.WorldActors)
                actorCount[a.xy.x, a.xy.y]++;

            foreach (Actor a in Game.Level.WorldActors
                .Where(a => Game.Player.Vision[a.xy.x, a.xy.y] || WizMode)
                .Where(a => screen.ContainsPoint(a.xy)))
            {
                if (actorCount[a.xy.x, a.xy.y] == 1)
                    DrawToScreen(
                        a.xy, a.Definition.Background,
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
                Color.Black,
                Color.WhiteSmoke,
                ' ',
                null
            );

            _inputRowConsole.CellData.Print(
                0, 0, (WizMode ? "" : IO.Question + " ") + IO.Answer);
        }

        private void RenderInventory()
        {
            int inventoryW = _inventoryConsole.ViewArea.Width;
            int inventoryH = _inventoryConsole.ViewArea.Height;

            _inventoryConsole.CellData.Clear();
            _inventoryConsole.CellData.Fill(Color.White, Color.Black, ' ', null);

            DrawBorder(
                _inventoryConsole,
                new Rect(
                    new Point(0, 0),
                    new Point(inventoryW, inventoryH)),
                Color.Black,
                Color.DarkGray
            );

            _inventoryConsole.CellData.Print(
                2, 0, Player.Definition.Name, Color.White);

            for (int i = 0; i < Player.Inventory.Count; i++)
            {
                Item it = Player.Inventory[i];

                bool equipped = Game.Player.IsEquipped(it);

                string name = "" + ((char)(97 + i));
                name += " - ";


                if (it.Definition.Stacking)
                    name += it.Count + "x ";

                if (Player.Inventory[i].Mod != 0)
                {
                    name += Player.Inventory[i].Mod >= 0 ? "+" : "-";
                    name += Math.Abs(Player.Inventory[i].Mod) + " ";
                }

                name += Player.Inventory[i].GetName("Name");
                //LH-021214: We might actually not know the number of charges..?
                if (it.Charged)
                {
                    name += "["+it.Count+"]";
                }
                if (equipped) name += " (eq)";

                _inventoryConsole.CellData.Print(
                    2, i + 1, name
                );
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

            string namerow = Player.Definition.Name;
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
                (" " + LastingEffect.EffectNames[lastingEffect.Type]));

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

        #region engineshit
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            Engine.Draw(gameTime);
            base.Draw(gameTime);
        }
        #endregion
    }
}