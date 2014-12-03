using System;
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
        Inventory,
        QuestionPrompt,
        QuestionPromptSingle,
        Targeting,
        PlayerInput,
        //LH-021214: Should essentially only be used for spells,
        //           where it means "none-targeted", pretty much.
        None
    }

    public class IO
    {
        public static Game1 Game;

        static KeyboardState _ks, _oks;
        public static bool Shift;

        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string Indexes = Lowercase + Uppercase;
        public static string ViKeys = "hjklyubn";
        public static List<char> AcceptedInput = new List<char>();

        public enum Key
        {
            North, South, West, East,
            NorthEast, NorthWest,
            SouthEast, SouthWest,
            Enter
        }

        public static Dictionary<Key, List<Keys>> KeyBindings =
            new Dictionary<Key, List<Keys>>
        {
            {Key.North,     new List<Keys>{Keys.Up, Keys.NumPad8, Keys.K}},
            {Key.South,     new List<Keys>{Keys.Down, Keys.NumPad2, Keys.J}},
            {Key.West,      new List<Keys>{Keys.Left, Keys.NumPad4, Keys.H}},
            {Key.East,      new List<Keys>{Keys.Right, Keys.NumPad6, Keys.L}},
            {Key.NorthEast, new List<Keys>{Keys.NumPad9, Keys.U}},
            {Key.NorthWest, new List<Keys>{Keys.NumPad7, Keys.Y}},
            {Key.SouthEast, new List<Keys>{Keys.NumPad3, Keys.N}},
            {Key.SouthWest, new List<Keys>{Keys.NumPad1, Keys.B}},
            {Key.Enter,     new List<Keys>{Keys.NumPad5, Keys.Enter}},
        };

        public static void Update(bool final)
        {
            Shift =
                (_ks.IsKeyDown(Keys.LeftShift) ||
                 _ks.IsKeyDown(Keys.RightShift));

            if (IOState != InputType.QuestionPrompt)
                Answer = "";

            if (!final) _ks = Keyboard.GetState();
            else _oks = _ks;
        }

        public static bool KeyPressed(Keys k)
        {
            return _ks.IsKeyDown(k) && !_oks.IsKeyDown(k);
        }

        public static bool KeyPressed(Key binding)
        {
            if (!KeyBindings.ContainsKey(binding))
                throw new ArgumentException();
            return KeyBindings[binding].Any(
                x => _ks.IsKeyDown(x) && !_oks.IsKeyDown(x));
        }

        private static void SubmitAnswer()
        {
            if (IOState == InputType.PlayerInput)
                throw new Exception("Submitted answer in playerinput mode");

            //LH-011214: Note! We switch IOState /FIRST/, because some questions
            //           are going to generate new ones.
            //           Changing the IOState to PlayerInput after would then
            //           suppress those questions.
            //           In reality, maybe we should let the reactions
            //           themselves switch IOState..? Might be too clumsy and
            //           repetetive though, since most questions do /not/ chain.

            switch (IOState)
            {
                case InputType.QuestionPrompt:
                case InputType.QuestionPromptSingle:
                    Game.QpAnswerStack.Push(Answer);
                    IOState = InputType.PlayerInput;
                    Game.QuestionReaction();
                    break;
                case InputType.Targeting:
                    IOState = InputType.PlayerInput;
                    Game.QuestionReaction();
                    break;
            }
        }

        public static void QuestionPromptInput()
        {
            //check every available key
            for (int i = 0; i < Enum.GetNames(typeof(Keys)).Length; i++)
            {
                if (!KeyPressed((Keys)i)) continue;

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

                if (i >= (int)Keys.A && i <= (int)Keys.Z)
                {
                    //nudge it so we get either A or a, dep. on shiftstate
                    c = (char)(i + (Shift ? 0 : 32));
                }

                if (!AcceptedInput.Contains(c)) continue;

                Answer += c;

                if (IOState == InputType.QuestionPromptSingle)
                    SubmitAnswer();
            }

            if (KeyPressed(Keys.Back))
                if (Answer.Length > 0)
                    Answer = Answer.Substring(0, Answer.Length - 1);

            if (!KeyPressed(Keys.Enter)) return;

            if (!Game.WizMode)
                SubmitAnswer();
            else
            {
                Wizard.WmHistory.Add(Answer);
                Wizard.WmCommand(Answer);
            }
        }

        public static InputType IOState = InputType.PlayerInput;
        public static string Question;
        public static string Answer;

        public static void AskPlayer(
            string question,
            InputType type,
            Action reaction
        ) {
            Answer = "";
            IOState = type;
            Question = question;
            Game.QuestionReaction = reaction;
            if(type == InputType.Targeting)
                Game.Target = Game.Player.xy;
        }

        public static void TargetInput()
        {
            Point offset = new Point(0, 0);

            if (KeyPressed(Keys.NumPad8)) offset.Nudge(0, -1);
            if (KeyPressed(Keys.NumPad9)) offset.Nudge(1, -1);
            if (KeyPressed(Keys.NumPad6)) offset.Nudge(1, 0);
            if (KeyPressed(Keys.NumPad3)) offset.Nudge(1, 1);
            if (KeyPressed(Keys.NumPad2)) offset.Nudge(0, 1);
            if (KeyPressed(Keys.NumPad1)) offset.Nudge(-1, 1);
            if (KeyPressed(Keys.NumPad4)) offset.Nudge(-1, 0);
            if (KeyPressed(Keys.NumPad7)) offset.Nudge(-1, -1);

            Game.Target.Nudge(offset);

            if (
                KeyPressed(Keys.NumPad5) ||
                KeyPressed(Keys.OemPeriod) ||
                KeyPressed(Keys.Enter)
            ) {
                SubmitAnswer();
            }
        }

        //might want to consider ejecting either the stuff above or below
        //into its own place
        //which might seem a bit silly since I just restructured shit
        //but even though they're both sort of IO, they're not really,
        //like, the same kind? Could put below into file or save or something

        #region File IO
        public static void Save()
        {
            Stream stream = new Stream();
            stream.Write(Game.Levels.Count, 2);
            for (int i = 0; i < Game.Levels.Count; i++)
            {
                if (Game.Levels[i].WorldActors.Contains(Game.Player))
                    stream.Write(i, 2);
                Game.Levels[i].WriteLevelSave("Save/level" + i + ".sv");
            }

            //okay, so I really don't think anyone's going to hit
            //gametick 0xFFFFFFFF, that'd be ludicrous.
            //but 0xFFFF might be hit, and 0xFFFFF looks ugly.
            stream.Write(Game.GameTick, 8);
            stream.Write(Game.Seed, 8);

            foreach (int ided in ItemDefinition.IdentifiedDefs)
            {
                stream.Write(ided, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            WriteToFile("Save/game.sv", stream.ToString());
        }
        public static void Load()
        {
            Stream stream = new Stream(ReadFromFile("Save/game.sv"));
            int levels = stream.ReadHex(2);
            int playerLocation = stream.ReadHex(2);

            if (Game.Levels != null)
            {
                for (int i = 0; i < Game.Levels.Count; i++)
                    Game.Levels[i] = null;
                Game.Levels.Clear();
            } else Game.Levels = new List<Level>();

            for (int i = 0; i < levels; i++)
                Game.Levels.Add(new Level("Save/level" + i + ".sv"));

            Game.GameTick = stream.ReadHex(8);
            Game.Seed = stream.ReadHex(8);

            string identifieds = stream.ReadString();
            foreach (string ided in identifieds.Split(',')
                .Where(ided => ided != ""))
            {
                ItemDefinition.IdentifiedDefs.Add(ReadHex(ided));
            }

            Game.Level = Game.Levels[playerLocation];
            Game.SetupBrains();
            Game.Containers = new Dictionary<int, List<Item>>();
        }

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
            }
            catch (UnauthorizedAccessException)
            {
                Game.Log(
                    "~ERROR~: Could not write to file " +
                    cwd + "/" + path + " (Unauthorized access)."
                );
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

            //LH-021214: Notice! Stripping tabs out of the file, atleast for
            //           now, since the primary use of this is reading data
            //           files from disk, and it's a whole lot more human
            //           readable if we're allowed to use tabs in it, so
            //           stripping those to make sure we don't mess with the
            //           actual content.
            content = content.Replace("\t", "");

            return content;
        }

        public static string WriteItemDefinitionsToFile(string path)
        {
            string output = "";
            //todo: probably a better way to do this.
            //      saving/loading is sort of assumed to be pretty slow anyways
            //      so it's not a huge deal. this is actually not even slow atm.
            for (int i = 0; i < 0xFFFF; i++)
            {
                if (ItemDefinition.ItemDefinitions[i] == null) continue;

                output +=
                    ItemDefinition.ItemDefinitions[i].WriteItemDefinition();
                output += "##";
            }
            WriteToFile(path, output);
            return output;
        }
        public static void ReadItemDefinitionsFromFile(string path)
        {
            while (ItemDefinition.ItemDefinitions[
                gObjectDefinition.TypeCounter
            ] != null)
                gObjectDefinition.TypeCounter++;

            string content = ReadFromFile(path);
            List<string> definitions = content.Split(
                new[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            definitions.ForEach(definition => new ItemDefinition(definition));
        }

        public static string WriteActorDefinitionsToFile(string path)
        {
            string output = "";
            for (int i = 0; i < 0xFFFF; i++)
            {
                if (ActorDefinition.ActorDefinitions[i] == null) continue;

                output +=
                    ActorDefinition.ActorDefinitions[i].
                        WriteActorDefinition();
                output += "##";
            }
            WriteToFile(path, output);
            return output;
        }
        public static void ReadActorDefinitionsFromFile(string path)
        {
            ActorDefinition.ActorDefinitions = new ActorDefinition[0xFFFF];

            string content = ReadFromFile(path);
            List<string> definitions = content.Split(
                new[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            definitions.ForEach(definition => new ActorDefinition(definition));
        }

        public static Stream WriteTileDefinitionsToFile(string path)
        {
            Stream stream = new Stream();
            for (int i = 0; i < 0xFFFF; i++)
            {
                if (TileDefinition.Definitions[i] == null) continue;
                stream.Write(
                    TileDefinition.Definitions[i].WriteTileDefinition()
                        .ToString(), false
                    );
                stream.Write("##", false);
            }
            WriteToFile(path, stream.ToString());
            return stream;
        }
        public static void ReadTileDefinitionsFromFile(string path)
        {
            string content = ReadFromFile(path);
            List<string> definitions = content.Split(
                new[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            definitions.ForEach(definition => new TileDefinition(definition));
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
        public static string WriteHex(int i, int len)
        {
            string format = "{0:X"+len+"}";
            return String.Format(format, i);
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
        public static string Write(Color? c)
        {
            string s = "";
            if (c.HasValue) Write(c.Value);
            else s += "XXXXXX";
            return s;
        }
        public static Color? ReadNullableColor(string s)
        {
            if (s.Contains("X") || s.Contains("x"))
                return null;
            return ReadColor(s);
        }
        public static string Write(Color c)
        {
            string s = "";
            s += String.Format("{0:X2}", c.R);
            s += String.Format("{0:X2}", c.G);
            s += String.Format("{0:X2}", c.B);
            return s;
        }
        public static Color ReadColor(string s)
        {
            Color c = new Color(
                Int32.Parse(s.Substring(0, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(2, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(4, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            return c;
        }

        #endregion
    }
}
