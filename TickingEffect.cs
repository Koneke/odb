using System;

namespace ODB
{
    public class TickingEffectDefinition
    {
        protected bool Equals(TickingEffectDefinition other)
        {
            return
                ID == other.ID &&
                string.Equals(Name, other.Name) &&
                Frequency == other.Frequency &&
                Equals(Effect, other.Effect)
            ;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ID;
                hashCode = (hashCode*397) ^
                           (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Frequency;
                hashCode = (hashCode*397) ^
                           (Effect != null ? Effect.GetHashCode() : 0);
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TickingEffectDefinition)obj);
        }

        public static TickingEffectDefinition[] Definitions =
            new TickingEffectDefinition[0xFFFF];
        public static int IDCounter;

        public int ID;
        public string Name;
        public int Frequency;
        public Action<Actor> Effect;

        public TickingEffectDefinition(
            string name,
            int frequency,
            Action<Actor> effect)
        {
            ID = IDCounter++;
            Name = name;
            Frequency = frequency;
            Effect = effect;

            Definitions[ID] = this;
        }
    }
}
