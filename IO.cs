using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Bind = ODB.KeyBindings.Bind;

namespace ODB
{
    public enum InputType
    {
        Inventory,
        QuestionPrompt,
        QuestionPromptSingle,
        Targeting,
        PlayerInput,
        Splash, //splash screen or --more-- prompt
        //LH-021214: Should essentially only be used for spells,
        //           where it means "none-targeted", pretty much.
        None
    }

    public class IO
    {
        static KeyboardState _ks, _oks;
        public static bool ShiftState;

        public static InputType IOState = InputType.PlayerInput;
        public static string Question;
        public static string Answer = "";
        public static int AnswerLimit = 20;
        public const int AnswerLimitDefault = 20;

        public static Point Target;
        public static Action QuestionReaction;
        public static Command CurrentCommand;

        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string Numbers = "0123456789";
        public static string Indexes = Lowercase + Uppercase;
        public static string ViKeys = "hjklyubn";
        public static List<char> AcceptedInput = new List<char>();

        public static void SetInput(params object[] args)
        {
            AcceptedInput.Clear();
            foreach (object o in args)
            {
                if (o is char) Add((char)o);
                else if (o is string) Add((string)o);
            }
        }
        public static void Add(char c)
        {
            AcceptedInput.Add(c);
        }
        public static void Add(string s)
        {
            foreach (char c in s)
                AcceptedInput.Add(c);
        }

        public static void Update(bool final)
        {
            ShiftState =
                (_ks.IsKeyDown(Keys.LeftShift) ||
                 _ks.IsKeyDown(Keys.RightShift));

            if (!final) _ks = Keyboard.GetState();
            else _oks = _ks;
        }

        public static bool KeyDown(Keys k)
        {
            return _ks.IsKeyDown(k);
        }
        public static bool KeyPressed(Keys k)
        {
            return _ks.IsKeyDown(k) && !_oks.IsKeyDown(k);
        }

        private static void SubmitAnswer()
        {
            //LH-011214: Note! We switch IOState /FIRST/, because some questions
            //           are going to generate new ones.
            //           Changing the IOState to PlayerInput after would then
            //           suppress those questions.
            //           In reality, maybe we should let the reactions
            //           themselves switch IOState..? Might be too clumsy and
            //           repetetive though, since most questions do /not/ chain.

            //LH-171214: Might want to have the PlayerInput-state in GameState
            //           instead. QP/QPS/Target makes sense to have here, I
            //           guess though.
            IOState = InputType.PlayerInput;
            if (CurrentCommand != null)
            {
                CurrentCommand.Answer = Answer;
                CurrentCommand.Target = Target;
            }

            //LH-171214: If we're using IO.Answer just for the text itself,
            //           we might not actually have a reaction set up.
            if (QuestionReaction != null)
                QuestionReaction();
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

                if(Answer.Length < AnswerLimit)
                    Answer += c;

                if (IOState == InputType.QuestionPromptSingle)
                    SubmitAnswer();
            }

            if (KeyPressed(Keys.Back))
                if (Answer.Length > 0)
                    Answer = Answer.Substring(0, Answer.Length - 1);

            if (!KeyPressed(Keys.Enter)) return;
            if (Answer.Length <= 0) return;

            if (!Game.WizMode)
                SubmitAnswer();
            else
            {
                Wizard.WmHistory.Add(Answer);
                Wizard.WmCommand(Answer);
                Wizard.WmScrollback = 0;
            }
        }

        public static void AskPlayer(
            string question,
            InputType type,
            Action reaction
        ) {
            Answer = "";
            IOState = type;
            Question = question;
            QuestionReaction = reaction;

            if(type == InputType.Targeting)
                Target = Game.Player.xy;
        }

        public static void TargetInput()
        {
            Point offset = new Point(0, 0);

            if (KeyBindings.Pressed(Bind.NorthWest)) offset.Nudge(-1, -1);
            if (KeyBindings.Pressed(Bind.North)) offset.Nudge(0, -1);
            if (KeyBindings.Pressed(Bind.NorthEast)) offset.Nudge(1, -1);
            if (KeyBindings.Pressed(Bind.West)) offset.Nudge(-1, 0);
            if (KeyBindings.Pressed(Bind.East)) offset.Nudge(1, 0);
            if (KeyBindings.Pressed(Bind.SouthWest)) offset.Nudge(-1, 1);
            if (KeyBindings.Pressed(Bind.South)) offset.Nudge(0, 1);
            if (KeyBindings.Pressed(Bind.SouthEast)) offset.Nudge(1, 1);

            Target.Nudge(offset);

            if (KeyBindings.Pressed(Bind.Target_Accept))
                SubmitAnswer();
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
