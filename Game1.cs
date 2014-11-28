using System;
using System.Collections.Generic;

using SadConsole;
using Console = SadConsole.Consoles.Console;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using xnaPoint = Microsoft.Xna.Framework.Point;

//~~~ QUEST TRACKER for ?? nov ~~~
// * Item value and paid-for status
// * Eating
// * Wizard mode area select
// * Clean up wizard mode class a bit
//   * Fairly low prio, since it's not part of the /game/ per se,
//     but it /is/ fairly messy.
// * Switch from numbers in actor def to die?

//~~~ QUEST TRACKER for 28 nov ~~~
// * Actor intrinsics
//   * In essence, just rip the mods straight from the items.
//     Hell, we don't even need new modtypes.
// * Save/load level name (just put it in the header)
//   * n/ or name/ in WM to name for now.

namespace ODB
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Console dfc;
        Console logConsole;
        Console inputRowConsole;
        Console inventoryConsole;
        Console statRowConsole;

        Game1 Game;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public bool wizMode;

        public int GameTick;

        public List<Level> Levels;
        public Level Level;
        public List<Brain> Brains;

        public Actor player;

        int camX, camY;
        int scrW, scrH;

        int logSize;
        List<string> log;

        public Point Target;
        public Spell TargetedSpell;
        public Action QuestionReaction;
        public Stack<string> qpAnswerStack;

        public int standardActionLength = 10;

        public void SetupConsoles()
        {
            dfc = new Console(80, 25);
            SadConsole.Engine.ActiveConsole = dfc;

            //22 instead of 25 so inputRow and statRows fit.
            logConsole = new Console(80, 22);
            //part of the console is offscreen, so we can resize it downwards
            logConsole.Position = new xnaPoint(0, -19);

            inputRowConsole = new Console(80, 1);
            inputRowConsole.Position = new xnaPoint(0, 3);
            inputRowConsole.VirtualCursor.IsVisible = true;

            inventoryConsole = new Console(80, 25);
            inventoryConsole.Position =
                new xnaPoint(
                    dfc.ViewArea.Width - inventoryConsole.ViewArea.Width,
                    0
                );
            inventoryConsole.IsVisible = false;

            statRowConsole = new Console(80, 2);
            statRowConsole.Position = new xnaPoint(0, 23);

            //draw order
            SadConsole.Engine.ConsoleRenderStack.Add(dfc);
            SadConsole.Engine.ConsoleRenderStack.Add(logConsole);
            SadConsole.Engine.ConsoleRenderStack.Add(inputRowConsole);
            SadConsole.Engine.ConsoleRenderStack.Add(statRowConsole);
            SadConsole.Engine.ConsoleRenderStack.Add(inventoryConsole);
        }

        public void SetupBrains() {
            if(Brains == null) Brains = new List<Brain>();
            else Brains.Clear();
            foreach (Actor actor in Level.WorldActors)
                //shouldn't be needed, but
                //what did i even mean with that comment
                if (actor.id == 0) Game.player = actor;
                else Brains.Add(new Brain(actor));
        }

        public void SwitchLevel(Level newLevel, bool gotoStairs = true)
        {
            bool downwards =
                Game.Levels.IndexOf(newLevel) > Game.Levels.IndexOf(Level);
            Level.WorldActors.Remove(player);
            Level = newLevel;
            Level.WorldActors.Add(player);
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
                    {
                        if (
                            (Level.Map[x, y].Stairs == Stairs.Up &&
                                downwards) ||
                            (Level.Map[x, y].Stairs == Stairs.Down &&
                                !downwards)
                        )
                            player.xy = new Point(x, y);
                    }
        }

        void SetupMagic()
        {
            Spell Forcebolt = new Spell(
                "forcebolt",
                new List<Action<Actor, Point>>() {
                    delegate(Actor caster, Point p) {
                        Actor a = Game.Level.ActorOnTile(p);
                        if(a != null) {
                            Util.Game.Log(a.Definition.name +
                                " is hit by the bolt!"
                            );
                            a.Damage(Util.Roll("1d4"));
                        }
                    }
                }, 3, 7, 3
            );

            Spell FieryTouch = new Spell(
                "fiery touch",
                new List<Action<Actor, Point>>() {
                    delegate(Actor caster, Point p) {
                        Actor a = Game.Level.ActorOnTile(p);
                        Util.Game.Log(
                            a.Definition.name +
                            " is burned by " +
                            caster.Definition.name + "'s touch!"
                        );
                        a.Damage(Util.Roll("6d2"));

                        if(Util.Roll("1d6") >= 5) {
                            Game.Log(a.GetName() + " starts bleeding!");
                            TickingEffectDefinition bleed =
                                Util.TEDefByName("bleed");
                            if (!a.HasEffect(bleed))
                                a.TickingEffects.Add(bleed.Instantiate(a));
                        }
                    }
                },
                0, 1, 1
            );
        }

        void SetupTickingEffects()
        {
            TickingEffectDefinition hpReg = new TickingEffectDefinition(
                "passive hp regeneration",
                100, //trigger once every 100 ticks
                delegate(Actor holder)
                {
                    if (holder.hpCurrent < holder.hpMax)
                        holder.hpCurrent++;
                }
            );

            TickingEffectDefinition mpReg = new TickingEffectDefinition(
                "passive mp regeneration",
                100, //trigger once every 100 ticks
                delegate(Actor holder)
                {
                    if (holder.mpCurrent < holder.mpMax)
                        holder.mpCurrent++;
                }
            );

            TickingEffectDefinition bleed = new TickingEffectDefinition(
                "bleed",
                25,
                delegate(Actor holder)
                {
                    if(holder == Game.player)
                        Game.Log("Your wound bleeds!");
                    else
                        Game.Log(
                            Util.Capitalize(
                                holder.GetName(true)+"'s wound bleeds!"
                            )
                        );
                    holder.Damage(Util.Roll("2d3"));
                }
            );
        }

        protected override void Initialize()
        {
            #region engineshit
            //this is starting to look dumb
            PlayerResponses.Game =
            gObject.Game =
            Player.Game =
            Wizard.Game =
            Brain.Game =
            Util.Game =
            IO.Game =
            Game = this;

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            SadConsole.Engine.Initialize(GraphicsDevice);
            SadConsole.Engine.UseMouse = false;
            SadConsole.Engine.UseKeyboard = true;

            using (var stream = System.IO.File.OpenRead("Fonts/IBM.font"))
                SadConsole.Engine.DefaultFont =
                    SadConsole.Serializer.Deserialize<Font>(stream);

            SadConsole.Engine.DefaultFont.ResizeGraphicsDeviceManager(
                graphics, 80, 25, 0, 0
            );
            #endregion

            SetupConsoles();

            camX = camY = 0;
            scrW = 80; scrH = 25;

            TileDefinition floor = new TileDefinition(
                Color.Black, Color.LightGray, ".", false);
            TileDefinition wall = new TileDefinition(
                Color.Gray, Color.Gray, " ", true);

            IO.WriteTileDefinitionsToFile("Data/tiles.def");

            SetupMagic(); //essentially magic defs, but we hardcode magic
            SetupTickingEffects(); //same as magic, cba to add scripting
            IO.ReadActorDefinitionsFromFile("Data/actors.def");
            IO.ReadItemDefinitionsFromFile("Data/items.def");
            ItemDefinition.ApperanceOffset = Util.Roll("1d100");

            //IO.Load(); //load entire game (except definitions atm)
            Game.Levels = new List<ODB.Level>();
            Game.Level = new ODB.Level(80, 25);
            Game.Levels.Add(Game.Level);

            Game.Level.WorldActors.Add(
                player = new Actor(
                    new Point(13, 15),
                    Util.ADefByName("Moribund")
                )
            );

            SetupBrains();

            logSize = 3;
            log = new List<string>();
            Log("Welcome!");

            qpAnswerStack = new Stack<string>();

            //wiz
            Wizard.wmCursor = Game.player.xy;
            Wizard.wmHistory = new List<string>();

            base.Initialize();
        }

        void DrawToScreen(Point xy, Color? bg, Color fg, String tile)
        {
            if (bg != null)
                dfc.CellData.SetBackground(
                    xy.x, xy.y, bg.Value
                );
            else
            {
                Tile bgtile = Game.Level.Map[xy.x, xy.y];
                if(bgtile != null)
                    dfc.CellData.SetBackground(
                        xy.x, xy.y,
                        bgtile.bg
                    );
            }

            dfc.CellData.SetForeground(xy.x, xy.y, fg);

            dfc.CellData.Print(xy.x, xy.y, tile);
        }

        void DrawBorder(Console c, Rect r, Color bg, Color fg)
        {
            for (int x = 0; x < r.wh.x; x++)
            {
                inventoryConsole.CellData.Print(
                    x, 0, (char)205+"", Color.DarkGray
                );
                inventoryConsole.CellData.Print(
                    x, r.wh.y-1, (char)205+"", Color.DarkGray
                );
            }
            for (int y = 0; y < r.wh.y; y++)
            {
                inventoryConsole.CellData.Print(
                    0, y, (char)186+"", Color.DarkGray
                );
                inventoryConsole.CellData.Print(
                    r.wh.x-1, y, (char)186+"", Color.DarkGray
                );
            }
            inventoryConsole.CellData.Print(0, 0, (char)201 + "");
            inventoryConsole.CellData.Print(r.wh.x-1, 0, (char)187 + "");
            inventoryConsole.CellData.Print(0, r.wh.y-1, (char)200 + "");
            inventoryConsole.CellData.Print(r.wh.x-1, r.wh.y-1, (char)188 + "");
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

        protected override void Update(GameTime gameTime)
        {
            SadConsole.Engine.Update(gameTime, this.IsActive);
            IO.Update(false);

            #region ui interaction
            if (IO.KeyPressed(Keys.Escape))
            {
                if (wizMode) {
                    wizMode = false; IO.IOState = InputType.PlayerInput; }
                else if (IO.IOState != InputType.PlayerInput)
                    IO.IOState = InputType.PlayerInput;
                else if (inventoryConsole.IsVisible == true)
                    inventoryConsole.IsVisible = false;
                else this.Exit();
            }

            if (IO.KeyPressed(Keys.I) && !wizMode)
                inventoryConsole.IsVisible =
                    !inventoryConsole.IsVisible;

            if (IO.KeyPressed((Keys)0x6B)) //np+
                logSize = Math.Min(logConsole.ViewArea.Height, ++logSize);
            if (IO.KeyPressed((Keys)0x6D)) //np-
                logSize = Math.Max(0, --logSize);
            logConsole.Position = new xnaPoint(
                0, -logConsole.ViewArea.Height + logSize
            );

            //todo: edge scrolling
            int scrollSpeed = 3;
            if (IO.KeyPressed(Keys.Right)) camX += scrollSpeed;
            if (IO.KeyPressed(Keys.Left)) camX -= scrollSpeed;
            if (IO.KeyPressed(Keys.Up)) camY += scrollSpeed;
            if (IO.KeyPressed(Keys.Down)) camY -= scrollSpeed;

            camX = Math.Max(0, camX);
            camX = Math.Min(Game.Level.LevelSize.x - scrW, camX);
            camY = Math.Max(0, camY);
            camY = Math.Min(Game.Level.LevelSize.y - scrH, camY);
            #endregion camera

            if (wizMode) Wizard.wmInput();
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
                        if (player.Cooldown == 0)
                            Player.PlayerInput();
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
                if (wizMode)
                {
                    IO.Answer = "";
                    IO.IOState = InputType.PlayerInput;
                }
                wizMode = !wizMode;
            }
            #endregion

            RenderConsoles();
            IO.Update(true);
            base.Update(gameTime);
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
                log.Add(ss);
        }

        public void ProcessNPCs()
        {
            while(Game.player.Cooldown > 0)
            {
                foreach (Brain b in Brains)
                    if (
                        Game.Level.WorldActors.Contains(
                            b.MeatPuppet
                        ) && b.MeatPuppet.Cooldown == 0
                    )
                        b.Tick();

                foreach (Brain b in Brains)
                    b.MeatPuppet.Cooldown--;
                Game.player.Cooldown--;

                GameTick++;
                List<Actor> wa = new List<Actor>(Level.WorldActors);
                foreach (Actor a in wa)
                {
                    foreach (TickingEffect effect in a.TickingEffects)
                        effect.Tick();
                    a.TickingEffects.RemoveAll(x => x.Die);
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

            #region reticule
            if (IO.IOState == InputType.Targeting)
            {
                Cell cs = dfc.CellData[Target.x, Target.y];

                bool blink = (DateTime.Now.Millisecond % 500 > 250);

                cs.Background = blink ?
                    Util.InvertColor(cs.Background) : Color.White;

                cs.Foreground = blink ?
                    Util.InvertColor(cs.Foreground) : Color.White;
            }
            if (wizMode)
            {
                Cell cs = dfc.CellData[
                    Wizard.wmCursor.x, Wizard.wmCursor.y];

                bool blink = (DateTime.Now.Millisecond % 500 > 250);

                cs.Background = blink ?
                    Util.InvertColor(cs.Background) : Color.White;

                cs.Foreground = blink ?
                    Util.InvertColor(cs.Foreground) : Color.White;
            }
            #endregion
        }

        public void RenderMap()
        {
            dfc.CellData.Clear();

            for (int x = 0; x < scrW; x++) for (int y = 0; y < scrH; y++)
            {
                Tile t = Game.Level.Map[x + camX, y + camY];

                if (t == null) continue;
                if (
                    !(Game.Level.Seen[x + camX, y + camY] || wizMode)
                ) continue;

                bool inVision =
                    Game.player.Vision[x + camX, y + camY] || wizMode;

                string tileToDraw = t.tile;
                //doors override the normal tile
                //which shouldn't be a problem
                //if it is a problem, it's not, it's something else
                if (t.Door == Door.Closed) tileToDraw = "+";
                if (t.Door == Door.Open) tileToDraw = "/";
                if (t.Stairs == Stairs.Down) tileToDraw = ">";
                if (t.Stairs == Stairs.Up) tileToDraw = "<";

                DrawToScreen(
                    new Point(x, y),
                    t.bg,
                    t.fg * (inVision ? 1f : 0.6f),
                    tileToDraw
                );
            }
        }

        public void RenderItems()
        {
            Rect screen = new Rect(new Point(camX, camY), new Point(80, 25));

            int[,] itemCount = new int[
                Game.Level.LevelSize.x,
                Game.Level.LevelSize.y
            ];

            foreach (Item i in Game.Level.WorldItems)
                itemCount[i.xy.x, i.xy.y]++;
            foreach (Item i in Game.Level.WorldItems)
            {
                if (
                    !(Game.player.Vision[i.xy.x, i.xy.y] || wizMode)
                ) continue;
                if (!screen.ContainsPoint(i.xy)) continue;

                if (itemCount[i.xy.x, i.xy.y] == 1)
                    DrawToScreen(
                        i.xy,
                        i.Identified ? i.Definition.bg : null,
                        i.Identified ? i.Definition.fg : Color.Gray,
                        i.Definition.tile
                    );
                //not sure I like the + for pile, since doors are +
                else DrawToScreen(i.xy, null, Color.White, "+");
            }
        }

        public void RenderActors()
        {
            Rect screen = new Rect(new Point(camX, camY), new Point(80, 25));

            int[,] actorCount = new int[
                Game.Level.LevelSize.x,
                Game.Level.LevelSize.y
            ];

            foreach (Actor a in Game.Level.WorldActors)
                actorCount[a.xy.x, a.xy.y]++;

            foreach (Actor a in Game.Level.WorldActors)
            {
                if (
                    !(Game.player.Vision[a.xy.x, a.xy.y] || wizMode)
                ) continue;
                if (!screen.ContainsPoint(a.xy)) continue;

                if (actorCount[a.xy.x, a.xy.y] == 1)
                    DrawToScreen(
                        a.xy, a.Definition.bg,
                        a.Definition.fg, a.Definition.tile
                    );
                //draw a "pile" (shouldn't happen at all atm
                else DrawToScreen(a.xy, null, Color.White, "*");
            }
        }

        public void RenderLog()
        {
            logConsole.CellData.Clear();
            logConsole.CellData.Fill(Color.White, Color.Black, ' ', null);
            for (
                int i = log.Count, n = 0;
                i > 0 && n < logConsole.ViewArea.Height;
                i--, n++
            ) {
                logConsole.CellData.Print(
                    0, logConsole.ViewArea.Height - (n + 1),
                    log[i - 1]
                );
            }
        }

        public void RenderPrompt()
        {
            inputRowConsole.IsVisible =
                (IO.IOState != InputType.PlayerInput) || wizMode;
            if (inputRowConsole.IsVisible)
            {
                inputRowConsole.Position = new xnaPoint(0, logSize);
                inputRowConsole.CellData.Fill(
                    Color.Black,
                    Color.WhiteSmoke,
                    ' ',
                    null
                );
                inputRowConsole.CellData.Print(
                    0, 0,
                    (wizMode ? "" : IO.Question + " ") + IO.Answer
                );
            }
        }

        public void RenderInventory()
        {
            int inventoryW = inventoryConsole.ViewArea.Width;
            int inventoryH = inventoryConsole.ViewArea.Height;

            inventoryConsole.CellData.Clear();
            inventoryConsole.CellData.Fill(Color.White, Color.Black, ' ', null);

            DrawBorder(
                inventoryConsole,
                new Rect(
                    new Point(0, 0),
                    new Point(inventoryW, inventoryH)),
                Color.Black,
                Color.DarkGray
            );

            for (int i = 0; i < player.Definition.name.Length; i++)
            {
                inventoryConsole.CellData.Print(
                    2, 0, player.Definition.name, Color.White);
            }

            for (int i = 0; i < player.inventory.Count; i++)
            {
                Item it = player.inventory[i];

                bool equipped = Game.player.IsEquipped(it);

                string name = "" + ((char)(97 + i));
                name += " - ";


                if (it.Definition.stacking)
                    name += it.count + "x ";

                if (player.inventory[i].mod != 0)
                {
                    name += player.inventory[i].mod >= 0 ? "+" : "-";
                    name += Math.Abs(player.inventory[i].mod) + " ";
                }

                //name += player.inventory[i].Definition.name;
                name += player.inventory[i].GetName(false, true, true);
                if (it.Charged)
                {
                    name += "["+it.count+"]";
                }
                if (equipped) name += " (eq)";

                inventoryConsole.CellData.Print(
                    2, i + 1, name
                );
            }
        }

        public void RenderStatus()
        {
            statRowConsole.CellData.Clear();
            statRowConsole.CellData.Fill(Color.White, Color.Black, ' ', null);

            if (wizMode)
            {
                string str = "W I Z A R D";
                str += " (" + String.Format("{0:X2}", Wizard.wmCursor.x);
                str += " ; ";
                str += String.Format("{0:X2}", Wizard.wmCursor.y) + ")";
                str += " (" + Wizard.wmCursor.x + " ; " +
                    Wizard.wmCursor.y + ")";
                statRowConsole.CellData.Print(
                    0, 0, str 
                );
                return;
            }

            //string namerow = player.Definition.name + " - Title";
            string namerow = player.Definition.name;
            namerow += " ";
            namerow += "STR " + player.Get(Stat.Strength) + "  ";
            namerow += "DEX " + player.Get(Stat.Dexterity) + "  ";
            namerow += "INT " + player.Get(Stat.Intelligence) + "  ";
            namerow += "AC " + player.GetAC();

            string statrow = "";
            statrow += "[";
            statrow += player.hpCurrent.ToString().PadLeft(3, ' ');
            statrow += "/";
            statrow += player.Definition.hpMax.ToString().PadLeft(3, ' ');
            statrow += "]";

            statrow += " ";

            statrow += "[";
            statrow += player.mpCurrent.ToString().PadLeft(3, ' ');
            statrow += "/";
            statrow += player.Definition.mpMax.ToString().PadLeft(3, ' ');
            statrow += "]";

            statrow += " ";
            statrow += "T:";
            statrow += string.Format("{0:F1}", (float)(GameTick / 10f));

            statrow += " ";
            statrow += "D:" + (Game.Levels.IndexOf(Game.Level) + 1) + "0M";

            float playerHealthPcnt =
                player.hpCurrent /
                (float)player.hpMax
            ;
            float playerManaPcnt =
                player.mpCurrent /
                (float)player.mpMax
            ;
            float colorStrength = 0.6f + 0.4f - (0.4f * playerHealthPcnt);
            float manaColorStrength = 0.6f + 0.4f - (0.4f * playerManaPcnt);

            for (int x = 0; x < 9; x++)
                statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        colorStrength - colorStrength * playerHealthPcnt,
                        colorStrength * playerHealthPcnt,
                        0
                    )
                );

            for (int x = 10; x < 19; x++)
                statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        manaColorStrength - manaColorStrength * playerManaPcnt,
                        manaColorStrength * playerManaPcnt / 2,
                        manaColorStrength * playerManaPcnt
                    )
                );

            statRowConsole.CellData.Print(0, 0, namerow);
            statRowConsole.CellData.Print(0, 1, statrow);
        }

        #region engineshit
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            SadConsole.Engine.Draw(gameTime);

            base.Draw(gameTime);
        }
        #endregion

    }
}