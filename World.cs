using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ODB
{
    [DataContract]
    public class World
    {
        private static World _instance;
        public static World Instance
        {
            get { return _instance ?? (_instance = new World()); }
        }

        private World()
        {
            Levels = new List<Level>();
            AllItems = new List<Item>();
            WorldItems = new List<Item>();
            WorldActors = new List<Actor>();
        }

        [DataMember] private static int _level;
        public static Level Level {
            get { return LevelByID(_level); }
            set { _level = value.ID; }
        }

        //switch to dict? ID:Level
        [DataMember] public List<Level> Levels; 

        //could probably do something about these two, not too gracious atm
        [DataMember] public List<Item> AllItems;
        [DataMember] public List<Item> WorldItems;
        [DataMember] public List<Actor> WorldActors;
        [DataMember] public InventoryManager WorldContainers;

        public static Level LevelByID(int target)
        {
            return Instance.Levels.First(l => l.ID == target);
        }

        public static void Load(World deserialized)
        {
            _instance = deserialized;
            Game.SetupBrains();
            Game.Player.HasMoved = true;
        }
    }
}