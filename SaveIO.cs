using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ODB
{
    static internal class SaveIO
    {
        public static void Save()
        {
            Stream stream = new Stream();
            stream.Write(IO.Game.Levels.Count, 2);
            for (int i = 0; i < IO.Game.Levels.Count; i++)
            {
                if (IO.Game.Levels[i].WorldActors.Contains(IO.Game.Player))
                    stream.Write(i, 2);
                IO.Game.Levels[i].WriteLevelSave("Save/level" + i + ".sv");
            }

            //okay, so I really don't think anyone's going to hit
            //gametick 0xFFFFFFFF, that'd be ludicrous.
            //but 0xFFFF might be hit, and 0xFFFFF looks ugly.
            stream.Write(Util.Game.GameTick, 8);
            stream.Write(Util.Game.Seed, 8);
            stream.Write(Util.Game.Food, 8);

            string containers = "";
            foreach (int container in InventoryManager.ContainerIDs.Keys)
            {
                containers += IO.WriteHex(container, 4);
                containers = InventoryManager.ContainerIDs[container]
                    .Aggregate(containers,
                    (current, item) => current + IO.WriteHex(item, 4));
                containers += ",";
            }
            stream.Write(containers);

            foreach (int ided in ItemDefinition.IdentifiedDefs)
            {
                stream.Write(ided, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            WriteToFile("Save/game.sv", stream.ToString());
        }

        public static void Load()
        {
            Stream stream = new Stream(ReadFromFile("Save/game.sv"));
            int levels = stream.ReadHex(2);
            int playerLocation = stream.ReadHex(2);

            if (IO.Game.Levels != null)
            {
                for (int i = 0; i < IO.Game.Levels.Count; i++)
                    IO.Game.Levels[i] = null;
                IO.Game.Levels.Clear();
            } else IO.Game.Levels = new List<Level>();

            for (int i = 0; i < levels; i++)
                IO.Game.Levels.Add(new Level("Save/level" + i + ".sv"));

            Util.Game.GameTick = stream.ReadHex(8);
            Util.Game.Seed = stream.ReadHex(8);
            Util.Game.Food = stream.ReadHex(8);

            Util.Game.Level = IO.Game.Levels[playerLocation];

            string containers = stream.ReadString();
            List<int> containerItems = new List<int>();
            InventoryManager.ContainerIDs = new Dictionary<int, List<int>>();
            foreach (string container in containers.Split(','))
            {
                if(container == "") continue;

                int count = container.Length / 4 - 1;
                int id;

                Stream strm = new Stream(container);
                InventoryManager.ContainerIDs.Add(
                    id = strm.ReadHex(4), new List<int>());

                for (int i = 0; i < count; i++)
                {
                    int itemid = strm.ReadHex(4);
                    InventoryManager.ContainerIDs[id].Add(itemid);
                    containerItems.Add(itemid);
                }
            }

            foreach (Level level in Util.Game.Levels)
                level.WorldItems.RemoveAll(x => containerItems.Contains(x.ID));

            string identifieds = stream.ReadString();
            foreach (string ided in identifieds.Split(',')
                .Where(ided => ided != ""))
            {
                ItemDefinition.IdentifiedDefs.Add(IO.ReadHex(ided));
            }

            IO.Game.SetupBrains();
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
                IO.Game.Log(
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
    }
}