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

        public Color? Background;
        public Color Foreground;
        public string Tile;
        public string Name;
        public int Type;

        public gObjectDefinition(
            Color? background, Color foreground, string tile, string name
        ) {
            Background = background;
            Foreground = foreground;
            Tile = tile;
            Name = name;
            Type = TypeCounter++;
            Definitions[Type] = this;
        }

        public gObjectDefinition(string s)
        {
            ReadGObjectDefinition(s);
        }

        public Stream ReadGObjectDefinition(string s)
        {
            Stream stream = new Stream(s);
            Type = stream.ReadHex(4);
            Background = stream.ReadNullableColor();
            Foreground = stream.ReadColor();
            Tile = stream.ReadString(1);
            Name = stream.ReadString();

            Definitions[Type] = this;
            //LH-011214: Creating new definitions after reading from file
            //           makes weird stuff happen without this, since we
            //           do not increment the type counter normally (i.e. when
            //           we "create" the def) here, so we make the counter
            //           search for a new empty spot.
            while (Definitions[TypeCounter++] != null) { }
            return stream;
        }

        public Stream WriteGObjectDefinition()
        {
            Stream stream = new Stream();
            stream.Write(Type, 4);
            stream.Write(Background);
            stream.Write(Foreground);
            stream.Write(Tile, false);
            stream.Write(Name);
            return stream;
        }

        //ReSharper disable once InconsistentNaming
        //LH-011214: yes, horrible crime against humanity to have it lowercase,
        //           but it makes sense together with A().
        //           In reality, I want it NetHack-style a(Item.Name),
        //           but we can't really have "global" functions like that,
        //           since C# apparently doesn't like that :(
        //           This is the second best.
        public string a()
        {
            return Util.Article(Name) + " " + Name;
        }
        public string A()
        {
            return Util.Capitalize(a());
        }

        //ReSharper disable once InconsistentNaming
        //LH-011214: see above
        public string the()
        {
            return "the" + " " + Name;
        }
        public string The()
        {
            return Util.Capitalize(the());
        }

        //ReSharper disable once InconsistentNaming
        //LH-011214: Simple way of pluralizing.
        //           In a method mainly if we have irregular plurals and such
        //           later on (learned my lesson of not futureproofing in
        //           roguelikeprojects before...).
        public string s()
        {
            return Name + "s";
        }

        //ReSharper disable once InconsistentNaming
        public string thes()
        {
            return "the " + s();
        }
        public string Thes()
        {
            return Util.Capitalize(thes());
        }
    }

    public class gObject
    {
        public static Game1 Game;

        //ReSharper disable once InconsistentNaming
        public Point xy;
        public gObjectDefinition Definition;

        public gObject(
            Point xy, gObjectDefinition definition
        ) {
            this.xy = xy;
            Definition = definition;
        }

        public gObject(string s)
        {
            ReadGOBject(s);
        }

        public Stream WriteGOBject()
        {
            Stream stream = new Stream();
            stream.Write(Definition.Type, 4);
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
