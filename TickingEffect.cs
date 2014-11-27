﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODB
{
    public class TickingEffectDefinition
    {
        public static TickingEffectDefinition[] Definitions =
            new TickingEffectDefinition[0xFFFF];
        public static int IDCounter;

        public int id;
        public string Name;
        public int Frequency;
        public Action<Actor> Effect;

        public TickingEffectDefinition(
            string Name,
            int frequency,
            Action<Actor> Effect)
        {
            id = IDCounter++;
            this.Name = Name;
            this.Frequency = frequency;
            this.Effect = Effect;

            Definitions[id] = this;
        }

        public TickingEffect Instantiate(Actor Holder)
        {
            return new TickingEffect(Holder, this);
        }
    }

    public class TickingEffect
    {
        public TickingEffectDefinition Definition;
        public int Timer;
        public Actor Holder;

        //does not need to be saved
        //if die, it should just not be saved (since it's about to be removed)
        public bool Die;

        public int Type { get { return Definition.id; } }

        public TickingEffect(
            Actor Holder,
            TickingEffectDefinition definition
        ) {
            this.Holder = Holder;
            this.Definition = definition;
            Timer = 0;
        }

        public TickingEffect(string s)
        {
            ReadTickingEffect(s);
        }

        public void Tick()
        {
            Timer--;
            if (Timer <= 0)
            {
                Definition.Effect(Holder);
                Timer += Definition.Frequency;
            }
        }

        public Stream WriteTickingEffect()
        {
            Stream stream = new Stream();
            stream.Write(Definition.id, 4);
            stream.Write(Timer, 4); //if you need more than 0xFFFF ticks, gtfo
            return stream;
        }

        public Stream ReadTickingEffect(string s)
        {
            Stream stream = new Stream(s);
            Definition = TickingEffectDefinition.Definitions[stream.ReadHex(4)];
            Timer = stream.ReadHex(4);
            return stream;
        }
    }

}