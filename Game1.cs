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

//~~~ QUEST TRACKER for 26 nov ~~~
// * Spell cast cost
// * Mana and hp/mp regeneration
// * Brains respecting other NPC-actors

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

        public List<Level> Levels;
        public Level Level;
        public List<Brain> Brains;

        public Actor player;

        int camX, camY;
        int scrW, scrH;

        int logSize;
        public List<string> log;

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

        void SwitchLevel(Level newLevel)
        {
            Level.WorldActors.Remove(player);
            Level = newLevel;
            Level.WorldActors.Add(player);
            foreach (Actor a in Level.WorldActors)
                //reset vision, incase the level we moved to is a different size
                a.Vision = null;
            SetupBrains();
        }

        void SetupMagic()
        {
            Spell Forcebolt = new Spell(
                "forcebolt",
                new List<Action<Actor, Point>>() {
                    delegate(Actor caster, Point p) {
                        Actor a = Game.Level.ActorOnTile(p);
                        if(a != null) {
                            Util.Game.log.Add(a.Definition.name +
                                " is hit by the bolt!"
                            );
                            a.Damage(Util.Roll("1d4"));
                        }
                    }
                },
                7, 3
            );

            Spell FieryTouch = new Spell(
                "Fiery touch",
                new List<Action<Actor, Point>>() {
                    delegate(Actor caster, Point p) {
                        Actor a = Game.Level.ActorOnTile(p);
                        Util.Game.log.Add(
                            a.Definition.name +
                            " is burned by " +
                            caster.Definition.name + "'s touch!"
                        );
                        a.Damage(Util.Roll("6d2"));
                    }
                },
                0, 1
            );
        }

        protected override void Initialize()
        {
            #region engineshit
            //this is starting to look dumb
            PlayerResponses.Game =
            gObject.Game =
            Player.Game =
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

            SetupMagic(); //essentially magic defs, but we hardcode magic
            IO.ReadActorDefinitionsFromFile("Data/actors.def");
            IO.ReadItemDefinitionsFromFile("Data/items.def");

            IO.Load(); //load entire game (except definitions atm)
            SetupBrains();

            Level.Spawn(
                new Item(
                    new Point(12, 11),
                    new ItemDefinition(
                        null, Color.Pink, "[", "Apron", "", 2,
                        false, new List<DollSlot>() {
                            DollSlot.Torso
                        }
                    )
                )
            );

            logSize = 3;
            log = new List<string>();
            log.Add("Welcome!");

            qpAnswerStack = new Stack<string>();

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
                dfc.CellData.SetBackground(
                    xy.x - camX, xy.y - camY,
                    bgtile.bg
                );
            }

            dfc.CellData.SetForeground(xy.x - camX, xy.y - camY, fg);

            dfc.CellData.Print(xy.x - camX, xy.y - camY, tile);
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
                if (IO.IOState != InputType.PlayerInput)
                    IO.IOState = InputType.PlayerInput;
                else if (inventoryConsole.IsVisible == true)
                    inventoryConsole.IsVisible = false;
                else this.Exit();
            }

            if (IO.KeyPressed(Keys.I))
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

            #region f-keys //devstuff
            if (IO.KeyPressed(Keys.F1))
            {
                IO.WriteActorDefinitionsToFile("Data/actors.def");
                IO.WriteItemDefinitionsToFile("Data/items.def");
            }

            if (IO.KeyPressed(Keys.F5)) IO.Save();
            if (IO.KeyPressed(Keys.F6)) IO.Load();
            #endregion

            if (IO.KeyPressed(Keys.OemPeriod) && IO.shift)
                if (Level.Map[
                    Game.player.xy.x,
                    Game.player.xy.y].stairs == Stairs.Down)
                {
                    int depth = Levels.IndexOf(Level);
                    if (depth + 1 <= Levels.Count - 1)
                    {
                        SwitchLevel(Levels[depth + 1]);
                        Game.log.Add("You descend the stairs...");
                    }
                }

            if (IO.KeyPressed(Keys.OemComma) && IO.shift)
                if (Level.Map[
                    Game.player.xy.x,
                    Game.player.xy.y].stairs == Stairs.Up)
                {
                    int depth = Levels.IndexOf(Level);
                    if (depth - 1 >= 0)
                    {
                        SwitchLevel(Levels[depth - 1]);
                        Game.log.Add("You ascend the stairs...");
                    }
                }

            //only do player movement if we're not currently asking something
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
                    else ProcessNPCs();
                    break;
                //if this happens,
                //you're breaking some kind of weird shit somehow
                default: throw new Exception("");
            }

            foreach (Actor a in Game.Level.WorldActors)
            {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }

            RenderConsoles();
            IO.Update(true);
            base.Update(gameTime);
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
                dfc.CellData[Target.x, Target.y].Background =
                Util.InvertColor(
                    dfc.CellData[Target.x, Target.y].Background
                );
                dfc.CellData[Target.x, Target.y].Foreground =
                Util.InvertColor(
                    dfc.CellData[Target.x, Target.y].Foreground
                );
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
                if (!Game.Level.Seen[x + camX, y + camY]) continue;

                bool inVision = Game.player.Vision[x + camX, y + camY];

                string tileToDraw = t.tile;
                //doors override the normal tile
                //which shouldn't be a problem
                //if it is a problem, it's not, it's something else
                if (t.door == Door.Closed) tileToDraw = "+";
                if (t.door == Door.Open) tileToDraw = "/";
                if (t.stairs == Stairs.Down) tileToDraw = ">";
                if (t.stairs == Stairs.Up) tileToDraw = "<";

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
                if (!Game.player.Vision[i.xy.x, i.xy.y]) continue;
                if (!screen.ContainsPoint(i.xy)) continue;

                if (itemCount[i.xy.x, i.xy.y] == 1)
                    DrawToScreen(
                        i.xy,
                        i.Definition.bg,
                        i.Definition.fg,
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
                if (!Game.player.Vision[a.xy.x, a.xy.y]) continue;
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
            )
                logConsole.CellData.Print(
                    0, logConsole.ViewArea.Height - (n + 1),
                    log[i - 1]
                );
        }

        public void RenderPrompt()
        {
            inputRowConsole.IsVisible = IO.IOState != InputType.PlayerInput;
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
                    0, 0, IO.Question + " " + IO.Answer
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

            string namerow = player.Definition.name + " - Title";
            namerow += "  ";
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

            float playerHealthPcnt =
                player.hpCurrent /
                (float)player.Definition.hpMax
            ;
            float colorStrength = 0.6f + 0.4f - (0.4f * playerHealthPcnt);

            for (int x = 0; x < 9; x++)
                statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        colorStrength - colorStrength * playerHealthPcnt,
                        colorStrength * playerHealthPcnt,
                        0
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