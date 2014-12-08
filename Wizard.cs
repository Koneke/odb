using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class Wizard
    {
        public static Game1 Game;

        public static Point WmCursor;
        public static List<string> WmHistory = new List<string>();
        public static int WmScrollback;

        public static void WmInput()
        {
            //wizmode terminal

            bool scrolled = false;
            if (IO.KeyPressed(Keys.Up)) { WmScrollback++; scrolled = true; }
            if (IO.KeyPressed(Keys.Down)) { WmScrollback--; scrolled = true; }

            if (scrolled)
            {
                if (WmScrollback < 0) WmScrollback = 0;
                if (WmScrollback > WmHistory.Count)
                    WmScrollback = WmHistory.Count;
                IO.Answer = WmScrollback > 0
                    ?  WmHistory[WmHistory.Count - WmScrollback]
                    : "";
            }

            bool b = false;
            if (IO.KeyPressed(Keys.NumPad8))
                { WmCursor.Nudge(0, -1); b = true; }
            if (IO.KeyPressed(Keys.NumPad9)) {
                WmCursor.Nudge(1, -1); b = true; }
            if (IO.KeyPressed(Keys.NumPad6)) {
                WmCursor.Nudge(1, 0); b = true; }
            if (IO.KeyPressed(Keys.NumPad3)) {
                WmCursor.Nudge(1, 1); b = true; }
            if (IO.KeyPressed(Keys.NumPad2)) {
                WmCursor.Nudge(0, 1); b = true; }
            if (IO.KeyPressed(Keys.NumPad1)) {
                WmCursor.Nudge(-1, 1); b = true; }
            if (IO.KeyPressed(Keys.NumPad4)) {
                WmCursor.Nudge(-1, 0); b = true; }
            if (IO.KeyPressed(Keys.NumPad7)) {
                WmCursor.Nudge(-1, -1); b = true; }

            if (IO.KeyPressed(Keys.OemPeriod) && IO.ShiftState)
            {
                if (Game.Levels.IndexOf(Game.Level) == Game.Levels.Count - 1)
                {
                    Level l;
                    Game.Levels.Add(l = new Level(80, 25));
                    Game.Level = l;
                    Game.Log("Spawning new level.");
                }
                else
                {
                    Game.Level = Game.Levels
                        [Game.Levels.IndexOf(Game.Level) + 1];
                    Game.Log("Descending stairs.");
                }
            }

            if (IO.KeyPressed(Keys.OemComma) && IO.ShiftState)
            {
                if (Game.Levels.IndexOf(Game.Level) > 0)
                    Game.Level = Game.Levels
                        [Game.Levels.IndexOf(Game.Level) - 1];
            }

            if (b) return; //so we don't get numbers in the prompt when we
            //try to target stuff

            IO.IOState = InputType.QuestionPrompt;

            IO.AcceptedInput.Clear();
            foreach (char c in IO.Indexes) IO.AcceptedInput.Add(c); //letters
            IO.AcceptedInput.Add(' ');
            for (int i = 48; i < 58; i++) IO.AcceptedInput.Add((char)i); //nums
            if (IO.KeyPressed(Keys.OemComma)) IO.Answer += "~"; //arg. delimiter
            if (IO.KeyPressed(Keys.OemQuestion)) //shift-. to repeat
            {
                if (WmHistory.Count > 0 && IO.Answer == "")
                {
                    WmCommand(WmHistory[WmHistory.Count - 1]);
                    WmHistory.Add(WmHistory[WmHistory.Count - 1]);
                }
            }
            else if (IO.KeyPressed(Keys.OemPeriod)) IO.Answer += ".";
            if (IO.KeyPressed(Keys.OemSemicolon)) IO.Answer += ";";
            if (IO.KeyPressed(Keys.OemBackslash)) IO.Answer += "/";
            if (IO.KeyPressed(Keys.OemMinus)) IO.Answer += "-";

            IO.QuestionPromptInput();
        }

        public static void WmCommand(string s)
        {
            string[] split = s.Split('/');
            string cmd = split[0];

            string log = " > " + cmd + "/";

            string[] args = null;
            if (split.Length > 1)
            {
                args = split[1].Split('~');

                log = args.Aggregate(
                    log, (current, t) => current + (t + "~")
                );

                log = log.Substring(0, log.Length - 1);
            } else log += "";
            args = args ?? new string[0];

            Game.Log(log);

            bool realcmd = false;

            string prefix = "";
            if (cmd.Contains('-'))
                prefix = cmd.Substring(0, cmd.IndexOf('-'));

            switch (prefix)
            {
                case "":
                    realcmd = GenericCommands(cmd, args);
                    break;
                case "ad":
                    realcmd = ActorDefinitionCommands(cmd, args);
                    break;
                case "id":
                    realcmd = ItemDefinitionCommands(cmd, args);
                    break;
                case "ai":
                    realcmd = ActorInstanceCommands(cmd, args);
                    break;
                case "ii":
                    realcmd = ItemInstanceCommands(cmd, args);
                    break;
                case "lv":
                    realcmd = LevelCommands(cmd, args);
                    break;
            }

            if (!realcmd) Game.Log("No such command: " + cmd);

            IO.Answer = "";
        }

        private static bool GenericCommands(string cmd, string[] args)
        {
            switch (cmd)
            {
                //get item definition (id) (by name)
                case "gid":
                    Game.Log(IO.WriteHex(Util.ItemDefByName(args[0]).Type, 4));
                    break;
                case "gad":
                    Game.Log(IO.WriteHex(Util.ADefByName(args[0]).Type, 4));
                    break;
                case "sa":
                case "spawnactor":
                    #region spawnactor

                    if (args.Length < 1)
                    {
                        Game.Log("");
                        break;
                    }
                    Actor act = new Actor(
                        WmCursor,
                        Util.ADefByName(args[0]),
                        args.Length > 1
                        ? IO.ReadHex(args[1])
                        : 1
                    );
                    Game.Level.WorldActors.Add(act);
                    Game.Brains.Add(new Brain(act));
                    Game.Level.CalculateActorPositions();
                    break;
                    #endregion
                case "si":
                case "spawnitem":
                    #region spawnitem
                    Item it = new Item(
                        WmCursor,
                        Util.ItemDefByName(args[0]),
                        IO.ReadHex(args[1])
                    );
                    Game.Level.AllItems.Add(it);
                    Game.Level.WorldItems.Add(it);
                    break;
                    #endregion
                case "saveadefs":
                case "sad":
                    #region sad
                    SaveIO.WriteActorDefinitionsToFile("Data/" + args[0]);
                    break;
                    #endregion

                case "saveidefs":
                case "sid":
                    SaveIO.WriteItemDefinitionsToFile("Data/" + args[0]);
                    break;
                case "sp":
                case "setplayer":
                    #region setplayer
                    Game.Player = Game.Level.ActorOnTile(
                        new Point(WmCursor.x, WmCursor.y));
                    break;
                    #endregion
                case "teleport":
                case "tp":
                    Game.Player.xy = WmCursor;
                    break;
                case "printseed":
                case "prints":
                case "ps":
                    Game.Log(Game.Seed + "");
                    break;
                case "save":
                    SaveIO.Save();
                    break;
                case "load":
                    SaveIO.Load();
                    break;
                default: return false;
            }
            return true;
        }

        private static bool LevelCommands(string cmd, string[] args)
        {
            switch (cmd)
            {
                case "lv-new":
                    #region new
                    bool hadplayer = false;
                    Game.Level.Size.x = IO.ReadHex(args[0]);
                    Game.Level.Size.y = IO.ReadHex(args[1]);
                    if (Game.Level.WorldActors.Contains(Game.Player))
                        hadplayer = true;
                    Game.Level.Clear();
                    if (hadplayer) Game.Level.WorldActors.Add(Game.Player);

                    //reset vision
                    foreach (Actor actor in Game.Level.WorldActors)
                        actor.Vision = null;
                    break;
                    #endregion
                case "lv-moveup":
                    #region mu
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth > 0)
                    {
                        Game.Levels.Remove(Game.Level);
                        Game.Levels.Insert(depth - 1, Game.Level);
                    }
                    break;
                    #endregion
                case "lv-door":
                    #region door
                    if (Game.Level.Map[WmCursor.x, WmCursor.y] != null)
                    {
                        Tile t = Game.Level.Map[WmCursor.x, WmCursor.y];
                        t.Door = (Door)((((int)t.Door) + 1) % 3);
                    }
                    break;
                    #endregion
                case "lv-stairs":
                    #region stairs
                    if (Game.Level.Map[WmCursor.x, WmCursor.y] != null)
                    {
                        Tile t = Game.Level.Map[WmCursor.x, WmCursor.y];
                        t.Stairs = (Stairs)((((int)t.Stairs) + 1) % 3);
                    }
                    break;
                    #endregion
                case "lv-deltile":
                    #region deltile
                    Game.Level.Map[WmCursor.x, WmCursor.y] = null;
                    break;
                    #endregion
                case "lv-delroom":
                    #region delroom
                    List<Room> roomsAtCursor =
                        Util.GetRooms(new Point(WmCursor.x, WmCursor.y));
                    Game.Level.Rooms.RemoveAll(roomsAtCursor.Contains);
                    break;
                    #endregion
                case "lv-settile":
                    #region settile
                    Game.Level.Map[WmCursor.x, WmCursor.y].Definition =
                        Util.TileDefinitionByName(IO.ReadHex(args[0]));
                    break;
                    #endregion
                case "lv-cr":
                case "lv-createroom":
                    #region createroom
                    if (args.Length >= 5)
                    {
                        Game.Level.CreateRoom(
                            Game.Level,
                            new Rect(
                                new Point(
                                    IO.ReadHex(args[0]),
                                    IO.ReadHex(args[1])
                                ),
                                new Point(
                                    IO.ReadHex(args[2]),
                                    IO.ReadHex(args[3])
                                )
                            ),
                            Util.TileDefinitionByName(IO.ReadHex(args[4])),
                            args.Length >= 6
                                ? Util.TileDefinitionByName(IO.ReadHex(args[5]))
                                : null
                        );
                        Game.Level.CalculateRoomLinks();
                    }
                    break;
                    #endregion
                case "lv-mr":
                case "lv-mergerooms":
                    Room a = Util.Game.Level.Rooms[IO.ReadHex(args[0])];
                    Room b = Util.Game.Level.Rooms[IO.ReadHex(args[1])];
                    a.Rects.AddRange(b.Rects);
                    Game.Level.Rooms.Remove(b);
                    Game.Level.CalculateRoomLinks();
                    Game.Level.CalculateActorPositions();
                    break;
                case "lv-rid":
                    Game.Log(Util.GetRooms(WmCursor).Aggregate(
                        "Rooms at cursor:",
                        (c, n) => c + " " + Game.Level.Rooms.IndexOf(n))
                    );
                    break;
                case "lv-savelvl":
                case "lv-sl":
                    #region save
                    Game.Level.WriteLevelSave("Save/" + args[0]);
                    break;
                    #endregion
                case "lv-loadlvl":
                case "lv-ll":
                    #region load
                    Game.Level.LoadLevelSave("Save/" + args[0]);
                    Game.SetupBrains();
                    Game.Level.CalculateActorPositions();
                    break;
                    #endregion
                case "lv-rl":
                case "lv-rmlvl":
                    #region rmlvl
                    if (Game.Levels.Count > 0)
                    {
                        Game.Levels.Remove(Game.Level);
                        if (Game.Level.WorldActors.Contains(Game.Player))
                            Game.Levels[0].WorldActors.Add(Game.Player);
                        Game.Level = Game.Levels[0];
                    }
                    break;
                    #endregion
                case "lv-cl":
                case "lv-countlvl":
                    #region countlevel
                    Game.Log(Game.Levels.Count + "");
                    break;
                    #endregion
                case "lv-engrave":
                    #region engrave
                    Game.Level.Map[WmCursor.x, WmCursor.y].Engraving = args[0];
                    break;
                    #endregion
                case "lv-name":
                    #region name
                    Game.Level.Name = args[0];
                    break;
                    #endregion
                default: return false;
            }
            return true;
        }

        private static bool ActorDefinitionCommands(string cmd, string[] args)
        {
            ActorDefinition adef = Game.Level.ActorOnTile(WmCursor).Definition;

            if (adef == null)
            {
                Game.Log("No actor on tile.");
                return true;
            }

            switch (cmd)
            {
                case "ad-is":
                case "ad-get":
                    break;
                case "ad-p":
                    Game.Log(adef.WriteActorDefinition().ToString());
                    break;
                case "ad-bg":
                    adef.Background = IO.ReadNullableColor(args[0]);
                    break;
                case "ad-fg":
                    adef.Foreground = IO.ReadColor(args[0]);
                    break;
                case "ad-tile":
                    adef.Tile = args[0];
                    break;
                case "ad-name":
                    adef.Name = args[0];
                    break;
                case "ad-named":
                    adef.Named = IO.ReadBool(args[0]);
                    break;
                case "ad-stat":
                    adef.Set(
                        (Stat)IO.ReadHex(args[0]),
                        args[0]
                    );
                    break;
                case "ad-addbodypart":
                    adef.BodyParts.Add((DollSlot)IO.ReadHex(args[0]));
                    break;
                case "ad-rembodypart":
                    adef.BodyParts.Remove((DollSlot)IO.ReadHex(args[0]));
                    break;
                case "ad-corpsetype":
                    //should probably be useful about never,
                    //but for completeness sake
                    //LH-011214: Hah, turns out it was useful afterall...
                    adef.CorpseType = IO.ReadHex(args[0]);
                    break;
                case "ad-addspell":
                    adef.Spellbook.Add(IO.ReadHex(args[0]));
                    break;
                case "ad-remspell":
                    adef.Spellbook.Remove(IO.ReadHex(args[0]));
                    break;
                case "ad-addsi":
                    adef.SpawnIntrinsics.Add(
                        new Mod(
                            (ModType)IO.ReadHex(args[0]),
                            IO.ReadHex(args[1])
                        )
                    );
                    break;
                case "ad-remsi":
                    for (int i = 0; i < adef.SpawnIntrinsics.Count; i++) {
                        Mod si = adef.SpawnIntrinsics[i];
                        if (si.Type != (ModType) IO.ReadHex(args[0])) continue;
                        adef.SpawnIntrinsics.RemoveAt(i);
                    }
                    break;
                default: return false;
            }
            return true;
        }

        private static bool ItemDefinitionCommands(string cmd, string[] args)
        {
            ItemDefinition idef = Game.Level.ItemsOnTile(WmCursor)
                [0].Definition;

            if (idef == null)
            {
                Game.Log("No item on tile");
                return true; //no need to log bad cmd err, already logging this
            }

            switch (cmd)
            {
                case "id-is":
                case "id-get":
                    #region get
                    switch (args[0])
                    {
                        case "bg": Game.Log(IO.Write(idef.Background)); break;
                        case "fg": Game.Log(IO.Write(idef.Background)); break;
                        case "tile": Game.Log(idef.Tile); break;
                        case "name": Game.Log(idef.Name); break;
                        case "stacking": Game.Log(idef.Stacking+""); break;
                        case "cat": Game.Log(IO.WriteHex(idef.Category, 4));
                            break;
                    }
                    break;
                    #endregion
                case "id-id":
                    ItemDefinition.IdentifiedDefs.Add(idef.Type);
                    break;
                case "id-p":
                    Game.Log(idef.WriteItemDefinition().ToString());
                    break;
                case "id-bg":
                    idef.Background = IO.ReadNullableColor(args[0]);
                    break;
                case "id-fg":
                    idef.Foreground = IO.ReadColor(args[0]);
                    break;
                case "id-tile":
                    idef.Tile = args[0];
                    break;
                case "id-name":
                    idef.Name = args[0];
                    break;
                case "id-stack":
                case "id-stacking":
                    idef.Stacking = IO.ReadBool(args[0]);
                    break;
                case "id-use":
                case "id-cat":
                case "id-category":
                    idef.Category = IO.ReadHex(args[0]);
                    break;
                default: return false;
            }
            return true;
        }

        private static bool ActorInstanceCommands(string cmd, string[] args)
        {
            Actor a = Game.Level.ActorOnTile(WmCursor);

            if (a == null)
            {
                Game.Log("No actor on tile");
                return true;
            }

            switch (cmd)
            {
                case "ai-hurt":
                    a.Damage(IO.ReadHex(args[0]));
                    break;
                case "ai-sdef":
                case "ai-setdef":
                    a.Definition = ActorDefinition.ActorDefinitions
                        [IO.ReadHex(args[0])];
                    break;
                case "ai-p":
                case "ai-print":
                    #region pa
                    Game.Log(
                        Game.Level.ActorOnTile(WmCursor).WriteActor().ToString()
                    );
                    break;
                    #endregion
                case "ai-pd":
                case "ai-pdef":
                    #region pad
                    Game.Log(
                        a.Definition.WriteActorDefinition().ToString()
                    );
                    break;
                    #endregion
                case "ai-addbp":
                case "ai-addbodypart":
                    #region addbodypart
                    a.PaperDoll.Add(
                        new BodyPart(
                            (DollSlot)IO.ReadHex(args[0])
                        )
                    );
                    break;
                    #endregion
                case "ai-rembp":
                case "ai-rembodypart":
                    #region remove bp
                    for (int i = 0; i < a.PaperDoll.Count; i++)
                        if (a.PaperDoll[i].Type ==
                            (DollSlot)IO.ReadHex(args[0])
                        ) {
                            a.PaperDoll.RemoveAt(i);
                            break;
                        }
                    break;
                    #endregion
                case "ai-am":
                case "ai-addmod":
                    #region addmod
                    a.Intrinsics.Add(
                        new Mod(
                            (ModType)IO.ReadHex(args[0]),
                            IO.ReadHex(args[1])
                        )
                    );
                    break;
                    #endregion
                case "ai-rm":
                case "ai-remmod":
                case "ai-delmod":
                    #region remove mod
                    for (int i = 0; i < a.Intrinsics.Count; i++) {
                        if (a.Intrinsics[i].Type !=
                            (ModType)IO.ReadHex(args[0])) continue;

                        a.Intrinsics.RemoveAt(i);
                        break;
                    }
                    break;
                    #endregion
                case "ai-awake":
                    a.Awake = IO.ReadBool(args[0]);
                    break;
                case "ai-ale":
                case "ai-addle":
                    a.AddEffect(
                        (StatusType)IO.ReadHex(args[0]),
                        IO.ReadHex(args[1]));
                    break;
                default: return false;
            }
            return true;
        }

        private static bool ItemInstanceCommands(string cmd, string[] args)
        {
            List<Item> items = Game.Level.ItemsOnTile(WmCursor);

            if (items.Count <= 0) {
                Game.Log("No item on tile.");
                return true;
            }

            switch (cmd) {
                case "ii-setmod":
                    foreach (Item it in items)
                        it.Mod = IO.ReadHex(args[0]);
                    break;
                case "ii-sdef":
                case "ii-setdef":
                    foreach (Item it in Game.Level.ItemsOnTile(WmCursor))
                        it.Definition = ItemDefinition.ItemDefinitions
                            [IO.ReadHex(args[0])];
                    break;
                case "ii-p":
                case "ii-print":
                    #region pi
                    foreach(Item it in Game.Level.ItemsOnTile(WmCursor))
                        Game.Log(
                            it.WriteItem().ToString()
                        );
                    break;
                    #endregion
                case "ii-pd":
                case "ii-pdef":
                    #region pid
                    foreach(Item piditem in Game.Level.ItemsOnTile(WmCursor))
                        Game.Log(
                            piditem.Definition.WriteItemDefinition().ToString()
                        );
                    break;
                    #endregion
                case "ii-id":
                    #region id
                    foreach (Item iditem in Game.Level.ItemsOnTile(WmCursor))
                        iditem.Identify();
                    break;
                    #endregion
                case "ii-unid":
                    #region unid
                    foreach (Item iditem in Game.Level.ItemsOnTile(WmCursor))
                        ItemDefinition.IdentifiedDefs.Remove(iditem.Type);
                    break;
                    #endregion
                case "ii-am":
                case "ii-addmod":
                    #region addmod
                    foreach (Item item in Game.Level.ItemsOnTile(WmCursor))
                    {
                        item.Mods.Add(
                            new Mod(
                                (ModType)IO.ReadHex(args[0]),
                                IO.ReadHex(args[1])
                            )
                        );
                    }
                    break;
                    #endregion
                case "ii-rm":
                case "ii-remmod":
                case "ii-delmod":
                case "rm": case "remmod": case "removemod":
                    #region remove mod
                    bool removed = false;
                    foreach (Item item in Game.Level.ItemsOnTile(WmCursor))
                    {
                        if (removed) break;
                        for (int i = 0; i < item.Mods.Count; i++)
                        {
                            if (item.Mods[i].Type !=
                                (ModType)IO.ReadHex(args[0]))
                                continue;
                            item.Mods.RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }
                    break;
                    #endregion
                default: return false;
            }

            return true;
        }
    }
}
