using System;
using System.Linq;
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
        public static bool ShiftState;

        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string Indexes = Lowercase + Uppercase;
        public static string ViKeys = "hjklyubn";
        public static List<char> AcceptedInput = new List<char>();

        //so we can map keys requiring certain shift-status
        public struct Keybind
        {
            public bool Shift;
            public Keys Kb;
        }

        public enum Input
        {
            North, South, West, East,
            NorthEast, NorthWest,
            SouthEast, SouthWest,
            Enter
        }

        public static Dictionary<Input, Keybind[]> KeyBindings =
            new Dictionary<Input, Keybind[]>
        {
            {Input.North, new[] {
                new Keybind{Kb = Keys.Up, Shift = false},
                new Keybind{Kb = Keys.NumPad8, Shift = false},
                new Keybind{Kb = Keys.K, Shift = false}}},
            {Input.South, new[] {
                new Keybind{Kb = Keys.Down, Shift = false},
                new Keybind{Kb = Keys.NumPad2, Shift = false},
                new Keybind{Kb = Keys.J, Shift = false}}},
            {Input.West, new[] {
                new Keybind{Kb = Keys.Left, Shift = false},
                new Keybind{Kb = Keys.NumPad4, Shift = false},
                new Keybind{Kb = Keys.H, Shift = false}}},
            {Input.East, new[] {
                new Keybind{Kb = Keys.Right, Shift = false},
                new Keybind{Kb = Keys.NumPad6, Shift = false},
                new Keybind{Kb = Keys.L, Shift = false}}},
            {Input.NorthEast, new[] {
                new Keybind{Kb = Keys.NumPad9, Shift = false},
                new Keybind{Kb = Keys.U, Shift = false}}},
            {Input.NorthWest, new[] {
                new Keybind{Kb = Keys.NumPad7, Shift = false},
                new Keybind{Kb = Keys.Y, Shift = false}}},
            {Input.SouthEast, new[] {
                new Keybind{Kb = Keys.NumPad3, Shift = false},
                new Keybind{Kb = Keys.N, Shift = false}}},
            {Input.SouthWest, new[] {
                new Keybind{Kb = Keys.NumPad1, Shift = false},
                new Keybind{Kb = Keys.B, Shift = false}}},
            {Input.Enter, new[] {
                new Keybind{Kb = Keys.NumPad5, Shift = false},
                new Keybind{Kb = Keys.OemPeriod, Shift = false},
                new Keybind{Kb = Keys.Enter, Shift = false}}},
        };

        public static void Update(bool final)
        {
            ShiftState =
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
        public static bool KeyPressed(Input binding)
        {
            if (!KeyBindings.ContainsKey(binding))
                throw new ArgumentException();
            return KeyBindings[binding]
                .Where(x => x.Shift == ShiftState)
                .Any(x => _ks.IsKeyDown(x.Kb) && !_oks.IsKeyDown(x.Kb))
            ;
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
                    c = (char)(i + (ShiftState ? 0 : 32));
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
                Wizard.WmScrollback = 0;
            }
        }

        public static InputType IOState = InputType.PlayerInput;
        //todo: should keep info about source as well
        //      since the item is sometime removed
        //      -actor.ID should be actor inventory
        public static Item UsedItem;
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

            if (KeyPressed(Input.NorthWest)) offset.Nudge(-1, -1);
            if (KeyPressed(Input.North)) offset.Nudge(0, -1);
            if (KeyPressed(Input.NorthEast)) offset.Nudge(1, -1);
            if (KeyPressed(Input.West)) offset.Nudge(-1, 0);
            if (KeyPressed(Input.East)) offset.Nudge(1, 0);
            if (KeyPressed(Input.SouthWest)) offset.Nudge(-1, 1);
            if (KeyPressed(Input.South)) offset.Nudge(0, 1);
            if (KeyPressed(Input.SouthEast)) offset.Nudge(1, 1);

            Game.Target.Nudge(offset);

            if (KeyPressed(Input.Enter)) {
                SubmitAnswer();
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
    }
}
