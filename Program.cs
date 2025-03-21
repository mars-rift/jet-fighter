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

    public enum CountermeasureType
    {
        Chaff,  // Defeats radar-guided missiles
        Flares, // Defeats heat-seeking missiles
        ECM     // Electronic countermeasures
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

        public bool AttemptFire(JetFighter attacker, JetFighter target, int distance)
        {
            if (Quantity <= 0) return false;
            
            // Check if missile requires lock and can achieve lock
            if (RequiresLock)
            {
                // Calculate detection probability based on range and stealth
                double detectionModifier = (double)attacker.RadarRange / distance;
                double stealthModifier = 100.0 / target.StealthRating;
                
                // Detection probability - harder to detect stealthy aircraft at range
                double detectionProbability = detectionModifier * stealthModifier * 0.8;
                
                // Apply random factor
                if (new Random().NextDouble() * 100 > detectionProbability)
                {
                    return false; // Failed to achieve lock
                }
            }
            
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

        public int StealthRating { get; private set; } // Lower is stealthier
        public int Maneuverability { get; private set; } // Higher is better
        public int RadarRange { get; private set; } // Detection capability
        public Dictionary<CountermeasureType, int> Countermeasures { get; private set; } = new Dictionary<CountermeasureType, int>();
        public Dictionary<CountermeasureType, int> InitialCountermeasures { get; private set; } = new Dictionary<CountermeasureType, int>();

        public int MissilesFired { get; private set; } = 0;
        public int MissilesHit { get; private set; } = 0;
        public int CannonRoundsFired { get; private set; } = 0;
        public int WeaponHits { get; private set; } = 0;

        public JetFighter(string name, int health, bool isWestern)
        {
            Name = name;
            Health = health;
            TotalDamageDealt = 0;
            InitializeCharacteristics(isWestern);
            InitializeWeapons(isWestern);
            // Store initial countermeasures
            foreach (var cm in Countermeasures)
            {
                InitialCountermeasures[cm.Key] = cm.Value;
            }
        }

        public void AddDamage(int damage)
        {
            TotalDamageDealt += damage;
        }

        private void InitializeWeapons(bool isWestern)
        {
            if (isWestern)
            {
                // F-22 weapons: Better medium range, high accuracy, less raw damage
                Weapons.Add(WeaponType.AIM120_AMRAAM, new Weapon(WeaponType.AIM120_AMRAAM, "AIM-120D AMRAAM", 45, 85, 6, 55, true));
                Weapons.Add(WeaponType.AIM9X_Sidewinder, new Weapon(WeaponType.AIM9X_Sidewinder, "AIM-9X Sidewinder", 40, 90, 4, 25, true));
                Weapons.Add(WeaponType.M61A2_Vulcan, new Weapon(WeaponType.M61A2_Vulcan, "M61A2 Vulcan", 28, 60, 480, 3, false));
            }
            else
            {
                // Su-57 weapons: Higher damage, slightly lower accuracy, better close-range
                Weapons.Add(WeaponType.R77, new Weapon(WeaponType.R77, "R-77M", 50, 80, 6, 50, true));
                Weapons.Add(WeaponType.R73, new Weapon(WeaponType.R73, "R-73M", 45, 85, 4, 30, true));
                Weapons.Add(WeaponType.GSh301, new Weapon(WeaponType.GSh301, "GSh-30-1", 35, 55, 450, 2, false));
            }
        }

        private void InitializeCharacteristics(bool isWestern)
        {
            if (isWestern) // F-22
            {
                StealthRating = 20; // Very stealthy
                Maneuverability = 85;
                RadarRange = 160;
                Countermeasures.Add(CountermeasureType.Chaff, 6);
                Countermeasures.Add(CountermeasureType.Flares, 12);
                Countermeasures.Add(CountermeasureType.ECM, 8);
            }
            else // Su-57
            {
                StealthRating = 35; // Decent but not as stealthy as F-22
                Maneuverability = 95; // Better thrust vectoring
                RadarRange = 140;
                if (!Countermeasures.ContainsKey(CountermeasureType.Chaff))
                {
                    Countermeasures.Add(CountermeasureType.Chaff, 8);
                }
                Countermeasures.Add(CountermeasureType.Flares, 14);
                Countermeasures.Add(CountermeasureType.ECM, 6);
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

        public bool DeployCountermeasure(CountermeasureType type)
        {
            if (!Countermeasures.ContainsKey(type) || Countermeasures[type] <= 0)
                return false;
                
            Countermeasures[type]--;
            return true;
        }

        public void IncrementMissilesFired()
        {
            MissilesFired++;
        }

        public void IncrementMissilesHit()
        {
            MissilesHit++;
        }

        public void IncrementCannonRoundsFired()
        {
            CannonRoundsFired += 10; // Assuming each cannon "shot" represents 10 rounds
        }

        public void IncrementWeaponHits()
        {
            WeaponHits++;
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

        private static int CalculateDamage(Weapon weapon, JetFighter attacker, JetFighter target, int distance)
        {
            // Track firing statistics
            if (weapon.RequiresLock) {
                attacker.IncrementMissilesFired();
            } else {
                attacker.IncrementCannonRoundsFired();
            }
            
            // Base calculation
            int minDamage = (int)(weapon.BaseDamage * 0.8);
            int maxDamage = (int)(weapon.BaseDamage * 1.2);
            int baseDamage = random.Next(minDamage, maxDamage + 1);
            
            // Calculate hit probability based on accuracy, distance, and target maneuverability
            double baseHitChance = weapon.Accuracy / 100.0;
            double distanceFactor = Math.Min(1.0, weapon.Range / (double)distance);
            double maneuverabilityPenalty = target.Maneuverability / 200.0; // Higher maneuverability = harder to hit
            
            double hitChance = baseHitChance * distanceFactor * (1.0 - maneuverabilityPenalty);
            
            // Random roll to see if weapon hits
            if (random.NextDouble() > hitChance)
            {
                // Missed the target
                return 0;
            }
            
            // Record weapon hit
            if (weapon.RequiresLock) {
                attacker.IncrementMissilesHit();
            }
            attacker.IncrementWeaponHits();
            
            // Apply distance modifier for damage
            double distanceModifier = 1.0;
            if (weapon.Range < distance)
            {
                distanceModifier = 0.3; // Significant penalty for out-of-range shots
            }
            else if (weapon.Range / 2 > distance && !weapon.RequiresLock)
            {
                // Short-range weapons are more effective at close range
                distanceModifier = 1.4;
            }
            else if (distance < 10 && weapon.RequiresLock)
            {
                // Missiles are less effective at very close range
                distanceModifier = 0.7;
            }
            
            // Apply final calculation
            double finalDamage = baseDamage * distanceModifier;
            return (int)Math.Max(1, finalDamage);
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

        private static void HandleManeuvers(JetFighter player, JetFighter enemy)
        {
            Console.WriteLine("\nManeuver Options:");
            Console.WriteLine("1. Close in aggressively (decrease distance significantly)");
            Console.WriteLine("2. Close in cautiously (decrease distance slightly)");
            Console.WriteLine("3. Maintain distance");
            Console.WriteLine("4. Increase distance slightly");
            Console.WriteLine("5. Disengage (increase distance significantly)");
            Console.WriteLine($"Current distance: {player.Distance}km | {GetDistanceDescription(player.Distance)}");

            int choice;
            while (true)
            {
                string? input = Console.ReadLine();
                if (int.TryParse(input, out choice) && choice >= 1 && choice <= 5)
                    break;
                Console.WriteLine("Invalid choice. Please enter a number between 1 and 5.");
            }
            
            // Calculate player distance change based on maneuverability
            int baseDistanceChange;
            switch (choice)
            {
                case 1: baseDistanceChange = -20; break;   // Close aggressively
                case 2: baseDistanceChange = -10; break;   // Close cautiously
                case 3: baseDistanceChange = 0; break;     // Maintain
                case 4: baseDistanceChange = 10; break;    // Increase slightly
                case 5: baseDistanceChange = 20; break;    // Disengage
                default: baseDistanceChange = 0; break;
            }
            
            // Apply maneuverability factor - higher maneuverability makes moves more effective
            int playerDistanceChange = (int)(baseDistanceChange * (player.Maneuverability / 80.0));
            playerDistanceChange += random.Next(-3, 4); // Small random component
            
            // Enemy AI distance response with consideration for aircraft capabilities
            EnemyStrategy strategy = DetermineEnemyStrategy(enemy, player);
            var enemyOptimalWeapon = enemy.SelectBestWeapon(player, strategy);
            int optimalDistance = enemyOptimalWeapon.Range / 2;
            
            int enemyBaseDistanceChange = 0;
            
            // Advanced distance management for enemy
            if (enemy.Distance < optimalDistance - 15)
            {
                // Way too close, try to increase distance significantly
                enemyBaseDistanceChange = 15;
            }
            else if (enemy.Distance < optimalDistance - 5)
            {
                // Too close, increase slightly
                enemyBaseDistanceChange = 8;
            }
            else if (enemy.Distance > optimalDistance + 15)
            {
                // Way too far, decrease significantly
                enemyBaseDistanceChange = -15;
            }
            else if (enemy.Distance > optimalDistance + 5)
            {
                // Too far, decrease slightly
                enemyBaseDistanceChange = -8;
            }
            else
            {
                // Within optimal range, make smaller adjustments based on strategy
                switch (strategy)
                {
                    case EnemyStrategy.Aggressive: enemyBaseDistanceChange = -5; break;
                    case EnemyStrategy.Defensive: enemyBaseDistanceChange = 3; break;
                    case EnemyStrategy.Evasive: enemyBaseDistanceChange = 7; break;
                }
            }
            
            // Apply enemy maneuverability factor
            int enemyDistanceChange = (int)(enemyBaseDistanceChange * (enemy.Maneuverability / 80.0));
            enemyDistanceChange += random.Next(-3, 4); // Small random component
            
            // Calculate net distance change considering both aircraft's actions
            // Aircraft with better maneuverability has more influence on the outcome
            double playerInfluence = player.Maneuverability / (double)(player.Maneuverability + enemy.Maneuverability);
            double enemyInfluence = 1.0 - playerInfluence;
            
            int netDistanceChange = (int)((playerDistanceChange * playerInfluence) + (enemyDistanceChange * enemyInfluence));
            
            player.UpdateDistance(enemy, netDistanceChange);
            enemy.Distance = player.Distance;
            
            Console.WriteLine($"Distance is now: {player.Distance}km");
            Console.WriteLine($"The {enemy.Name} is {GetDistanceDescription(player.Distance)}");
            
            // Report on enemy activity
            if (enemyDistanceChange < -5)
                Console.WriteLine($"The {enemy.Name} is aggressively closing in!");
            else if (enemyDistanceChange < 0)
                Console.WriteLine($"The {enemy.Name} is moving closer.");
            else if (enemyDistanceChange > 5)
                Console.WriteLine($"The {enemy.Name} is attempting to disengage!");
            else if (enemyDistanceChange > 0)
                Console.WriteLine($"The {enemy.Name} is increasing distance.");
            else
                Console.WriteLine($"The {enemy.Name} is maintaining position.");
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

            Console.WriteLine("\nCombat Statistics:");
            Console.WriteLine($"{playerJet.Name} Accuracy: {CalculateAccuracy(playerJet)}%");
            Console.WriteLine($"{enemyJet.Name} Accuracy: {CalculateAccuracy(enemyJet)}%");
            Console.WriteLine($"Total Attacks: {attackDetails.Count}");
            Console.WriteLine($"Average Damage per Attack: {attackDetails.Average(d => d.Damage):F1}");
            
            Console.WriteLine("\nCountermeasures Used:");
            foreach (CountermeasureType cm in Enum.GetValues(typeof(CountermeasureType)))
            {
                int playerInitial = playerJet.Countermeasures.ContainsKey(cm) ? 
                    playerJet.InitialCountermeasures[cm] : 0;
                int playerRemaining = playerJet.Countermeasures.ContainsKey(cm) ?
                    playerJet.Countermeasures[cm] : 0;
                    
                int enemyInitial = enemyJet.Countermeasures.ContainsKey(cm) ?
                    enemyJet.InitialCountermeasures[cm] : 0;
                int enemyRemaining = enemyJet.Countermeasures.ContainsKey(cm) ?
                    enemyJet.Countermeasures[cm] : 0;
                    
                Console.WriteLine($"{cm}: Player used {playerInitial - playerRemaining}, " +
                                 $"Enemy used {enemyInitial - enemyRemaining}");
            }
            
            // Game outcome
            Console.WriteLine("\nGame Over!");
            if (playerJet.Health > 0)
            {
                Console.WriteLine($"{playerJet.Name} wins with {playerJet.Health} health remaining!");
            }
            else
            {
                Console.WriteLine($"{enemyJet.Name} wins with {enemyJet.Health} health remaining!");
            }
        }

        private double CalculateAccuracy(JetFighter jet)
        {
            int totalShots = jet.MissilesFired + (jet.CannonRoundsFired / 10); // Count every 10 cannon rounds as 1 "shot"
            int totalHits = jet.MissilesHit + (jet.WeaponHits - jet.MissilesHit);
            
            if (totalShots == 0) return 0;
            return (totalHits / (double)totalShots) * 100;
        }

        public static bool AttemptCountermeasure(JetFighter defender, Weapon incomingWeapon)
        {
            // AI logic to decide when to use countermeasures
            if (!defender.IsPlayer && new Random().Next(100) < 75) // 75% chance AI will try countermeasures
            {
                CountermeasureType bestType;
                
                // Choose appropriate countermeasure based on weapon type
                if (incomingWeapon.Type == WeaponType.AIM120_AMRAAM || incomingWeapon.Type == WeaponType.R77)
                    bestType = CountermeasureType.Chaff; // Radar-guided missiles
                else if (incomingWeapon.Type == WeaponType.AIM9X_Sidewinder || incomingWeapon.Type == WeaponType.R73)
                    bestType = CountermeasureType.Flares; // IR-guided missiles
                else
                    bestType = CountermeasureType.ECM; // Default

                if (defender.DeployCountermeasure(bestType))
                {
                    // Calculate evasion chance based on defender's maneuverability
                    double evasionChance = defender.Maneuverability * 0.7 / 100.0;
                    return new Random().NextDouble() < evasionChance;
                }
            }
            
            // For player, countermeasure choice is manual (handled elsewhere)
            return false;
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
                    
                    // Allow enemy to use countermeasures for missiles that require lock
                    bool evaded = false;
                    if (selectedWeapon.RequiresLock)
                    {
                        evaded = AttemptCountermeasure(enemyJet, selectedWeapon);
                        if (evaded)
                        {
                            Console.WriteLine($"{enemyJet.Name} deployed countermeasures and evaded your attack!");
                            playerJet.IncrementMissilesFired(); // Still count as fired even if evaded
                        }
                    }
                    
                    // Only calculate damage if not evaded
                    if (!evaded)
                    {
                        int damage = CalculateDamage(selectedWeapon, playerJet, enemyJet, playerJet.Distance);
                        enemyJet.Health -= damage;
                        playerJet.AddDamage(damage);
                        attackDetails.Add(new AttackDetail(playerJet.Name, enemyJet.Name, damage, enemyJet.Health));
                        Console.WriteLine($"{enemyJet.Name} took {damage} damage. Remaining health: {enemyJet.Health}");
                    }
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
                    
                    // Allow player to use countermeasures if the weapon requires lock
                    bool evaded = false;
                    if (enemyWeapon.RequiresLock)
                    {
                        Console.WriteLine("\nIncoming missile! Deploy countermeasures?");
                        Console.WriteLine("1. Deploy Chaff (effective against radar-guided missiles)");
                        Console.WriteLine("2. Deploy Flares (effective against heat-seeking missiles)");
                        Console.WriteLine("3. Use ECM (electronic countermeasures)");
                        Console.WriteLine("4. Attempt evasive maneuvers (no countermeasures)");
                        Console.WriteLine($"Chaff: {playerJet.Countermeasures[CountermeasureType.Chaff]} | Flares: {playerJet.Countermeasures[CountermeasureType.Flares]} | ECM: {playerJet.Countermeasures[CountermeasureType.ECM]}");
                        
                        int cmChoice;
                        while (true)
                        {
                            string? input = Console.ReadLine();
                            if (int.TryParse(input, out cmChoice) && cmChoice >= 1 && cmChoice <= 4)
                                break;
                            Console.WriteLine("Invalid choice. Please enter a number between 1 and 4.");
                        }
                        
                        CountermeasureType type = CountermeasureType.Chaff;
                        bool deployed = false;
                        
                        switch(cmChoice)
                        {
                            case 1: 
                                deployed = playerJet.DeployCountermeasure(CountermeasureType.Chaff);
                                type = CountermeasureType.Chaff;
                                break;
                            case 2: 
                                deployed = playerJet.DeployCountermeasure(CountermeasureType.Flares);
                                type = CountermeasureType.Flares;
                                break;
                            case 3: 
                                deployed = playerJet.DeployCountermeasure(CountermeasureType.ECM);
                                type = CountermeasureType.ECM;
                                break;
                            case 4:
                                // No countermeasures, just evasion chance
                                break;
                        }
                        
                        if (deployed)
                        {
                            Console.WriteLine($"Deployed {type}!");
                            double effectivenessModifier = 1.0;
                            
                            // Different countermeasures are effective against different weapons
                            if ((type == CountermeasureType.Chaff && 
                                 (enemyWeapon.Type == WeaponType.AIM120_AMRAAM || enemyWeapon.Type == WeaponType.R77)) ||
                                (type == CountermeasureType.Flares && 
                                 (enemyWeapon.Type == WeaponType.AIM9X_Sidewinder || enemyWeapon.Type == WeaponType.R73)))
                            {
                                effectivenessModifier = 1.5; // More effective against appropriate missile type
                            }
                            
                            // Calculate evasion chance based on countermeasure effectiveness and maneuverability
                            double evasionChance = (playerJet.Maneuverability * 0.5 / 100.0) * effectivenessModifier;
                            if (random.NextDouble() < evasionChance)
                            {
                                evaded = true;
                                Console.WriteLine("Countermeasures successful! Missile evaded!");
                            }
                            else
                            {
                                Console.WriteLine("Countermeasures failed to divert the missile!");
                            }
                        }
                        else if (cmChoice != 4)
                        {
                            Console.WriteLine($"No {type} countermeasures available!");
                        }
                        else
                        {
                            // Just evasive maneuvers, lower chance of success
                            double evasionChance = playerJet.Maneuverability * 0.2 / 100.0;
                            if (random.NextDouble() < evasionChance)
                            {
                                evaded = true;
                                Console.WriteLine("You performed an incredible evasive maneuver and dodged the missile!");
                            }
                        }
                        
                        enemyJet.IncrementMissilesFired(); // Count missile as fired even if evaded
                    }
                    
                    // Only calculate damage if not evaded
                    if (!evaded)
                    {
                        int damage = CalculateDamage(enemyWeapon, enemyJet, playerJet, enemyJet.Distance);
                        playerJet.Health -= damage;
                        enemyJet.AddDamage(damage);
                        attackDetails.Add(new AttackDetail(enemyJet.Name, playerJet.Name, damage, playerJet.Health));
                        Console.WriteLine($"{playerJet.Name} took {damage} damage. Remaining health: {playerJet.Health}");
                    }
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
