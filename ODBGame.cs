using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using SadConsole;
using Microsoft.Xna.Framework;
using xnaPoint = Microsoft.Xna.Framework.Point;

//~~~ QUEST TRACKER for ?? dec ~~~
// * Item paid-for status.
// * Wizard mode area select.
//   * Probably not very important any more.
// * Inventory stuff currently doesn't make noise.
//   * Should it?
// * Don't save the level itself as json, think that's the slow bit
//   * Connector info and sim. is okay, but homerolling the level will probably
//     be faster, I feel.
// * Save int/byte instead of tile for gObjects, since some tiles are not
//   visible in ordinary text.

namespace ODB
{
    //general idea, start moving shit here, let ODBGame be the app, handling
    //states and similar.

    [DataContract]
    public class ODBGame : Microsoft.Xna.Framework.Game
    {
        private static Microsoft.Xna.Framework.Game _instance;
        public static new void Exit() { _instance.Exit(); }

        public static string Hash;

        //we can't have several instances of the app anyways, so might as well
        //use static here and make things shorter to access.
        private static AppState _state;
        public static MenuState MenuState;
        public static GameState GameState;

        public ODBGame()
        {
            _instance = this;
            UI.Graphics = new GraphicsDeviceManager(this);
            UI.Graphics.SynchronizeWithVerticalRetrace = false;
            Content.RootDirectory = "Content";
        }

        private void Load()
        {
            Spell.SetupMagic(); //essentially magic defs, but we hardcode magic
            SaveIO.JsonLoadActorDefinitions("Data/actors.json");
            SaveIO.JsonLoadItemDefinitions("Data/items.json");
            SaveIO.ReadTileDefinitionsFromFile("Data/tiles.def");
            KeyBindings.ReadBinds(SaveIO.ReadFromFile("Data/keybindings.kb"));

            //todo: mig this later
            Game.Brains = new List<Brain>();
        }

        protected override void Initialize()
        {
            new Game();

            GenerateGameHash();
            Load();

            Game.UI = new UI { ScreenSize = new xnaPoint(80, 25) };
            MenuState = new MenuState();
            GameState = new GameState();

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            Game.UI.Log("Welcome!");

            SwitchState(MenuState);

            Game.UI.CycleFont();
            Game.UI.FontSize();

            base.Initialize();
        }

        private void GenerateGameHash()
        {
            using (MD5 md5 = MD5.Create())
            {
                Hash = "";

                using (var stream = File.OpenRead(
                    Directory.GetCurrentDirectory() + "/ODB.exe"))
                {
                    Hash += BitConverter.ToString(md5.ComputeHash(stream))
                        .Replace("-", "")
                        .ToLower().Substring(0, 5) + "-";
                }

                using (var stream = File.OpenRead(
                    Directory.GetCurrentDirectory() + "/Test/jsonactors.def"))
                {
                    Hash += BitConverter.ToString(md5.ComputeHash(stream))
                        .Replace("-", "")
                        .ToLower().Substring(0, 5) + "-";
                }

                using (var stream = File.OpenRead(
                    Directory.GetCurrentDirectory() + "/Test/jsonitems.def"))
                {
                    Hash += BitConverter.ToString(md5.ComputeHash(stream))
                        .Replace("-", "")
                        .ToLower().Substring(0, 5);
                }
            }
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

        public static void SwitchState(AppState state)
        {
            _state = state;
            state.SwitchTo();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            Engine.Draw(gameTime);
            base.Draw(gameTime);
        }

        public static void SaveQuit()
        {
            SaveIO.JsonSave();
            Exit();
        }
    }
}