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

        public Stream ReadGObjectDefinition(string s)
        {
            Stream str = new Stream(s);
            type = str.ReadHex(4);
            bg = str.ReadNullableColor();
            fg = str.ReadColor();
            tile = str.ReadString(1);
            name = str.ReadString();

            Definitions[this.type] = this;
            return str;
        }

        public Stream WriteGObjectDefinition()
        {
            Stream stream = new Stream();
            stream.Write(type, 4);
            stream.Write(bg);
            stream.Write(fg);
            stream.Write(tile, false);
            stream.Write(name);
            return stream;
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

        public Stream WriteGOBject()
        {
            Stream stream = new Stream();
            stream.Write(Definition.type, 4);
            stream.Write(xy);
            return stream;
        }

        public Stream ReadGOBject(string s)
        {
            Stream stream = new Stream(s);
            Definition = gObjectDefinition.Definitions[
                stream.ReadHex(4)
            ];

            xy = stream.ReadPoint();
            return stream;
        }
    }

}
