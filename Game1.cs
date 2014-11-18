using System;
using SadConsole;
using System.Linq;
using SadConsole.Consoles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Console = SadConsole.Consoles.Console;
using xnaPoint = Microsoft.Xna.Framework.Point;

namespace ODB
{
    #region structure
    class Tile
    {
        public Color bg, fg;
        public string tile;

        public Tile(Color bg, Color fg, string tile)
        {
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
        }
    }

    struct Point
    {
        public int x, y;
        
        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void Nudge(int x, int y)
        {
            this.x += x;
            this.y += y;
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }
    }

    struct Rect
    {
        public Point xy, wh;

        public Rect(Point xy, Point wh)
        {
            this.xy = xy;
            this.wh = wh;
        }

        public bool ContainsPoint(Point p)
        {
            return
                p.x >= xy.x &&
                p.y >= xy.y &&
                p.x < xy.x + wh.x &&
                p.y < xy.y + wh.y;
        }
    }

    class Room
    {
        public List<Rect> rects;

        public Room()
        {
            rects = new List<Rect>();
        }

        public bool ContainsPoint(Point p)
        {
            foreach (Rect r in rects)
            {
                if (r.ContainsPoint(p)) return true;
            }
            return false;
        }
    }

    class gObject
    {
        public Point xy;
        public Color? bg;
        public Color fg;
        public string tile;
        public string name;

        public gObject(
            Point xy, Color? bg, Color fg, string tile, string name
        ) {
            this.xy = xy;
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
            this.name = name;
        }
    }

    class Actor : gObject
    {
        public List<Item> inventory;

        public Actor(
            Point xy, Color? bg, Color fg, string tile, string name
        ) :
            base(xy, bg, fg, tile, name)
        {
            inventory = new List<Item>();
        }
    }

    class Item : gObject
    {
        int count;

        public Item(
            Point xy, Color? bg, Color fg, string tile, string name
        ) :
            base(xy, bg, fg, tile, name)
        {
            count = 1;
        }
    }
    #endregion

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
        #endregion

        KeyboardState ks, oks;

        Tile[,] map;
        bool[,] seen;
        bool[,] vision;
        List<Room> rooms;

        List<Actor> actors;
        List<Item> items;

        int camX, camY;
        int lvlW, lvlH;
        int scrW, scrH;

        Actor player;

        Console logConsole;
        List<string> log;

        Console inputRow;
        bool awaitingMenuInput;

        protected override void Initialize()
        {
            #region engineshit
            IsMouseVisible = true;
            IsFixedTimeStep = false;

            SadConsole.Engine.Initialize(GraphicsDevice);
            SadConsole.Engine.UseMouse = false;

            using (var stream = System.IO.File.OpenRead("Fonts/IBM.font"))
                SadConsole.Engine.DefaultFont =
                    SadConsole.Serializer.Deserialize<Font>(stream);

            SadConsole.Engine.DefaultFont.ResizeGraphicsDeviceManager(
                graphics, 80, 25, 0, 0);

            dfc = new Console(80, 25);

            SadConsole.Engine.ConsoleRenderStack.Add(dfc);
            SadConsole.Engine.ActiveConsole = dfc;
            #endregion

            camX = camY = 0;
            scrW = 80;
            scrH = 25;

            lvlW = 160;
            lvlH = 25;
            map = new Tile[lvlW, lvlH];
            seen = new bool[lvlW, lvlH];
            vision = new bool[lvlW, lvlH]; //playervision

            actors = new List<Actor>();
            items = new List<Item>();

            log = new List<string>();
            log.Add("Something something dungeon");

            logConsole = new Console(80, 3);
            SadConsole.Engine.ConsoleRenderStack.Add(logConsole);

            inputRow = new Console(80, 1);
            inputRow.Position = new Microsoft.Xna.Framework.Point(0, 3);
            SadConsole.Engine.ConsoleRenderStack.Add(inputRow);

            #region dev dungeon
            rooms = new List<Room>();
            Room r;

            r = new Room();
            r.rects.Add(new Rect(new Point(10, 10), new Point(5, 7)));
            rooms.Add(r);

            r = new Room();
            r.rects.Add(new Rect(new Point(20, 10), new Point(5, 7)));
            rooms.Add(r);

            r = new Room();
            r.rects.Add(new Rect(new Point(14, 13), new Point(7, 1)));
            rooms.Add(r);
            #endregion

            #region dev actors
            actors.Add(
                player = new Actor(
                    new Point(12, 15), null, Color.Cyan, (char)1+"", "Moribund"
                )
            );

            actors.Add(
                new Actor(
                    new Point(12, 13), null, Color.Red, "&", "Demigorgon"
                )
            );
            #endregion

            #region dev items
            items.Add(
                new Item(
                    new Point(13, 13), null, Color.Green, ")", "Longsword"
                )
            );

            items.Add(
                new Item(
                    new Point(13, 13), null, Color.Green, ")", "Snickersnee"
                )
            );

            items.Add(
                new Item(
                    new Point(13, 12), null, Color.Green, ")", "Vorpal Blade"
                )
            );
            #endregion

            #region render rooms to map
            int[,] overlapCount = new int[lvlW, lvlH];
            foreach (Room q in rooms)
            {
                foreach (Rect qq in q.rects)
                {
                    for (int x = 0; x < qq.wh.x; x++)
                    {
                        for (int y = 0; y < qq.wh.y; y++)
                        {
                            map[qq.xy.x + x, qq.xy.y + y] = new Tile(
                                Color.Blue, Color.Red, "."
                            );
                            overlapCount[qq.xy.x + x, qq.xy.y + y]++;
                        }
                    }
                }
            }

            //generate walls
            foreach (Room q in rooms)
            {
                foreach (Rect qq in q.rects)
                {
                    //dont generate walls for corridors, it'll get annoying
                    if (!(qq.wh.x >= 3 && qq.wh.y >= 3)) continue;

                    for (int x = 0; x < qq.wh.x; x++)
                    {
                        for (int y = 0; y < qq.wh.y; y++)
                        {
                            bool wall = false;
                            for (int xx = -1; xx <= 1; xx++)
                            {
                                for (int yy = -1; yy <= 1; yy++)
                                {
                                    if (
                                        x < 0 || y < 0 ||
                                        x > lvlW || y > lvlH)
                                    {
                                        wall = true;
                                    }
                                    else
                                    {
                                        if (map[
                                            qq.xy.x + x + xx,
                                            qq.xy.y + y + yy
                                        ] == null) wall = true;
                                    }
                                }
                            }
                            if (wall)
                            {
                                if(overlapCount[qq.xy.x+x, qq.xy.y+y] <= 1)
                                    map[qq.xy.x + x, qq.xy.y + y] =
                                        new Tile(Color.Gray, Color.Gray, " ");
                            }
                        }
                    }
                }
            }
#endregion

            base.Initialize();
        }

        List<Room> GetRoom(gObject go)
        {
            List<Room> roomList = new List<Room>();
            foreach (Room r in rooms)
                if (r.ContainsPoint(go.xy)) roomList.Add(r);
            return roomList;
        }

        List<Item> ItemsOnTile(Point xy)
        {
            return items.FindAll(x => x.xy == xy);
        }

        bool KeyPressed(Keys k)
        {
            return ks.IsKeyDown(k) && !oks.IsKeyDown(k);
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

        protected override void Update(GameTime gameTime)
        {
            #region engineshit
            SadConsole.Engine.Update(gameTime, this.IsActive);
            ks = Keyboard.GetState();
            dfc.CellData.Clear();
            #endregion

            if (KeyPressed(Keys.Q) || KeyPressed(Keys.Escape)) this.Exit();

            #region camera
            if (KeyPressed(Keys.A))
                camX+=10;
            if(KeyPressed(Keys.F))
                camX-=10;

            camX = Math.Max(0, camX);
            camX = Math.Min(lvlW - scrW, camX);
            camY = Math.Max(0, camY);
            camY = Math.Min(lvlH - scrH, camY);
            #endregion camera

            #region player movement
            Point offset = new Point(0, 0);

            if (KeyPressed(Keys.NumPad8)) offset.Nudge( 0,-1);
            if (KeyPressed(Keys.NumPad9)) offset.Nudge( 1,-1);
            if (KeyPressed(Keys.NumPad6)) offset.Nudge( 1, 0);
            if (KeyPressed(Keys.NumPad3)) offset.Nudge( 1, 1);
            if (KeyPressed(Keys.NumPad2)) offset.Nudge( 0, 1);
            if (KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
            if (KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
            if (KeyPressed(Keys.NumPad7)) offset.Nudge(-1,-1);

            /*if (KeyPressed(Keys.G))
            {
                inputRow.CellData.Print(0, 0, "Get what? ");
                inputRow.VirtualCursor.Position =
                    new xnaPoint("Get what?".Length+1, 0);
                awaitingMenuInput = true;
            }*/

            player.xy.Nudge(offset.x, offset.y);

            if (offset.x != 0 || offset.y != 0) {
                //log.Add(player.xy.x + ", " + player.xy.y);
                List<Item> itemsOnSquare = ItemsOnTile(player.xy);
                switch (itemsOnSquare.Count)
                {
                    case 0:
                        break;
                    case 1:
                        log.Add(
                            //TODO: a/an
                            "There is a(n) " + itemsOnSquare[0].name + " here."
                        );
                        break;
                    default:
                        log.Add(
                            "There are " + itemsOnSquare.Count + " items here."
                        );
                        break;
                }
            }
            #endregion

            #region vision
            for (int x = 0; x < lvlW; x++)
            {
                for (int y = 0; y < lvlH; y++)
                {
                    //reset vision
                    vision[x, y] = false;
                }
            }

            //see room
            foreach (Room R in GetRoom(player))
            {
                foreach (Rect r in R.rects)
                {
                    for (int x = 0; x < r.wh.x; x++)
                    {
                        for (int y = 0; y < r.wh.y; y++)
                        {
                            seen[r.xy.x + x, r.xy.y + y] = true;
                            vision[r.xy.x + x, r.xy.y + y] = true;
                        }
                    }
                }
            }
            #endregion

            #region render level to screen
            //render to screen
            for (int x = 0; x < 80; x++)
            {
                for (int y = 0; y < 25; y++)
                {
                    Tile t = map[x + camX, y + camY];
                    if (t == null) continue;
                    if (!seen[x + camX, y + camY]) continue;

                    dfc.CellData.SetBackground(x, y, t.bg * 1f);
                    dfc.CellData.SetForeground(
                        x, y, t.fg * (vision[x+camX,y+camY] ? 1f : 0.3f)
                    );
                    dfc.CellData.Print(x, y, t.tile);
                }
            }
            #endregion

            Rect screen = new Rect(new Point(camX, camY), new Point(80, 25));

            #region render items to screen
            int[,] itemCount = new int[lvlW, lvlH];
            foreach (Item i in items)
            {
                itemCount[i.xy.x, i.xy.y]++;
            }
            foreach (Item i in items)
            {
                if (!vision[i.xy.x, i.xy.y]) continue;
                if (screen.ContainsPoint(i.xy))
                {
                    if (itemCount[i.xy.x, i.xy.y] == 1)
                    {
                        DrawToScreen(i.xy, i.bg, i.fg, i.tile);
                    }
                    else //draw a "pile"
                    {
                        DrawToScreen(i.xy, null, Color.White, "+");
                    }
                }
            }
            #endregion

            #region render actors to screen
            int[,] actorCount = new int[lvlW, lvlH];
            foreach (Actor a in actors)
            {
                actorCount[a.xy.x, a.xy.y]++;
            }
            foreach (Actor a in actors)
            {
                if (!vision[a.xy.x, a.xy.y]) continue;
                if (screen.ContainsPoint(a.xy))
                {
                    if (actorCount[a.xy.x, a.xy.y] == 1)
                    {
                        DrawToScreen(a.xy, a.bg, a.fg, a.tile);
                    }
                    else //draw a "pile"
                    {
                        DrawToScreen(a.xy, null, Color.White, "*");
                    }
                }
            }
            #endregion

            #region shittydevhumour<3
            //CHRISTMAS!
            /*Random rn = new Random();
            for (int i = 0; i < 80*25; i++)
            {
                dfc.CellData.Print(i % 80, (i - i % 80) / 80,
                    * ((char)(i%255)) + "");
                dfc.CellData.SetForeground(i % 80, (i - i % 80) / 80,
                    rn.NextDouble() > 0.5 ? Color.Red : Color.Blue);
            }*/
            #endregion

            #region render ui
            logConsole.CellData.Clear();
            for (
                int i = log.Count, n = 0;
                i > 0 && n < logConsole.ViewArea.Height;
                i--, n++
            ) {
                logConsole.CellData.Print(
                    0, logConsole.ViewArea.Height-(n+1),
                    log[i-1]
                );
            }

            inputRow.VirtualCursor.IsVisible = awaitingMenuInput;
            #endregion

            oks = ks;

            base.Update(gameTime);
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