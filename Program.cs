using System;
using System.Collections.Generic;
using System.Linq;

namespace JetFighterCombatSim
{
    public enum WeaponType
    {
        AIM120_AMRAAM,    // Medium-range air-to-air missile
        AIM9X_Sidewinder, // Short-range air-to-air missile
        M61A2_Vulcan,     // 20mm cannon
        R77,              // Russian medium-range missile
        R73,              // Russian short-range missile
        GSh301            // Russian 30mm cannon
    }

    public class Weapon
    {
        public WeaponType Type { get; private set; }
        public string Name { get; private set; }
        public int BaseDamage { get; private set; }
        public int Accuracy { get; private set; }
        public int Quantity { get; private set; }
        public int Range { get; private set; } // Added range property
        public bool RequiresLock { get; private set; } // For missiles

        public Weapon(WeaponType type, string name, int damage, int accuracy, int quantity, int range, bool requiresLock)
        {
            Type = type;
            Name = name;
            BaseDamage = damage;
            Accuracy = accuracy;
            Quantity = quantity;
            Range = range;
            RequiresLock = requiresLock;
        }

        public bool Fire()
        {
            if (Quantity <= 0) return false;
            Quantity--;
            return true;
        }
    }

    public class JetFighter
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public Dictionary<WeaponType, Weapon> Weapons { get; private set; } = new Dictionary<WeaponType, Weapon>();
        public int TotalDamageDealt { get; private set; }
        public int Distance { get; private set; } = 100; // Starting distance between aircraft
        private bool HasRadarLock { get; set; }
        private static Random random = new Random();

        public JetFighter(string name, int health, bool isWestern)
        {
            Name = name;
            Health = health;
            TotalDamageDealt = 0;
            InitializeWeapons(isWestern);
        }

        public void AddDamage(int damage)
        {
            TotalDamageDealt += damage;
        }

        private void InitializeWeapons(bool isWestern)
        {
            if (isWestern)
            {
                Weapons.Add(WeaponType.AIM120_AMRAAM, new Weapon(WeaponType.AIM120_AMRAAM, "AIM-120 AMRAAM", 50, 70, 4, 50, true));
                Weapons.Add(WeaponType.AIM9X_Sidewinder, new Weapon(WeaponType.AIM9X_Sidewinder, "AIM-9X Sidewinder", 40, 90, 2, 20, true));
                Weapons.Add(WeaponType.M61A2_Vulcan, new Weapon(WeaponType.M61A2_Vulcan, "M61A2 Vulcan", 20, 50, 500, 1, false));
            }
            else
            {
                Weapons.Add(WeaponType.R77, new Weapon(WeaponType.R77, "R-77", 50, 70, 4, 50, true));
                Weapons.Add(WeaponType.R73, new Weapon(WeaponType.R73, "R-73", 40, 90, 2, 20, true));
                Weapons.Add(WeaponType.GSh301, new Weapon(WeaponType.GSh301, "GSh-30-1", 30, 50, 500, 1, false));
            }
        }
    }

    public class AttackDetail
    {
        public string Attacker { get; set; }
        public string Target { get; set; }
        public int Damage { get; set; }
        public int TargetHealthAfterAttack { get; set; }

        public AttackDetail(string attacker, string target, int damage, int targetHealthAfterAttack)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            TargetHealthAfterAttack = targetHealthAfterAttack;
        }
    }

    class Program
    {
        private static Random random = new Random();
        static void Main(string[] args)
        {
            Console.WriteLine("Choose your jet:");
            Console.WriteLine("1. F-22 Raptor (Health: 100, Attack Power: 20)");
            Console.WriteLine("2. Sukhoi Su-57 Felon (Health: 100, Attack Power: 15)");
            int choice;
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null)
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                    continue;
                }
                if (int.TryParse(input, out choice))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                }
            }

            JetFighter playerJet;
            JetFighter enemyJet;

            if (choice == 1)
            {
                playerJet = new JetFighter("F-22 Raptor", 100, true);
                enemyJet = new JetFighter("Sukhoi Su-57 Felon", 100, false);
            }
            else
            {
                playerJet = new JetFighter("Sukhoi Su-57 Felon", 100, false);
                enemyJet = new JetFighter("F-22 Raptor", 100, true);
            }

            // Create an instance of Program to call the non-static method
            Program program = new Program();

            List<AttackDetail> attackDetails = new List<AttackDetail>();

            // Start the game loop or next steps here
            while (playerJet.Health > 0 && enemyJet.Health > 0)
            {
                // Player's turn
                program.ShowMenu(playerJet);
                Console.WriteLine("Enter the number of the weapon you want to use:");
                string? weaponChoice = Console.ReadLine();
                if (weaponChoice == null || !int.TryParse(weaponChoice, out int weaponIndex) || weaponIndex < 1 || weaponIndex > playerJet.Weapons.Count)
                {
                    Console.WriteLine("Invalid choice. Please try again.");
                    continue;
                }

                var selectedWeapon = playerJet.Weapons.ElementAt(weaponIndex - 1).Value;
                if (selectedWeapon.Fire())
                {
                    Console.WriteLine($"Fired {selectedWeapon.Name}!");
                    int damage = CalculateDamage(selectedWeapon);
                    enemyJet.Health -= damage;
                    playerJet.AddDamage(damage); // Use the new method to update TotalDamageDealt
                    attackDetails.Add(new AttackDetail(playerJet.Name, enemyJet.Name, damage, enemyJet.Health));
                    Console.WriteLine($"{enemyJet.Name} took {damage} damage. Remaining health: {enemyJet.Health}");
                }
                else
                {
                    Console.WriteLine($"{selectedWeapon.Name} is out of ammo!");
                }

                if (enemyJet.Health <= 0) break;

                // Enemy's turn (simple AI)
                var enemyWeapon = enemyJet.Weapons.Values.FirstOrDefault(w => w.Quantity > 0);
                if (enemyWeapon != null && enemyWeapon.Fire())
                {
                    Console.WriteLine($"{enemyJet.Name} fired {enemyWeapon.Name}!");
                    int damage = CalculateDamage(enemyWeapon);
                    playerJet.Health -= damage;
                    enemyJet.AddDamage(damage); // Use the new method to update TotalDamageDealt
                    attackDetails.Add(new AttackDetail(enemyJet.Name, playerJet.Name, damage, playerJet.Health));
                    Console.WriteLine($"{playerJet.Name} took {damage} damage. Remaining health: {playerJet.Health}");
                }
                else
                {
                    Console.WriteLine($"{enemyJet.Name} is out of ammo!");
                }

                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }

            // Display battle summary
            program.DisplayBattleSummary(attackDetails, playerJet, enemyJet);
        }

        // Update the ShowMenu method to accept a JetFighter parameter
        public void ShowMenu(JetFighter jetFighter)
        {
            Console.WriteLine($"\nCurrent Distance: {jetFighter.Distance}km");
            Console.WriteLine("\nChoose your weapon:");
            int index = 1;
            foreach (var weapon in jetFighter.Weapons)
            {
                Console.WriteLine($"{index}. {weapon.Value.Name} " +
                                 $"(Damage: {weapon.Value.BaseDamage}, " +
                                 $"Accuracy: {weapon.Value.Accuracy}%, " +
                                 $"Range: {weapon.Value.Range}km, " +
                                 $"Remaining: {weapon.Value.Quantity})");
                index++;
            }
        }

        public void DisplayBattleSummary(List<AttackDetail> attackDetails, JetFighter playerJet, JetFighter enemyJet)
        {
            Console.WriteLine("\nBattle Summary:");
            Console.WriteLine("Attacker\tTarget\t\tDamage\tTarget Health After Attack");
            foreach (var detail in attackDetails)
            {
                Console.WriteLine($"{detail.Attacker}\t\t{detail.Target}\t\t{detail.Damage}\t{detail.TargetHealthAfterAttack}");
            }

            Console.WriteLine("\nTotal Damage Dealt:");
            Console.WriteLine($"{playerJet.Name}: {playerJet.TotalDamageDealt}");
            Console.WriteLine($"{enemyJet.Name}: {enemyJet.TotalDamageDealt}");

            Console.WriteLine("\nGame Over!");
            if (playerJet.Health > 0)
            {
                Console.WriteLine($"{playerJet.Name} wins!");
            }
            else
            {
                Console.WriteLine($"{enemyJet.Name} wins!");
            }
        }

        private static int CalculateDamage(Weapon weapon)
        {
            return weapon.BaseDamage * weapon.Accuracy / 100;
        }
    }
}
