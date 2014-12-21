using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace ODB
{
    [DataContract]
    public class ActorDefinition : gObjectDefinition
    {
        //LH-01214: Note, the concept of equality here does not refer to
        //          reference, but rather, actual values.
        //          This means that if I were to theoretically create an
        //          exact copy of another definition, they would and SHOULD
        //          test equal.
        protected bool Equals(ActorDefinition other)
        {
            bool bodyPartsEqual = (BodyParts.Count == other.BodyParts.Count);
            if (!bodyPartsEqual) return false;
            if (BodyParts.Where(
                (t, i) => t != other.BodyParts[i]).Any())
                return false;

            bool spellbookEqual = (Spellbook.Count == other.Spellbook.Count);
            if (!spellbookEqual) return false;
            if (Spellbook.Where(
                (t, i) => t != other.Spellbook[i]).Any())
                return false;

            bool spawnIntrinsicsEqual =
                (SpawnIntrinsics.Count == other.SpawnIntrinsics.Count);
            if (!spawnIntrinsicsEqual) return false;
            if (SpawnIntrinsics.Where(
                (t, i) => t != other.SpawnIntrinsics[i]).Any())
                return false;

            return
                base.Equals(other) &&
                    Named.Equals(other.Named) &&
                    Strength == other.Strength &&
                    Dexterity == other.Dexterity &&
                    Intelligence == other.Intelligence &&
                    Speed == other.Speed &&
                    Quickness == other.Quickness &&
                    CorpseType == other.CorpseType
                ;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                //todo: There is a risk of hashcollision.
                //      We're not using BodyParts, SpawnIntrinsics,
                //      or Spellbook here, simply because they still check
                //      based on /REFERENCE/, and not value, and .Equals here
                //      checks value-equivalence, not identity.
                //      Really though, it should happen unless you define two
                //      exactly identical ActorDefinitions and try to use them
                //      as separate keys in a dict for some reason...
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ Named.GetHashCode();
                hashCode = (hashCode*397) ^ Speed;
                hashCode = (hashCode*397) ^ Quickness;
                hashCode = (hashCode*397) ^ CorpseType;
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ActorDefinition)obj);
        }

        public static Dictionary<int, ActorDefinition> DefDict =
            new Dictionary<int, ActorDefinition>();

        [DataMember(Order= 6)] public bool Named; //for uniques and what not
        [DataMember(Order= 7)] public Monster.GenerationType GenerationType;
        [DataMember(Order= 8)] public string Strength, Dexterity, Intelligence;
        [DataMember(Order= 9)] public int Speed, Quickness;
        [DataMember(Order=10)] public string HitDie, ManaDie;
        [DataMember(Order=11)] public int Experience;
        [DataMember(Order=12)] public int Difficulty;
        [DataMember(Order=13)] public List<DollSlot> BodyParts;
        [DataMember(Order=14)] public int CorpseType;
        [DataMember(Order=15)] public List<int> Spellbook;
        //intrinsics spawned with
        [DataMember(Order=16)] public List<Mod> SpawnIntrinsics;
        [DataMember(Order=17)] public AttackComponent NaturalAttack;

        public ActorDefinition() { }

        public ActorDefinition(
            Color? background, Color foreground,
            string tile, string name,
            string strength, string dexterity, string intelligence,
            List<DollSlot> bodyParts,
            List<int> spellbook,
            bool named
        ) : base(background, foreground, tile, name) {
            Strength = strength;
            Dexterity = dexterity;
            Intelligence = intelligence;
            BodyParts = bodyParts ?? new List<DollSlot>();

            DefDict[Type] = this;

            ItemDefinition corpse = new ItemDefinition(
                null, Color.Red, "%", name + " corpse");
            CorpseType = corpse.Type;

            Spellbook = spellbook ?? new List<int>();
            Named = named;
            SpawnIntrinsics = new List<Mod>();
            NaturalAttack = new AttackComponent
            {
                AttackType = AttackType.Bash,
                Damage = "1d4"
            };
        }

        public void Set(Stat stat, string value)
        {
            switch (stat)
            {
                case Stat.Strength:
                    Strength = value;
                    break;
                case Stat.Dexterity:
                    Dexterity = value;
                    break;
                case Stat.Intelligence:
                    Intelligence = value;
                    break;
                case Stat.Speed:
                    Speed = IO.ReadHex(value);
                    break;
                case Stat.Quickness:
                    Quickness = IO.ReadHex(value);
                    break;
                default:
                    Game.UI.Log("~ERROR~: Bad stat.");
                    break;
            }
        }
    }
}