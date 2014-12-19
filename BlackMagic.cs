namespace ODB
{
    class BlackMagic
    {
        public static void CheckCircle(Actor actor, string chant)
        {
            string engraving = "";

            if (World.Level.At(actor.xy).Neighbours.Count < 8)
            {
                Game.UI.Log("Nothing happens.");
                return;
            }

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    TileInfo ti = World.Level.At(actor.xy + new Point(x, y));

                    bool empty = !ti.Blood || ti.Tile.Engraving == "";

                    engraving +=
                        empty
                        ? "0,"
                        : ti.Tile.Engraving.ToLower() + ",";

                }
                engraving = engraving.Substring(0, engraving.Length - 1);
                engraving += " ";
            }

            engraving = engraving.Substring(0, engraving.Length - 1);

            switch (engraving)
            {
                case "0,tor,0 zok,0,khr 0,bal,0":
                    if (chant.ToLower() != "tor zok khr bal") break;

                    Game.UI.Log(
                        "Darkness envelopes {1}...",
                        actor.GetName("name")
                    );
                    Game.UI.Log(
                        "{1} {2} good.",
                        actor.GetName("Name"),
                        actor.Verb("#feel")
                    );
                    actor.Heal(Util.Roll("2d4"));
                    break;

                default:
                    Game.UI.Log("Nothing happens.");
                    break;
            }
        }
    }
}
