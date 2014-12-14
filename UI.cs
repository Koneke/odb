using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SadConsole;
using Console = SadConsole.Consoles.Console;

using Bind = ODB.KeyBindings.Bind;

namespace ODB
{
    public class UI
    {
        public Microsoft.Xna.Framework.Point ScreenSize;

        public static GraphicsDeviceManager Graphics;

        public static Game1 Game;

        private List<Console> _consoles;
        private Console _dfc;
        private Console _logConsole;
        private Console _inputRowConsole;
        private Console _inventoryConsole;
        private Console _statRowConsole;

        private int _logSize = 3;

        private List<Font> _fonts;

        public Point Camera;
        private Point _cameraOffset;

        public UI()
        {
            Load();

            Camera = new Point(0, 0);
            _cameraOffset = new Point(0, 0);
        }

        public void Load()
        {
            Engine.Initialize(Graphics.GraphicsDevice);
            Engine.UseMouse = false;
            Engine.UseKeyboard = true;

            _fonts = new List<Font>();
            using (var stream = System.IO.File.OpenRead("Fonts/IBM.font"))
                _fonts.Add(Serializer.Deserialize<Font>(stream));
            using (var stream = System.IO.File.OpenRead("Fonts/IBM2x.font"))
                _fonts.Add(Serializer.Deserialize<Font>(stream));

            Engine.DefaultFont = _fonts[0];

            Engine.DefaultFont.ResizeGraphicsDeviceManager(
                Graphics, 80, 25, 0, 0
                );

            SetupConsoles();
        }

        public void SetupConsoles()
        {
            _consoles = new List<Console>();

            _dfc = new Console(80, 25);
            Engine.ActiveConsole = _dfc;

            //22 instead of 25 so inputRow and statRows fit.
            _logConsole = new Console(80, 22) {
                Position = new Microsoft.Xna.Framework.Point(0, -19)
            };
            //part of the console is offscreen, so we can resize it downwards

            _inputRowConsole = new Console(80, 1) {
                Position = new Microsoft.Xna.Framework.Point(0, 3),
                VirtualCursor = { IsVisible = true }
            };

            _inventoryConsole = new Console(80, 23);
            _inventoryConsole.Position =
                new Microsoft.Xna.Framework.Point(
                    _dfc.ViewArea.Width - _inventoryConsole.ViewArea.Width,
                    0
                    );

            _statRowConsole = new Console(80, 2) {
                Position = new Microsoft.Xna.Framework.Point(0, 23)
            };

            _consoles.Add(_dfc);
            _consoles.Add(_logConsole);
            _consoles.Add(_inputRowConsole);
            _consoles.Add(_statRowConsole);
            _consoles.Add(_inventoryConsole);

            //draw order
            Engine.ConsoleRenderStack.Add(_dfc);
            Engine.ConsoleRenderStack.Add(_logConsole);
            Engine.ConsoleRenderStack.Add(_inputRowConsole);
            Engine.ConsoleRenderStack.Add(_statRowConsole);
            Engine.ConsoleRenderStack.Add(_inventoryConsole);
        }

        public void RenderConsoles()
        {
            RenderMap();
            RenderItems();
            RenderActors();

            RenderLog();
            RenderPrompt();
            RenderInventory();
            RenderStatus();

            RenderTarget();
            RenderWmCursor();
        }

        private void RenderMap()
        {
            _dfc.CellData.Clear();

            for (int x = 0; x < ScreenSize.X; x++)
                for (int y = 0; y < ScreenSize.Y; y++)
                {
                    TileInfo ti = Game.Level.At(Camera + new Point(x, y));

                    if (ti == null) continue;
                    if(!(ti.Seen | Game.WizMode)) continue;

                    bool inVision = Game.Player.Vision
                        [x + Camera.x, y + Camera.y] || Game.WizMode;

                    Color background = ti.Tile.Background;
                    if (ti.Blood) background = Color.DarkRed;

                    DrawToScreen(
                        new Point(x, y),
                        background,
                        ti.Tile.Foreground * (inVision ? 1f : 0.6f),
                        ti.Tile.Render()
                    );
                }
        }

        private void RenderItems()
        {
            Rect screen = new Rect(Camera, new Point(80, 25));

            int[,] itemCount = new int[
                Game.Level.Size.x,
                Game.Level.Size.y
                ];

            foreach (Item i in World.WorldItems
                .Where(it => it.LevelID == Game.Level.ID))
                itemCount[i.xy.x, i.xy.y]++;

            foreach (Item i in World.WorldItems
                .Where(i => i.LevelID == Game.Level.ID)
                .Where(i => Game.Player.Vision[i.xy.x, i.xy.y] || Game.WizMode)
                .Where(i => screen.ContainsPoint(i.xy)))
            {
                if (itemCount[i.xy.x, i.xy.y] == 1)
                    DrawToScreen(
                        i.xy - Camera,
                        i.Known ? i.Definition.Background : null,
                        i.Known ? i.Definition.Foreground : Color.Gray,
                        i.Definition.Tile
                        );
                    //not sure I like the + for pile, since doors are +
                else DrawToScreen(i.xy, null, Color.White, "+");
            }
        }

        private void RenderActors()
        {
            Rect screen = new Rect(Camera, new Point(80, 25));

            int[,] actorCount = new int[
                Game.Level.Size.x,
                Game.Level.Size.y
                ];

            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == Game.Level.ID))
                actorCount[a.xy.x, a.xy.y]++;

            foreach (Actor a in World.WorldActors
                .Where(a => a.LevelID == Game.Level.ID)
                .Where(a => Game.Player.Vision[a.xy.x, a.xy.y] || Game.WizMode)
                .Where(a => screen.ContainsPoint(a.xy)))
            {
                if (actorCount[a.xy.x, a.xy.y] == 1)
                    DrawToScreen(
                        a.xy - Camera,
                        a.Definition.Background,
                        a.Definition.Foreground, a.Definition.Tile
                        );
                    //draw a "pile" (shouldn't happen at all atm
                else DrawToScreen(a.xy, null, Color.White, "*");
            }
        }

        private void RenderLog()
        {
            _logConsole.CellData.Clear();
            _logConsole.CellData.Fill(Color.White, Color.Black, ' ', null);
            for (
                int i = Game.LogText.Count, n = 0;
                i > 0 && n < _logConsole.ViewArea.Height;
                i--, n++
            ) {
                ColorString cs = Game.LogText[i - 1];
                for (int j = -1; j < cs.ColorPoints.Count; j++)
                {
                    int current = j == -1
                        ? 0
                        : cs.ColorPoints[j].Item1;

                    int next = j == cs.ColorPoints.Count - 1
                        ? cs.String.Length
                        : cs.ColorPoints[j + 1].Item1;

                    _logConsole.CellData.Print(
                        current, _logConsole.ViewArea.Height - (n + 1),
                        cs.String.Substring(current, next-current),
                        j == -1
                            ? Color.White
                            : cs.ColorPoints[j].Item2
                    );
                }
            }
        }

        private void RenderPrompt()
        {
            _inputRowConsole.IsVisible =
                (IO.IOState != InputType.PlayerInput) || Game.WizMode;

            if (!_inputRowConsole.IsVisible) return;

            _inputRowConsole.Position = new Microsoft.Xna.Framework.Point(0, Game.LogSize);
            _inputRowConsole.CellData.Fill(
                //Color.WhiteSmoke,
                Color.Black,
                Color.WhiteSmoke,
                ' ',
                null
                );

            _inputRowConsole.CellData.Print(
                0, 0, (Game.WizMode ? "" : IO.Question + " ") + IO.Answer + "_");
        }

        //todo: Needs tender love, care and attention.
        private void RenderInventory()
        {
            _inventoryConsole.IsVisible = IO.IOState == InputType.Inventory;
            if (IO.IOState != InputType.Inventory) return;

            int inventoryW = _inventoryConsole.ViewArea.Width;
            int inventoryH = _inventoryConsole.ViewArea.Height;

            _inventoryConsole.CellData.Clear();
            _inventoryConsole.CellData.Fill(
                Color.White, Color.Black, ' ', null);

            DrawBorder(
                _inventoryConsole,
                new Rect(
                    new Point(0, 0),
                    new Point(inventoryW, inventoryH)),
                Color.Black,
                Color.DarkGray
            );

            const int offset = 14;

            DrawBorder(
                _inventoryConsole,
                new Rect(
                    new Point(inventoryW - (offset + 3), 0),
                    new Point(offset + 3, inventoryH)),
                Color.Black,
                Color.DarkGray
                );

            int j = 2;

            if (InventoryManager.CurrentContainer != -1)
            {
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<p>ut into", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<t>ake out", Color.White);
            }
            else
            {
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<d>rop", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<e>at", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<r>ead", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<R>emove", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<S>heath", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<Q>uiver", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<w>ield", Color.White);
                _inventoryConsole.CellData.Print(
                    inventoryW - offset, j++, "<W>ear", Color.White);
            }

            _inventoryConsole.CellData.Print(
                inventoryW - (offset), j++ + 1, "Free:", Color.White);

            List<DollSlot> emptySlots =
                Game.Player.PaperDoll
                    .Where(bp => bp.Item == null)
                    .OrderBy(bp => bp.Type)
                    .Select(bp => bp.Type)
                    .ToList();


            foreach (DollSlot slot in emptySlots)
            {
                _inventoryConsole.CellData.Print(
                    inventoryW - (offset), j++ + 1,
                    BodyPart.BodyPartNames[slot], Color.White);
            }

            //border texts
            if (InventoryManager.CurrentContainer == -1)
                _inventoryConsole.CellData.Print(
                    2, 0, Game.Player.GetName("Name", true), Color.White);
            else
                _inventoryConsole.CellData.Print(
                    2, 0,
                    Util.GetItemByID(InventoryManager.CurrentContainer)
                        .GetName("Name"),
                    Color.White);

            string weightString =
                Game.Player.GetCarriedWeight() + "/" +
                    Game.Player.GetCarryingCapacity() + "dag";

            _inventoryConsole.CellData.Print(
                _inventoryConsole.ViewArea.Width-(2+weightString.Length), 0,
                weightString, Color.White);

            _inventoryConsole.CellData.Print(
                2, _inventoryConsole.ViewArea.Height-1,
                Game.LogText[Game.LogText.Count-1].String,
                Color.White);
            //end border texts

            List<Item> items = new List<Item>();
            if (InventoryManager.CurrentContainer != -1)
                items.AddRange(Game.InvMan.CurrentContents);
            else items.AddRange(Game.Player.Inventory);

            int y = 1;
            for(int i = 0; i < items.Count; i++)
            {
                Item item = items[i];

                string name = "";

                name += IO.Indexes[items.IndexOf(item)];
                name += ") ";

                if (item.Stacking) name += item.Count + "x ";
                if ((item.HasComponent<WearableComponent>() ||
                    item.HasComponent<AttackComponent>()) &&
                    item.Known)
                    name += item.Mod.ToString("+#;-#;+0") + " ";
                name += item.GetName("Name");
                if (item.Stacking && item.Count > 1) name += "s";

                //LH-021214: We might actually not know the number of charges..?
                if (item.Charged) name += "[" + item.Count + "]";

                name += " " + item.GetWeight() + "dag";

                if (item.HasComponent<ContainerComponent>())
                {
                    name += " (holding ";
                    name += InventoryManager.Containers[item.ID]
                        .Count + " item";
                    if (InventoryManager.Containers[item.ID].Count == 1)
                        name += "s";
                    name += ")";
                }

                if (Game.Player.IsEquipped(item))
                {
                    if (Game.Player.Quiver == item) name += " (quivered)";
                    else if (Game.Player.PaperDoll.Any(
                        x => x.Type == DollSlot.Hand && x.Item == item))
                        name += " (wielded)";
                    else
                        name += " (worn)";
                }

                Color bg = Color.Black;
                if (i == InventoryManager.Selection)
                {
                    if (InventoryManager.State ==
                        InventoryManager.InventoryState.Inserting ||
                        InventoryManager.State ==
                            InventoryManager.InventoryState.Joining)
                        name = "> " + name;
                    bg = Color.DimGray;
                }

                int yoffs=0;
                const int xoffs = 6;
                while (name.Length > 0)
                {
                    int cap = 58 + (yoffs > 0 ? -xoffs : 0);

                    int len;
                    if (name.Length > cap)
                    {
                        len = cap;
                        //check if we have a better place to break on
                        for (int k = 0; k < 10; k++)
                        {
                            if (name.Substring(cap - k, 1) != " ") continue;

                            len -= k;
                            name = name.Remove(cap - k, 1);
                            break;
                        }
                    }
                    else len = name.Length;

                    _inventoryConsole.CellData.Print(
                        yoffs > 0 ? 2 + xoffs : 2, y + yoffs++,
                        name.Substring(0, len), Color.White, bg
                        );
                    name = name.Substring(len, name.Length - len);
                }
                y += yoffs;
            }
        }

        private void RenderStatus()
        {
            _statRowConsole.CellData.Clear();
            _statRowConsole.CellData.Fill(Color.White, Color.Black, ' ', null);

            if (Game.WizMode)
            {
                string str = "W I Z A R D";
                str += " (" + String.Format("{0:X2}", Wizard.WmCursor.x);
                str += " ; ";
                str += String.Format("{0:X2}", Wizard.WmCursor.y) + ")";
                str += " (" + Wizard.WmCursor.x + " ; " +
                    Wizard.WmCursor.y + ") ";

                str += "D:" + Game.Level.Depth + "0M";
                str += " (" + Game.Level.Name + ")";

                _statRowConsole.CellData.Print(
                    0, 0, str 
                    );
                return;
            }

            //Not using GetName() here, simply because that'd yield "You"
            //since it is the player.
            string namerow = Game.Player.GetName("Name", true);
            namerow += "  ";
            namerow += "STR " + Game.Player.Get(Stat.Strength) + "  ";
            namerow += "DEX " + Game.Player.Get(Stat.Dexterity) + "  ";
            namerow += "INT " + Game.Player.Get(Stat.Intelligence) + "  ";
            namerow += "AC " + Game.Player.GetArmor();

            if (Game.Player.GetFoodStatus() != Actor.FoodStatus.Satisfied)
                namerow += " " +
                    Actor.FoodStatusString(Game.Player.GetFoodStatus());

            namerow = Game.Player.LastingEffects.Aggregate(
                namerow,
                (current, lastingEffect) => current +
                    (
                        lastingEffect.Type == StatusType.None
                            ? ""
                            : " " + LastingEffect.EffectNames
                                [lastingEffect.Type]
                        )
                );

            string statrow = "";
            statrow += "[";
            statrow += ("" + Game.Player.HpCurrent).PadLeft(3, ' ');
            statrow += "/";
            statrow += ("" + Game.Player.HpMax).PadLeft(3, ' ');
            statrow += "]";

            statrow += " ";
            statrow += "[";
            statrow += ("" + Game.Player.MpCurrent).PadLeft(3, ' ');
            statrow += "/";
            statrow += ("" + Game.Player.MpMax).PadLeft(3, ' ');
            statrow += "]";

            statrow += " ";
            statrow += "XP:";
            statrow += Game.Player.Level + "";

            statrow += " ";
            statrow += "$:";
            statrow +=
                Game.Player.Inventory
                    .Where(item => item.Type == 0x8000)
                    .Sum(item => item.Count);

            statrow += " ";
            statrow += "T:";
            statrow += string.Format("{0:F1}", Game.GameTick / 10f);

            statrow += " ";
            statrow += "D:" + Game.Level.Depth + "0M";

            statrow += " (" + Game.Level.Name + ")";

            float playerHealthPcnt =
                Game.Player.HpCurrent /
                    (float)Game.Player.HpMax;

            float playerManaPcnt =
                Game.Player.MpCurrent /
                    (float)Game.Player.MpMax;

            float colorStrength = 0.6f + 0.4f - (0.4f * playerHealthPcnt);
            float manaColorStrength = 0.6f + 0.4f - (0.4f * playerManaPcnt);

            for (int x = 0; x < 9; x++)
                _statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        colorStrength - colorStrength * playerHealthPcnt,
                        colorStrength * playerHealthPcnt,
                        0
                        )
                    );

            for (int x = 10; x < 19; x++)
                _statRowConsole.CellData.SetBackground(
                    x, 1, new Color(
                        manaColorStrength - manaColorStrength * playerManaPcnt,
                        manaColorStrength * playerManaPcnt / 2,
                        manaColorStrength * playerManaPcnt
                        )
                    );

            _statRowConsole.CellData.Print(0, 0, namerow);
            _statRowConsole.CellData.Print(0, 1, statrow);
        }

        private void RenderTarget()
        {
            if (IO.IOState != InputType.Targeting) return;

            Cell cs = _dfc.CellData[Game.Target.x, Game.Target.y];

            bool blink = (DateTime.Now.Millisecond%500 > 250);

            cs.Background = blink
                ? Util.InvertColor(cs.Background)
                : Color.White;

            cs.Foreground = blink
                ? Util.InvertColor(cs.Foreground)
                : Color.White;
        }

        private void RenderWmCursor()
        {
            if (!Game.WizMode) return;

            Cell cs = _dfc.CellData[
                Wizard.WmCursor.x, Wizard.WmCursor.y];

            bool blink = (DateTime.Now.Millisecond%500 > 250);

            cs.Background = blink
                ? Util.InvertColor(cs.Background)
                : Color.White;

            cs.Foreground = blink
                ? Util.InvertColor(cs.Foreground)
                : Color.White;
        }

        public void DrawToScreen(Point xy, Color? bg, Color fg, String tile)
        {
            if (bg != null)
                _dfc.CellData.SetBackground(
                    xy.x, xy.y, bg.Value
                    );

            _dfc.CellData.SetForeground(xy.x, xy.y, fg);

            _dfc.CellData.Print(xy.x, xy.y, tile);
        }

        void DrawBorder(Console c, Rect r, Color bg, Color fg)
        {
            for (int x = 0; x < r.wh.x; x++)
            {
                c.CellData.Print(r.xy.x + x, 0, (char)205+"", fg, bg);
                c.CellData.Print(r.xy.x + x, r.wh.y-1, (char)205+"", fg, bg);
            }
            for (int y = 0; y < r.wh.y; y++)
            {
                c.CellData.Print(r.xy.x, y, (char)186+"", fg, bg);
                c.CellData.Print(r.xy.x + r.wh.x-1, y, (char)186+"", fg, bg);
            }
            _inventoryConsole.CellData.Print(
                r.xy.x, 0, (char)201 + "");
            _inventoryConsole.CellData.Print(
                r.xy.x + r.wh.x-1, 0, (char)187 + "");
            _inventoryConsole.CellData.Print(
                r.xy.x, r.wh.y-1, (char)200 + "");
            _inventoryConsole.CellData.Print(
                r.xy.x + r.wh.x-1, r.wh.y-1, (char)188 + "");
        }

        public void CycleFont()
        {
            int i = _fonts.IndexOf(Engine.DefaultFont);
            i++;
            i = i % _fonts.Count;
            Engine.DefaultFont = _fonts[i];
            foreach (Console c in _consoles)
                c.Font = Engine.DefaultFont;
            Engine.DefaultFont.ResizeGraphicsDeviceManager(
                Graphics, ScreenSize.X, ScreenSize.Y, 0, 0
                );
        }

        public void Input()
        {
            if (KeyBindings.Pressed(Bind.Log_Size_Up))
                _logSize = Math.Min(_logConsole.ViewArea.Height, ++_logSize);

            if (KeyBindings.Pressed(Bind.Log_Size_Down))
                _logSize = Math.Max(0, --_logSize);

            _logConsole.Position = new Microsoft.Xna.Framework.Point(
                0, -_logConsole.ViewArea.Height + _logSize
            );
        }

        public void UpdateCamera()
        {
            if (KeyBindings.Pressed(Bind.Camera_Right)) _cameraOffset.x++;
            if (KeyBindings.Pressed(Bind.Camera_Left)) _cameraOffset.x--;
            if (KeyBindings.Pressed(Bind.Camera_Down)) _cameraOffset.y++;
            if (KeyBindings.Pressed(Bind.Camera_Up)) _cameraOffset.y--;

            Camera.x = Game.Player.xy.x - 40;
            Camera.y = Game.Player.xy.y - 12;
            Camera += _cameraOffset;

            Camera.x = Math.Max(0, Camera.x);
            Camera.x = Math.Min(Game.Level.Size.x - ScreenSize.X, Camera.x);
            Camera.y = Math.Max(0, Camera.y);
            Camera.y = Math.Min(Game.Level.Size.y - ScreenSize.Y, Camera.y);
        }
    }
}