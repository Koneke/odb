using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    internal class KeyBind
    {
        public bool Shift;
        public bool Alt;
        public bool Control;
        public Keys Key;

        public KeyBind(bool shift, bool alt, bool control, Keys key)
        {
            Shift = shift;
            Alt = alt;
            Control = control;
            Key = key;
        }
    }

    class KeyBindings
    {
        public enum Bind
        {
// ReSharper disable InconsistentNaming
            Accept,

            East, West, North, South,
            NorthEast, NorthWest,
            SouthEast, SouthWest,
            Up,
            Down,
            Wait,

            Target_Accept,
            Target_Cancel,

            Dev_ToggleConsole,
            Dev_SaveActors,
            Dev_SaveItems,
            Window_Size,
            Exit,
            Log_Size_Up,
            Log_Size_Down,

            Camera_Up,
            Camera_Down,
            Camera_Left,
            Camera_Right,

            Apply,
            Chant,
            Close,
            Drop,
            Eat,
            Engrave,
            Fire,
            Get,
            Inventory,
            Look,
            Open,
            Quaff,
            Quiver,
            Read,
            Remove,
            Sheath,
            Throw,
            Wear,
            Wield,
            Zap,

            Split,
            Join,
            TakeOut,
            PutInto,
            Inv_Up,
            Inv_Down,
            Inv_Select,
            Inv_Cancel,

            Wm_Scrollback,
            Wm_Scrolldown,
            Wm_East, Wm_West, Wm_North, Wm_South,
            Wm_NorthEast, Wm_NorthWest,
            Wm_SouthEast, Wm_SouthWest,

            Inv_Jump_0, Inv_Jump_1, Inv_Jump_2, Inv_Jump_3, Inv_Jump_4,
            Inv_Jump_5, Inv_Jump_6, Inv_Jump_7, Inv_Jump_8, Inv_Jump_9,
            Inv_Jump_10, Inv_Jump_11, Inv_Jump_12, Inv_Jump_13, Inv_Jump_14,
            Inv_Jump_15, Inv_Jump_16, Inv_Jump_17, Inv_Jump_18, Inv_Jump_19,
            Inv_Jump_20, Inv_Jump_21, Inv_Jump_22, Inv_Jump_23,
            Switch_Font,
            Sleep,
            Sneak
// ReSharper restore InconsistentNaming
        }

        public static bool Pressed(Bind bind)
        {
            if (!Binds.ContainsKey(bind)) return false;

            foreach (KeyBind kb in Binds[bind])
            {
                bool shift =
                    (IO.KeyDown(Keys.LeftShift) ||
                     IO.KeyDown(Keys.RightShift)) == kb.Shift;
                bool alt =
                    (IO.KeyDown(Keys.LeftAlt) ||
                     IO.KeyDown(Keys.RightAlt)) == kb.Alt;
                bool control =
                    (IO.KeyDown(Keys.LeftControl) ||
                     IO.KeyDown(Keys.RightControl)) == kb.Control;

                bool key = (IO.KeyPressed(kb.Key));

                if (shift && alt && control && key) return true;
            }

            return false;
        }

        public static Dictionary<Bind, List<KeyBind>> Binds
            = new Dictionary<Bind,List<KeyBind>>();

        public static void ReadBinds(string s)
        {
            Binds = new Dictionary<Bind,List<KeyBind>>();
            foreach (string bind in s.Split(';'))
            {
                if (bind == "") continue;
                ReadBind(bind);
            }
        }

        public static void ReadBind(string s)
        {
            //strip tabs and spaces to make our file more human friendly
            s = s.Replace("\t", "");
            s = s.Replace(" ", "");

            //Samples:
            //west  = :h, :numpad4, :left
            //wield = :w
            //wear  = s:w

            string bindstring = s.Split('=')[0];
            Bind bind;
            Enum.TryParse(bindstring, true, out bind);

            if (!Binds.ContainsKey(bind))
                Binds.Add(bind, new List<KeyBind>());

            string rest = s.Split('=')[1];
            foreach(string binding in rest.Split(','))
            {
                string modifiers = binding.Split(':')[0];
                string keystring = binding.Split(':')[1];

                bool shift = modifiers.IndexOf('s') != -1;
                bool alt = modifiers.IndexOf('a') != -1;
                bool control = modifiers.IndexOf('c') != -1;

                Keys key;
                Enum.TryParse(keystring, true, out key);

                Binds[bind].Add(new KeyBind(shift, alt, control, key));
            }
        }

        public static Stream WriteBinds()
        {
            Stream stream = new Stream();
            foreach (Bind b in Binds.Keys)
                stream.Write(WriteBind(b));
            stream.Back();
            return stream;
        }
        public static Stream WriteBind(Bind bind)
        {
            Stream stream = new Stream();
            stream.Write(bind.ToString(), false);
            stream.Write("=", false);
            foreach (KeyBind kb in Binds[bind])
            {
                if (kb.Shift) stream.Write("s", false);
                if (kb.Alt) stream.Write("a", false);
                if (kb.Control) stream.Write("c", false);
                stream.Write(":", false);
                stream.Write(kb.Key.ToString(), false);
                stream.Write(",", false);
            }
            stream.Back();
            return stream;
        }
    }
}
