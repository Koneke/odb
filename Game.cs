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
            if (Instance != null) throw new Exception();
            Instance = this;
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

        public static void SwitchLevel(Level newLevel, bool gotoStairs = false)
        {
            //assuming only one connector
            Point target = newLevel.Connectors
                .First(lc => lc.Target == World.Level.ID).Position;

            Player.LevelID = newLevel.ID;

            World.Level = newLevel;
            foreach (Item item in Player.Inventory)
                item.MoveTo(newLevel);

            foreach (Actor a in World.Level.Actors)
            {
                //reset vision, incase the level we moved to is a different size
                a.Vision = null;
                a.ResetVision();
            }

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
    }
}