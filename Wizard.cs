using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class Wizard
    {
        public static Point WmCursor;
        public static List<string> WmHistory = new List<string>();
        public static int WmScrollback;

        public static void WmInput()
        {
            //wizmode terminal

            bool scrolled = false;
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_Scrollback))
                { WmScrollback++; scrolled = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_Scrolldown))
                { WmScrollback--; scrolled = true; }

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
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_North))
                { WmCursor.Nudge(0, -1); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_NorthEast))
                { WmCursor.Nudge(1, -1); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_East))
                { WmCursor.Nudge(1, 0); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_SouthEast))
                { WmCursor.Nudge(1, 1); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_South))
                { WmCursor.Nudge(0, 1); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_SouthWest))
                { WmCursor.Nudge(-1, 1); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_West))
                { WmCursor.Nudge(-1, 0); b = true; }
            if (KeyBindings.Pressed(KeyBindings.Bind.Wm_NorthWest))
                { WmCursor.Nudge(-1, -1); b = true; }

            if (b) return; //so we don't get numbers in the prompt when we
            //try to target stuff

            IO.IOState = InputType.QuestionPrompt;
            IO.SetInput(IO.Indexes, ' ', IO.Numbers);

            if (IO.KeyPressed(Keys.OemComma))IO.Answer += "~"; //arg. delimiter
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

            Game.UI.Log(log);

            bool realcmd = false;

            string prefix = "";
            if (cmd.Contains('-'))
                prefix = cmd.Substring(0, cmd.IndexOf('-'));

            switch (prefix)
            {
                case "": realcmd = GenericCommands(cmd, args); break;
                case "ad": realcmd = ActorDefinitionCommands(cmd, args); break;
                case "id": realcmd = ItemDefinitionCommands(cmd, args); break;
                case "ai": realcmd = ActorInstanceCommands(cmd, args); break;
                case "ii": realcmd = ItemInstanceCommands(cmd, args); break;
                case "lv": realcmd = LevelCommands(cmd, args); break;
            }

            if (!realcmd) Game.UI.Log("No such command: " + cmd);

            IO.Answer = "";
        }

        private static bool GenericCommands(string cmd, string[] args)
        {
            switch (cmd)
            {
                case "togglerolls":
                    Game.OpenRolls = !Game.OpenRolls;
                    Game.UI.Log("=> " + Game.OpenRolls);
                    break;
                //get item definition (id) (by name)
                case "gid":
                    Game.UI.Log(
                        IO.WriteHex(Util.ItemDefByName(args[0]).Type, 4)
                    );
                    break;
                case "gad":
                    Game.UI.Log(IO.WriteHex(Util.ADefByName(args[0]).Type, 4));
                    break;
                case "sa":
                case "spawnactor":
                    #region spawnactor
                    if (args.Length < 1)
                    {
                        Game.UI.Log("");
                        break;
                    }
                    Actor act = new Actor(
                        WmCursor,
                        Util.ADefByName(args[0]),
                        args.Length > 1
                        ? IO.ReadHex(args[1])
                        : 1
                    );
                    World.Instance.WorldActors.Add(act);
                    Game.Brains.Add(new Brain(act));
                    World.Level.CalculateActorPositions();
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
                    World.Level.Spawn(it);
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
                    Game.Player = World.Level.ActorOnTile(
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
                    Game.UI.Log(Game.Seed + "");
                    break;
                case "save":
                    SaveIO.JsonSave();
                    break;
                case "load":
                    SaveIO.JsonLoad();
                    break;
                default: return false;
            }
            return true;
        }

        private static bool LevelCommands(string cmd, string[] args)
        {
            switch (cmd)
            {
                case "lv-door":
                    #region door
                    if (World.Level.At(WmCursor) != null)
                    {
                        Tile t = World.Level.At(WmCursor).Tile;
                        t.Door = (Door)((((int)t.Door) + 1) % 3);
                    }
                    break;
                    #endregion
                case "lv-stairs":
                    #region stairs
                    if (World.Level.At(WmCursor) != null)
                    {
                        Tile t = World.Level.At(WmCursor).Tile;
                        t.Stairs = (Stairs)((((int)t.Stairs) + 1) % 3);
                    }
                    break;
                    #endregion
                case "lv-deltile":
                    #region deltile
                    World.Level.At(WmCursor).Tile = null;
                    break;
                    #endregion
                case "lv-delroom":
                    #region delroom
                    List<Room> roomsAtCursor =
                        Util.GetRooms(new Point(WmCursor.x, WmCursor.y));
                    World.Level.Rooms.RemoveAll(roomsAtCursor.Contains);
                    break;
                    #endregion
                case "lv-settile":
                    #region settile
                    World.Level.At(WmCursor).Tile.Definition =
                        Util.TileDefinitionByName(IO.ReadHex(args[0]));
                    break;
                    #endregion
                case "lv-cr":
                case "lv-createroom":
                    #region createroom
                    if (args.Length >= 5)
                    {
                        World.Level.CreateRoom(
                            World.Level,
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
                        World.Level.CalculateRoomLinks();
                    }
                    break;
                    #endregion
                case "lv-mr":
                case "lv-mergerooms":
                    Room a = World.Level.Rooms[IO.ReadHex(args[0])];
                    Room b = World.Level.Rooms[IO.ReadHex(args[1])];
                    a.Rects.AddRange(b.Rects);
                    World.Level.Rooms.Remove(b);
                    World.Level.CalculateRoomLinks();
                    World.Level.CalculateActorPositions();
                    break;
                case "lv-rid":
                    Game.UI.Log(Util.GetRooms(WmCursor).Aggregate(
                        "Rooms at cursor:",
                        (c, n) => c + " " + World.Level.Rooms.IndexOf(n))
                    );
                    break;
                case "lv-savelvl":
                case "lv-sl":
                    #region save
                    World.Level.WriteLevelSave("Save/" + args[0]);
                    break;
                    #endregion
                case "lv-loadlvl":
                case "lv-ll":
                    #region load
                    World.Level.LoadLevelSave("Save/" + args[0]);
                    Game.SetupBrains();
                    World.Level.CalculateActorPositions();
                    break;
                    #endregion
                case "lv-engrave":
                    #region engrave
                    World.Level.At(WmCursor).Tile.Engraving = args[0];
                    break;
                    #endregion
                case "lv-name":
                    #region name
                    World.Level.Name = args[0];
                    break;
                    #endregion
                default: return false;
            }
            return true;
        }

        private static bool ActorDefinitionCommands(string cmd, string[] args)
        {
            ActorDefinition adef = World.Level.ActorOnTile(WmCursor).Definition;

            if (adef == null)
            {
                Game.UI.Log("No actor on tile.");
                return true;
            }

            switch (cmd)
            {
                case "ad-is":
                case "ad-get":
                    break;
                case "ad-p":
                    Game.UI.Log(adef.WriteActorDefinition().ToString());
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
            ItemDefinition idef = World.Level.ItemsOnTile(WmCursor)
                [0].Definition;

            if (idef == null)
            {
                Game.UI.Log("No item on tile");
                return true; //no need to log bad cmd err, already logging this
            }

            switch (cmd)
            {
                case "id-is":
                case "id-get":
                    #region get
                    switch (args[0])
                    {
                        case "bg": Game.UI.Log(IO.Write(idef.Background)); break;
                        case "fg": Game.UI.Log(IO.Write(idef.Background)); break;
                        case "tile": Game.UI.Log(idef.Tile); break;
                        case "name": Game.UI.Log(idef.Name); break;
                        case "stacking": Game.UI.Log(idef.Stacking+""); break;
                        case "cat": Game.UI.Log(IO.WriteHex(idef.Category, 4));
                            break;
                    }
                    break;
                    #endregion
                case "id-id":
                    Game.Identify(idef.Type);
                    break;
                case "id-p":
                    Game.UI.Log(idef.WriteItemDefinition().ToString());
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
            Actor a = World.Level.ActorOnTile(WmCursor);

            if (a == null)
            {
                Game.UI.Log("No actor on tile");
                return true;
            }

            switch (cmd)
            {
                case "ai-hurt":
                    DamageSource ds = new DamageSource
                    {
                        Damage = Util.Roll(args[0]),
                        AttackType = AttackType.Magic,
                        DamageType = DamageType.Physical,
                        Source = null,
                        Target = a
                    };
                    a.Damage(ds);
                    break;
                case "ai-pd":
                case "ai-pdef":
                    #region pad
                    Game.UI.Log(
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
            List<Item> items = World.Level.ItemsOnTile(WmCursor);

            if (items.Count <= 0) {
                Game.UI.Log("No item on tile.");
                return true;
            }

            switch (cmd) {
                case "ii-setmod":
                    foreach (Item it in items)
                        it.Mod = IO.ReadHex(args[0]);
                    break;
                case "ii-p":
                case "ii-print":
                    #region pi
                    foreach(Item it in World.Level.ItemsOnTile(WmCursor))
                        Game.UI.Log(
                            it.WriteItem().ToString()
                        );
                    break;
                    #endregion
                case "ii-pd":
                case "ii-pdef":
                    #region pid
                    foreach(Item piditem in World.Level.ItemsOnTile(WmCursor))
                        Game.UI.Log(
                            piditem.Definition.WriteItemDefinition().ToString()
                        );
                    break;
                    #endregion
                case "ii-id":
                    #region id
                    foreach (Item iditem in World.Level.ItemsOnTile(WmCursor))
                        iditem.Identify();
                    break;
                    #endregion
                case "ii-am":
                case "ii-addmod":
                    #region addmod
                    foreach (Item item in World.Level.ItemsOnTile(WmCursor))
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
                    foreach (Item item in World.Level.ItemsOnTile(WmCursor))
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
