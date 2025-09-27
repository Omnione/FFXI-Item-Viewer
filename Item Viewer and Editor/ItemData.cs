using System.Collections.Generic;
using System.Drawing;

namespace Item_Viewer_and_Editor
{
    // This class holds static lookup data used throughout the application.
    public static class ItemData
    {
        // Data for mapping hexadecimal skill values to descriptive names
        public static readonly Dictionary<int, string> skillData = new Dictionary<int, string>
        {
            { 0x0000, "SKILL: PetItem/Non Throwable ammo/Grip"},
            { 0x0001, "SKILL: HandToHand" },
            { 0x0002, "SKILL: Dagger" },
            { 0x0003, "SKILL: Sword" },
            { 0x0004, "SKILL: Greatsword" },
            { 0x0005, "SKILL: Axe" },
            { 0x0006, "SKILL: Greataxe" },
            { 0x0007, "SKILL: Scythe" },
            { 0x0008, "SKILL: Polearm" },
            { 0x0009, "SKILL: Katana" },
            { 0x000A, "SKILL: Greatkatana" },
            { 0x000B, "SKILL: Club" },
            { 0x000C, "SKILL: Staff" },
            { 0x0019, "SKILL: Bow/Arrow" },
            { 0x001A, "SKILL: Marksmanship" },
            { 0x001B, "SKILL: Throwing" },
            { 0x002A, "SKILL: Horn/Flute"},
            { 0x0029, "SKILL: Harp"},
            { 0x0030, "SKILL: FishingRod/Bait" }
        };

        // Class structure for element data (name and color)
        public class ElementInfo
        {
            // FIX: Using null-forgiving operator to satisfy non-nullable property requirement
            public string Name { get; set; } = null!;
            public Color Color { get; set; }
        }

        // Data for mapping hexadecimal element values to names and colors
        public static readonly Dictionary<int, ElementInfo> elementData = new Dictionary<int, ElementInfo>
        {
            { 0, new ElementInfo { Name = "Fire", Color = Color.OrangeRed } },
            { 1, new ElementInfo { Name = "Ice", Color = Color.SkyBlue } },
            { 2, new ElementInfo { Name = "Wind", Color = Color.LimeGreen } },
            { 3, new ElementInfo { Name = "Earth", Color = Color.Sienna } },
            { 4, new ElementInfo { Name = "Lightning", Color = Color.Yellow } },
            { 5, new ElementInfo { Name = "Water", Color = Color.Blue } },
            { 6, new ElementInfo { Name = "Light", Color = Color.White } },
            { 7, new ElementInfo { Name = "Dark", Color = Color.Purple } }
        };

        // Data for mapping hexadecimal equipment slot values to names
        public static readonly Dictionary<int, string> slotData = new Dictionary<int, string>
        {
            { 0x0001, "Main Hand" },
            { 0x0002, "Sub Hand" },
            { 0x0004, "Ranged" },
            { 0x0008, "Ammo" },
            { 0x0010, "Head" },
            { 0x0020, "Body" },
            { 0x0040, "Hands" },
            { 0x0080, "Legs" },
            { 0x0100, "Feet" },
            { 0x0200, "Neck" },
            { 0x0400, "Waist" },
            { 0x0800, "Left Ear" },
            { 0x1000, "Right Ear" },
            { 0x2000, "Left Ring" },
            { 0x4000, "Right Ring" },
            { 0x8000, "Back" }
        };

        // Class structure for flag data (name and value)
        public class FlagInfo
        {
            // FIX: Using null-forgiving operator to satisfy non-nullable property requirement
            public string Name { get; set; } = null!;
            public int Value { get; set; }
        }

        // List to store the hex flag data
        public static readonly List<FlagInfo> hexFlags = new List<FlagInfo>
        {
            new FlagInfo { Name = "WALLHANGING", Value = 0x0001 },
            new FlagInfo { Name = "ITEM FLAG 01", Value = 0x0002 },
            new FlagInfo { Name = "AVAILABLE FROM MYSTERY BOX (GOBBIE BOX ECT.)", Value = 0x0004 },
            new FlagInfo { Name = "AVAILABLE FROM MOG GARDEN", Value = 0x0008 },
            new FlagInfo { Name = "CAN MAIL TO SAME ACCOUNT", Value = 0x0010 },
            new FlagInfo { Name = "INSCRIBABLE", Value = 0x0020 },
            new FlagInfo { Name = "CANNOT PUT UP FOR AUCTION", Value = 0x0040 },
            new FlagInfo { Name = "ITEM IS A SCROLL", Value = 0x0080 },
            new FlagInfo { Name = "LINKSHELL (PEARL/SACK)", Value = 0x0100 },
            new FlagInfo { Name = "CAN USE ITEM (EXAMPLE: CHARGED ITEMS)", Value = 0x0200 },
            new FlagInfo { Name = "CAN TRADE TO AN NPC", Value = 0x0400 },
            new FlagInfo { Name = "CAN EQUIP ITEM", Value = 0x0800 },
            new FlagInfo { Name = "NOT SELABLE", Value = 0x1000 },
            new FlagInfo { Name = "NO DELIVERY FROM AH", Value = 0x2000 },
            new FlagInfo { Name = "EX", Value = 0x4000 },
            new FlagInfo { Name = "RARE", Value = 0x8000 }
        };

        // Data for mapping job names to their respective hexadecimal bitmask values
        public static readonly Dictionary<string, int> jobHexData = new Dictionary<string, int>
        {
            { "WAR", 0x00001 },
            { "MNK", 0x00002 },
            { "WHM", 0x00004 },
            { "BLM", 0x00008 },
            { "RDM", 0x00010 },
            { "THF", 0x00020 },
            { "PLD", 0x00040 },
            { "DRK", 0x00080 },
            { "BST", 0x00100 },
            { "BRD", 0x00200 },
            { "RNG", 0x00400 },
            { "SAM", 0x00800 },
            { "NIN", 0x01000 },
            { "DRG", 0x02000 },
            { "SMN", 0x04000 },
            { "BLU", 0x08000 },
            { "COR", 0x10000 },
            { "PUP", 0x20000 },
            { "DNC", 0x40000 },
            { "SCH", 0x80000 },
            { "GEO", 0x100000 },
            { "RUN", 0x200000 }
        };

        // Data for mapping job names to their respective LSB (Least Significant Bit) values 
        // LSB is typically used in the game's item_basic table for job requirements
        public static readonly Dictionary<string, int> jobLSBData = new Dictionary<string, int>
        {
            { "WAR", 1 },
            { "MNK", 2 },
            { "WHM", 4 },
            { "BLM", 8 },
            { "RDM", 16 },
            { "THF", 32 },
            { "PLD", 64 },
            { "DRK", 128 },
            { "BST", 256 },
            { "BRD", 512 },
            { "RNG", 1024 },
            { "SAM", 2048 },
            { "NIN", 4096 },
            { "DRG", 8192 },
            { "SMN", 16384 },
            { "BLU", 32768 },
            { "COR", 65536 },
            { "PUP", 131072 },
            { "DNC", 262144 },
            { "SCH", 524288 },
            { "GEO", 1048576 },
            { "RUN", 2097152 }
        };
    }
}
