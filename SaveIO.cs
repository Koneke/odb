using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ODB
{
    static internal class SaveIO
    {
        public static bool SaveExists
        {
            get
            {
                return File.Exists(
                    Directory.GetCurrentDirectory() + "/Save/game.sv");
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
            }
            catch (UnauthorizedAccessException)
            {
                Game.UI.Log(
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
            while (ItemDefinition.ItemDefinitions
                [gObjectDefinition.TypeCounter] != null)
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

        public static void JsonSave()
        {
            WriteToFile(
                "Test/bar.txt", 
                JsonConvert.SerializeObject(
                    Game.Instance,
                    Formatting.Indented
                )
            );
            WriteToFile(
                "Test/foo.txt", 
                JsonConvert.SerializeObject(
                    World.Instance,
                    Formatting.Indented
                )
            );
        }

        public static void JsonLoad()
        {
            string gs = ReadFromFile("Test/bar.txt");
            Game.Instance = JsonConvert.DeserializeObject<Game>(gs);

            string content = ReadFromFile("Test/foo.txt");
            World.Load(JsonConvert.DeserializeObject<World>(content));
        }
    }
}