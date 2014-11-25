﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    public enum InputType
    {
        QuestionPrompt,
        QuestionPromptSingle,
        Targeting,
        PlayerInput
    }

    class IO
    {
        public static Game1 Game;

        static KeyboardState ks, oks;
        public static bool shift;

        public static string lowercase = "abcdefghijklmnopqrstuvwxyz";
        public static string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string indexes = lowercase + uppercase;
        public static List<char> AcceptedInput = new List<char>();

        public static void Update(bool final)
        {
            shift =
                (IO.ks.IsKeyDown(Keys.LeftShift) ||
                IO.ks.IsKeyDown(Keys.RightShift));

            if (IOState != InputType.QuestionPrompt)
                Answer = "";

            if (!final) ks = Keyboard.GetState();
            else oks = ks;
        }

        public static bool KeyPressed(Keys k)
        {
            return ks.IsKeyDown(k) && !oks.IsKeyDown(k);
        }

        public static void QuestionPromptInput()
        {
            //check every available key
            for (int i = 0; i < Enum.GetNames(typeof(Keys)).Length; i++)
            {
                if (KeyPressed((Keys)i))
                {
                    //because sometimes, the key-char mapping isn't botched
                    char c = (char)i;

                    if (i >= (int)Keys.NumPad1 && i <= (int)Keys.NumPad9)
                    {
                        //mapping kp1-9 to chars 0-9
                        //this means that you can't have a question which
                        //treats 0-9 and kp0-9 differently,
                        //but I don't see where that'd be a problem anyways
                        c = (char)(48 + i - Keys.NumPad0);
                    }

                    if (i >= (int)Keys.A && i < (int)Keys.Z)
                    {
                        //nudge it so we get either A or a, dep. on shiftstate
                        c = (char)(i + (shift ? 0 : 32));
                    }

                    if (AcceptedInput.Contains(c))
                    {
                        Answer += c;

                        if (IOState == InputType.QuestionPromptSingle)
                        {
                            IOState = InputType.PlayerInput;
                            Game.qpAnswerStack.Push(Answer);
                            Game.questionReaction(Answer);
                        }
                    }
                }
            }

            if (IO.KeyPressed(Keys.Back))
                if (Answer.Length > 0)
                    Answer = Answer.Substring(0, Answer.Length - 1);

            if (IO.KeyPressed(Keys.Enter))
            {
                IOState = InputType.PlayerInput;
                Game.qpAnswerStack.Push(Answer);
                Game.questionReaction(Answer);
            }
        }

        public static InputType IOState = InputType.PlayerInput;
        public static string Question;
        public static string Answer;

        //prompt question
        public static void AskPlayer(
            string question,
            InputType type,
            Action<string> reaction
        ) {
            Answer = "";
            IOState = type;
            Question = question;
            Game.questionReaction = reaction;
        }

        //targeting question
        public static void AskPlayer(
            string question,
            InputType type,
            Action<Point> reaction
        ) {
            Answer = "";
            IOState = type;
            Question = question;
            Game.targetingReaction = reaction;
            Game.target = Game.player.xy;
        }

        public static void TargetInput()
        {
            Point offset = new Point(0, 0);

            if (IO.KeyPressed(Keys.NumPad8)) offset.Nudge(0, -1);
            if (IO.KeyPressed(Keys.NumPad9)) offset.Nudge(1, -1);
            if (IO.KeyPressed(Keys.NumPad6)) offset.Nudge(1, 0);
            if (IO.KeyPressed(Keys.NumPad3)) offset.Nudge(1, 1);
            if (IO.KeyPressed(Keys.NumPad2)) offset.Nudge(0, 1);
            if (IO.KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
            if (IO.KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
            if (IO.KeyPressed(Keys.NumPad7)) offset.Nudge(-1, -1);

            Game.target.Nudge(offset);

            if (
                IO.KeyPressed(Keys.NumPad5) ||
                IO.KeyPressed(Keys.OemPeriod) ||
                IO.KeyPressed(Keys.Enter)
            ) {
                //Game.targeting = false;
                IO.IOState = InputType.PlayerInput;
                Game.targetingReaction(Game.target);
            }
        }

        //might want to consider ejecting either the stuff above or below
        //into its own place
        //which might seem a bit silly since I just restructured shit
        //but even though they're both sort of IO, they're not really,
        //like, the same kind? Could put below into file or save or something

        #region File IO
        public static void WriteToFile(string path, string content)
        {
            string cwd = Directory.GetCurrentDirectory();
            try
            {
                if (File.Exists(cwd + "/" + path))
                    File.Delete(cwd + "/" + path);
                using (FileStream fs = File.Create(cwd + "/" + path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(content);
                    fs.Write(info, 0, info.Length);
                }
            } catch (Exception ex)
            {
            }
        }
        public static string ReadFromFile(string path)
        {
            string cwd = Directory.GetCurrentDirectory();

            if(!File.Exists(cwd + "/" + path))
                throw new Exception("Trying to load non-existing file.");

            string content = "";

            using(StreamReader reader =
                new StreamReader(cwd + "/" + path, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    content += line;
            }

            return content;
        }

        public static string WriteItemDefinitionsToFile(string path)
        {
            string output = "";
            //for (int i = 0; i < ItemDefinition.TypeCounter; i++)
            for (int i = 0; i < 0xFFFF; i++)
            {
                if (ItemDefinition.ItemDefinitions[i] != null)
                {
                    output +=
                        ItemDefinition.ItemDefinitions[i].WriteItemDefinition();
                    output += "##";
                }
            }
            WriteToFile(path, output);
            return output;
        }
        public static void ReadItemDefinitionsFromFile(string path)
        {
            ItemDefinition.ItemDefinitions = new ItemDefinition[0xFFFF];

            string content = ReadFromFile(path);
            List<string> itemStrings = content.Split(
                new string[]{ "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            foreach (string definition in itemStrings)
            {
                ItemDefinition idef = new ItemDefinition(definition);
            }
        }

        public static string WriteActorDefinitionsToFile(string path)
        {
            string output = "";
            //for (int i = 0; i < ActorDefinition.TypeCounter; i++)
            for (int i = 0; i < 0xFFFF; i++)
            {
                if (ActorDefinition.ActorDefinitions[i] != null)
                {
                    output +=
                        ActorDefinition.ActorDefinitions[i].
                        WriteActorDefinition();
                    output += "##";
                }
            }
            WriteToFile(path, output);
            return output;
        }
        public static void ReadActorDefinitionsFromFile(string path)
        {
            ActorDefinition.ActorDefinitions = new ActorDefinition[0xFFFF];

            string content = ReadFromFile(path);
            List<string> itemStrings = content.Split(
                new string[]{ "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            foreach (string definition in itemStrings)
            {
                ActorDefinition idef = new ActorDefinition(definition);
            }
        }

        public static string Write(Point p)
        {
            string s = p.x + "x" + p.y + ";";
            return s;
        }
        public static Point ReadPoint(string s)
        {
            s = s.Substring(0, s.Length - 1); //strip ;
            Point p = new Point(
                int.Parse(s.Split('x')[0]),
                int.Parse(s.Split('x')[1])
            );
            return p;
        }
        public static string WriteHex(int i, int len) {
            return String.Format("{0:X"+len+"}", i);
        }
        public static int ReadHex(string s) {
            return Int32.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }
        public static string Write(bool b)
        {
            return b ? "1" : "0";
        }
        public static bool ReadBool(string s)
        {
            return s.Substring(0, 1) == "1";
        }
        #endregion
    }
}
