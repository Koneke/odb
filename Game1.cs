using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using SadConsole;
using SadConsole.Consoles;
using Console = SadConsole.Consoles.Console;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using xnaPoint = Microsoft.Xna.Framework.Point;

//~~~ QUEST TRACKER for 23 nov ~~~
// * Inventory textwrapping //postponed
// * Item value and paid-for status

//~~~ QUEST TRACKER for 24 nov ~~~
// * Extract drawy bits from Update [x]

namespace ODB
{

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        #region engineshit

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Console dfc;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        Game1 Game;
        #endregion

        public Tile[,] map;
        public bool[,] seen;
        public List<Room> rooms;

        public List<Actor> worldActors; //in world (levelfloor)
        public List<Item> worldItems; //in world (levelfloor)
        public List<Item> allItems; //in level
        public List<Brain> Brains;

        //semi-temp?
        //should probably start subclassing gObj later
        public List<Projectile> projectiles;

        public Actor player;

        int camX, camY;
        int scrW, scrH;
        public int lvlW, lvlH;

        int logSize;
        Console logConsole;
        public List<string> log;

        Console inputRowConsole;

        public Point target;
        //make these two Action, and merge into one?
        //the targetting reaction functions can just read the
        //Game.target directly
        //and the others can use the stack
        //perhaps make a stack for the targets as well later,
        //but doesn't seem necessary atm
        public Action<Point> targetingReaction;
        public Action<string> questionReaction;
        public Stack<string> qpAnswerStack;

        public List<int> letters, numbers, directions;
        int space;

        Console inventoryConsole;

        List<DollSlot> standardHuman;

        Console statRowConsole;

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

            inventoryConsole = new Console(40, 25);
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

        protected override void Initialize()
        {
            #region engineshit
            //this is starting to look dumb
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
            scrW = 80;
            scrH = 25;

            lvlW = 160;
            lvlH = 25;

            map = new Tile[lvlW, lvlH];
            seen = new bool[lvlW, lvlH];

            worldActors = new List<Actor>();
            worldItems = new List<Item>();
            allItems = new List<Item>();
            Brains = new List<Brain>();

            logSize = 3;
            log = new List<string>();
            log.Add("Welcome!");

            standardHuman = new List<DollSlot>();
            standardHuman.Add(DollSlot.Head);
            standardHuman.Add(DollSlot.Torso);
            standardHuman.Add(DollSlot.Hand);
            standardHuman.Add(DollSlot.Hand);
            standardHuman.Add(DollSlot.Legs);
            standardHuman.Add(DollSlot.Feet);

            qpAnswerStack = new Stack<string>();

            letters = new List<int>();
            numbers = new List<int>();
            for (int i = 65; i <= 90; i++) letters.Add(i);
            for (int i = 48; i <= 57; i++) numbers.Add(i);
            directions = new List<int>{
                (int)Keys.NumPad7, (int)Keys.NumPad8, (int)Keys.NumPad9,
                (int)Keys.NumPad4,                    (int)Keys.NumPad6,
                (int)Keys.NumPad1, (int)Keys.NumPad2, (int)Keys.NumPad3,
            };
            space = 32;

            rooms = new List<Room>();

            IO.ReadActorDefinitionsFromFile("Data/actors.def");
            IO.ReadItemDefinitionsFromFile("Data/items.def");

            IO.ReadLevelFromFile("Save/level.sv");
            IO.ReadRoomsFromFile("Save/rooms.sv");
            IO.ReadAllItemsFromFile("Save/items.sv");
            IO.ReadAllActorsFromFile("Save/actors.sv");
            IO.ReadSeenFromFile("Save/seen.sv");
            player = Util.GetActorByID(0);

            projectiles = new List<Projectile>();

            Spell ForceBolt = new Spell(
                "Force bolt",
                new List<Action<Point>>()
                {
                    delegate(Point p) {
                        foreach (Actor a in Util.ActorsOnTile(p))
                        {
                            Util.Game.log.Add(a.Definition.name +
                                " is hit by the bolt!"
                            );
                            a.Damage(Util.Roll("1d4"));
                        }
                    }
                },
                7, 3
            );

            player.Spellbook.Add(ForceBolt);

            for (int x = 0; x < lvlW; x++)
                for (int y = 0; y < lvlH; y++)
                    Game.seen[x, y] = false;
            //currently, let's just overwrite it, while testing stuff
            IO.WriteSeenToFile("Save/seen.sv");

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
                Tile bgtile = map[xy.x, xy.y];
                dfc.CellData.SetBackground(
                    xy.x - camX, xy.y - camY,
                    bgtile.bg
                );
            }

            dfc.CellData.SetForeground(
                xy.x - camX, xy.y - camY,
                fg
            );

            dfc.CellData.Print(
                xy.x - camX, xy.y - camY,
                tile
            );
        }

        void DrawBorder(Console c, Rect r, Color bg, Color fg)
        {
            for (int x = 0; x < r.wh.x; x++)
            {
                inventoryConsole.CellData.Print(
                    x, 0, (char)205+"", Color.DarkGray);
                inventoryConsole.CellData.Print(
                    x, r.wh.y-1, (char)205+"", Color.DarkGray);
            }
            for (int y = 0; y < r.wh.y; y++)
            {
                inventoryConsole.CellData.Print(
                    0, y, (char)186+"", Color.DarkGray);
                inventoryConsole.CellData.Print(
                    r.wh.x-1, y, (char)186+"", Color.DarkGray);
            }
            inventoryConsole.CellData.Print(
                0, 0, (char)201 + ""
            );
            inventoryConsole.CellData.Print(
                r.wh.x-1, 0, (char)187 + ""
            );
            inventoryConsole.CellData.Print(
                0, r.wh.y-1, (char)200 + ""
            );
            inventoryConsole.CellData.Print(
                r.wh.x-1, r.wh.y-1, (char)188 + ""
            );
        }

        public Point NumpadToDirection(char c)
        {
            Point p;
            switch (c)
            {
                case (char)Keys.D7:
                    p = new Point(-1, -1);
                    break;
                case (char)Keys.D8:
                    p = new Point(0, -1);
                    break;
                case (char)Keys.D9:
                    p = new Point(1, -1);
                    break;
                case (char)Keys.D4:
                    p = new Point(-1, 0);
                    break;
                case (char)Keys.D6:
                    p = new Point(1, 0);
                    break;
                case (char)Keys.D1:
                    p = new Point(-1, 1);
                    break;
                case (char)Keys.D2:
                    p = new Point(0, 1);
                    break;
                case (char)Keys.D3:
                    p = new Point(1, 1);
                    break;
                default:
                    throw new Exception(
                        "Bad input (expected numpad keycode," +
                        " got something weird instead).");
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
                else
                    this.Exit();
            }
            if (IO.KeyPressed(Keys.Q)) this.Exit();

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
            int scrollSpeed = 3; ;
            if (IO.KeyPressed(Keys.Right))
                camX+=scrollSpeed;
            if(IO.KeyPressed(Keys.Left))
                camX-=scrollSpeed;
            if (IO.KeyPressed(Keys.Up))
                camY+=scrollSpeed;
            if(IO.KeyPressed(Keys.Down))
                camY-=scrollSpeed;

            camX = Math.Max(0, camX);
            camX = Math.Min(lvlW - scrW, camX);
            camY = Math.Max(0, camY);
            camY = Math.Min(lvlH - scrH, camY);
            #endregion camera

            #region f-keys //devstuff
            //temp disable so we don't botch things
            /*if (KeyPressed(Keys.F1))
            {
                IO.WriteLevelToFile("Save/level.sv");
                IO.WriteRoomsToFile("Save/rooms.sv");
                IO.WriteAllItemsToFile("Save/items.sv");
                IO.WriteAllActorsToFile("Save/actors.sv");
                IO.WriteSeenToFile("Save/seen.sv");
            }*/

            if (IO.KeyPressed(Keys.F2))
            {
                IO.ReadLevelFromFile("Save/level.sv");
                IO.ReadRoomsFromFile("Save/rooms.sv");
                IO.ReadAllItemsFromFile("Save/items.sv");
                IO.ReadAllActorsFromFile("Save/actors.sv");
                IO.ReadSeenFromFile("Save/seen.sv");
                player = Util.GetActorByID(0);
            }
            #endregion

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
                    if(player.Cooldown == 0)
                        Player.PlayerInput();
                    else
                        while(Game.player.Cooldown > 0)
                        {
                            projectiles.RemoveAll(x => x.Die);

                            foreach (Brain b in Brains)
                                if (
                                    Game.worldActors.Contains(b.MeatPuppet) &&
                                    b.MeatPuppet.Cooldown == 0
                                )
                                    b.Tick();

                            foreach (Brain b in Brains) b.MeatPuppet.Cooldown--;
                            Game.player.Cooldown--;
                        }
                    break;
                //if this happens,
                //you're breaking some kind of weird shit somehow
                default: throw new Exception("");
            }

            foreach (Actor a in Game.worldActors)
            {
                a.ResetVision();
                foreach (Room r in Util.GetRooms(a))
                    a.AddRoomToVision(r);
            }

            RenderConsoles();
            IO.Update(true);
            base.Update(gameTime);
        }

        public void RenderConsoles()
        {
            Rect screen = new Rect(new Point(camX, camY), new Point(80, 25));

            #region world
            #region map to screen
            dfc.CellData.Clear();

            for (int x = 0; x < scrW; x++) for (int y = 0; y < scrH; y++)
            {
                Tile t = map[x + camX, y + camY];

                if (t == null) continue;
                if (!seen[x + camX, y + camY]) continue;

                bool inVision = Game.player.Vision[x + camX, y + camY];

                dfc.CellData.SetBackground(x, y, t.bg * 1f);
                dfc.CellData.SetForeground(
                    x, y, t.fg * (inVision ? 1f : 0.6f)
                );

                string tileToDraw = t.tile;
                //doors override the normal tile
                //which shouldn't be a problem
                //if it is a problem, it's not, it's something else
                if (t.doorState == Door.Closed) tileToDraw = "+";
                if (t.doorState == Door.Open) tileToDraw = "/";

                dfc.CellData.Print(x, y, tileToDraw);
            }
            #endregion

            #region render items to screen
            int[,] itemCount = new int[lvlW, lvlH];

            foreach (Item i in worldItems) itemCount[i.xy.x, i.xy.y]++;
            foreach (Item i in worldItems)
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
            #endregion

            #region render actors to screen
            int[,] actorCount = new int[lvlW, lvlH];

            foreach (Actor a in worldActors) actorCount[a.xy.x, a.xy.y]++;
            foreach (Actor a in worldActors)
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
            #endregion

            #region projectiles to screen
            //visible for ~half a sec, if that, but we'll keep it until we
            //(possibly) add a trail
            foreach (Projectile p in projectiles)
                dfc.CellData.Print(
                    p.xy.x, p.xy.y,
                    "*", Color.Cyan
                );
            #endregion
            #endregion

            #region render ui
            #region log
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
            #endregion

            #region inputrow
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
            #endregion

            #region inventory
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
                bool equipped = Game.player.IsEquipped(player.inventory[i]);

                string name = "" + ((char)(97 + i));
                name += " - ";
                if (player.inventory[i].Definition.stacking)
                    name += player.inventory[i].count + "x ";

                if (player.inventory[i].mod != 0)
                {
                    name += player.inventory[i].mod >= 0 ? "+" : "-";
                    name += Math.Abs(player.inventory[i].mod) + " ";
                }

                name += player.inventory[i].Definition.name;
                if (equipped) name += " (eq)";

                inventoryConsole.CellData.Print(
                    2, i + 1, name
                );
            }
            #endregion

            #region statrow
            statRowConsole.CellData.Clear();
            statRowConsole.CellData.Fill(Color.White, Color.Black, ' ', null);

            string namerow = player.Definition.name + " - Title";
            namerow += "  ";
            namerow += "STR " + player.GetStrength() + "  ";
            namerow += "DEX " + player.GetDexterity() + "  ";
            namerow += "INT " + player.GetIntelligence() + "  ";
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
            #endregion

            #region reticule
            if (IO.IOState == InputType.Targeting)
            {
                dfc.CellData[target.x, target.y].Background =
                Util.InvertColor(
                    dfc.CellData[target.x, target.y].Background
                );
                dfc.CellData[target.x, target.y].Foreground =
                Util.InvertColor(
                    dfc.CellData[target.x, target.y].Foreground
                );
            }
            #endregion
            #endregion
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