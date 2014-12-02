using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ODB
{
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
                    HpMax == other.HpMax &&
                    MpMax == other.MpMax &&
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
                hashCode = (hashCode*397) ^ Strength;
                hashCode = (hashCode*397) ^ Dexterity;
                hashCode = (hashCode*397) ^ Intelligence;
                hashCode = (hashCode*397) ^ Speed;
                hashCode = (hashCode*397) ^ Quickness;
                hashCode = (hashCode*397) ^ HpMax;
                hashCode = (hashCode*397) ^ MpMax;
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

        public static ActorDefinition[] ActorDefinitions =
            new ActorDefinition[0xFFFF];

        public bool Named; //for uniques and what not
        public int Strength, Dexterity, Intelligence;
        public int Speed, Quickness;
        public int HpMax, MpMax;
        public List<DollSlot> BodyParts;
        public int CorpseType;
        public List<int> Spellbook;
        //intrinsics spawned with
        public List<Mod> SpawnIntrinsics;

        public ActorDefinition(
            Color? background, Color foreground,
            string tile, string name,
            int strength, int dexterity, int intelligence, int hp,
            List<DollSlot> bodyParts,
            List<int> spellbook,
            bool named
            )
            : base(background, foreground, tile, name) {
            Strength = strength;
            Dexterity = dexterity;
            Intelligence = intelligence;
            HpMax = hp;
            BodyParts = bodyParts ?? new List<DollSlot>();
            ActorDefinitions[Type] = this;

            ItemDefinition corpse = new ItemDefinition(
                null, Color.Red, "%", name + " corpse");
            CorpseType = corpse.Type;

            Spellbook = spellbook ?? new List<int>();
            Named = named;
            SpawnIntrinsics = new List<Mod>();
            }

        public ActorDefinition(string s) : base(s)
        {
            ReadActorDefinition(s);
        }

        public void Set(Stat stat, int value)
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
                    Speed = value;
                    break;
                case Stat.Quickness:
                    Quickness = value;
                    break;
                default:
                    Util.Game.Log("~ERROR~: Bad stat.");
                    break;
            }
        }

        public Stream WriteActorDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Named);
            stream.Write(Strength, 2);
            stream.Write(Dexterity, 2);
            stream.Write(Intelligence, 2);
            stream.Write(HpMax, 2);
            stream.Write(MpMax, 2);
            stream.Write(Speed, 2);
            stream.Write(Quickness, 2);

            foreach (DollSlot ds in BodyParts)
                stream.Write((int)ds + ",", false);
            stream.Write(";", false);

            stream.Write(CorpseType, 4);

            foreach (int spellId in Spellbook)
            {
                stream.Write(spellId, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            foreach (Mod m in SpawnIntrinsics)
            {
                stream.Write((int)m.Type, 4);
                stream.Write(":", false);
                stream.Write(m.RawValue, 4);
                stream.Write(",", false);
            }
            stream.Write(";", false);

            return stream;
        }

        public Stream ReadActorDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            Named = stream.ReadBool();
            Strength = stream.ReadHex(2);
            Dexterity = stream.ReadHex(2);
            Intelligence = stream.ReadHex(2);
            HpMax = stream.ReadHex(2);
            MpMax = stream.ReadHex(2);
            Speed = stream.ReadHex(2);
            Quickness = stream.ReadHex(2);

            BodyParts = new List<DollSlot>();
            foreach (string ss in stream.ReadString().Split(',')
                .Where(ss => ss != ""))
                BodyParts.Add((DollSlot)int.Parse(ss));

            CorpseType = stream.ReadHex(4);

            if (Util.ItemDefByName(Name + " corpse") == null)
            {
                //should be put into a func of its own..?
                ItemDefinition corpse = new ItemDefinition(
                    null, Color.Red, "%", Name + " corpse");
                CorpseType = corpse.Type;
            }

            Spellbook = new List<int>();
            string spellbook = stream.ReadString();
            foreach (string spell in spellbook.Split(',')
                .Where(spell => spell != ""))
                Spellbook.Add(IO.ReadHex(spell));

            SpawnIntrinsics = new List<Mod>();
            string readIntrinsics = stream.ReadString();
            foreach (
                Mod m in
                    from mod in readIntrinsics.Split(',')
                    where mod != ""
                    select new Mod(
                        (ModType)
                            IO.ReadHex(mod.Split(':')[0]),
                        IO.ReadHex(mod.Split(':')[1])
                        )
                ) {
                    SpawnIntrinsics.Add(m);
                }

            ActorDefinitions[Type] = this;
            return stream;
        }
    }
}