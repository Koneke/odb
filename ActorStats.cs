using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ODB
{
    public enum Stat
    {
        Strength,
        Dexterity,
        Intelligence,
        PoisonRes
    }

    //to make actor class itself a bit cleaner
    [DataContract]
    public class ActorStats
    {
        [DataMember] private readonly int _actorID ;
        private Actor _actor { get { return Util.GetActorByID(_actorID); } }

        [DataMember] public int Strength;
        [DataMember] public int Dexterity;
        [DataMember] public int Intelligence;

        [DataMember] public int HpMax;
        [DataMember] public int HpCurrent;
        [DataMember] public int HpRegCooldown;

        [DataMember] public int MpMax;
        [DataMember] public int MpCurrent;
        [DataMember] public int MpRegCooldown;

        [DataMember] public List<Mod> Intrinsics;

        //for json.net
        public ActorStats() { }

        public ActorStats(Actor actor)
        {
            _actorID = actor.ID;
            Intrinsics = new List<Mod>();
        }

        public int GetMod(Stat stat)
        {
            int mod = 0;

            if (_actor == null) return mod;

            ModType mt;
            switch (stat)
            {
                case Stat.Strength: mt = ModType.Strength; break;
                case Stat.Dexterity: mt = ModType.Dexterity; break;
                case Stat.Intelligence: mt = ModType.Intelligence; break;
                case Stat.PoisonRes: mt = ModType.PoisonRes; break;
                default: throw new ArgumentException();
            }

            mod += _actor.GetEquippedItems()
                .SelectMany(it => it.Mods)
                .Where(m => m.Type == mt)
                .Sum(m => m.GetValue());

            mod += Intrinsics
                .Where(m => m.Type == mt)
                .Sum(m => m.GetValue());

            return mod;
        }

        public int Get(Stat stat)
        {
            switch (stat)
            {
                case Stat.Strength: return Strength + GetMod(stat);
                case Stat.Dexterity: return Dexterity + GetMod(stat);
                case Stat.Intelligence: return Intelligence + GetMod(stat);
                case Stat.PoisonRes: return GetMod(stat);
                default: throw new ArgumentException();
            }
        }
    }
}