using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSRSCombatSim.Models {

    public enum AttackType {
        Stab,
        Slash,
        Crush,
        Ranged
    }

    public enum CombatStyle {
        Accurate,
        Aggressive,
        Defensive,
        Controlled
    }

    public enum GearSlot {
        Head,
        Cape,
        Neck,
        Ammo,
        ExtraAmmo,
        Weapon,
        Shield,
        Body,
        Legs,
        Hands,
        Feet,
        Ring
    }

    public enum WeaponType {
        None,
        Dagger,
        Sword,
        Longsword,
        Scimitar,
        TwoHandedSword,
        Battleaxe,
        Axe,
        Whip,
        Halberd,
        Mace,
        Warhammer,
        Maul,
        Spear,
        Pickaxe,
        Rapier,
        ThrowingKnife,
        Dart,
        Bow,
        Crossbow,
        Chinchompa,
        Blowpipe
    }

    public class Combatant {

        public Combatant(string name, int maxHp) {
            Name = name;
            MaxHitpoints = maxHp;
            Hitpoints = maxHp;
        }

        public List<Prayer> ActivePrayers { get; set; } = [];

        // Combat stats
        public int AttackLevel { get; set; }

        public CombatStyle CombatStyle { get; set; }
        public int DefenceLevel { get; set; }
        public Weapon EquippedWeapon { get; set; }
        public int Hitpoints { get; set; }
        public bool IsDead => Hitpoints <= 0;
        public int MaxHitpoints { get; set; }

        // Defensive bonuses (simplified, can be expanded)
        public int MeleeDefenceBonus { get; set; }

        public string Name { get; set; }

        // Prayer points for the combatant (max 100 in OSRS)
        public int PrayerPoints { get; set; } = 100;

        public int RangedDefenceBonus { get; set; }
        public int RangedLevel { get; set; }
        public int StrengthLevel { get; set; }
    }

    public class Prayer {
        public double AccuracyMultiplier { get; set; } = 1.0;
        public int DrainRatePerTick { get; set; } = 0;
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public bool ProtectFromMelee { get; set; } = false;
        public bool ProtectFromRanged { get; set; } = false;
        public double StrengthMultiplier { get; set; } = 1.0;
    }

    public class Weapon {
        public int AttackBonus { get; set; }
        public AttackType AttackType { get; set; }
        public string Name { get; set; }
        public int RangedBonus { get; set; }
        public int SpeedTicks { get; set; }  // How many game ticks per attack (usually 4-6)

        // Simplified aggregate bonus
        public int StrengthBonus { get; set; }
    }
}