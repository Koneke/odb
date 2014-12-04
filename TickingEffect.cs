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

        public TickingEffect Instantiate(Actor holder)
        {
            return new TickingEffect(holder, this);
        }
    }

    public class TickingEffect
    {
        protected bool Equals(TickingEffect other)
        {
            return
                Equals(Definition, other.Definition) &&
                Timer == other.Timer &&
                Equals(Holder, other.Holder) &&
                LifeTime == other.LifeTime && 
                Die.Equals(other.Die)
            ;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Definition != null ?
                                Definition.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Timer;
                hashCode = (hashCode*397) ^
                           (Holder != null ? Holder.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ LifeTime;
                hashCode = (hashCode*397) ^ Die.GetHashCode();
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TickingEffect)obj);
        }

        public TickingEffectDefinition Definition;
        public int Timer;
        public Actor Holder;
        public int LifeTime;

        //does not need to be saved
        //if die, it should just not be saved (since it's about to be removed)
        public bool Die;

        public int Type { get { return Definition.ID; } }

        public TickingEffect(
            Actor holder,
            TickingEffectDefinition definition
        ) {
            Holder = holder;
            Definition = definition;
            Timer = 0;
            LifeTime = 0;
        }

        public TickingEffect(string s)
        {
            ReadTickingEffect(s);
        }

        public void Tick()
        {
            LifeTime++;
            Timer--;

            if (Timer > 0) return;

            Definition.Effect(Holder);
            Timer += Definition.Frequency;
        }

        public Stream WriteTickingEffect()
        {
            Stream stream = new Stream();
            stream.Write(Definition.ID, 4);
            stream.Write(Timer, 4); //if you need more than 0xFFFF ticks, gtfo
            //0xFFFFFFFF is a ridiculous limit, no single effect should
            //live that long without resetting its own lifetime before that
            stream.Write(LifeTime, 8);
            return stream;
        }

        public Stream ReadTickingEffect(string s)
        {
            Stream stream = new Stream(s);
            Definition = TickingEffectDefinition.Definitions
                [stream.ReadHex(4)];
            Timer = stream.ReadHex(4);
            LifeTime = stream.ReadHex(8);
            return stream;
        }
    }
}
