using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ODB
{
    static internal class SaveIO
    {
        public static JsonSerializerSettings Sets = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };

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

        /*public static string WriteActorDefinitionsToFile(string path)
        {
            string output = "";
            for (int i = 0; i < 0xFFFF; i++)
            {
                if (ActorDefinition.DefDict[i] == null) continue;

                output +=
                    ActorDefinition.DefDict[i].
                        WriteActorDefinition();
                output += "##";
            }
            WriteToFile(path, output);
            return output;
        }

        public static void ReadActorDefinitionsFromFile(string path)
        {
            ActorDefinition.DefDict = new Dictionary<int, ActorDefinition>();

            string content = ReadFromFile(path);
            List<string> definitions = content.Split(
                new[] { "##" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            definitions.ForEach(definition => new ActorDefinition(definition));
        }*/

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

        private static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Converters = { new StringEnumConverter() },
                Formatting = Formatting.Indented
            };

        public static void JsonSave()
        {
            WriteToFile(
                "Save/game.sv", 
                JsonConvert.SerializeObject(
                    Game.Instance,
                    Settings
                )
            );
            WriteToFile(
                "Save/world.sv", 
                JsonConvert.SerializeObject(
                    World.Instance,
                    Settings
                )
            );
        }

        public static void JsonLoad()
        {
            Game.Instance = JsonConvert.DeserializeObject<Game>(
                ReadFromFile("Save/game.sv"),
                Settings
            );

            World.Load(JsonConvert.DeserializeObject<World>(
                ReadFromFile("Save/world.sv"),
                Settings
            ));
        }

        public static void JsonWriteItemDefinitions(string path)
        {
            WriteToFile(
                path,
                JsonConvert.SerializeObject(
                    ItemDefinition.DefDict,
                    Settings
                )
            );
        }

        public static void JsonWriteActorDefinitions(string path)
        {
            WriteToFile(
                path,
                JsonConvert.SerializeObject(
                    ActorDefinition.DefDict,
                    Settings
                )
            );
        }

        public static void JsonLoadItemDefinitions(string path)
        {
            ItemDefinition.DefDict = JsonConvert.DeserializeObject
                <Dictionary<int, ItemDefinition>>(
                    ReadFromFile(path),
                    Settings
            );

            foreach (int key in ItemDefinition.DefDict.Keys)
                gObjectDefinition.GObjectDefs.Add(
                    key, ItemDefinition.DefDict[key]
                );
        }

        public static void JsonLoadActorDefinitions(string path)
        {
            ActorDefinition.DefDict =
                JsonConvert.DeserializeObject
                <Dictionary<ActorID, ActorDefinition>>(
                    ReadFromFile(path),
                    Settings
            );

            Monster.MonstersByDifficulty =
                new Dictionary<int, List<ActorDefinition>>();

            foreach (ActorID key in ActorDefinition.DefDict.Keys)
            {
                int difficulty = ActorDefinition.DefDict[key].Difficulty;

                if (!Monster.MonstersByDifficulty.ContainsKey(difficulty))
                    Monster.MonstersByDifficulty.Add(
                        difficulty, new List<ActorDefinition>());

                Monster.MonstersByDifficulty[difficulty].Add(
                    ActorDefinition.DefDict[key]
                );
            }
        }

        public static void KillSave()
        {
            string cwd = Directory.GetCurrentDirectory() + "/";
            if(File.Exists(cwd + "Save/game.sv"))
                File.Delete(cwd + "Save/game.sv");
            if(File.Exists(cwd + "Save/world.sv"))
                File.Delete(cwd + "Save/world.sv");
        }
    }
}