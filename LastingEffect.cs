using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        Sneak
    }

    [DataContract]
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
                { StatusType.Sleep, "Sleep" },
                { StatusType.Sneak, "Sneak" },
            };

        [DataMember] public StatusType Type;
        [DataMember] public int Life;
        [DataMember] public int Holder;
        [DataMember] private int? _ticker;
        [DataMember] public int LifeLength;

        public TickingEffectDefinition Ticker {
            get
            {
                return _ticker.HasValue
                    ? TickingEffectDefinition.Definitions[_ticker.Value]
                    : null;
            }
        }

        public LastingEffect() { }

        public LastingEffect(
            int holder,
            StatusType type,
            int lifeLength,
            TickingEffectDefinition ticker = null
        ) {
            Holder = holder;
            Type = type;
            LifeLength = lifeLength;
            if (ticker != null) _ticker = ticker.ID;
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
                    if (Life % 100 == 0)
                        PoisonEffect(Util.GetActorByID(Holder));
                    break;
                case StatusType.Bleed:
                    if (Life % 100 == 0)
                        BleedEffect(Util.GetActorByID(Holder));
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

            int? ticker = Ticker == null ? (int?)null : Ticker.ID;
            stream.Write(ticker);

            stream.Write(LifeLength);
            return stream;
        }
        public void ReadLastingEffect(string s)
        {
            Stream stream = new Stream(s);
            Type = (StatusType)stream.ReadHex(4);
            Life = stream.ReadHex(4);
            Holder = stream.ReadHex(4);

            int? ticker = stream.ReadNInt();
            if(ticker.HasValue)
                _ticker = ticker.Value;

            //LifeLength = stream.ReadHex(8);
            LifeLength = stream.ReadInt();
        }

        public static void PoisonEffect(Actor holder)
        {
            Game.UI.Log(
                (holder == Game.Player
                    ? "You feel "
                    : (holder.GetName("Name") + " looks ")) +
                "sick..."
            );
            DamageSource ds = new DamageSource(
                "R.I.P {0}, succumbed to poison on dungeon level {2}."
            ) {
                //todo: at_poison, dt_poison
                Damage = Util.Roll("1d4"),
                AttackType = AttackType.Bash,
                DamageType = DamageType.Physical,
                Source = null,
                Target = holder
            };
            //holder.Damage(Util.Roll("1d4"), null);
            holder.Damage(ds);
        }

        public static void BleedEffect(Actor holder)
        {
            World.Level.At(holder.xy).Blood = true;

            if(holder == Game.Player || Game.Player.Sees(holder.xy))
                Game.UI.Log(
                    holder.GetName("Name") + " " +
                    holder.Verb("bleed") + "!"
                );
            DamageSource ds = new DamageSource
            {
                //todo: at_?, dt_bleed?
                Damage = Util.Roll("2d3"),
                AttackType = AttackType.Bash,
                DamageType = DamageType.Physical,
                Source = null,
                Target = holder
            };
            holder.Damage(ds);
        }
    }
}