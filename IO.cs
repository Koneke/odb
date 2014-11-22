using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace ODB
{
    class IO
    {
        public static Game1 Game;

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

        public static string WriteAllItemsToFile(string path)
        {
            string output = "";
            foreach (Item item in Game.allItems)
                output += item.WriteItem() + "##";
            WriteToFile(path, output);
            return output;
        }

        public static void ReadAllItemsFromFile(string path)
        {
            Game.allItems = new List<Item>();

            string content = ReadFromFile(path);
            List<string> itemStrings = content.Split(
                new string[]{ "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            foreach (string s in itemStrings)
                Game.allItems.Add(new Item(s));
            Item.IDCounter = Math.Max(itemStrings.Count - 1, 0);

            //when we spawn in actors, they are responsible for making sure
            //that the items in their inventories are not left in the worldItems
            //list.
            Game.worldItems = new List<Item>();
            Game.worldItems.AddRange(Game.allItems);
        }

        public static string WriteAllActorsToFile(string path)
        {
            string output = "";
            foreach (Actor actor in Game.worldActors)
                output += actor.WriteActor() + "##";
            WriteToFile(path, output);
            return output;
        }

        public static void ReadAllActorsFromFile(string path)
        {
            Game.worldActors = new List<Actor>();
            Game.Brains = new List<Brain>();

            string content = ReadFromFile(path);
            List<string> actorStrings = content.Split(
                new string[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            foreach (string s in actorStrings)
            {
                Actor a = new Actor(s);

                if (a.id == 0) Game.player = a;
                else Game.Brains.Add(new Brain(a));

                Game.worldActors.Add(a);
            }
            Actor.IDCounter = Math.Max(actorStrings.Count - 1, 0);
        }

        public static string Write(Color c)
        {
            string s = "";
            s += String.Format("{0:X2}", c.R);
            s += String.Format("{0:X2}", c.G);
            s += String.Format("{0:X2}", c.B);
            return s;
        }

        public static string Write(Color? c)
        {
            string s = "";
            if (c.HasValue) s += Write(c.Value);
            else s += "XXXXXX";
            return s;
        }

        public static string Write(Point p)
        {
            string s = "";
            //var length data, add ;
            s += p.x + "x" + p.y + ";";
            return s;
        }

        public static string Write(String ss)
        {
            string s = ss + ";";
            return s;
        }

        public static string WriteHex(int i, int len) {
            return String.Format("{0:X"+len+"}", i);
        }

        public static Point ReadPoint(
            string s, ref int read, int start = 0
        ) {
            int i = start;
            string inp = s.Substring(start, s.Length - start);
            inp = inp.Split(';')[0];
            Point p = new Point(
                int.Parse(inp.Split('x')[0]),
                int.Parse(inp.Split('x')[1])
            );
            read += inp.Length + 1;
            return p;
        }

        public static Color? ReadNullableColor(
            string s, ref int read, int start = 0
        ) {
            int i = start;
            if (s.Substring(start, 6).Contains("X"))
            {
                read += 6;
                return null;
            }
            Color c = new Color(
                Int32.Parse(s.Substring(start, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(start + 2, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(start + 4, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            read += 6; //we read 6 chars
            return c;
        }

        public static Color ReadColor(
            string s, ref int read, int start = 0
        ) {
            int i = start;
            Color c = new Color(
                Int32.Parse(s.Substring(start, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(start + 2, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(start + 4, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            read += 6; //we read 6 chars
            return c;
        }

        public static string ReadString(
            string s, ref int read, int start = 0
        ) {
            int i = start;
            string ss = s.Substring(start, s.Length - start);
            ss = ss.Split(';')[0];
            read += ss.Length + 1;
            return ss;
        }

        public static int ReadHex(
            string s
        ) {
            return Int32.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }

        public static int ReadHex(
            string s, int len, ref int read, int start = 0
        ) {
            int i = start;
            string ss = s.Substring(start, s.Length - start);
            ss = ss.Substring(0, len);
            read += len;
            //return Int32.Parse(ss, System.Globalization.NumberStyles.HexNumber);
            return ReadHex(ss);
        }
    }
}
