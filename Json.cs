using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ODB
{
    public class ActorConverter : JsonConverter
    {
        public override void WriteJson(
            JsonWriter w,
            object value,
            JsonSerializer s
        ) {
            ActorDefinition def = (ActorDefinition)value;
            w.Formatting = Formatting.Indented;
            w.WriteStartObject();

            w.WritePropertyName("Name"); w.WriteValue(def.Name);
            w.WritePropertyName("Type"); s.Serialize(w, def.ActorType);
            w.WritePropertyName("Tile"); w.WriteValue((byte)def.Tile);
            w.WritePropertyName("Foreground"); s.Serialize(w, def.Foreground);
            w.WritePropertyName("Background"); s.Serialize(w, def.Background);

            w.WritePropertyName("Named"); w.WriteValue(def.Named);
            w.WritePropertyName("GenerationType"); s.Serialize(w, def.GenerationType);
            w.WritePropertyName("Strength"); w.WriteValue(def.Strength);
            w.WritePropertyName("Dexterity"); w.WriteValue(def.Dexterity);
            w.WritePropertyName("Intelligence"); w.WriteValue(def.Intelligence);
            w.WritePropertyName("Speed"); w.WriteValue(def.Speed);
            w.WritePropertyName("Quickness"); w.WriteValue(def.Quickness);
            w.WritePropertyName("HitDie"); w.WriteValue(def.HitDie);
            w.WritePropertyName("ManaDie"); w.WriteValue(def.ManaDie);
            w.WritePropertyName("Experience"); w.WriteValue(def.Experience);
            w.WritePropertyName("Difficulty"); w.WriteValue(def.Difficulty);
            w.WritePropertyName("BodyParts"); s.Serialize(w, def.BodyParts);
            w.WritePropertyName("CorpseType"); w.WriteValue(def.CorpseType);
            w.WritePropertyName("Spellbook"); s.Serialize(w, def.Spellbook);
            w.WritePropertyName("SpawnIntrinsics"); s.Serialize(w, def.SpawnIntrinsics);
            w.WritePropertyName("NaturalAttack"); s.Serialize(w, def.NaturalAttack);

            w.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        ) {
            JObject jObj = JObject.Load(reader);

            string name = jObj["Name"].ToObject<string>();
            ActorID aid = jObj["Type"].ToObject<ActorID>();
            char tile = jObj["Tile"].ToObject<char>();
            Color foreground = jObj["Foreground"].ToObject<Color>();
            Color? background = jObj["Background"].ToObject<Color?>();

            bool named = jObj["Named"].ToObject<bool>();
            Monster.GenerationType generationType = jObj["GenerationType"].ToObject<Monster.GenerationType>();
            string strength = jObj["Strength"].ToObject<string>();
            string dexterity = jObj["Dexterity"].ToObject<string>();
            string intelligence = jObj["Intelligence"].ToObject<string>();
            int speed = jObj["Speed"].ToObject<int>();
            int quickness = jObj["Quickness"].ToObject<int>();
            string hitDie = jObj["HitDie"].ToObject<string>();
            string manaDie = jObj["ManaDie"].ToObject<string>();
            int experience = jObj["Experience"].ToObject<int>();
            int difficulty = jObj["Difficulty"].ToObject<int>();
            List<DollSlot> bodyParts = jObj["BodyParts"].ToObject<List<DollSlot>>();
            ItemID corpseType = jObj["CorpseType"].ToObject<ItemID>();
            List<int> spellbook = jObj["Spellbook"].ToObject<List<int>>();
            List<Mod> spawnIntrinsics = jObj["SpawnIntrinsics"].ToObject<List<Mod>>();
            AttackComponent naturalAttack = jObj["NaturalAttack"].ToObject<AttackComponent>();

            return new ActorDefinition
            {
                Name = name,
                ActorType = aid,
                Tile = tile,
                Foreground = foreground,
                Background = background,
                Named = named,
                GenerationType = generationType,
                Strength = strength,
                Dexterity = dexterity,
                Intelligence = intelligence,
                Speed = speed,
                Quickness = quickness,
                HitDie = hitDie,
                ManaDie = manaDie,
                Experience = experience,
                Difficulty = difficulty,
                BodyParts = bodyParts,
                CorpseType = corpseType,
                Spellbook = spellbook,
                SpawnIntrinsics = spawnIntrinsics,
                NaturalAttack = naturalAttack
            };
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }

    public class ItemConverter : JsonConverter
    {
        public override void WriteJson(
            JsonWriter w,
            object value,
            JsonSerializer s
        ) {
            ItemDefinition def = (ItemDefinition)value;
            w.Formatting = Formatting.Indented;
            w.WriteStartObject();

            w.WritePropertyName("Name"); w.WriteValue(def.Name);
            w.WritePropertyName("Type"); s.Serialize(w, def.ItemType);
            w.WritePropertyName("Tile"); w.WriteValue((byte)def.Tile);
            w.WritePropertyName("Foreground"); s.Serialize(w, def.Foreground);
            w.WritePropertyName("Background"); s.Serialize(w, def.Background);

            w.WritePropertyName("Stacking"); w.WriteValue(def.Stacking);
            w.WritePropertyName("Category"); w.WriteValue(def.Category);
            w.WritePropertyName("Weight"); w.WriteValue(def.Weight);
            w.WritePropertyName("Value"); w.WriteValue(def.Value);
            w.WritePropertyName("Material"); s.Serialize(w, def.Material);
            w.WritePropertyName("Health"); w.WriteValue(def.Health);
            w.WritePropertyName("Tags"); s.Serialize(w, def.Tags);
            w.WritePropertyName("Components"); s.Serialize(w, def.Components);
            w.WritePropertyName("GenerationLowBound"); w.WriteValue(def.GenerationLowBound);
            w.WritePropertyName("GenerationHighBound"); w.WriteValue(def.GenerationHighBound);

            w.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        ) {
            JObject jObj = JObject.Load(reader);

            string name = jObj["Name"].ToObject<string>();
            ItemID type = jObj["Type"].ToObject<ItemID>();
            char tile = jObj["Tile"].ToObject<char>();
            Color foreground = jObj["Foreground"].ToObject<Color>();
            Color? background = jObj["Background"].ToObject<Color?>();

            bool stacking = jObj["Stacking"].ToObject<bool>();
            int category = jObj["Category"].ToObject<int>();
            int weight = jObj["Weight"].ToObject<int>();
            int value = jObj["Value"].ToObject<int>();
            Material material = jObj["Material"].ToObject<Material>();
            int health = jObj["Health"].ToObject<int>();
            List<ItemTag> tags = jObj["Tags"].ToObject<List<ItemTag>>();
            int genLowBound = jObj["GenerationLowBound"].ToObject<int>();
            int genHighBound = jObj["GenerationHighBound"].ToObject<int>();

            List<Component> components = new List<Component>();

            var a = jObj["Components"].ToList();
            foreach (JToken j in a)
            {
                string componentType = j["$type"].Value<string>();
                componentType = componentType
                    .Substring(4, componentType.Length - 9);

                if (componentType == AttackComponent.ComponentName)
                    components.Add(j.ToObject<AttackComponent>());
                else if (componentType == ContainerComponent.ComponentName)
                    components.Add(j.ToObject<ContainerComponent>());
                else if (componentType == DrinkableComponent.ComponentName)
                    components.Add(j.ToObject<DrinkableComponent>());
                else if (componentType == EdibleComponent.ComponentName)
                    components.Add(j.ToObject<EdibleComponent>());
                else if (componentType == EffectComponent.ComponentName)
                    components.Add(j.ToObject<EffectComponent>());
                else if (componentType == LauncherComponent.ComponentName)
                    components.Add(j.ToObject<LauncherComponent>());
                else if (componentType == LearnableComponent.ComponentName)
                    components.Add(j.ToObject<LearnableComponent>());
                else if (componentType == ProjectileComponent.ComponentName)
                    components.Add(j.ToObject<ProjectileComponent>());
                else if (componentType == ReadableComponent.ComponentName)
                    components.Add(j.ToObject<ReadableComponent>());
                else if (componentType == UsableComponent.ComponentName)
                    components.Add(j.ToObject<UsableComponent>());
                else if (componentType == WearableComponent.ComponentName)
                    components.Add(j.ToObject<WearableComponent>());
                else
                    throw new Exception();
            }

            return new ItemDefinition
            {
                Name = name,
                ItemType = type,
                Tile = tile,
                Foreground = foreground,
                Background = background,
                Stacking = stacking,
                Category = category,
                Weight = weight,
                Value = value,
                Material = material,
                Health = health,
                Tags = tags,
                Components = components,
                GenerationLowBound = genLowBound,
                GenerationHighBound = genHighBound
            };
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}
