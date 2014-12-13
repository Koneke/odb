namespace ODB
{
    class BlackMagic
    {
        public static void CheckCircle(Actor actor, string chant)
        {
            string engraving = "";

            if (Util.Game.Level.At(actor.xy).Neighbours.Count < 8)
            {
                Util.Game.Log("Nothing happens.");
                return;
            }

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    TileInfo ti = Util.Game.Level.At(actor.xy);
                    engraving +=
                        ti.Blood
                        ? ti.Tile.Engraving.ToLower()+","
                        : "0,";

                }
                engraving = engraving.Substring(0, engraving.Length - 1);
                engraving += " ";
            }

            engraving = engraving.Substring(0, engraving.Length - 1);

            switch (engraving)
            {
                case "0,tor,0 zok,0,khr 0,bal,0":
                    if (chant.ToLower() != "tor zok khr bal") break;

                    Util.Game.Log("Darkness envelopes " +
                        actor.GetName("name") +
                        "...");
                    Util.Game.Log(
                        actor.GetName("Name") + " " +
                        actor.Verb("#feel") + " " +
                        "good.");
                    actor.Heal(Util.Roll("2d4"));
                    break;

                default:
                    Util.Game.Log("Nothing happens.");
                    break;
            }
        }
    }
}
