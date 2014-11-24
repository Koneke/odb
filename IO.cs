using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ODB
{
    class IO
    {
        public static Game1 Game;

        static KeyboardState ks, oks;
        public static bool shift;

        public static void Update(bool final)
        {
            shift =
                (IO.ks.IsKeyDown(Keys.LeftShift) ||
                IO.ks.IsKeyDown(Keys.RightShift));
            if (!final) ks = Keyboard.GetState();
            else oks = ks;
        }

        public static bool KeyPressed(Keys k)
        {
            return ks.IsKeyDown(k) && !oks.IsKeyDown(k);
        }

        public static void QuestionPromptInput()
        {
            Keys[] pk = ks.GetPressedKeys();
            Keys[] opk = oks.GetPressedKeys();

            foreach (int i in Game.acceptedInput)
            {
                if (pk.Contains((Keys)i) && !opk.Contains((Keys)i))
                {
                    char c = (char)i;
                    //if our char is a letter, affect it by shift
                    if(i >= 65 && i <= 90)
                        c += (char)(shift ? 0 : 32);
                    Game.questionPromptAnswer += c;
                    
                    //type it out

                    if (Game.questionPrompOneKey)
                    {
                        Game.questionPromptOpen = false;
                        Game.qpAnswerStack.Push(Game.questionPromptAnswer);
                        Game.questionReaction(Game.questionPromptAnswer);
                    }
                }
            }
            if (IO.KeyPressed(Keys.Back))
            {
                if (Game.questionPromptAnswer.Length > 0)
                {
                    Game.questionPromptAnswer =
                        Game.questionPromptAnswer.Substring(
                        0, Game.questionPromptAnswer.Length - 1
                    );
                    /*inputRowConsole.VirtualCursor.Left(1);
                    inputRowConsole.CellData.Print(
                        inputRowConsole.VirtualCursor.Position.X,
                        inputRowConsole.VirtualCursor.Position.Y,
                        " ");*/
                }
            }
            if (IO.KeyPressed(Keys.Enter))
            {
                Game.questionPromptOpen = false;
                Game.qpAnswerStack.Push(Game.questionPromptAnswer);
                Game.questionReaction(Game.questionPromptAnswer);
            }
            if (IO.KeyPressed(Keys.Escape))
            {
                Game.questionPromptAnswer = "";
                Game.questionPromptOpen = false;
            }
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
                Game.targeting = false;
                Game.targetingReaction(Game.target);
            }
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
            //Actor.IDCounter = Math.Max(actorStrings.Count - 1, 0);
        }

        public static string WriteLevelToFile(string path)
        {
            string output = "";
            output += Game.lvlW + "x" + Game.lvlH + ";";
            output += "##"; //end of header

            for (int y = 0; y < Game.lvlH ; y++)
                for (int x = 0; x < Game.lvlW; x++)
                {
                    if (Game.map[x, y] != null)
                        output += Game.map[x, y].writeTile();
                    output += ";";
                }
            WriteToFile(path, output);
            return output;
        }
        public static void ReadLevelFromFile(string path)
        {
            string content = ReadFromFile(path);
            List<string> header = content.Split(
                new string[]{"##"}, StringSplitOptions.RemoveEmptyEntries
            )[0].Split(';').ToList();

            Game.lvlW = int.Parse(header[0].Split('x')[0]);
            Game.lvlH = int.Parse(header[0].Split('x')[1]);

            Game.map = new Tile[Game.lvlW, Game.lvlH];
            Game.seen = new bool[Game.lvlW, Game.lvlH];
            Game.vision = new bool[Game.lvlW, Game.lvlH];

            List<string> body = content.Split(
                //do NOT remove empty entries, they are null tiles!
                new string[]{"##"}, StringSplitOptions.None
            )[1].Split(';').ToList();

            for (int i = 1; i < Game.lvlW * Game.lvlH; i++)
            {
                int x = i % Game.lvlW;
                int y = (i - (i % Game.lvlW))/Game.lvlW;
                if (body[i] == "")
                    Game.map[x, y] = null;
                else
                    Game.map[x, y] = new Tile(body[i]);
            }
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

        public static string WriteRoomsToFile(string path)
        {
            string output = "";
            foreach (Room r in Game.rooms)
                output += r.WriteRoom() + "##";
            WriteToFile(path, output);
            return output;
        }
        public static void ReadRoomsFromFile(string path)
        {
            string content = ReadFromFile(path);
            Game.rooms = new List<Room>();
            foreach (
                String s in content.Split(
                    new string[]{ "##" },
                    StringSplitOptions.RemoveEmptyEntries
            ).ToList()) {
                Room r = new Room(s);
                Game.rooms.Add(r);
            }
        }

        public static string WriteSeenToFile(string path)
        {
            string output = "";
            for (int i = 0; i < Game.lvlW * Game.lvlH; i++)
                output += IO.Write(Game.seen[
                    i % Game.lvlW,
                    (i - (i % Game.lvlW)) / Game.lvlW]
                );
            IO.WriteToFile(path, output);
            return output;
        }
        public static void ReadSeenFromFile(string path)
        {
            string content = IO.ReadFromFile(path);
            int n = 0;
            for (int i = 0; i < Game.lvlW * Game.lvlH; i++)
                Game.seen[i % Game.lvlW, (i - (i % Game.lvlW)) / Game.lvlW] =
                    IO.ReadBool(content, ref n, i);
        }

        public static string Write(Color c)
        {
            string s = "";
            s += String.Format("{0:X2}", c.R);
            s += String.Format("{0:X2}", c.G);
            s += String.Format("{0:X2}", c.B);
            return s;
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

        public static string Write(Color? c)
        {
            string s = "";
            if (c.HasValue) s += Write(c.Value);
            else s += "XXXXXX";
            return s;
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

        public static string Write(Point p)
        {
            string s = "";
            //var length data, add ;
            s += p.x + "x" + p.y + ";";
            return s;
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

        public static string Write(String ss, bool appendDelim = true)
        {
            string s = ss + (appendDelim ? ";" : "");
            return s;
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
        public static string ReadString(
            string s, int len, ref int read, int start = 0
        ) {
            int i = start;
            string ss = s.Substring(start, len);
            read += len;
            return ss;
        }

        public static string WriteHex(int i, int len) {
            return String.Format("{0:X"+len+"}", i);
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
            return ReadHex(ss);
        }

        public static string Write(bool b)
        {
            return b ? "1" : "0";
        }
        public static bool ReadBool(string s, ref int read, int start = 0)
        {
            read += 1;
            return s.Substring(start, 1) == "1";
        }
    }
}
