using OSRSCombatSim.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OSRSCombatSim.Engine {

    public class CombatEngine {
        private readonly Timer _combatTimer;
        private readonly int _tickIntervalMs = 600;
        private int _monsterAttackTickCounter = 0;
        private int _playerAttackTickCounter = 0;

        public CombatEngine(Combatant player, Combatant monster) {
            Player = player;
            Monster = monster;

            _combatTimer = new Timer(_tickIntervalMs);
            _combatTimer.Elapsed += OnTick;
        }

        public event EventHandler<CombatEventArgs> AttackOccurred;

        public event EventHandler CombatEnded;

        public bool AutoBattleEnabled { get; set; } = true;

        public Combatant Monster { get; }
        public Combatant Player { get; }

        public void ManualPlayerAttack() {
            if (Player.IsDead || Monster.IsDead) {
                return;
            }

            // Only perform if not auto-battling or to supplement it
            PerformAttack(Player, Monster);
        }

        public void Start() {
            _combatTimer.Start();
        }

        public void Stop() {
            _combatTimer.Stop();
        }

        private void ApplyDamage(Combatant defender, int damage, AttackType attackType) {
            defender.Hitpoints -= damage;
            if (defender.Hitpoints < 0) {
                defender.Hitpoints = 0;
            }
        }

        private int CalculateDamage(Combatant attacker, Combatant defender, AttackType attackType) {
            // Base max hit calculation simplified:
            double baseMaxHit = attacker.StrengthLevel * GetPrayerStrengthMultiplier(attacker, attackType) +
                                attacker.EquippedWeapon.StrengthBonus;

            // Protect prayers reduce damage taken
            if (IsProtectionActive(defender, attackType)) {
                baseMaxHit = 0; // Full protection blocks damage
            }

            // Max hit must be at least 1 if attack hits (except protection)
            int maxHit = Math.Max(1, (int)Math.Floor(baseMaxHit));

            var rng = new Random();
            return rng.Next(0, maxHit + 1); // Damage is random between 0 and maxHit inclusive
        }

        private bool CalculateHitSuccess(Combatant attacker, Combatant defender, AttackType attackType) {
            // Simplified OSRS-style accuracy formula:

            // Effective Attack
            double effectiveAttack = attacker.AttackLevel * GetPrayerAccuracyMultiplier(attacker, attackType) +
                attacker.EquippedWeapon.AttackBonus;

            // Effective Defence
            double effectiveDefence = defender.DefenceLevel * GetDefencePrayerMultiplier(defender, attackType) +
                GetDefenceBonus(defender, attackType);

            // Chance to hit calculation (very simplified)
            double hitChance = effectiveAttack / (effectiveAttack + effectiveDefence);

            // Clamp hitChance between 0.05 and 0.95 (5%-95%)
            hitChance = Math.Clamp(hitChance, 0.05, 0.95);

            var rng = new Random();
            return rng.NextDouble() < hitChance;
        }

        private void DrainPrayerPoints(Combatant c) {
            int totalDrain = 0;
            foreach (var p in c.ActivePrayers) {
                if (p.IsActive)
                    totalDrain += p.DrainRatePerTick;
            }
            c.PrayerPoints -= totalDrain;
            if (c.PrayerPoints < 0)
                c.PrayerPoints = 0;

            // Auto-disable prayers if prayer points run out
            if (c.PrayerPoints == 0) {
                foreach (var p in c.ActivePrayers)
                    p.IsActive = false;
            }
        }

        private int GetDefenceBonus(Combatant c, AttackType attackType) {
            if (attackType == AttackType.Stab || attackType == AttackType.Slash || attackType == AttackType.Crush)
                return c.MeleeDefenceBonus;
            else if (attackType == AttackType.Ranged)
                return c.RangedDefenceBonus;
            else
                return 0; // Or throw, if unexpected
        }

        private double GetDefencePrayerMultiplier(Combatant c, AttackType attackType) {
            double multiplier = 1.0;
            foreach (var p in c.ActivePrayers) {
                if (!p.IsActive) continue;

                if ((attackType == AttackType.Stab || attackType == AttackType.Slash || attackType == AttackType.Crush) && p.ProtectFromMelee) {
                    multiplier *= 1.15;
                } else if (attackType == AttackType.Ranged && p.ProtectFromRanged) {
                    multiplier *= 1.15;
                }
            }
            return multiplier;
        }

        private double GetPrayerAccuracyMultiplier(Combatant c, AttackType attackType) {
            double multiplier = 1.0;
            foreach (var p in c.ActivePrayers)
                if (p.IsActive)
                    multiplier *= p.AccuracyMultiplier;
            return multiplier;
        }

        private double GetPrayerStrengthMultiplier(Combatant c, AttackType attackType) {
            double multiplier = 1.0;
            foreach (var p in c.ActivePrayers)
                if (p.IsActive)
                    multiplier *= p.StrengthMultiplier;
            return multiplier;
        }

        private bool IsProtectionActive(Combatant defender, AttackType attackType) {
            foreach (var prayer in defender.ActivePrayers) {
                if (!prayer.IsActive) continue;

                // Protect from Melee applies if attackType is Stab, Slash, or Crush
                if ((attackType == AttackType.Stab || attackType == AttackType.Slash || attackType == AttackType.Crush)
                    && prayer.ProtectFromMelee) {
                    return true;
                }

                // Protect from Ranged applies if attackType is Ranged
                if (attackType == AttackType.Ranged && prayer.ProtectFromRanged) {
                    return true;
                }
            }
            return false;
        }

        private void OnTick(object? sender, ElapsedEventArgs e) {
            if (Player.IsDead || Monster.IsDead) {
                _combatTimer.Stop();
                CombatEnded?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Prayer drain per tick
            DrainPrayerPoints(Player);
            DrainPrayerPoints(Monster);

            // Player attack logic
            _playerAttackTickCounter++;
            if (_playerAttackTickCounter >= Player.EquippedWeapon.SpeedTicks) {
                _playerAttackTickCounter = 0;
                if (AutoBattleEnabled)
                    PerformAttack(Player, Monster);
            }

            // Monster attack logic
            _monsterAttackTickCounter++;
            if (_monsterAttackTickCounter >= Monster.EquippedWeapon.SpeedTicks) {
                _monsterAttackTickCounter = 0;
                if (AutoBattleEnabled)
                    PerformAttack(Monster, Player);
            }
        }

        private void PerformAttack(Combatant attacker, Combatant defender) {
            var attackType = attacker.EquippedWeapon.AttackType;
            bool hitSuccess = CalculateHitSuccess(attacker, defender, attackType);
            int damage = 0;

            if (hitSuccess) {
                damage = CalculateDamage(attacker, defender, attackType);
                ApplyDamage(defender, damage, attackType);
            }

            AttackOccurred?.Invoke(this, new CombatEventArgs(attacker, defender, damage, hitSuccess, attackType));

            if (defender.IsDead) {
                CombatEnded?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class CombatEventArgs : EventArgs {

        public CombatEventArgs(Combatant attacker, Combatant defender, int damage, bool hitSuccess, AttackType attackType) {
            Attacker = attacker;
            Defender = defender;
            Damage = damage;
            HitSuccess = hitSuccess;
            AttackType = attackType;
        }

        public Combatant Attacker { get; }
        public AttackType AttackType { get; }
        public int Damage { get; }
        public Combatant Defender { get; }
        public bool HitSuccess { get; }
    }
}