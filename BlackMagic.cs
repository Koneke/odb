using Microsoft.Xna.Framework;

namespace ODB
{
    class BlackMagic
    {
        public static void CheckCircle(Actor actor, string chant)
        {
            string engraving = "";

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Point center = actor.xy;

                    if (x + center.x < 0 || y + center.y < 0 ||
                        x + center.x > Util.Game.Level.Size.x ||
                        y + center.y > Util.Game.Level.Size.y)
                    {
                        Util.Game.Log("Nothing happens.");
                        return;
                    }

                    bool blood = Util.Game.Level.Blood
                        [x + center.x, y + center.y];

                    string e = 
                        Util.Game.Level.Map[x + center.x, y + center.y]
                            .Engraving.ToLower();
                    engraving += ((e == "" || !blood) ? "0" : e) + ",";
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
