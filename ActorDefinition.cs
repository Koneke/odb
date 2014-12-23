using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ODB
{
    public enum ActorID
    {
        Mon_Player,
        Mon_Rat,
        Mon_Newt,
        Mon_Kobold,
        Mon_Kilik
    }

    [JsonConverter(typeof(ActorConverter))]
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

        public static Dictionary<ActorID, ActorDefinition> DefDict =
            new Dictionary<ActorID, ActorDefinition>();

        //LH-231214: We don't actually NEED to tag these as datamembers,
        //           since we handle what to write/read in our custom
        //           serializer, it is just so we can get a quick overview here.
        [DataMember] public ActorID ActorType;
        [DataMember] public bool Named; //for uniques and what not
        [DataMember] public Monster.GenerationType GenerationType;
        [DataMember] public string Strength, Dexterity, Intelligence;
        [DataMember] public int Speed, Quickness;
        [DataMember] public string HitDie, ManaDie;
        [DataMember] public int Experience;
        [DataMember] public int Difficulty;
        [DataMember] public List<DollSlot> BodyParts;
        [DataMember] public int CorpseType;
        [DataMember] public List<int> Spellbook;
        [DataMember] public List<Mod> SpawnIntrinsics;
        [DataMember] public AttackComponent NaturalAttack;
    }
}