using System;
using System.Collections.Generic;

namespace ODB
{
    public enum Material
    {
        Organic,
        Metal,
        Wood,
        Leather,
        Paper,
        Cloth,
        Glass,
    }

    public class Materials
    {
        public static Dictionary<Material, List<string>> DamageStrings
            = new Dictionary<Material, List<string>>()
        {
            { Material.Metal, new List<string> {
                "scratched", "dented", "battered" }},
            { Material.Wood, new List<string> {
                "scratched", "splintered", "cracked" }},
            { Material.Cloth, new List<string> {
                "torn" }},
            { Material.Paper, new List<string> {
                "ripped" }},
            { Material.Organic, new List<string> {
                "leaky", "mashed" }},
        };

        //likelyness to take damage when thrown/bashed
        //d20 >= value
        //very early test values, to be tweaked, maybe go to d40 or something,
        //we'll see.
        private const int OrganicHardness = 10;
        private const int MetalHardness = 20;
        private const int WoodHardness = 16;
        private const int LeatherHardness = 18;
        private const int PaperHardness = 7;
        private const int ClothHardness = 20;
        private const int GlassHardness = 0; //guaranteed

        public static int GetHardness(Material m)
        {
            switch (m)
            {
                case Material.Organic: return OrganicHardness;
                case Material.Wood: return WoodHardness;
                case Material.Metal: return MetalHardness;
                case Material.Leather: return LeatherHardness;
                case Material.Paper: return PaperHardness;
                case Material.Cloth: return ClothHardness;
                case Material.Glass: return GlassHardness;
                default: throw new ArgumentException();
            }
        }

        public static Material ReadMaterial(string s)
        {
            switch(s.ToLower())
            {
                case "mt_organic": return Material.Organic;
                case "mt_wood": return Material.Wood;
                case "mt_metal": return Material.Metal;
                case "mt_leather": return Material.Leather;
                case "mt_paper": return Material.Paper;
                case "mt_cloth": return Material.Cloth;
                case "mt_glass": return Material.Glass;
                default: throw new ArgumentException();
            }
        }

        public static string WriteMaterial(Material m)
        {
            switch(m)
            {
                case Material.Organic: return "mt_organic";
                case Material.Wood: return "mt_wood";
                case Material.Metal: return "mt_metal";
                case Material.Leather: return "mt_leather";
                case Material.Paper: return "mt_paper";
                case Material.Cloth: return "mt_cloth";
                case Material.Glass: return "mt_glass";
                default: throw new ArgumentException();
            }
        }
    }
}