namespace jet_fighter
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

    public enum EnemyStrategy
    {
        Aggressive,
        Defensive,
        Evasive
    }

    public class Weapon
    {
        public WeaponType Type { get; private set; }
        public string Name { get; private set; }
        public int BaseDamage { get; private set; }
        public int Accuracy { get; private set; }
        public int Quantity { get; private set; }
        public int Range { get; private set; }
        public bool RequiresLock { get; private set; }

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
        public int Distance { get; set; } = 100; // Starting distance between aircraft
        public bool IsPlayer { get; set; }
        public int ManeuverCapability { get; set; } = 70; // Base value that affects distance changes
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
                // Slightly reduce Western aircraft weapons while keeping them competitive
                Weapons.Add(WeaponType.AIM120_AMRAAM, new Weapon(WeaponType.AIM120_AMRAAM, "AIM-120 AMRAAM", 48, 75, 4, 50, true));
                Weapons.Add(WeaponType.AIM9X_Sidewinder, new Weapon(WeaponType.AIM9X_Sidewinder, "AIM-9X Sidewinder", 40, 90, 2, 22, true));
                Weapons.Add(WeaponType.M61A2_Vulcan, new Weapon(WeaponType.M61A2_Vulcan, "M61A2 Vulcan", 28, 55, 500, 3, false));
            }
            else
            {
                // Boost Russian aircraft weapons to make the enemy more challenging
                Weapons.Add(WeaponType.R77, new Weapon(WeaponType.R77, "R-77", 52, 78, 4, 55, true));
                Weapons.Add(WeaponType.R73, new Weapon(WeaponType.R73, "R-73", 48, 92, 2, 22, true));
                Weapons.Add(WeaponType.GSh301, new Weapon(WeaponType.GSh301, "GSh-30-1", 32, 60, 400, 2, false));
            }
        }
        public void UpdateDistance(JetFighter opponent, int distanceChange)
        {
            Distance = Math.Max(1, Math.Min(150, Distance + distanceChange));
        }

        public Weapon SelectBestWeapon(JetFighter opponent, EnemyStrategy strategy)
        {
            if (!IsPlayer) // Enhanced logic for enemy only
            {
                // Get weapons with ammo
                var weaponsWithAmmo = Weapons.Values.Where(w => w.Quantity > 0).ToList();
                if (!weaponsWithAmmo.Any())
                    return new Weapon(WeaponType.M61A2_Vulcan, "Default Weapon", 0, 0, 0, 0, false);

                // Special logic for low player health - go for the kill with highest damage
                if (opponent.Health < 25)
                {
                    var bestDamageWeapon = weaponsWithAmmo
                        .Where(w => w.Range >= Distance * 0.9) // Within 90% of range to account for movement
                        .OrderByDescending(w => w.BaseDamage * w.Accuracy / 100.0)
                        .FirstOrDefault();

                    if (bestDamageWeapon != null)
                        return bestDamageWeapon;
                }

                // Filter weapons that are in range (or close to in range)
                var inRangeWeapons = weaponsWithAmmo.Where(w => w.Range >= Distance * 0.95).ToList();

                // If no weapons in range or close to range, fallback to any weapon
                if (!inRangeWeapons.Any())
                    inRangeWeapons = weaponsWithAmmo;

                // Enhanced weapon selection based on strategy and circumstances
                switch (strategy)
                {
                    case EnemyStrategy.Aggressive:
                        // When aggressive, prioritize damage but consider accuracy too
                        return inRangeWeapons
                            .OrderByDescending(w => (w.BaseDamage * 0.7) + (w.Accuracy * 0.3))
                            .First();

                    case EnemyStrategy.Defensive:
                        // When defensive, prioritize accuracy and range
                        return inRangeWeapons
                            .OrderByDescending(w => (w.Accuracy * 0.6) + (w.Range * 0.4))
                            .First();

                    case EnemyStrategy.Evasive:
                        // When evasive, prioritize range
                        return inRangeWeapons
                            .OrderByDescending(w => w.Range * 1.5)
                            .First();

                    default:
                        return inRangeWeapons.First();
                }
            }

            // Original logic for player
            // Filter weapons that are in range and have ammo
            var availableWeapons = Weapons.Values.Where(w => w.Quantity > 0 && w.Range >= Distance).ToList();

            // If no weapons in range, try to find any weapon with ammo
            if (!availableWeapons.Any())
                availableWeapons = Weapons.Values.Where(w => w.Quantity > 0).ToList();

            if (!availableWeapons.Any())
                return new Weapon(WeaponType.M61A2_Vulcan, "Default Weapon", 0, 0, 0, 0, false);

            switch (strategy)
            {
                case EnemyStrategy.Aggressive:
                    return availableWeapons.OrderByDescending(w => w.BaseDamage * (w.Accuracy / 50.0)).First();
                case EnemyStrategy.Defensive:
                    return availableWeapons.OrderByDescending(w => w.Accuracy * (w.Range - Distance)).First();
                case EnemyStrategy.Evasive:
                    return availableWeapons.OrderByDescending(w => w.Range * (w.Accuracy / 75.0)).First();
                default:
                    return availableWeapons.First();
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

        private static int CalculateDamage(Weapon weapon, int distance, bool isEnemyAttack = false)
        {
            // Base calculation
            int minDamage = (int)(weapon.BaseDamage * 0.9);
            int maxDamage = (int)(weapon.BaseDamage * 1.1);
            int baseDamage = random.Next(minDamage, maxDamage + 1);

            // Apply difficulty boost/reduction based on player selection
            if (isEnemyAttack)
            {
                baseDamage = (int)(baseDamage * 1.15); // Increased from 1.05 to 1.15 (15% boost)
            }
            else
            {
                baseDamage = (int)(baseDamage * 1.05); // Reduced from 1.1 to 1.05 (5% boost for player)
            }

            // Apply distance modifier with slight advantage to enemy if they're attacking
            double distanceModifier = 1.0;
            if (weapon.Range < distance)
            {
                // Weapon is out of optimal range
                distanceModifier = isEnemyAttack ? 0.4 : 0.3; // Less penalty for enemy
            }
            else if (weapon.Range / 2 > distance && !weapon.RequiresLock)
            {
                // Short-range weapons (like cannons) are more effective at close range
                distanceModifier = isEnemyAttack ? 1.35 : 1.25; // More bonus for enemy
            }
            else if (distance < 10 && weapon.RequiresLock)
            {
                // Missiles are less effective at very close range
                distanceModifier = isEnemyAttack ? 0.8 : 0.7; // Less penalty for enemy
            }

            // Add a small random factor with slight bias toward enemy when they're attacking
            double randomFactor = isEnemyAttack ?
                random.Next(95, 115) / 100.0 :  // 95-115% for enemy
                random.Next(95, 115) / 100.0;   // 90-110% for player

            // Apply final calculation with accuracy and distance modifier
            return (int)(baseDamage * weapon.Accuracy / 100.0 * distanceModifier * randomFactor);
        }

        private static EnemyStrategy DetermineEnemyStrategy(JetFighter enemy, JetFighter player)
        {
            // Make the AI adapt to player's health
            int playerHealthPercent = player.Health;

            // Factor in the distance between aircraft with smarter decisions
            if (enemy.Distance < 15)
            {
                // At close range, be more aggressive, especially if player is weakened
                return playerHealthPercent < 50 ? EnemyStrategy.Aggressive :
                    random.Next(0, 100) < 80 ? EnemyStrategy.Aggressive : EnemyStrategy.Defensive;
            }
            else if (enemy.Distance > 40)
            {
                // At long range, adjust tactics based on health advantage
                if (enemy.Health > playerHealthPercent + 20)
                {
                    // If enemy has health advantage, be aggressive
                    return EnemyStrategy.Aggressive;
                }
                else if (playerHealthPercent > enemy.Health + 20)
                {
                    // If player has significant health advantage, be defensive
                    return EnemyStrategy.Defensive;
                }
                else
                {
                    // Otherwise mix strategies with bias toward aggression
                    return random.Next(0, 100) < 60 ? EnemyStrategy.Aggressive : EnemyStrategy.Defensive;
                }
            }

            // More sophisticated strategy selection based on game state
            if (enemy.Health < 30)
            {
                // When critically low on health, favor defensive or evasive strategies
                int choice = random.Next(0, 100);
                if (choice < 15) return EnemyStrategy.Aggressive;  // Small chance to be aggressive
                else if (choice < 60) return EnemyStrategy.Defensive;
                else return EnemyStrategy.Evasive;
            }
            else if (enemy.Health < 60)
            {
                // When moderately damaged, mix of strategies but favor defensive
                int choice = random.Next(0, 100);
                if (choice < 30) return EnemyStrategy.Aggressive;
                else if (choice < 80) return EnemyStrategy.Defensive;
                else return EnemyStrategy.Evasive;
            }
            else if (playerHealthPercent < 40)
            {
                // When player health is low, be very aggressive to finish them off
                return random.Next(0, 100) < 85 ? EnemyStrategy.Aggressive : EnemyStrategy.Defensive;
            }
            else
            {
                // Mix of strategies in normal conditions - more aggressive than before
                int choice = random.Next(0, 100);
                if (choice < 55) return EnemyStrategy.Aggressive;  // Increased from 40% to 55%
                else if (choice < 85) return EnemyStrategy.Defensive;
                else return EnemyStrategy.Evasive;
            }
        }

        private static string GetDistanceDescription(int distance)
        {
            if (distance <= 5) return "extremely close - in visual range";
            if (distance <= 15) return "at close range";
            if (distance <= 30) return "at medium range";
            if (distance <= 60) return "at long range";
            return "very far away";
        }

        // Improved HandleManeuvers method with smarter enemy AI
        private static void HandleManeuvers(JetFighter player, JetFighter enemy)
        {
            Console.WriteLine("\nManeuver Options:");
            Console.WriteLine("1. Close in (decrease distance)");
            Console.WriteLine("2. Maintain distance");
            Console.WriteLine("3. Increase distance");

            int choice;
            while (true)
            {
                string? input = Console.ReadLine();
                if (int.TryParse(input, out choice) && choice >= 1 && choice <= 3)
                    break;
                Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
            }
            int playerDistanceChange = 0;
            switch (choice)
            {
                case 1: playerDistanceChange = -15 - random.Next(0, 10); break;   // Close distance
                case 2: playerDistanceChange = random.Next(-5, 6); break;         // Maintain
                case 3: playerDistanceChange = 15 + random.Next(0, 10); break;    // Increase distance
            }

            // Enemy AI distance response based on strategy - now more adaptive
            EnemyStrategy strategy = DetermineEnemyStrategy(enemy, player);

            // Enemy analyzes optimal range based on their weapons
            var enemyOptimalWeapon = enemy.SelectBestWeapon(player, strategy);
            int optimalDistance = enemyOptimalWeapon.Range / 2; // Aim for middle of weapon range

            int enemyDistanceChange = 0;

            // Enemy tries to maintain optimal distance for their chosen weapon
            if (enemy.Distance < optimalDistance - 10)
            {
                // Too close, try to increase distance
                enemyDistanceChange = 12 + random.Next(0, 10);
            }
            else if (enemy.Distance > optimalDistance + 10)
            {
                // Too far, try to decrease distance
                enemyDistanceChange = -12 - random.Next(0, 10);
            }
            else
            {
                // Within optimal range, make smaller adjustments based on strategy
                switch (strategy)
                {
                    case EnemyStrategy.Aggressive:
                        enemyDistanceChange = -5 - random.Next(0, 8);
                        break;
                    case EnemyStrategy.Defensive:
                        enemyDistanceChange = random.Next(-3, 8);
                        break;
                    case EnemyStrategy.Evasive:
                        enemyDistanceChange = 5 + random.Next(0, 8);
                        break;
                }
            }

            // Calculate net distance change with a bias toward enemy's maneuver
            int netDistanceChange = (int)((playerDistanceChange + enemyDistanceChange * 1.4) / 2.4);
            player.UpdateDistance(enemy, netDistanceChange);
            enemy.Distance = player.Distance;

            Console.WriteLine($"Distance is now: {player.Distance}km");
            Console.WriteLine($"The {enemy.Name} is {GetDistanceDescription(player.Distance)}");
        }
        public void ShowMenu(JetFighter jetFighter)
        {
            Console.WriteLine("\n========== Combat Information ==========");
            Console.WriteLine($"Your Aircraft: {jetFighter.Name} | Health: {jetFighter.Health}");
            Console.WriteLine($"Current Distance: {jetFighter.Distance}km");

            Console.WriteLine("\nAvailable Weapons:");
            Console.WriteLine("--------------------------");
            int index = 1;
            foreach (var weapon in jetFighter.Weapons)
            {
                string rangeStatus = weapon.Value.Range >= jetFighter.Distance ? "IN RANGE" : "OUT OF RANGE";
                string effectivenessNote = "";

                if (weapon.Value.Range < jetFighter.Distance)
                    effectivenessNote = "(Low effectiveness at this range)";
                else if (weapon.Value.Range / 2 > jetFighter.Distance && !weapon.Value.RequiresLock)
                    effectivenessNote = "(High effectiveness at close range)";
                else if (jetFighter.Distance < 10 && weapon.Value.RequiresLock)
                    effectivenessNote = "(Difficult to lock at very close range)";

                Console.WriteLine($"{index}. {weapon.Value.Name} " +
                                 $"(Damage: {weapon.Value.BaseDamage}, " +
                                 $"Accuracy: {weapon.Value.Accuracy}%, " +
                                 $"Range: {weapon.Value.Range}km, " +
                                 $"Remaining: {weapon.Value.Quantity}) - {rangeStatus} {effectivenessNote}");
                index++;
            }
            Console.WriteLine("=========================================");
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
        static void Main(string[] args)
        {
            Console.WriteLine("Choose your jet:");
            Console.WriteLine("1. F-22 Raptor (Health: 100, Attack Power: 20)");
            Console.WriteLine("2. Sukhoi Su-57 Felon (Health: 100, Attack Power: 20)");
            int choice;
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null)
                {
                    Console.WriteLine("Invalid input. Please enter a number.");
                    continue;
                }
                if (int.TryParse(input, out choice) && (choice == 1 || choice == 2))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter 1 or 2.");
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

            playerJet.IsPlayer = true;
            enemyJet.IsPlayer = false;

            // Create an instance of Program to call the non-static methods
            Program program = new Program();

            List<AttackDetail> attackDetails = new List<AttackDetail>();

            // Game loop
            while (playerJet.Health > 0 && enemyJet.Health > 0)
            {
                // Use the improved HandleManeuvers method
                HandleManeuvers(playerJet, enemyJet);

                // Player's turn
                program.ShowMenu(playerJet);
                Console.WriteLine("Enter the number of the weapon you want to use:");
                string? weaponChoice = Console.ReadLine();
                if (weaponChoice == null || !int.TryParse(weaponChoice, out int weaponIndex) ||
                    weaponIndex < 1 || weaponIndex > playerJet.Weapons.Count)
                {
                    Console.WriteLine("Invalid choice. Please try again.");
                    continue;
                }

                var selectedWeapon = playerJet.Weapons.ElementAt(weaponIndex - 1).Value;
                if (selectedWeapon.Fire())
                {
                    Console.WriteLine($"Fired {selectedWeapon.Name}!");
                    // Use regular damage calculation for player
                    int damage = CalculateDamage(selectedWeapon, playerJet.Distance);
                    enemyJet.Health -= damage;
                    playerJet.AddDamage(damage);
                    attackDetails.Add(new AttackDetail(playerJet.Name, enemyJet.Name, damage, enemyJet.Health));
                    Console.WriteLine($"{enemyJet.Name} took {damage} damage. Remaining health: {enemyJet.Health}");
                }
                else
                {
                    Console.WriteLine($"{selectedWeapon.Name} is out of ammo!");
                }

                if (enemyJet.Health <= 0) break;

                // Enemy's turn with enhanced AI
                EnemyStrategy strategy = DetermineEnemyStrategy(enemyJet, playerJet);
                Console.WriteLine($"{enemyJet.Name} uses {strategy} strategy.");

                var enemyWeapon = enemyJet.SelectBestWeapon(playerJet, strategy);
                if (enemyWeapon.Fire())
                {
                    Console.WriteLine($"{enemyJet.Name} fired {enemyWeapon.Name}!");
                    // Pass true for isEnemyAttack to get the enhanced damage
                    int damage = CalculateDamage(enemyWeapon, enemyJet.Distance, true);
                    playerJet.Health -= damage;
                    enemyJet.AddDamage(damage);
                    attackDetails.Add(new AttackDetail(enemyJet.Name, playerJet.Name, damage, playerJet.Health));
                    Console.WriteLine($"{playerJet.Name} took {damage} damage. Remaining health: {playerJet.Health}");
                }
                else
                {
                    Console.WriteLine($"{enemyJet.Name} is out of ammo or couldn't find a suitable weapon!");
                }

                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }

            // Display battle summary
            program.DisplayBattleSummary(attackDetails, playerJet, enemyJet);
        }
    }
}
