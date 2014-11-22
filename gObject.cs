using System;
using Microsoft.Xna.Framework;

namespace ODB
{
    //definition should hold all data non-specific to the instance
    //instance specific data is e.g. position, instance id, stack-count.
    //definitions should not be saved per level, but are global, so to speak
    //instances, in contrast, is per level rather than global

    public class gObjectDefinition
    {
        //afaik, atm we won't have anything that's /just/
        // a game object, it'll always be an item/actor
        //but in case.
        public static gObjectDefinition[] Definitions =
            new gObjectDefinition[0xFFFF];
        public static int TypeCounter = 0;

        public Color? bg;
        public Color fg;
        public string tile;
        public string name;
        public int type;

        public gObjectDefinition(
            Color? bg, Color fg, string tile, string name
        ) {
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
            this.name = name;
            this.type = TypeCounter++;
            Definitions[this.type] = this;
        }

        public gObjectDefinition(string s)
        {
            ReadGObjectDefinition(s);
        }

        public int ReadGObjectDefinition(string s)
        {
            int read = 0;
            type = IO.ReadHex(s, 4, ref read, read);
            bg = IO.ReadNullableColor(s, ref read, read);
            fg = IO.ReadColor(s, ref read, read);
            tile = s.Substring(read++, 1);
            name = IO.ReadString(s, ref read, read);
            Definitions[this.type] = this;
            return read;
        }

        public string WriteGObjectDefinition()
        {
            string output = "";
            output += IO.WriteHex(type, 4);
            output += IO.Write(bg);
            output += IO.Write(fg);
            output += tile;
            output += IO.Write(name);
            return output;
        }
    }

    public class gObject
    {
        public static Game1 Game;

        public Point xy;
        public gObjectDefinition Definition;

        public gObject(
            Point xy, gObjectDefinition def
        ) {
            this.xy = xy;
            this.Definition = def;
        }

        public gObject(string s)
        {
            ReadGOBject(s);
        }

        public string WriteGOBject()
        {
            string s = "";
            s += IO.WriteHex(Definition.type, 4);
            s += IO.Write(xy);
            return s;
        }

        //return how many characters we read
        //so subclasses know where to start
        public int ReadGOBject(string s)
        {
            int read = 0;
            this.Definition = gObjectDefinition.Definitions[
                IO.ReadHex(s, 4, ref read, read)
            ];
            this.xy = IO.ReadPoint(s, ref read, read);
            return read;
        }
    }

}
