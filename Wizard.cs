using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class Wizard
    {
        public static Game1 Game;
        public static Point wmCursor;
        public static List<string> wmHistory;
        static int wmScrollback;

        public static void wmInput()
        {
            //wizmode terminal

            bool scrolled = false;
            if (IO.KeyPressed(Keys.Up)) { wmScrollback++; scrolled = true; }
            if (IO.KeyPressed(Keys.Down)) { wmScrollback--; scrolled = true; }

            if (scrolled)
            {
                if (wmScrollback < 0) wmScrollback = 0;
                if (wmScrollback > wmHistory.Count)
                    wmScrollback = wmHistory.Count;
                if (wmScrollback > 0)
                    IO.Answer = wmHistory[wmHistory.Count - wmScrollback];
                else IO.Answer = "";
            }

            bool b = false;
            if (IO.KeyPressed(Keys.NumPad8))
                { wmCursor.Nudge(0, -1); b = true; }
            if (IO.KeyPressed(Keys.NumPad9)) {
                wmCursor.Nudge(1, -1); b = true; }
            if (IO.KeyPressed(Keys.NumPad6)) {
                wmCursor.Nudge(1, 0); b = true; }
            if (IO.KeyPressed(Keys.NumPad3)) {
                wmCursor.Nudge(1, 1); b = true; }
            if (IO.KeyPressed(Keys.NumPad2)) {
                wmCursor.Nudge(0, 1); b = true; }
            if (IO.KeyPressed(Keys.NumPad1)) {
                wmCursor.Nudge(-1, 1); b = true; }
            if (IO.KeyPressed(Keys.NumPad4)) {
                wmCursor.Nudge(-1, 0); b = true; }
            if (IO.KeyPressed(Keys.NumPad7)) {
                wmCursor.Nudge(-1, -1); b = true; }

            if (IO.KeyPressed(Keys.OemPeriod) && IO.shift)
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

            if (IO.KeyPressed(Keys.OemComma) && IO.shift)
            {
                if (Game.Levels.IndexOf(Game.Level) > 0)
                    Game.Level = Game.Levels
                        [Game.Levels.IndexOf(Game.Level) - 1];
            }

            if (b) return; //so we don't get numbers in the prompt when we
            //try to target stuff

            IO.IOState = InputType.QuestionPrompt;

            IO.AcceptedInput.Clear();
            foreach (char c in IO.indexes) IO.AcceptedInput.Add(c); //letters
            IO.AcceptedInput.Add(' ');
            for (int i = 48; i < 58; i++) IO.AcceptedInput.Add((char)i); //nums
            if (IO.KeyPressed(Keys.OemComma)) IO.Answer += "~"; //arg. delimiter
            if (IO.KeyPressed(Keys.OemQuestion)) //shift-. to repeat
            {
                if (wmHistory.Count > 0 && IO.Answer == "")
                {
                    wmCommand(wmHistory[wmHistory.Count - 1]);
                    wmHistory.Add(wmHistory[wmHistory.Count - 1]);
                }
            }
            else if (IO.KeyPressed(Keys.OemPeriod)) IO.Answer += ".";
            if (IO.KeyPressed(Keys.OemSemicolon)) IO.Answer += ";";
            if (IO.KeyPressed(Keys.OemBackslash)) IO.Answer += "/";
            if (IO.KeyPressed(Keys.OemMinus)) IO.Answer += "-";

            IO.QuestionPromptInput();
        }

        public static void wmCommand(string s)
        {
            string[] split = s.Split('/');
            string cmd = split[0];

            string log = " > " + cmd + "/";

            string[] args = null;
            if (split.Length > 1)
            {
                args = split[1].Split('~');

                for (int i = 0; i < args.Length; i++)
                {
                    log += args[i] + "~";
                }
                log = log.Substring(0, log.Length - 1) + "";
            } else log += "";

            Game.Log(log);

            bool realcmd = false;

            #region parsecmds
            if(cmd.Contains('-'))
                switch (cmd.Substring(0, cmd.IndexOf('-')))
                {
                    case "ad":
                        realcmd |= ADefCommands(cmd, args);
                        break;
                    case "id":
                        realcmd |= IDefCommands(cmd, args);
                        break;
                    case "ai":
                        realcmd |= ActorCommands(cmd, args);
                        break;
                    case "ii":
                        realcmd |= ItemCommands(cmd, args);
                        break;
                    case "lv":
                        realcmd |= LevelCommands(cmd, args);
                        break;
                }

            if (!realcmd)
            {
                realcmd = true;
                switch (cmd)
                {
                    case "sda":
                    case "spawndummyactor":
                        #region sda
                        ActorDefinition adef = new ActorDefinition(
                            null, Color.White, "X", "DUMMY",
                            0, 0, 0, 100, null, null, false
                        );
                        Actor dummy = new Actor(
                            Wizard.wmCursor,
                            adef
                        );
                        Game.Level.WorldActors.Add(dummy);
                        break;
                        #endregion
                    case "sdi":
                    case "spawndummyitem":
                        #region sdi
                        ItemDefinition idef = new ItemDefinition(
                            null, Color.White, "X", "DUMMY"
                        );
                        Item dummyitem = new Item(
                            Wizard.wmCursor,
                            idef
                        );
                        Game.Level.WorldItems.Add(dummyitem);
                        break;
                        #endregion

                    case "sa":
                    case "spawnactor":
                        #region spawnactor
                        Actor act = new Actor(
                            Wizard.wmCursor,
                            Util.ADefByName(args[0])
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
                            Wizard.wmCursor,
                            Util.IDefByName(args[0]),
                            IO.ReadHex(args[1])
                        );
                        Game.Level.AllItems.Add(it);
                        Game.Level.WorldItems.Add(it);
                        break;
                        #endregion

                    case "da":
                    case "defa":
                    case "defactor":
                    case "defineactor":
                        #region defactor
                        ActorDefinition newadef = new ActorDefinition(args[0]);
                        break;
                        #endregion
                    case "saveadefs":
                    case "sad":
                        #region sad
                        IO.WriteActorDefinitionsToFile("Data/" + args[0]);
                        break;
                        #endregion

                    case "di":
                    case "defi":
                    case "defitem":
                    case "defineitem":
                        #region defitem
                        ItemDefinition newidef = new ItemDefinition(args[0]);
                        break;
                        #endregion
                    case "saveidefs":
                    case "sid":
                        IO.WriteItemDefinitionsToFile("Data/" + args[0]);
                        break;

                    case "sp":
                    case "setplayer":
                        #region setplayer
                        Game.player = Game.Level.ActorOnTile(
                            new Point(wmCursor.x, wmCursor.y));
                        break;
                        #endregion
                    //di/da are untested atm
                    case "cast":
                        #region cast
                        Spell.Spells[IO.ReadHex(args[0])].Cast(
                            Game.player,
                            wmCursor
                        ).Move();
                        break;
                        #endregion
                    case "teleport":
                    case "tp":
                        Game.player.xy = wmCursor;
                        break;
                    case "printseed":
                    case "prints":
                    case "ps":
                        Game.Log(Game.Seed + "");
                        break;
                    case "save":
                        IO.Save();
                        break;
                    case "load":
                        IO.Load();
                        break;

                    default: realcmd = false; break;
                }
            }

            if (!realcmd) Game.Log("No such command: " + cmd);
            #endregion

            IO.Answer = "";
        }

        static bool LevelCommands(string cmd, string[] args)
        {
            switch (cmd)
            {
                case "lv-new":
                    #region new
                    bool hadplayer = false;
                    Game.Level.LevelSize.x = IO.ReadHex(args[0]);
                    Game.Level.LevelSize.y = IO.ReadHex(args[1]);
                    if (Game.Level.WorldActors.Contains(Game.player))
                        hadplayer = true;
                    Game.Level.Clear();
                    if (hadplayer) Game.Level.WorldActors.Add(Game.player);
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
                    if (Game.Level.Map[wmCursor.x, wmCursor.y] != null)
                    {
                        Tile t = Game.Level.Map[wmCursor.x, wmCursor.y];
                        t.Door = (Door)((((int)t.Door) + 1) % 3);
                    }
                    break;
                    #endregion
                case "lv-stairs":
                    #region stairs
                    if (Game.Level.Map[wmCursor.x, wmCursor.y] != null)
                    {
                        Tile t = Game.Level.Map[wmCursor.x, wmCursor.y];
                        t.Stairs = (Stairs)((((int)t.Stairs) + 1) % 3);
                    }
                    break;
                    #endregion
                case "lv-deltile":
                    #region deltile
                    Game.Level.Map[wmCursor.x, wmCursor.y] = null;
                    break;
                    #endregion
                case "lv-delroom":
                    #region delroom
                    List<Room> rooms =
                        Util.GetRooms(new Point(wmCursor.x, wmCursor.y));
                    Game.Level.Rooms.RemoveAll(x => rooms.Contains(x));
                    break;
                    #endregion
                case "lv-settile":
                    #region settile
                    Game.Level.Map[wmCursor.x, wmCursor.y].Definition =
                        Util.TDef(IO.ReadHex(args[0]));
                    break;
                    #endregion
                case "lv-cr":
                case "createroom":
                    #region createroom
                    if (args.Length >= 5)
                    {
                        Game.Level.CreateRoom(
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
                            Util.TDef(IO.ReadHex(args[4])),
                            args.Length >= 6 ?
                                Util.TDef(IO.ReadHex(args[5])) :
                                null
                        );
                        Game.Level.CalculateRoomLinks();
                    }
                    break;
                    #endregion
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
                    break;
                    #endregion
                case "lv-rl":
                case "lv-rmlvl":
                    #region rmlvl
                    if (Game.Levels.Count > 0)
                    {
                        Game.Levels.Remove(Game.Level);
                        if (Game.Level.WorldActors.Contains(Game.player))
                            Game.Levels[0].WorldActors.Add(Game.player);
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
                    Game.Level.Map[wmCursor.x, wmCursor.y].Engraving = args[0];
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

        static bool ADefCommands(string cmd, string[] args)
        {
            ActorDefinition adef = Game.Level.ActorOnTile(wmCursor).Definition;

            if (adef == null)
            {
                Game.Log("No actor on tile.");
                return true;
            }

            switch (cmd)
            {
                case "ad-p":
                    Game.Log(adef.WriteActorDefinition().ToString());
                    break;
                case "ad-bg":
                    adef.bg = IO.ReadNullableColor(args[0]);
                    break;
                case "ad-fg":
                    adef.fg = IO.ReadColor(args[0]);
                    break;
                case "ad-tile":
                    adef.tile = args[0];
                    break;
                case "ad-name":
                    adef.name = args[0];
                    break;
                case "ad-named":
                    adef.Named = IO.ReadBool(args[0]);
                    break;
                case "ad-stat":
                    adef.Set(
                        (Stat)IO.ReadHex(args[0]),
                        IO.ReadHex(args[1])
                    );
                    break;
                case "ad-pointmax":
                    if (!IO.ReadBool(args[0]))
                        adef.hpMax = IO.ReadHex(args[1]);
                    else
                        adef.mpMax = IO.ReadHex(args[1]);
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
                    Actor rmactor = Game.Level.ActorOnTile(wmCursor);
                    for (int i = 0; i < adef.SpawnIntrinsics.Count; i++) {
                        Mod si = adef.SpawnIntrinsics[i];
                        if (si.Type == (ModType)IO.ReadHex(args[0]))
                        {
                            adef.SpawnIntrinsics.RemoveAt(i);
                            break;
                        }
                    }
                    break;
                default: return false;
            }
            return true;
        }

        static bool IDefCommands(string cmd, string[] args)
        {
            ItemDefinition idef = Game.Level.ItemsOnTile(wmCursor)
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
                        case "bg": Game.Log(IO.Write(idef.bg)); break;
                        case "fg": Game.Log(IO.Write(idef.bg)); break;
                        case "tile": Game.Log(idef.tile); break;
                        case "name": Game.Log(idef.name); break;
                        case "dmg": Game.Log(idef.Damage); break;
                        case "ac": Game.Log(idef.AC+""); break;
                        case "stacking": Game.Log(idef.stacking+""); break;
                        case "equip":
                            string s = "";
                            foreach (DollSlot ds in idef.equipSlots)
                                s += IO.WriteHex((int)ds, 2) + ", ";
                            Game.Log(s);
                            break;
                        case "ranged": Game.Log(idef.Ranged+""); break;
                        case "ammo":
                            string ss = "";
                            foreach (int id in idef.AmmoTypes)
                                ss += IO.WriteHex((int)id, 4) + ", ";
                            Game.Log(ss);
                            break;
                        case "rdmg": Game.Log(idef.RangedDamage); break;
                        case "use": Game.Log(IO.WriteHex(idef.UseEffect, 4));
                            break;
                        case "cat": Game.Log(IO.WriteHex(idef.Category, 4));
                            break;
                        case "nut": Game.Log(IO.WriteHex(idef.Nutrition, 4));
                            break;
                    }
                    break;
                    #endregion
                case "id-id":
                    ItemDefinition.IdentifiedDefs.Add(idef.type);
                    break;
                case "id-p":
                    Game.Log(idef.WriteItemDefinition().ToString());
                    break;
                case "id-bg":
                    idef.bg = IO.ReadNullableColor(args[0]);
                    break;
                case "id-fg":
                    idef.fg = IO.ReadColor(args[0]);
                    break;
                case "id-tile":
                    idef.tile = args[0];
                    break;
                case "id-name":
                    idef.name = args[0];
                    break;
                case "id-dmg":
                case "id-damage":
                    idef.Damage = args[0];
                    break;
                case "id-ac":
                    idef.AC = IO.ReadHex(args[0]);
                    break;
                case "id-stack":
                case "id-stacking":
                    idef.stacking = IO.ReadBool(args[0]);
                    break;
                case "id-addequip":
                    idef.equipSlots.Add((DollSlot)IO.ReadHex(args[0]));
                    break;
                case "id-remequip":
                case "id-delequip":
                    idef.equipSlots.Remove((DollSlot)IO.ReadHex(args[0]));
                    break;
                case "id-ranged":
                    idef.Ranged = IO.ReadBool(args[0]);
                    break;
                case "id-addammo":
                    idef.AmmoTypes.Add(IO.ReadHex(args[0]));
                    break;
                case "id-remammo":
                case "id-delammo":
                    idef.AmmoTypes.Remove(IO.ReadHex(args[0]));
                    break;
                case "id-rdmg":
                case "id-rangeddmg":
                case "id-rangeddamage":
                    idef.RangedDamage = args[0];
                    break;
                case "id-use":
                case "id-useeffect":
                    idef.UseEffect = IO.ReadHex(args[0]);
                    break;
                case "id-cat":
                case "id-category":
                    idef.Category = IO.ReadHex(args[0]);
                    break;
                case "id-n":
                case "id-nut":
                case "id-nutrition":
                case "id-food":
                    #region setnut
                    foreach (Item snitem in Game.Level.ItemsOnTile(wmCursor))
                        snitem.Definition.Nutrition = IO.ReadHex(args[0]);
                    break;
                    #endregion
                default: return false;
            }
            return true;
        }

        static bool ActorCommands(string cmd, string[] args)
        {
            Actor a = Game.Level.ActorOnTile(wmCursor);

            if (a == null)
            {
                Game.Log("No actor on tile");
                return true;
            }

            switch (cmd)
            {
                case "ai-p":
                case "ai-print":
                    #region pa
                    Game.Log(
                        Game.Level.ActorOnTile(wmCursor).WriteActor().ToString()
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
                        if (a.Intrinsics[i].Type ==
                            (ModType)IO.ReadHex(args[0])
                        ) {
                            a.Intrinsics.RemoveAt(i);
                            break;
                        }
                    }
                    break;
                    #endregion
                case "ai-addte": //add ticker
                    #region addte
                    a.TickingEffects.Add(new TickingEffect(
                            a, TickingEffectDefinition.
                                Definitions[IO.ReadHex(args[0])]
                        )
                    );
                    break;
                    #endregion
                case "ai-awake":
                    a.Awake = IO.ReadBool(args[0]);
                    break;
                case "ai-ale":
                case "ai-addle":
                    a.LastingEffects.Add(
                        new LastingEffect(
                            (StatusType)IO.ReadHex(args[0]),
                            IO.ReadHex(args[1])
                        )
                    );
                    break;
                default: return false;
            }
            return true;
        }

        static bool ItemCommands(string cmd, string[] args)
        {
            if (Game.Level.ItemsOnTile(wmCursor).Count <= 0) {
                Game.Log("No item on tile.");
                return true;
            }

            switch (cmd) {
                case "ii-p":
                case "ii-print":
                    #region pi
                    foreach(Item it in Game.Level.ItemsOnTile(Wizard.wmCursor))
                        Game.Log(
                            it.WriteItem().ToString()
                        );
                    break;
                    #endregion
                case "ii-pd":
                case "ii-pdef":
                    #region pid
                    foreach(Item piditem in Game.Level.ItemsOnTile(wmCursor))
                        Game.Log(
                            piditem.Definition.WriteItemDefinition().ToString()
                        );
                    break;
                    #endregion
                case "ii-id":
                    #region id
                    foreach (Item iditem in Game.Level.ItemsOnTile(wmCursor))
                        iditem.Identify();
                    break;
                    #endregion
                case "ii-unid":
                    #region unid
                    foreach (Item iditem in Game.Level.ItemsOnTile(wmCursor))
                        ItemDefinition.IdentifiedDefs.Remove(iditem.type);
                    break;
                    #endregion
                case "ii-am":
                case "ii-addmod":
                    #region addmod
                    foreach (Item item in Game.Level.ItemsOnTile(wmCursor))
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
                    foreach (Item item in Game.Level.ItemsOnTile(wmCursor))
                    {
                        if (removed) break;
                        for (int i = 0; i < item.Mods.Count; i++)
                        {
                            if (item.Mods[i].Type ==
                                (ModType)IO.ReadHex(args[0]))
                            {
                                item.Mods.RemoveAt(i);
                                removed = true;
                                break;
                            }
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
