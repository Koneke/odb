﻿using System;
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

            if (b) return; //so we don't get numbers in the prompt when we
            //try to target stuff

            IO.IOState = InputType.QuestionPrompt;

            IO.AcceptedInput.Clear();
            foreach (char c in IO.indexes) IO.AcceptedInput.Add(c); //letters
            IO.AcceptedInput.Add(' ');
            for (int i = 48; i < 58; i++) IO.AcceptedInput.Add((char)i); //nums
            if (IO.KeyPressed(Keys.OemComma)) IO.Answer += "~"; //arg. delimiter
            if (IO.KeyPressed(Keys.OemPeriod) && IO.shift) //shift-. to repeat
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
                    Game.Level = new Level(
                        IO.ReadHex(args[0]),
                        IO.ReadHex(args[1])
                    );
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
                case "si": case "spawnitem":
                    #region spawnitem
                    Item it = new Item(args[0]);
                    Game.Level.AllItems.Add(it);
                    Game.Level.WorldItems.Add(it);
                    break;
                    #endregion
                case "sa":  case "spawnactor":
                    #region spawnactor
                    Actor act = new Actor(args[0]);
                    Game.Level.WorldActors.Add(act);
                    break;
                    #endregion
                case "sp": case "setplayer":
                    #region setplayer
                    Game.player = Game.Level.ActorOnTile(
                        new Point(wmCursor.x, wmCursor.y));
                    break;
                    #endregion
                case "save":
                    #region save
                    Game.Level.WriteLevelSave("Save/" + args[0]);
                    break;
                    #endregion
                case "load":
                    #region load
                    Game.Level = new Level("Save/" + args[0]);
                    Game.SetupBrains();
                    break;
                    #endregion
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
                default:
                    Game.Log("Unrecognized cmd " + cmd);
                    break;
            }
            #endregion

            IO.Answer = "";
        }
    }
}