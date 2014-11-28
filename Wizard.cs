using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    wmCommand(wmHistory[wmHistory.Count - 1]);
            }
            else if (IO.KeyPressed(Keys.OemPeriod)) IO.Answer += ".";
            if (IO.KeyPressed(Keys.OemSemicolon)) IO.Answer += ";";
            if (IO.KeyPressed(Keys.OemBackslash)) IO.Answer += "/";

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
                    //Game.Log("  > \"" + args[i] + "\"");
                    log += args[i] + "~";
                }
                log = log.Substring(0, log.Length - 1) + "";
            } else log += "";

            Game.Log(log);

            #region parsecmds
            switch (cmd)
            {
                case "n": case "new":
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
                case "mu": case "moveup":
                    #region mu
                    int depth = Game.Levels.IndexOf(Game.Level);
                    if (depth > 0)
                    {
                        Game.Levels.Remove(Game.Level);
                        Game.Levels.Insert(depth - 1, Game.Level);
                    }
                    break;
                    #endregion
                case "d": case "door":
                    #region door
                    if (Game.Level.Map[wmCursor.x, wmCursor.y] != null)
                    {
                        Tile t = Game.Level.Map[wmCursor.x, wmCursor.y];
                        t.Door = (Door)((((int)t.Door)+1) % 3);
                    }
                    break;
                    #endregion
                case "s": case "stairs":
                    #region stairs
                    if (Game.Level.Map[wmCursor.x, wmCursor.y] != null)
                    {
                        Tile t = Game.Level.Map[wmCursor.x, wmCursor.y];
                        t.Stairs = (Stairs)((((int)t.Stairs)+1) % 3);
                    }
                    break;
                    #endregion
                case "dt": case "delt": case "deltile": case "deletetile":
                    #region deltile
                    Game.Level.Map[wmCursor.x, wmCursor.y] = null;
                    break;
                    #endregion
                case "dr": case "delr": case "delroom": case "deleteroom":
                    #region delroom
                    List<Room> rooms =
                        Util.GetRooms(new Point(wmCursor.x, wmCursor.y));
                    Game.Level.Rooms.RemoveAll(x => rooms.Contains(x));
                    break;
                    #endregion
                case "st": case "settile":
                    #region settile
                    Game.Level.Map[wmCursor.x, wmCursor.y].Definition =
                        Util.TDef(IO.ReadHex(args[0]));
                    break;
                    #endregion
                case "cr": case "createroom":
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
                    }
                    break;
                    #endregion
                case "abp": case "addbodypart":
                    #region addbodypart 
                    Game.Level.ActorOnTile(wmCursor).PaperDoll.Add(
                        new BodyPart(
                            (DollSlot)IO.ReadHex(args[0])
                        )
                    );
                    break;
                    #endregion
                case "rbp": case "rembodypart": case "removebodypart":
                    #region remove bp
                    Actor bpactor = Game.Level.ActorOnTile(wmCursor);
                    for (int i = 0; i < bpactor.PaperDoll.Count; i++)
                        if (bpactor.PaperDoll[i].Type ==
                            (DollSlot)IO.ReadHex(args[0]))
                        {
                            bpactor.PaperDoll.RemoveAt(i);
                            break;
                        }
                    break;
                    #endregion
                case "am": case "addmod":
                    #region addmod
                    foreach (Item item in Util.ItemsOnTile(wmCursor))
                    {
                        item.Mods.Add(
                            new Mod(
                                (ModType)IO.ReadHex(args[0]),
                                IO.ReadHex(args[1])
                            )
                        );
                    }
                    if (Game.Level.ActorOnTile(wmCursor) != null)
                    {
                        Game.Level.ActorOnTile(wmCursor).Intrinsics.Add(
                            new Mod(
                                (ModType)IO.ReadHex(args[0]),
                                IO.ReadHex(args[1])
                            )
                        );
                    }
                    break;
                    #endregion
                case "rm": case "remmod": case "removemod":
                    #region remove mod
                    bool removed = false;
                    foreach (Item item in Util.ItemsOnTile(wmCursor))
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
                    if (Game.Level.ActorOnTile(wmCursor) != null)
                    {
                        if (removed) break;
                        Actor rmactor = Game.Level.ActorOnTile(wmCursor);
                        for (int i = 0; i < rmactor.Intrinsics.Count; i++) {
                            if (rmactor.Intrinsics[i].Type ==
                                (ModType)IO.ReadHex(args[0])
                            ) {
                                rmactor.Intrinsics.RemoveAt(i);
                                removed = true;
                                break;
                            }
                        }
                    }
                    break;
                    #endregion
                case "si": case "spawnitem":
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
                case "sa":  case "spawnactor":
                    #region spawnactor
                    Actor act = new Actor(
                        Wizard.wmCursor,
                        Util.ADefByName(args[0])
                    );
                    Game.Level.WorldActors.Add(act);
                    Game.Brains.Add(new Brain(act));
                    break;
                    #endregion
                case "sp": case "setplayer":
                    #region setplayer
                    Game.player = Game.Level.ActorOnTile(
                        new Point(wmCursor.x, wmCursor.y));
                    break;
                    #endregion
                case "savelevel": case "savelvl": case "sl":
                    #region save
                    Game.Level.WriteLevelSave("Save/" + args[0]);
                    break;
                    #endregion
                case "loadlevel": case "loadlvl": case "ll":
                    #region load
                    Game.Level.LoadLevelSave("Save/" + args[0]);
                    Game.SetupBrains();
                    break;
                    #endregion
                case "rl": case "rmlvl": case "rmlevel": case "removelevel":
                    #region rmlvl
                    if (Game.Levels.Count > 0){
                        Game.Levels.Remove(Game.Level);
                        if (Game.Level.WorldActors.Contains(Game.player))
                            Game.Levels[0].WorldActors.Add(Game.player);
                        Game.Level = Game.Levels[0];
                    }
                    break;
                    #endregion
                case "cl": case "countlvl": case "countlevel":
                    Game.Log(Game.Levels.Count+"");
                    break;
                //di/da are untested atm
                case "da": case "defa": case "defactor": case "defineactor":
                    #region defactor
                    ActorDefinition adef = new ActorDefinition(args[0]);
                    break;
                    #endregion
                case "di": case "defi": case "defitem": case "defineitem":
                    #region defitem
                    ItemDefinition idef = new ItemDefinition(args[0]);
                    break;
                    #endregion
                case "printactor": case "pactor": case "pa":
                    #region pa
                    Game.Log(
                        Game.Level.ActorOnTile(wmCursor).WriteActor().ToString()
                    );
                    break;
                    #endregion
                case "printitem": case "pitem": case "pi":
                    #region pa
                    foreach(Item item in Util.ItemsOnTile(Wizard.wmCursor))
                        Game.Log(
                            item.WriteItem().ToString()
                        );
                    break;
                    #endregion
                case "cast":
                    #region cast
                    Spell.Spells[IO.ReadHex(args[0])].Cast(
                        Game.player,
                        wmCursor
                    ).Move();
                    break;
                    #endregion
                case "addte": //add ticker
                    #region addte
                    Actor a = Game.Level.ActorOnTile(wmCursor);
                    a.TickingEffects.Add(new TickingEffect(
                            a, TickingEffectDefinition.
                                Definitions[IO.ReadHex(args[0])]
                        )
                    );
                    break;
                    #endregion
                case "teleport": case "tp":
                    Game.player.xy = wmCursor;
                    break;
                case "identify": case "id":
                    foreach (Item iditem in Util.ItemsOnTile(wmCursor))
                        ItemDefinition.IdentifiedDefs.Add(iditem.type);
                    break;
                case "unidentify": case "uid":
                    foreach (Item iditem in Util.ItemsOnTile(wmCursor))
                        ItemDefinition.IdentifiedDefs.Remove(iditem.type);
                    break;
                case "engrave": case "e":
                    Game.Level.Map[wmCursor.x, wmCursor.y].Engraving = args[0];
                    break;
                case "printseed": case "prints": case "ps":
                    Game.Log(Game.Seed+"");
                    break;
                case "name":
                    Game.Level.Name = args[0];
                    break;
                case "saveadefs": case "sad":
                    IO.WriteActorDefinitionsToFile("Data/actors.def");
                    break;
                case "saveidefs": case "sid":
                    IO.WriteItemDefinitionsToFile("Data/items.def");
                    break;
                case "save":
                    IO.Save();
                    break;
                case "load":
                    IO.Load();
                    break;
                default:
                    Game.Seed++;
                    Game.Log("Unrecognized cmd " + cmd);
                    break;
            }
            #endregion

            IO.Answer = "";
        }

    }
}
