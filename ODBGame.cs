using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole;
using Microsoft.Xna.Framework;
using xnaPoint = Microsoft.Xna.Framework.Point;

//~~~ QUEST TRACKER for ?? dec ~~~
// * Item paid-for status.
// * Wizard mode area select.
// * Clean up wizard mode class a bit.
//   * Fairly low prio, since it's not part of the /game/ per se,
//     but it /is/ fairly messy.
// * Inventory stuff currently doesn't make noise.

namespace ODB
{
    public class ODBGame : Game
    {
        public UI UI;
        public static ODBGame Game;

        private AppState _state;
        public MenuState MenuState;
        public GameState GameState;

        public ODBGame()
        {
            UI.Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public bool WizMode;
        public int GameTick;
        public int Seed;
        public List<Brain> Brains;
        public bool OpenRolls = false;
        public InventoryManager InvMan;
        public Actor Player;
        public List<int> GeneratedUniques; 

        public const int StandardActionLength = 10;

        private void GameReferences()
        {
            InventoryManager.Game = 
            PlayerResponses.Game =
            ODB.Player.Game =
            gObject.Game =
            Wizard.Game =
            Brain.Game =
            Util.Game =
            IO.Game =
            UI.Game =
            Game = this;
        }

        private void Load()
        {
            Spell.SetupMagic(); //essentially magic defs, but we hardcode magic
            SaveIO.ReadActorDefinitionsFromFile("Data/actors.def");
            SaveIO.ReadItemDefinitionsFromFile("Data/items.def");
            SaveIO.ReadTileDefinitionsFromFile("Data/tiles.def");
            KeyBindings.ReadBinds(SaveIO.ReadFromFile("Data/keybindings.kb"));

            //todo: mig this later
            Brains = new List<Brain>();
        }

        private void SetupSeed()
        {
            Seed = Guid.NewGuid().GetHashCode();
            Util.SetSeed(Seed);
        }

        protected override void Initialize()
        {
            GameReferences();

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            UI = new UI { ScreenSize = new xnaPoint(80, 25) };

            Load();
            SetupSeed();

            InvMan = new InventoryManager();

            Player = new Actor(
                new Point(0, 0),
                0, Util.ADefByName("Moribund"), 1)
            { Awake = true };

            GeneratedUniques = new List<int>();

            World.Levels.Add(World.Level = new Generator().Generate(null, 1));
            World.Level.Spawn(Player);

            Player.xy = World.Level.RandomOpenPoint();

            SetupBrains();

            UI.Log("Welcome!");

            MenuState = new MenuState(this);
            GameState = new GameState(this);

            SwitchState(MenuState);

            UI.CycleFont();
            UI.FontSize();

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            Engine.Update(gameTime, IsActive);
            IO.Update(false);

            _state.Update();
            _state.Draw();

            IO.Update(true);
            base.Update(gameTime);
        }

        public void SetupBrains() {
            if(Brains == null) Brains = new List<Brain>();
            else Brains.Clear();
            foreach (Actor actor in World.WorldActors
                .Where(a => a.LevelID == World.Level.ID))
                //shouldn't be needed, but
                //what did i even mean with that comment
                if (actor.ID == 0) Game.Player = actor;
                else Brains.Add(new Brain(actor));
        }

        public void SwitchState(AppState state)
        {
            _state = state;
            state.SwitchTo();
        }

        public void SwitchLevel(Level newLevel, bool gotoStairs = false)
        {
            //assuming only one connector
            Point target = newLevel.Connectors
                .First(lc => lc.Target == World.Level).Position;

            Game.Player.LevelID = newLevel.ID;

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

            if (!gotoStairs) return;
            Game.Player.xy = target;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            Engine.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}