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

        public static ActorDefinition[] ActorDefinitions =
            new ActorDefinition[0xFFFF];

        public bool Named; //for uniques and what not
        public string Strength, Dexterity, Intelligence;
        public int Speed, Quickness;
        public string HitDie, ManaDie;
        public List<DollSlot> BodyParts;
        public int CorpseType;
        public List<int> Spellbook;
        //intrinsics spawned with
        public List<Mod> SpawnIntrinsics;
        public AttackComponent NaturalAttack;

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
            ActorDefinitions[Type] = this;

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

        public ActorDefinition(string s) : base(s)
        {
            ReadActorDefinition(s);
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
                    Util.Game.Log("~ERROR~: Bad stat.");
                    break;
            }
        }

        public Stream WriteActorDefinition()
        {
            Stream stream = WriteGObjectDefinition();

            stream.Write(Named);
            stream.Write(Strength);
            stream.Write(Dexterity);
            stream.Write(Intelligence);
            stream.Write(HitDie);
            stream.Write(ManaDie);
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

            stream.Write(NaturalAttack.WriteComponent().ToString(), false);

            return stream;
        }
        public Stream ReadActorDefinition(string s)
        {
            Stream stream = ReadGObjectDefinition(s);

            Named = stream.ReadBool();
            Strength = stream.ReadString();
            Dexterity = stream.ReadString();
            Intelligence = stream.ReadString();
            HitDie = stream.ReadString();
            ManaDie = stream.ReadString();
            Speed = stream.ReadHex(2);
            Quickness = stream.ReadHex(2);

            BodyParts = new List<DollSlot>();
            foreach (string ss in stream.ReadString().Split(',')
                .Where(ss => ss != ""))
                BodyParts.Add((DollSlot)int.Parse(ss));

            CorpseType = stream.ReadHex(4);

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

            //nat attack
            NaturalAttack =
            (AttackComponent)
            Component.CreateComponent(
                stream.ReadString(),
                stream.ReadBlock()
            );

            ActorDefinitions[Type] = this;
            return stream;
        }
    }
}