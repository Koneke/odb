using System;
using System.Collections.Generic;

namespace ODB
{
    public enum StatusType
    {
        None,
        Any,
        Stun,
        Confusion,
        Sleep,
        Bleed,
        Poison,
    }

    public class LastingEffect
    {
        public static StatusType ReadStatusType(string s)
        {
            switch (s)
            {
                case "st_stun": return StatusType.Stun;
                case "st_confusion": return StatusType.Confusion;
                case "st_sleep": return StatusType.Sleep;
                case "st_bleed": return StatusType.Bleed;
                case "st_poison": return StatusType.Poison;
                default: throw new ArgumentException();
            }
        }
        public static string WriteStatusType(StatusType st)
        {
            switch (st)
            {
                case StatusType.Stun: return "st_stun";
                case StatusType.Confusion: return "st_confusion";
                case StatusType.Sleep: return "st_sleep";
                case StatusType.Bleed: return "st_bleed";
                case StatusType.Poison: return "st_poison";
                default: throw new ArgumentException();
            }
        }
        public static LastingEffect Create(
            int holder, StatusType type, int lifelength
        ) {
            return new LastingEffect(holder, type, lifelength);
        }

        public static Dictionary<StatusType, string> EffectNames =
            new Dictionary<StatusType, string>
            {
                { StatusType.Stun, "Stun" },
                { StatusType.Confusion, "Confusion" },
                { StatusType.Bleed, "Bleed" },
                { StatusType.Poison, "Poison" },
            };

        public StatusType Type;
        public int Life;

        //-1 = permanent
        public int Holder;
        public TickingEffectDefinition Ticker;
        public int LifeLength;

        public LastingEffect(
            int holder,
            StatusType type,
            int lifeLength,
            TickingEffectDefinition ticker = null
        ) {
            Holder = holder;
            Type = type;
            LifeLength = lifeLength;
            Ticker = ticker;
        }

        public LastingEffect(string s)
        {
            ReadLastingEffect(s);
        }

        public void Tick()
        {
            Life++;

            switch (Type)
            {
                case StatusType.Poison:
                    if (Life % 10 == 0) PoisonEffect(Util.GetActorByID(Holder));
                    break;
                case StatusType.Bleed:
                    if (Life % 10 == 0) BleedEffect(Util.GetActorByID(Holder));
                    break;
            }

            if (Ticker == null) return;
            if (Life % Ticker.Frequency == 0)
                Ticker.Effect(Util.GetActorByID(Holder));
        }

        public Stream WriteLastingEffect()
        {
            Stream stream = new Stream();
            stream.Write((int)Type, 4);
            stream.Write(Life, 4);
            stream.Write(Holder, 4);
            stream.Write(Ticker.ID, 4);
            stream.Write(LifeLength, 8);
            return stream;
        }
        public void ReadLastingEffect(string s)
        {
            Stream stream = new Stream(s);
            Type = (StatusType)stream.ReadHex(4);
            Life = stream.ReadHex(4);
            Holder = stream.ReadHex(4);
            Ticker = TickingEffectDefinition.Definitions
                [stream.ReadHex(4)];
            LifeLength = stream.ReadHex(8);
        }

        public static void PoisonEffect(Actor holder)
        {
            Util.Game.UI.Log(
                (holder == Util.Game.Player
                    ? "You feel "
                    : (holder.GetName("Name") + " looks ")) +
                "sick..."
            );
            holder.Damage(Util.Roll("1d4"), null);
        }

        public static void BleedEffect(Actor holder)
        {
            holder.Damage(Util.Roll("2d3"), null);
            World.Level.At(holder.xy).Blood = true;

            if(holder == Util.Game.Player || Util.Game.Player.Sees(holder.xy))
                Util.Game.UI.Log(
                    holder.GetName("Name") + " " +
                    holder.Verb("bleed") + "!"
                );
        }
    }
}