using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ODB
{
    [DataContract]
    public class Game
    {
        public static Game Instance;

        public Game()
        {
            Instance = this;

            _generatedUniques = new List<int>();
            _gameTick = 0;
            SetupSeed();
            _idCounter = 0;
            _identified = new List<int>();
        }

        //we keep these purely static because we don't need to save/load them
        public static Actor Player;
        public static UI UI;
        public static bool OpenRolls;
        public static bool WizMode;
        public static List<Brain> Brains;

        public const int StandardActionLength = 10;

        [DataMember] private List<int> _generatedUniques;
        [DataMember] private int _gameTick;
        [DataMember] private int _seed;
        [DataMember] private int _idCounter;
        [DataMember] private List<int> _identified;

        public static List<int> GeneratedUniques
        {
            get { return Instance._generatedUniques; }
            set { Instance._generatedUniques = value; }
        }

        public static int GameTick
        {
            get { return Instance._gameTick; }
            set { Instance._gameTick = value; }
        }

        public static int Seed
        {
            get { return Instance._seed; }
            set { Instance._seed = value; }
        }

        public static int IDCounter
        {
            get { return Instance._idCounter; }
            set { Instance._idCounter = value; }
        }

        public static void Identify(int type)
        {
            Instance._identified.Add(type);
        }

        public static bool IsIdentified(int type)
        {
            return Instance._identified.Contains(type);
        }

        public static void SwitchLevel(Level newLevel, bool gotoStairs = false)
        {
            //assuming only one connector
            Point target = newLevel.Connectors
                .First(lc => lc.Target == World.Level.ID).Position;

            Player.LevelID = newLevel.ID;

            World.Level = newLevel;
            foreach (Item item in Player.Inventory)
                item.MoveTo(newLevel);

            SetupBrains();

            UI.FullRedraw();

            if (gotoStairs) Player.xy = target;
        }

        public static void SetupBrains()
        {
            if(Brains == null) Brains = new List<Brain>();
            else Brains.Clear();
            foreach (Actor actor in World.Level.Actors)
                if (actor.ID == 0) Player = actor;
                else Brains.Add(new Brain(actor));
        }

        private void SetupSeed()
        {
            Seed = Guid.NewGuid().GetHashCode();
            Util.SetSeed(Seed);
        }
    }
}