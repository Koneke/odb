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
// * Inventory textwrapping
// * Basic magic?
// * Item value and paid-for status

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

        KeyboardState ks, oks;
        bool shift;

        public Tile[,] map;
        public bool[,] seen;
        //consider moving vision into actor class
        public bool[,] vision;
        public List<Room> rooms;

        public List<Actor> worldActors;
        public List<Item> worldItems; //in world
        public List<Item> allItems;
        public List<Brain> Brains;

        public Actor player;

        int camX, camY;
        int scrW, scrH;
        public int lvlW, lvlH;

        public bool logPlayerActions;
        int logSize;
        Console logConsole;
        public List<string> log;

        Console inputRowConsole;

        public bool questionPromptOpen;

        //var to hold input before it being sent
        string questionPromptAnswer;
        //we're starting to need two questions for one thing now
        public Stack<string> qpAnswerStack;

        bool questionPrompOneKey;
        public List<int> acceptedInput;
        public Action<string> questionReaction;

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
            vision = new bool[lvlW, lvlH]; //playervision

            worldActors = new List<Actor>();
            worldItems = new List<Item>();
            allItems = new List<Item>();
            Brains = new List<Brain>();

            logPlayerActions = !true;
            logSize = 3;
            log = new List<string>();
            log.Add("Something something dungeon");

            standardHuman = new List<DollSlot>();
            standardHuman.Add(DollSlot.Head);
            standardHuman.Add(DollSlot.Torso);
            standardHuman.Add(DollSlot.Hand);
            standardHuman.Add(DollSlot.Hand);
            standardHuman.Add(DollSlot.Legs);
            standardHuman.Add(DollSlot.Feet);

            acceptedInput = new List<int>();
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

            base.Initialize();
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

        public void setupQuestionPrompt(string q, bool onekey = true)
        {
            q = q + " ";
            questionPromptAnswer = "";
            questionPrompOneKey = onekey;
            inputRowConsole.CellData.Clear();
            inputRowConsole.CellData.Fill(Color.Black, Color.White, ' ', null);
            inputRowConsole.CellData.Print(0, 0, q);
            inputRowConsole.VirtualCursor.Position =
                new xnaPoint(q.Length, 0);
        }

        public Point NumpadToDirection(char c)
        {
            Point p;
            switch (c)
            {
                case (char)Keys.NumPad7:
                    p = new Point(-1, -1);
                    break;
                case (char)Keys.NumPad8:
                    p = new Point(0, -1);
                    break;
                case (char)Keys.NumPad9:
                    p = new Point(1, -1);
                    break;
                case (char)Keys.NumPad4:
                    p = new Point(-1, 0);
                    break;
                case (char)Keys.NumPad6:
                    p = new Point(1, 0);
                    break;
                case (char)Keys.NumPad1:
                    p = new Point(-1, 1);
                    break;
                case (char)Keys.NumPad2:
                    p = new Point(0, 1);
                    break;
                case (char)Keys.NumPad3:
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
            #region engineshit
            SadConsole.Engine.Update(gameTime, this.IsActive);
            dfc.CellData.Clear();

            ks = Keyboard.GetState();
            shift =
                (ks.IsKeyDown(Keys.LeftShift) ||
                ks.IsKeyDown(Keys.RightShift));
            #endregion

            #region ui interaction
            if (
                KeyPressed(Keys.Q) ||
                (KeyPressed(Keys.Escape) && !questionPromptOpen)
            ) this.Exit();

            if (KeyPressed(Keys.I))
                inventoryConsole.IsVisible =
                    !inventoryConsole.IsVisible;
            #endregion

            #region log
            if (KeyPressed((Keys)0x6B)) //np+
                logSize = Math.Min(logConsole.ViewArea.Height, ++logSize);
            if (KeyPressed((Keys)0x6D)) //np-
                logSize = Math.Max(0, --logSize);
            logConsole.Position = new xnaPoint(
                0, -logConsole.ViewArea.Height + logSize
            );
            #endregion

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

            if (KeyPressed(Keys.F1))
            {
                IO.WriteLevelToFile("Save/level.sv");
                IO.WriteRoomsToFile("Save/rooms.sv");
                IO.WriteAllItemsToFile("Save/items.sv");
                IO.WriteAllActorsToFile("Save/actors.sv");
                IO.WriteSeenToFile("Save/seen.sv");
            }

            if (KeyPressed(Keys.F2))
            {
                IO.ReadLevelFromFile("Save/level.sv");
                IO.ReadRoomsFromFile("Save/rooms.sv");
                IO.ReadAllItemsFromFile("Save/items.sv");
                IO.ReadAllActorsFromFile("Save/actors.sv");
                IO.ReadSeenFromFile("Save/seen.sv");
                player = Util.GetActorByID(0);
            }

            if (KeyPressed(Keys.F3))
            {
                Game.allItems[0].Mods.Add(
                    new Mod(ModType.AddStr, 2)
                );
                var a = 0;
            }

            //only do player movement if we're not currently asking something
            if (!questionPromptOpen)
            {
                //pretty much every possible player interaction (that uses
                //ingame time) should be in this if-clause.
                if(player.Cooldown == 0)
                {
                    #region movement
                    Point offset = new Point(0, 0);

                    if (KeyPressed(Keys.NumPad8)) offset.Nudge(0, -1);
                    if (KeyPressed(Keys.NumPad9)) offset.Nudge(1, -1);
                    if (KeyPressed(Keys.NumPad6)) offset.Nudge(1, 0);
                    if (KeyPressed(Keys.NumPad3)) offset.Nudge(1, 1);
                    if (KeyPressed(Keys.NumPad2)) offset.Nudge(0, 1);
                    if (KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
                    if (KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
                    if (KeyPressed(Keys.NumPad7)) offset.Nudge(-1, -1);

                    if (KeyPressed(Keys.NumPad5)) Game.player.Pass(true);

                    Tile target =
                        map[player.xy.x + offset.x, player.xy.y + offset.y];

                    #region actual moving/attacking
                    if (offset.x != 0 || offset.y != 0)
                    {
                        bool legalMove = true;
                        if (target == null)
                            legalMove = false;
                        else if (
                            target.doorState == Door.Closed || target.solid
                        )
                            legalMove = false;

                        if (!legalMove)
                        {
                            offset = new Point(0, 0);
                            Game.log.Add("Bump!");
                        }
                        else
                        {
                            if (Util.ActorsOnTile(target).Count <= 0)
                            {
                                player.xy.Nudge(offset.x, offset.y);

                                Game.player.Pass(true);
                            }
                            else
                            {
                                //fighty time!
                                offset = new Point(0, 0);

                                //in reality, there should only be max 1.
                                //but yknow, in case of...
                                //this really shouldn't be a foreach.
                                foreach (Actor a in Util.ActorsOnTile(target))
                                {
                                    player.Attack(a);
                                }
                                Game.player.Pass();
                            }
                        }
                    }
                    #endregion

                    #region looking at our new tile
                    //if we have moved, do fun stuff.
                    if (offset.x != 0 || offset.y != 0)
                    {
                        List<Item> itemsOnSquare = Util.ItemsOnTile(player.xy);

                        switch (itemsOnSquare.Count)
                        {
                            case 0:
                                break;
                            case 1:
                                log.Add(
                                    "There is " +
                                    article(
                                        itemsOnSquare[0].Definition.name
                                    ) + " " + itemsOnSquare[0].Definition.name +
                                    " here."
                                );
                                break;
                            default:
                                log.Add(
                                    "There are " + itemsOnSquare.Count +
                                    " items here."
                                );
                                break;
                        }
                    }
                    #endregion

                    #endregion

                    #region drop
                    if (KeyPressed(Keys.D) && !shift)
                    {
                        string _q = "Drop what? [";
                        acceptedInput.Clear();
                        for (int i = 0; i < player.inventory.Count; i++)
                        {
                            char index = (char)(97 + i);
                            _q += index;
                            acceptedInput.Add((int)(index + "").ToUpper()[0]);
                        }
                        _q += "]";
                        setupQuestionPrompt(_q);
                        questionPromptOpen = true;

                        questionReaction = Player.Drop;
                    }
                    #endregion

                    #region open
                    if (KeyPressed(Keys.O) && !shift)
                    {
                        acceptedInput.Clear();
                        acceptedInput.AddRange(directions);
                        setupQuestionPrompt("Open where?");
                        questionPromptOpen = true;
                        questionReaction = Player.Open;
                    }
                    #endregion

                    #region close
                    if (KeyPressed(Keys.C) && !shift)
                    {
                        acceptedInput.Clear();
                        acceptedInput.AddRange(directions);
                        setupQuestionPrompt("Close where?");
                        questionPromptOpen = true;
                        questionReaction = Player.Close;
                    }
                    #endregion

                    #region wield/wear //AHAHAHA IM A GENIUS
                    if (KeyPressed(Keys.W))
                    {
                        List<Item> equipables = new List<Item>();

                        foreach (Item it in player.inventory)
                            //is it wieldable?
                            if (
                                it.Definition.equipSlots.Contains(DollSlot.Hand)
                                && !shift
                            )
                                equipables.Add(it);
                            else if (
                                it.Definition.equipSlots.FindAll(
                                    x => x != DollSlot.Hand
                                ).Count > 0 && shift
                            )
                                equipables.Add(it);

                        //remove items we've already equipped from the
                        //list of potential items to equip
                        foreach (BodyPart bp in player.PaperDoll)
                            equipables.Remove(bp.Item);

                        if (equipables.Count > 0)
                        {
                            string _q = (shift ? "Wear" : "Wield") + " what? [";
                            acceptedInput.Clear();
                            foreach (Item it in equipables)
                            {
                                //show the character corresponding with the one
                                //shown in the inventory.
                                char index =
                                    (char)(97 + player.inventory.IndexOf(it));
                                _q += index;
                                acceptedInput.Add((int)(index + "")
                                    .ToUpper()[0]
                                );
                            }
                            _q += "]";
                            setupQuestionPrompt(_q);
                            questionPromptOpen = true;

                            questionReaction = Player.Wield;
                        }
                        else
                        {
                            log.Add("Nothing to " +
                                (shift ? "wear" : "wield") + ".");
                        }
                    }
                    #endregion

                    #region sheath
                    if (KeyPressed(Keys.S) && shift)
                    {
                        List<Item> equipped = new List<Item>();
                        foreach (
                            BodyPart bp in Game.player.PaperDoll.FindAll(
                                x => x.Type == DollSlot.Hand
                            )
                        )
                        {
                            if (bp.Item != null)
                                equipped.Add(bp.Item);
                        }

                        if (equipped.Count > 0)
                        {
                            string _q = "Sheath what? [";
                            foreach (Item it in equipped)
                            {
                                char index =
                                    (char)(97 +
                                        Game.player.inventory.IndexOf(it)
                                    );
                                _q += index;
                                acceptedInput.Add((int)(index + "")
                                    .ToUpper()[0]
                                );
                            }
                            _q += "]";
                            setupQuestionPrompt(_q);
                            questionPromptOpen = true;

                            questionReaction = Player.Sheath;
                        }
                        else
                        {
                            Game.log.Add("Nothing to sheath.");
                        }
                    }
                    #endregion

                    #region get
                    if (KeyPressed(Keys.G) && !shift)
                    {
                        List<Item> onFloor = Util.ItemsOnTile(player.xy);
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
                                    acceptedInput.Add((int)(index + "").ToUpper()[0]);
                                }
                                _q += "]";
                                setupQuestionPrompt(_q);
                                questionPromptOpen = true;

                                acceptedInput.Clear();
                                acceptedInput.AddRange(letters);

                                questionReaction = Player.Get;
                            }
                            else //remove me? just show the option for just "a"
                            {
                                qpAnswerStack.Push("a");
                                Player.Get("a");
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region tick brains
                    while (Game.player.Cooldown > 0)
                    {
                        foreach (Brain b in Brains)
                            if (
                                Game.worldActors.Contains(b.MeatPuppet) &&
                                b.MeatPuppet.Cooldown == 0
                            )
                                b.Tick();

                        foreach (Brain b in Brains) b.MeatPuppet.Cooldown--;
                        Game.player.Cooldown--;
                    }
                    #endregion
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

                        if (questionPrompOneKey)
                        {
                            questionPromptOpen = false;
                            qpAnswerStack.Push(questionPromptAnswer);
                            questionReaction(questionPromptAnswer);
                        }
                    }
                }
                if (KeyPressed(Keys.Back))
                {
                    if (questionPromptAnswer.Length > 0)
                    {
                        questionPromptAnswer =
                            questionPromptAnswer.Substring(
                            0, questionPromptAnswer.Length - 1
                        );
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
                    qpAnswerStack.Push(questionPromptAnswer);
                    questionReaction(questionPromptAnswer);
                }
                if (KeyPressed(Keys.Escape))
                {
                    questionPromptAnswer = "";
                    questionPromptOpen = false;
                }
                #endregion
            }

            #region vision
            for (int x = 0; x < lvlW; x++)
            {
                for (int y = 0; y < lvlH; y++)
                {
                    //reset vision
                    vision[x, y] = false;
                     //wizion
                    /*vision[x, y] = true;
                    seen[x, y] = true;*/
                }
            }

            //see room
            foreach (Room R in Util.GetRooms(player))
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
                    //tile we're working on, (screen)X/Y and cam offsets
                    Tile t = map[x + camX, y + camY];

                    //if the coordinate is void, skip
                    if (t == null) continue;

                    //if the coordinate has no been seen, skip
                    if (!seen[x + camX, y + camY]) continue;

                    //whether or not the player currently sees the tile
                    bool inVision = vision[x + camX, y + camY];

                    dfc.CellData.SetBackground(x, y, t.bg * 1f);
                    dfc.CellData.SetForeground(
                        x, y, t.fg * (inVision ? 1f : 0.6f)
                    );

                    string tileToDraw = t.tile;
                    //doors override the normal tile
                    //which shouldn't be a problem
                    //if it is a problem, it's not, it's something else
                    if (t.doorState == Door.Closed)
                        tileToDraw = "+";
                    if (t.doorState == Door.Open)
                        tileToDraw = "/";

                    dfc.CellData.Print(x, y, tileToDraw);
                }
            }
            #endregion

            Rect screen = new Rect(new Point(camX, camY), new Point(80, 25));

            #region render items to screen
            int[,] itemCount = new int[lvlW, lvlH];
            foreach (Item i in worldItems)
            {
                itemCount[i.xy.x, i.xy.y]++;
            }
            foreach (Item i in worldItems)
            {
                if (!vision[i.xy.x, i.xy.y]) continue;
                if (screen.ContainsPoint(i.xy))
                {
                    if (itemCount[i.xy.x, i.xy.y] == 1)
                    {
                        DrawToScreen(
                            i.xy,
                            i.Definition.bg,
                            i.Definition.fg,
                            i.Definition.tile
                        );
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
            foreach (Actor a in worldActors)
            {
                actorCount[a.xy.x, a.xy.y]++;
            }
            foreach (Actor a in worldActors)
            {
                if (!vision[a.xy.x, a.xy.y]) continue;
                if (screen.ContainsPoint(a.xy))
                {
                    if (actorCount[a.xy.x, a.xy.y] == 1)
                    {
                        DrawToScreen(
                            a.xy, a.Definition.bg,
                            a.Definition.fg, a.Definition.tile
                        );
                    }
                    else //draw a "pile"
                    {
                        DrawToScreen(a.xy, null, Color.White, "*");
                    }
                }
            }
            #endregion

            #region render ui
            logConsole.CellData.Clear();
            logConsole.CellData.Fill(Color.White, Color.Black, ' ', null);
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
            inputRowConsole.Position = new xnaPoint(0, logSize);

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
                if (equipped) name += " (equipped)";

                inventoryConsole.CellData.Print(
                    2, i+1, name
                );
            }
            #endregion

            statRowConsole.CellData.Clear();
            statRowConsole.CellData.Fill(Color.White, Color.Black, ' ', null);
            string namerow = player.Definition.name + " - Delver";
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

            float playerHealthPcnt = (player.hpCurrent /
                (float)player.Definition.hpMax);
            float colourStrength = 0.6f + 0.4f - (0.4f * playerHealthPcnt);

            for (int x = 0; x < 9; x++)
                statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        colourStrength - colourStrength * playerHealthPcnt,
                        colourStrength * playerHealthPcnt,
                        0
                    )
                );

            statRowConsole.CellData.Print(0, 0, namerow);
            statRowConsole.CellData.Print(0, 1, statrow);

            #endregion

            #region engineshit
            oks = ks;

            base.Update(gameTime);
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