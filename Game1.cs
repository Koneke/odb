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

//general todo idea:
// question prompt ~stack~, instead of just one, like it is atm?

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
        #endregion

        KeyboardState ks, oks;
        bool shift;

        Tile[,] map;
        bool[,] seen;
        bool[,] vision;
        List<Room> rooms;

        public List<Actor> actors;
        public List<Item> items; //in world

        public Actor player;

        int camX, camY;
        int lvlW, lvlH;
        int scrW, scrH;

        Console logConsole;
        public List<string> log;

        Console inputRowConsole;

        bool questionPromptOpen;
        string questionPromptAnswer;
        List<int> acceptedInput;
        Action<string> questionReaction;

        List<int> letters;
        List<int> numbers;
        int space;

        Console inventoryConsole;

        List<dollSlot> standardHuman;

        Console statRowConsole;

        public void SetupConsoles()
        {
            dfc = new Console(80, 25);
            SadConsole.Engine.ConsoleRenderStack.Add(dfc);
            SadConsole.Engine.ActiveConsole = dfc;

            logConsole = new Console(80, 3);
            SadConsole.Engine.ConsoleRenderStack.Add(logConsole);

            inputRowConsole = new Console(80, 1);
            inputRowConsole.Position = new xnaPoint(0, 3);
            inputRowConsole.VirtualCursor.IsVisible = true;
            SadConsole.Engine.ConsoleRenderStack.Add(inputRowConsole);

            inventoryConsole = new Console(30, 25);
            inventoryConsole.Position = new xnaPoint(50, 0);
            inventoryConsole.IsVisible = false;
            SadConsole.Engine.ConsoleRenderStack.Add(inventoryConsole);

            statRowConsole = new Console(80, 2);
            statRowConsole.Position = new xnaPoint(0, 23);
            SadConsole.Engine.ConsoleRenderStack.Add(statRowConsole);
        }

        protected override void Initialize()
        {
            #region engineshit
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

            Player.Game = this;

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

            standardHuman = new List<dollSlot>();
            //todo: lazy non-just-loop-through-the-enum definition of human
            foreach(dollSlot ds in Enum.GetValues(typeof(dollSlot)))
                standardHuman.Add(ds);

            acceptedInput = new List<int>();

            letters = new List<int>();
            numbers = new List<int>();
            for (int i = 65; i <= 90; i++) letters.Add(i);
            for (int i = 48; i <= 57; i++) numbers.Add(i);
            space = 32;

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

            standardHuman.ForEach(x =>
                player.paperDoll.Add(x, null)
            );

            actors.Add(
                new Actor(
                    new Point(12, 13), null, Color.Red, "&", "Demigorgon"
                )
            );
            #endregion

            #region dev items
            Item it;

            items.Add(
                it = new Item(
                    new Point(13, 13), null, Color.Green, ")", "Longsword"
                )
            );

            it.equipSlots = new List<dollSlot>{ dollSlot.Hand };

            items.Add(
                it = new Item(
                    new Point(13, 13), null, Color.Green, ")", "Snickersnee"
                )
            );

            it.equipSlots = new List<dollSlot>{ dollSlot.Hand };

            items.Add(
                it = new Item(
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

        public List<Item> ItemsOnTile(Point xy)
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

        string article(string name)
        {
            //not ENTIRELY correct, whatwith exceptions,
            //but close enough.
            return
                new List<char>() { 'a', 'e', 'i', 'o', 'u' }
                    .Contains(name.ToLower()[0]) ?
                "an" : "a";
        }

        void setupQuestionPrompt(string q)
        {
            q = q + " ";
            questionPromptAnswer = "";
            inputRowConsole.CellData.Clear();
            inputRowConsole.CellData.Print(0, 0, q);
            inputRowConsole.VirtualCursor.Position =
                new xnaPoint(q.Length, 0);
        }

        protected override void Update(GameTime gameTime)
        {
            #region engineshit
            SadConsole.Engine.Update(gameTime, this.IsActive);
            ks = Keyboard.GetState();
            dfc.CellData.Clear();
            #endregion

            shift =
                (ks.IsKeyDown(Keys.LeftShift) ||
                ks.IsKeyDown(Keys.RightShift));

            if (
                KeyPressed(Keys.Q) ||
                (KeyPressed(Keys.Escape) && !questionPromptOpen)
            ) this.Exit();

            #region camera
            //todo: edge scrolling
            int scrollSpeed = 3; ;
            if (KeyPressed(Keys.Right))
                camX+=scrollSpeed;
            if(KeyPressed(Keys.Left))
                camX-=scrollSpeed;
            if (KeyPressed(Keys.Up))
                camY+=scrollSpeed;
            if(KeyPressed(Keys.Down))
                camY-=scrollSpeed;

            camX = Math.Max(0, camX);
            camX = Math.Min(lvlW - scrW, camX);
            camY = Math.Max(0, camY);
            camY = Math.Min(lvlH - scrH, camY);
            #endregion camera

            Point offset = new Point(0, 0);

            //only do player movement if we're not currently asking something
            if (!questionPromptOpen)
            {
                #region movement
                if (KeyPressed(Keys.NumPad8)) offset.Nudge( 0,-1);
                if (KeyPressed(Keys.NumPad9)) offset.Nudge( 1,-1);
                if (KeyPressed(Keys.NumPad6)) offset.Nudge( 1, 0);
                if (KeyPressed(Keys.NumPad3)) offset.Nudge( 1, 1);
                if (KeyPressed(Keys.NumPad2)) offset.Nudge( 0, 1);
                if (KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
                if (KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
                if (KeyPressed(Keys.NumPad7)) offset.Nudge(-1,-1);
                #endregion

                #region drop
                if (KeyPressed(Keys.D) && !shift)
                {
                    string _q = "Drop what? [";
                    acceptedInput.Clear();
                    for (int i = 0; i < player.inventory.Count; i++)
                    {
                        char index = (char)(97 + i);
                        //_q += (char)(97 + i);
                        _q += index;
                        acceptedInput.Add((int)(index+"").ToUpper()[0]);
                    }
                    _q += "]";
                    setupQuestionPrompt(_q);
                    questionPromptOpen = true;

                    questionReaction = Player.Drop;
                }
                #endregion

                #region wield
                if (KeyPressed(Keys.W) && !shift)
                {
                    List<Item> equipables = new List<Item>();

                    foreach (Item it in player.inventory)
                        //is it equipable?
                        if (it.equipSlots.Count > 0)
                            equipables.Add(it);

                    foreach (Item it in player.paperDoll.Values)
                        if (it != null)
                            //no double equipping :p
                            equipables.Remove(it);

                    if (equipables.Count > 0)
                    {
                        string _q = "Wield what? [";
                        acceptedInput.Clear();
                        foreach (Item it in equipables)
                        {
                            //show the character corresponding with the one
                            //shown in the inventory.
                            char index =
                                (char)(97 + player.inventory.IndexOf(it));
                            _q += index;
                            acceptedInput.Add((int)(index + "").ToUpper()[0]);
                        }
                        _q += "]";
                        setupQuestionPrompt(_q);
                        questionPromptOpen = true;

                        questionReaction = Player.Wield;
                    }
                    else
                    {
                        log.Add("Nothing to wield.");
                    }
                }
                #endregion

                #region get
                if (KeyPressed(Keys.G) && !shift)
                {
                    List<Item> onFloor = ItemsOnTile(player.xy);
                    if (onFloor.Count > 0)
                    {
                        if (onFloor.Count > 1)
                        {
                            string _q = "Pick up what? [";
                            acceptedInput.Clear();
                            for (int i = 0; i < onFloor.Count; i++)
                            {
                                char index = (char)(97 + i);
                                _q += index;
                                acceptedInput.Add((int)(index+"").ToUpper()[0]);
                            }
                            _q += "]";
                            setupQuestionPrompt(_q);
                            questionPromptOpen = true;

                            acceptedInput.Clear();
                            acceptedInput.AddRange(letters);

                            questionReaction = Player.Get;
                        }
                        else
                        {
                            player.inventory.Add(onFloor[0]);
                            items.Remove(onFloor[0]);
                            log.Add("Picked up " + onFloor[0].name + ".");
                        }
                    }
                }
                #endregion

                if (KeyPressed(Keys.I))
                    inventoryConsole.IsVisible = !inventoryConsole.IsVisible;

                //just general test thing
                if (KeyPressed(Keys.F1))
                {
                    setupQuestionPrompt("How many?");
                    questionPromptOpen = true;

                    acceptedInput.Clear();
                    acceptedInput.AddRange(numbers);

                    //questionReaction = DropItem;
                }
            }
            else
            {
                #region qp input
                Keys[] pk = ks.GetPressedKeys();
                Keys[] opk = oks.GetPressedKeys();

                foreach (int i in acceptedInput)
                {
                    if (pk.Contains((Keys)i) && !opk.Contains((Keys)i))
                    {
                        char c = (char)i;
                        //if our char is a letter, affect it by shift
                        if(i >= 65 && i <= 90)
                            c += (char)(shift ? 0 : 32);
                        questionPromptAnswer += c;
                        
                        //type it out
                        inputRowConsole.CellData.Print(
                            inputRowConsole.VirtualCursor.Position.X,
                            inputRowConsole.VirtualCursor.Position.Y,
                            c+"");
                        inputRowConsole.VirtualCursor.Left(-1);
                    }
                }
                if (KeyPressed(Keys.Back))
                {
                    if (questionPromptAnswer.Length > 0)
                    {
                        questionPromptAnswer = questionPromptAnswer.Substring(0, questionPromptAnswer.Length - 1);
                        inputRowConsole.VirtualCursor.Left(1);
                        inputRowConsole.CellData.Print(
                            inputRowConsole.VirtualCursor.Position.X,
                            inputRowConsole.VirtualCursor.Position.Y,
                            " ");
                    }
                }
                if (KeyPressed(Keys.Enter))
                {
                    questionPromptOpen = false;
                    questionReaction(questionPromptAnswer);
                }
                if (KeyPressed(Keys.Escape))
                {
                    questionPromptAnswer = "";
                    questionPromptOpen = false;
                }
                #endregion
            }

            player.xy.Nudge(offset.x, offset.y);

            //if we have moved, do fun stuff.
            if (offset.x != 0 || offset.y != 0) {
                List<Item> itemsOnSquare = ItemsOnTile(player.xy);

                switch (itemsOnSquare.Count)
                {
                    case 0:
                        break;
                    case 1:
                        log.Add(
                            "There is " +
                            article(itemsOnSquare[0].name) + " " +
                            itemsOnSquare[0].name +
                            " here."
                        );
                        break;
                    default:
                        log.Add(
                            "There are " + itemsOnSquare.Count + " items here."
                        );
                        break;
                }
            }

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

            inputRowConsole.IsVisible = questionPromptOpen;

            #region inventory
            int inventoryW = inventoryConsole.ViewArea.Width;
            int inventoryH = inventoryConsole.ViewArea.Height;

            inventoryConsole.CellData.Clear();

            DrawBorder(
                inventoryConsole,
                new Rect(
                    new Point(0, 0),
                    new Point(inventoryW, inventoryH)),
                Color.Black,
                Color.DarkGray
            );

            for (int i = 0; i < player.name.Length; i++)
            {
                inventoryConsole.CellData.Print(
                    2, 0, player.name, Color.White);
            }

            for (int i = 0; i < player.inventory.Count; i++)
            {
                bool equipped =
                    player.paperDoll.Values.Contains(player.inventory[i]);

                string name = "" + ((char)(97 + i));
                name += " - " + player.inventory[i].name;
                if (equipped) name += " (equipped)";

                inventoryConsole.CellData.Print(
                    2, i+1, name
                );
            }
            #endregion

            statRowConsole.CellData.Clear();
            string namerow = player.name + " - Delver";
            statRowConsole.CellData.Print(0, 0, namerow);
            string statrow = "";
            statrow += "STR - " + player.strength + " ; ";
            statrow += "DEX - " + player.dexterity + " ; ";
            statrow += "INT - " + player.intelligence;
            statRowConsole.CellData.Print(0, 1, statrow);

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