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
        Chaff,  // Counters radar-guided missiles
        Flares, // Counters infrared-guided missiles
        ECM     // Electronic Counter Measures - general jamming
    }

    public enum AircraftSystem
    {
        Engine,         // Affects speed and maneuverability
        Radar,          // Affects detection range and lock capability
        FlightControls, // Affects maneuverability
        WeaponSystems   // Affects weapon accuracy and reliability
    }

    public enum WeatherCondition
    {
        Clear,
        Cloudy,
        Storm,
        Fog
    }

    public class WeatherSystem
    {
        public WeatherCondition CurrentWeather { get; private set; }
        public int Visibility { get; private set; }
        
        public void SetWeather(WeatherCondition condition)
        {
            CurrentWeather = condition;
            
            switch (condition)
            {
                case WeatherCondition.Clear:
                    Visibility = 100;
                    break;
                case WeatherCondition.Cloudy:
                    Visibility = 70;
                    break;
                case WeatherCondition.Storm:
                    Visibility = 40;
                    break;
                case WeatherCondition.Fog:
                    Visibility = 20;
                    break;
            }
        }
        
        // Modify weapon accuracy based on weather
        public double GetAccuracyModifier(Weapon weapon)
        {
            // More nuanced weather effects
            return CurrentWeather switch
            {
                WeatherCondition.Clear => 1.0,
                WeatherCondition.Cloudy => weapon.RequiresLock ? 0.85 : 0.95,// 15% penalty for missiles, 5% for guns
                WeatherCondition.Storm => weapon.RequiresLock ? 0.70 : 0.80,// 30% penalty for missiles, 20% for guns
                WeatherCondition.Fog => weapon.RequiresLock ? 0.75 : 0.65,// 25% penalty for missiles, 35% for guns (fog affects visual more)
                _ => 0.9,// Default 10% penalty
            };
        }
        

        internal static void AddCombatNarrative(JetFighter target, Weapon weapon, int damage)
        {
            string[] missileHitDescriptions = [
                "slams into", "explodes near", "scores a direct hit on",
                "finds its mark on", "tracks perfectly to", "detonates against"
            ];

            string[] cannonHitDescriptions = [
                "tears through", "rips into", "perforates",
                "shreds", "punches holes in", "stitches across"
            ];

            string[] missDescriptions = [
                "flies wide of", "narrowly misses", "fails to track",
                "overshoots", "loses lock on", "detonates harmlessly near"
            ];

            string[] impactLocations = [
                "the fuselage", "the wing", "the engine housing",
                "the tail section", "the cockpit area", "the weapons bay"
            ];

            if (damage > 0)
            {
                // Vary description based on relative damage magnitude
                string hitVerb;
                if (damage > weapon.BaseDamage * 1.1)
                {
                    // For heavy hits
                    hitVerb = weapon.RequiresLock ?
                        missileHitDescriptions[JetFighter.random.Next(0, 3)] : // Use more dramatic descriptions
                        cannonHitDescriptions[JetFighter.random.Next(0, 3)];
                }
                else
                {
                    // For normal hits
                    hitVerb = weapon.RequiresLock ?
                        missileHitDescriptions[JetFighter.random.Next(3, missileHitDescriptions.Length)] :
                        cannonHitDescriptions[JetFighter.random.Next(3, cannonHitDescriptions.Length)];
                }

                string location = impactLocations[JetFighter.random.Next(impactLocations.Length)];

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{weapon.Name} {hitVerb} {target.Name}'s {location}!");
                Console.ResetColor();

                if (damage > weapon.BaseDamage * 0.8)
                    Console.WriteLine("That was a solid hit!");
            }
            else
            {
                string missVerb = missDescriptions[JetFighter.random.Next(missDescriptions.Length)];

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{weapon.Name} {missVerb} {target.Name}.");
                Console.ResetColor();
            }
        }
    }

    public class Weapon(WeaponType type, string name, int damage, int accuracy, int quantity, int range, bool requiresLock)
    {
        public WeaponType Type { get; } = type;
        public string Name { get; } = name;
        public int BaseDamage { get; } = damage;
        public int Accuracy { get; } = accuracy;
        public int Quantity { get; private set; } = quantity;
        public int Range { get; } = range;
        public bool RequiresLock { get; } = requiresLock;

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
                // Give the AI a much higher chance of achieving lock
                if (!attacker.IsPlayer)
                {
                    // Significant bonus for AI to achieve lock - 90% base chance
                    double lockProbability = 90.0;

                    // Only reduce for extreme conditions
                    if (distance > Range * 1.1)
                        lockProbability *= 0.7;

                    // Still apply system damage effects
                    if (attacker.SystemHealth[AircraftSystem.Radar] < 75)
                        lockProbability *= attacker.SystemHealth[AircraftSystem.Radar] / 100.0;

                    // More likely to succeed now
                    if (JetFighter.random.NextDouble() * 100.0 > lockProbability)
                        return false;
                }
                else
                {
                    // Original calculation for player locks
                    // Normalized distance factor (closer = easier lock)
                    double distanceFactor = 1.0 - (Math.Min(distance, attacker.RadarRange) / (double)attacker.RadarRange);

                    // Stealth factor (lower stealth rating = harder to lock)
                    double stealthFactor = target.StealthRating / 100.0;

                    // Radar power factor
                    double radarFactor = attacker.RadarRange / 160.0; // Normalized to F-22's radar

                    // IMPROVED: Base lock probability (70-95% depending on factors)
                    double lockProbability = 70.0 + (distanceFactor * 15.0) + (stealthFactor * 15.0) + (radarFactor * 10.0);

                    // Missiles have better lock at optimal ranges
                    if (distance < Range * 0.25)
                        lockProbability *= 0.8; // Too close is challenging (improved from 0.7)
                    else if (distance < Range * 0.8)
                        lockProbability *= 1.3; // Sweet spot for missile locks (improved from 1.2)

                    // Apply system damage effects
                    if (attacker.SystemHealth[AircraftSystem.Radar] < 75)
                        lockProbability *= attacker.SystemHealth[AircraftSystem.Radar] / 100.0;

                    // Minimum 50% chance to achieve lock if in range
                    if (distance <= Range)
                        lockProbability = Math.Max(50, lockProbability);

                    // Use shared random instance with better probability scale
                    if (JetFighter.random.NextDouble() * 100.0 > lockProbability)
                        return false;
                }
            }

            Quantity--;
            return true;
        }

        public void ReplenishAmmo(int amount)
        {
            Quantity += amount;
        }
    }

    public class JetFighter
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public Dictionary<WeaponType, Weapon> Weapons { get; private set; } = [];
        public int TotalDamageDealt { get; private set; }
        public int Distance { get; set; } = 100; // Starting distance between aircraft
        public bool IsPlayer { get; set; }
        public int ManeuverCapability { get; set; } = 70; // Base value that affects distance changes
        private bool HasRadarLock { get; set; }
        public static readonly Random random = new();

        public int StealthRating { get; private set; } // Lower is stealthier
        public int Maneuverability { get; private set; } // Higher is better
        public int RadarRange { get; private set; } // Detection capability

        public int MissilesFired { get; private set; } = 0;
        public int MissilesHit { get; private set; } = 0;
        public int CannonRoundsFired { get; private set; } = 0;
        public int WeaponHits { get; private set; } = 0;

        public Dictionary<AircraftSystem, int> SystemHealth { get; private set; } = [];

        public int Altitude { get; set; } = 30000; // Initial altitude in feet
        public int Fuel { get; set; } = 100; // New: Fuel system

        public Pilot? Pilot { get; private set; }

        public JetFighter(string name, int health, bool isWestern)
        {
            Name = name;
            Health = health;
            TotalDamageDealt = 0;
            InitializeCharacteristics(isWestern);
            InitializeWeapons(isWestern);
            InitializeSystems();
            // Remove countermeasure initialization
        }

        public void AssignPilot(Pilot pilot)
        {
            this.Pilot = pilot;
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
                Weapons.Add(WeaponType.AIM120_AMRAAM, new Weapon(WeaponType.AIM120_AMRAAM, "AIM-120D AMRAAM", 75, 85, 6, 55, true)); // From 45 to 65
                Weapons.Add(WeaponType.AIM9X_Sidewinder, new Weapon(WeaponType.AIM9X_Sidewinder, "AIM-9X Sidewinder", 65, 90, 4, 25, true)); // From 40 to 55
                Weapons.Add(WeaponType.M61A2_Vulcan, new Weapon(WeaponType.M61A2_Vulcan, "M61A2 Vulcan", 90, 75, 400, 3, false)); // From 28 to 35
            }
            else
            {
                // Su-57 weapons: Higher damage, slightly lower accuracy, better close-range
                Weapons.Add(WeaponType.R77, new Weapon(WeaponType.R77, "R-77M", 70, 80, 6, 50, true)); // From 50 to 70
                Weapons.Add(WeaponType.R73, new Weapon(WeaponType.R73, "R-73M", 60, 85, 4, 30, true)); // From 45 to 60
                Weapons.Add(WeaponType.GSh301, new Weapon(WeaponType.GSh301, "GSh-30-1", 85, 75, 300, 2, false)); // From 35 to 40
            }
        }

        private void InitializeCharacteristics(bool isWestern)
        {
            if (isWestern) // F-22
            {
                StealthRating = 20; // Very stealthy (unchanged)
                Maneuverability = 80; // Slightly reduced from 85
                RadarRange = 170; // Increased from 160
                // Remove these lines:
                // Countermeasures.Add(CountermeasureType.Chaff, 6);
                // Countermeasures.Add(CountermeasureType.Flares, 10); // Reduced from 12
                // Countermeasures.Add(CountermeasureType.ECM, 8);
            }
            else // Su-57
            {
                StealthRating = 40; // Less stealthy (increased from 35)
                Maneuverability = 95; // Better thrust vectoring (unchanged)
                RadarRange = 140; // Unchanged
                // Remove these lines:
                // Countermeasures.Add(CountermeasureType.Chaff, 8);
                // Countermeasures.Add(CountermeasureType.Flares, 12); // Reduced from 14
                // Countermeasures.Add(CountermeasureType.ECM, 6);
            }
        }

        private void InitializeSystems()
        {
            SystemHealth[AircraftSystem.Engine] = 100;
            SystemHealth[AircraftSystem.Radar] = 100;
            SystemHealth[AircraftSystem.FlightControls] = 100;
            SystemHealth[AircraftSystem.WeaponSystems] = 100;
        }

        public void UpdateDistance(int distanceChange)
        {
            Distance = Math.Max(1, Math.Min(150, Distance + distanceChange));
            // Realism: Fuel cost for maneuvering
            Fuel = Math.Max(0, Fuel - Math.Abs(distanceChange) / 2);
        }

        public void UpdateAltitude(int altitudeChange)
        {
            Altitude = Math.Max(1000, Math.Min(60000, Altitude + altitudeChange));
            // Realism: Fuel cost for climbing/diving
            Fuel = Math.Max(0, Fuel - Math.Abs(altitudeChange) / 1000);
        }

        public void UpdateRadarRange(int newRange)
        {
            RadarRange = Math.Max(0, newRange);
        }

        public Weapon SelectBestWeapon(EnemyStrategy strategy)
        {
            // First select weapons that are in range
            var inRangeWeapons = Weapons.Values.Where(w => w.Quantity > 0 && w.Range >= Distance).ToList();
            
            // If no weapons in range, get any weapon with ammo
            if (!inRangeWeapons.Any())
                inRangeWeapons = Weapons.Values.Where(w => w.Quantity > 0).ToList();
            
            // If still no weapons, throw exception
            if (!inRangeWeapons.Any())
                throw new InvalidOperationException("No weapons available to select.");
            
            // Based on strategy, pick the best weapon
            if (strategy == EnemyStrategy.Aggressive)
            {
                return inRangeWeapons.FirstOrDefault(w => w.RequiresLock) ?? inRangeWeapons.First();
            }
            else if (strategy == EnemyStrategy.Defensive)
            {
                return inRangeWeapons.FirstOrDefault(w => !w.RequiresLock) ?? inRangeWeapons.First();
            }
            else // Evasive
            {
                return inRangeWeapons.OrderByDescending(w => w.Range).FirstOrDefault() ?? inRangeWeapons.First();
            }
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

        // Add this method to JetFighter class
        public bool DeployCountermeasure(CountermeasureType countermeasureType)
        {
            // Always return false since countermeasures are removed
            return false;
        }

        // Add an empty stub for UpdateCooldowns
        public void UpdateCooldowns()
        {
           // 
        }
    }

    public class AttackDetail(string attacker, string target, int damage, int targetHealthAfterAttack)
    {
        public string Attacker { get; set; } = attacker;
        public string Target { get; set; } = target;
        public int Damage { get; set; } = damage;
        public int TargetHealthAfterAttack { get; set; } = targetHealthAfterAttack;
    }

    public abstract class Mission
    {
        public string Name { get; protected set; } = string.Empty;
        public string Description { get; protected set; } = string.Empty;
        public MissionObjective Objective { get; protected set; }
        public bool IsCompleted { get; protected set; }
        
        public abstract bool CheckCompletion(JetFighter player, JetFighter enemy);
        public abstract void DisplayMissionInfo();
    }

    public class AirSuperiority : Mission
    {
        public AirSuperiority()
        {
            Name = "Air Superiority";
            Description = "Eliminate enemy aircraft";
            Objective = MissionObjective.EliminateEnemy;
        }

        public override bool CheckCompletion(JetFighter player, JetFighter enemy)
        {
            return enemy.Health <= 0;
        }

        public override void DisplayMissionInfo()
        {
            Console.WriteLine($"Mission: {Name}");
            Console.WriteLine($"Objective: {Description}");
        }
    }

    public class PatrolAirspace : Mission
    {
        public PatrolAirspace()
        {
            Name = "Patrol Airspace";
            Description = "Monitor designated area for  turns";
            Objective = MissionObjective.PatrolAirspace;
            RemainingTurns = 10;
        }
        
        public int RemainingTurns { get; private set; }
        
        public override bool CheckCompletion(JetFighter player, JetFighter enemy)
        {
            RemainingTurns--;
            return RemainingTurns <= 0 || enemy.Health <= 0;
        }
        
        public override void DisplayMissionInfo()
        {
            Console.WriteLine($"Mission: {Name}");
            Console.WriteLine($"Objective: {Description}");
            Console.WriteLine($"Remaining patrol time: {RemainingTurns} turns");
        }
    }

    public class Reconnaissance : Mission
    {
        public Reconnaissance()
        {
            Name = "Reconnaissance";
            Description = "Gather intelligence without engaging enemy if possible";
            Objective = MissionObjective.Reconnaissance;
            IntelGathered = 0;
        }
        
        public int IntelGathered { get; private set; }
        
        public override bool CheckCompletion(JetFighter player, JetFighter enemy)
        {
            // Only gather intel when not in the same turn as a failed finishing move
            // Add a bool parameter to track if we're checking after a failed finishing move
            if (player.Distance < 60 && player.Distance > 30)
            {
                IntelGathered++;
                Console.WriteLine($"Intelligence gathered: {IntelGathered}/5");
            }
            
            return IntelGathered >= 5 || enemy.Health <= 0;
        }
        
        public override void DisplayMissionInfo()
        {
            Console.WriteLine($"Mission: {Name}");
            Console.WriteLine($"Objective: {Description}");
            Console.WriteLine($"Intelligence gathered: {IntelGathered}/5");
        }
    }

    public enum MissionObjective
    {
        EliminateEnemy,
        Reconnaissance,
        Escort,
        PatrolAirspace,
        Intercept
    }

    public class Pilot
    {
        public string Name { get; set; }
        public int Experience { get; private set; }
        public Dictionary<PilotSkill, int> Skills { get; private set; } = [];
        
        public Pilot(string name)
        {
            Name = name;
            Experience = 0;
            
            // Initialize skills at level 1
            foreach (PilotSkill skill in Enum.GetValues(typeof(PilotSkill)))
            {
                Skills[skill] = 1;
            }
        }
        
        public void AddExperience(int amount)
        {
            Experience += amount;
            CheckLevelUp();
        }
        
        private static void CheckLevelUp()
        {
            // Logic to improve skills based on experience
        }

        public double GetSkillModifier(PilotSkill skill)
        {
            return 1.0 + (Skills[skill] * 0.05); // 5% boost per skill level
        }
    }

    public enum PilotSkill
    {
        Gunnery,        // Improves cannon accuracy
        MissileGuidance,// Improves missile accuracy
        Evasion,        // Improves dodging
        Awareness,      // Improves detection
        Maneuverability // Improves aircraft handling
    }

    
    public class GameController
    {
        private readonly JetFighter _player;
        private readonly JetFighter _enemy;
        private readonly List<AttackDetail> _attackDetails = [];
        private readonly WeatherSystem _weatherSystem = new();
        private readonly Mission _currentMission;
        private readonly Program _programUI; // Reference to UI methods
        private bool _missedFinishingMoveThisTurn; // Add this flag
        private bool _executingFinishingMove = false; // Add this field
        
        public GameController(JetFighter player, JetFighter enemy, Mission mission)
        {
            _player = player;
            _enemy = enemy;
            _currentMission = mission;
            _programUI = new Program();
            
            // Initialize game state
            _player.IsPlayer = true;
            _enemy.IsPlayer = false;
        }
        
        public void SetWeather(WeatherCondition condition)
        {
            _weatherSystem.SetWeather(condition);
            Console.WriteLine($"Weather conditions: {condition}");
        }

        public void RunGameLoop()
        {
            _currentMission.DisplayMissionInfo();

            while (_player.Health > 0 && _enemy.Health > 0 && !_currentMission.CheckCompletion(_player, _enemy))
            {
                Console.WriteLine("\n===================== NEW TURN =====================");
                CheckForRandomEvents();

                // Player's turn
                Console.WriteLine("\n--- Player's Turn ---");
                ProcessPlayerTurn();

                // Check if enemy is defeated after player's turn
                if (_enemy.Health <= 0) break;

                // Enemy's turn
                Console.WriteLine("\n--- Enemy's Turn ---");
                ProcessEnemyTurn();

                // Display current status
                Console.WriteLine($"\n{_player.Name} Health: {_player.Health}, Distance: {_player.Distance}km");
                Console.WriteLine($"{_enemy.Name} Health: {_enemy.Health}, Distance: {_enemy.Distance}km");

                DisplayCombatProgress();

                // Apply mission logic but don't check completion if we just missed a finishing move
                bool missionComplete = false;
                if (!_missedFinishingMoveThisTurn)
                {
                    missionComplete = _currentMission.CheckCompletion(_player, _enemy);
                    if (missionComplete)
                    {
                        Console.WriteLine("\nMission objectives completed!");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                    }
                }
                else
                {
                    // Reset flag for next turn
                    _missedFinishingMoveThisTurn = false;
                }

                // Wait for user input before continuing to the next turn
                Console.WriteLine("\nPress any key to continue to the next turn...");
                Console.ReadKey();
            }

            // Display battle summary
            Program.DisplayBattleSummary(_attackDetails, _player, _enemy);

            // Mission completion status
            if (_player.Health > 0 && _currentMission.CheckCompletion(_player, _enemy))
            {
                Console.WriteLine("\nMISSION SUCCESS!");
            }
            else
            {
                Console.WriteLine("\nMISSION FAILED!");
            }
        }
          
        

        private static int ApplyDistanceChange(JetFighter attacker, JetFighter defender, int requestedChange)
        {
            // Apply maneuverability to determine actual change
            double attackerFactor = attacker.ManeuverCapability / 100.0;
            double defenderFactor = defender.ManeuverCapability / 100.0;

            // Calculate effective change based on both aircraft capabilities
            int effectiveChange;
            if (requestedChange < 0) // Attacker wants to close distance
            {
                effectiveChange = (int)(requestedChange * attackerFactor);

                // Defender response - tries to maintain distance
                double defenderResponse = JetFighter.random.NextDouble();
                if (defenderResponse > 0.7) // 30% chance defender counters effectively
                {
                    effectiveChange = (int)(effectiveChange * (1 - defenderFactor));
                    Console.WriteLine("Opponent counters your approach!");
                }
            }
            else if (requestedChange > 0) // Attacker wants to increase distance
            {
                effectiveChange = (int)(requestedChange * attackerFactor);

                // Defender response - may try to close
                double defenderResponse = JetFighter.random.NextDouble();
                if (defenderResponse > 0.6) // 40% chance defender pursues effectively
                {
                    effectiveChange = (int)(effectiveChange * (1 - defenderFactor));
                    Console.WriteLine("Opponent pursues aggressively!");
                }
            }
            else
            {
                effectiveChange = 0;
            }

            // Update a single distance value - stored in both fighters for simplicity
            int newDistance = Math.Max(1, Math.Min(150, attacker.Distance + effectiveChange));
            attacker.Distance = newDistance;
            defender.Distance = newDistance;
            
            Console.WriteLine($"Distance is now {newDistance}km");
            return effectiveChange;
        }

        private void ProcessPlayerTurn()
        {
            bool skipAttack = false;
            bool missedFinishingMove = false; // Add this flag
            
            // Check if any weapons are in range
            bool weaponsInRange = _player.Weapons.Values.Any(w => w.Range >= _player.Distance && w.Quantity > 0);
            
            if (!weaponsInRange)
            {
                Console.WriteLine("\nWARNING: All weapons are out of effective range!");
                Console.WriteLine("1. Fire anyway (reduced effectiveness)");
                Console.WriteLine("2. Skip attack and focus on maneuvering");
                
                int skipChoice;
                while (true)
                {
                    string? input = Console.ReadLine();
                    if (int.TryParse(input, out skipChoice) && (skipChoice == 1 || skipChoice == 2))
                        break;
                    Console.WriteLine("Invalid choice. Please enter 1 or 2.");
                }
                
                if (skipChoice == 2)
                {
                    Console.WriteLine("You hold your fire and focus on maneuvering.");
                    skipAttack = true; // Set flag instead of returning
                }
            }

            // Only process weapon selection and firing if not skipping attack
            if (!skipAttack)
            {
                // Show weapon menu and attack options
                Program.ShowMenu(_player);
                
                // Add option to not fire
                Console.WriteLine("\nEnter the number of the weapon you want to use (0 to skip attack):");
                
                int weaponChoice;
                while (true)
                {
                    string? input = Console.ReadLine();
                    if (int.TryParse(input, out weaponChoice) && weaponChoice >= 0 && 
                        weaponChoice <= _player.Weapons.Count)
                        break;
                        
                    Console.WriteLine($"Invalid choice. Please enter a number between 0 and {_player.Weapons.Count}.");
                }
                
                if (weaponChoice == 0)
                {
                    Console.WriteLine("You decide to hold your fire this turn.");
                    skipAttack = true; // Set flag instead of returning
                }
                else
                {
                    // Continue with weapon firing...
                    var weapon = _player.Weapons.Values.ElementAt(weaponChoice - 1);
                    
                    if (weapon.AttemptFire(_player, _enemy, _player.Distance))
                    {
                        // Calculate damage directly without countermeasure checks
                        int damage = CalculateDamage(weapon, _player, _enemy, _player.Distance);
                        _enemy.Health -= damage;
                        _player.AddDamage(damage);
                        
                        // Add combat narrative
                        AddCombatNarrative(_player, _enemy, weapon, damage);

                        // Chance of critical hit
                        if (damage > 0)
                            HandleCriticalHit(_enemy, weapon);

                        // Create attack record
                        _attackDetails.Add(new AttackDetail(_player.Name, _enemy.Name, damage, _enemy.Health));
                        
                        Console.WriteLine($"You fired {weapon.Name} and dealt {damage} damage!");

                        // Check for excellent hit
                        if (damage > weapon.BaseDamage * 1.1)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("EXCELLENT HIT! The target takes massive damage!");
                            Console.ResetColor();
                        }

                        if (_enemy.Health < 25) // If enemy is nearly defeated
                        {
                            Console.WriteLine("\nEnemy aircraft is heavily damaged! Attempt finishing move?");
                            Console.WriteLine("1. Yes - Higher damage but might miss");
                            Console.WriteLine("2. No - Continue normal attack");

                            if (int.TryParse(Console.ReadLine(), out int finishChoice) && finishChoice == 1)
                            {
                                Console.WriteLine("You line up for the kill shot...");
                                _executingFinishingMove = true; // Set flag for damage calculation
                                
                                if (JetFighter.random.Next(100) < 70) // 70% chance to succeed
                                {
                                    // Finishing move success logic
                                    int finishingDamage = Math.Min(100, _enemy.Health + JetFighter.random.Next(10, 30));
                                    _enemy.Health = 0;
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"FINISHING STRIKE! You deliver a devastating {finishingDamage} damage, destroying the enemy aircraft!");
                                    Console.ResetColor();
                                    return; // Keep this return
                                }
                                else
                                {
                                    // Missed finishing move logic
                                    Console.WriteLine("You missed the critical shot! The enemy aircraft manages to evade at the last second.");
                                    missedFinishingMove = true;
                                }
                                _executingFinishingMove = false; // Reset flag
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fire {weapon.Name} - weapon out of ammo or failed to achieve lock!");
                    }
                }
            }

            // Always present the movement options
            Console.WriteLine($"\nFuel remaining: {_player.Fuel}%");
            Console.WriteLine($"Current Altitude: {_player.Altitude} ft");
            Console.WriteLine("Do you want to change the distance?");
            Console.WriteLine("1. Move Closer");
            Console.WriteLine("2. Move Away");
            Console.WriteLine("3. Maintain Distance");
            Console.WriteLine("4. Change Altitude");
            int distanceChoice;
            while (true)
            {
                string? input = Console.ReadLine();
                if (int.TryParse(input, out distanceChoice) && (distanceChoice >= 1 && distanceChoice <= 4))
                    break;
                Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
            }
            int distanceChange = 0;
            if (distanceChoice == 1)
                distanceChange = -10;
            else if (distanceChoice == 2)
                distanceChange = 10;
            else if (distanceChoice == 3)
                distanceChange = 0;
            else if (distanceChoice == 4)
            {
                Console.WriteLine("Enter new altitude (1000-60000 ft):");
                int newAlt = _player.Altitude;
                while (true)
                {
                    string? altInput = Console.ReadLine();
                    if (int.TryParse(altInput, out newAlt) && newAlt >= 1000 && newAlt <= 60000)
                        break;
                    Console.WriteLine("Invalid altitude. Enter a value between 1000 and 60000.");
                }
                _player.UpdateAltitude(newAlt - _player.Altitude);
                Console.WriteLine($"Altitude changed to {_player.Altitude} ft.");
            }
            if (distanceChoice != 4)
            {
                ApplyDistanceChange(_player, _enemy, distanceChange);
            }
            // Pass the flag to game controller
            _missedFinishingMoveThisTurn = missedFinishingMove;
        }
        
        private void ProcessEnemyTurn()
        {
            // Determine strategy
            EnemyStrategy strategy = DetermineEnemyStrategy(_enemy, _player);
            
            // AI to decide on distance change
            int distanceChange = 0;
            if (strategy == EnemyStrategy.Aggressive)
            {
                distanceChange = -10; // Try to close
            }
            else if (strategy == EnemyStrategy.Evasive)
            {
                distanceChange = 10;  // Try to increase distance
            }
            else
            {
                distanceChange = JetFighter.random.Next(-5, 6); // Small random change
            }

            // Apply distance change
            ApplyDistanceChange(_enemy, _player, distanceChange);

            // Select weapon
            Weapon weapon = _enemy.SelectBestWeapon(strategy);

            // PROBLEM: This attempt often fails, leaving the enemy without attacks
            if (weapon.AttemptFire(_enemy, _player, _enemy.Distance))
            {
                // Calculate damage immediately
                int damage = CalculateDamage(weapon, _enemy, _player, _enemy.Distance);
                _player.Health -= damage;
                _enemy.AddDamage(damage);
                
                AddCombatNarrative(_enemy, _player, weapon, damage);

                // Only display hit messages if there was actual damage
                if (damage > 0)
                {
                    // Chance of critical hit
                    HandleCriticalHit(_player, weapon);

                    // Create attack record
                    _attackDetails.Add(new AttackDetail(_enemy.Name, _player.Name, damage, _player.Health));
                    
                    Console.WriteLine($"Enemy fired {weapon.Name} and dealt {damage} damage!");

                    // Check for excellent hit
                    if (damage > weapon.BaseDamage * 1.1)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("EXCELLENT HIT! The target takes massive damage!");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine($"Enemy fired {weapon.Name} but failed to cause significant damage!");
                    // Still record the attack with 0 damage
                    _attackDetails.Add(new AttackDetail(_enemy.Name, _player.Name, 0, _player.Health));
                }
            }
            else
            {
                Console.WriteLine($"Enemy failed to fire {weapon.Name} - weapon out of ammo or failed to achieve lock!");
            }
        }

        private void AddCombatNarrative(JetFighter attacker, JetFighter target, Weapon weapon, int damage)
        {
            WeatherSystem.AddCombatNarrative(target, weapon, damage);
        }

        private int CalculateDamage(Weapon weapon, JetFighter attacker, JetFighter target, int distance)
        {
            // Track statistics
            if (weapon.RequiresLock) {
                attacker.IncrementMissilesFired();
            } else {
                attacker.IncrementCannonRoundsFired();
            }
            
            // MISSILE FOCUS: Higher base damage for missiles
            int minDamage, maxDamage;
            if (weapon.RequiresLock) {
                minDamage = (int)(weapon.BaseDamage * 0.9);  // Increase minimum damage
                maxDamage = (int)(weapon.BaseDamage * 1.4);  // Increase maximum damage
            } else {
                minDamage = (int)(weapon.BaseDamage * 0.7);  // Decrease for cannon
                maxDamage = (int)(weapon.BaseDamage * 1.0);  // Decrease for cannon
            }
            
            int baseDamage = JetFighter.random.Next(minDamage, maxDamage + 1);
            
            // Calculate hit probability based on accuracy, distance, and target maneuverability
            double baseHitChance = weapon.Accuracy / 100.0;
            double distanceFactor = Math.Min(1.0, weapon.Range / (double)distance);
            double maneuverabilityPenalty = target.Maneuverability / 200.0; // Higher maneuverability = harder to hit
            
            double hitChance = baseHitChance * distanceFactor * (1.0 - maneuverabilityPenalty);
            
            // Apply weather accuracy modifier
            double weatherModifier = _weatherSystem.GetAccuracyModifier(weapon);
            hitChance *= weatherModifier;
            
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
            
            // Calculate final base damage before pilot skills
            double finalDamage = baseDamage * distanceModifier;
            
            // NOW apply pilot skill modifiers after finalDamage is calculated
            if (attacker.Pilot != null)
            {
                // Different skills affect different weapons
                if (weapon.RequiresLock)
                {
                    double missileSkill = attacker.Pilot.GetSkillModifier(PilotSkill.MissileGuidance);
                    hitChance *= missileSkill;
                    finalDamage *= missileSkill;
                }
                else
                {
                    double gunnerySkill = attacker.Pilot.GetSkillModifier(PilotSkill.Gunnery);
                    hitChance *= gunnerySkill;
                    finalDamage *= gunnerySkill;
                }
            }
            
            if (target.Pilot != null)
            {
                // Target's pilot can reduce damage through evasion skills
                double evasionSkill = target.Pilot.GetSkillModifier(PilotSkill.Evasion);
                hitChance /= evasionSkill; // Harder to hit with better evasion
            }
            
            // IMPROVED AI: Make AI as strong as player, but with a slight random bonus for realism
            if (!attacker.IsPlayer) {
                // AI gets same bonuses as player
                if (weapon.RequiresLock) {
                    finalDamage *= 2.0;
                    hitChance *= 1.6;
                } else {
                    finalDamage *= 1.2;
                    hitChance *= 1.1;
                }
                // AI minimum damage for missiles
                if (finalDamage > 0 && weapon.RequiresLock)
                    finalDamage = Math.Max(finalDamage, weapon.BaseDamage * 1.0);
                // AI gets a small random bonus to simulate unpredictability
                finalDamage *= (1.0 + JetFighter.random.NextDouble() * 0.15); // up to +15% random
            } else {
                // Player bonuses
                if (weapon.RequiresLock) {
                    finalDamage *= 2.0;
                    hitChance *= 1.6;
                } else {
                    finalDamage *= 1.2;
                    hitChance *= 1.1;
                }
                if (finalDamage > 0 && weapon.RequiresLock)
                    finalDamage = Math.Max(finalDamage, weapon.BaseDamage * 1.0);
            }
            
            // Random roll to see if weapon hits
            if (JetFighter.random.NextDouble() > hitChance)
            {
                // Missed the target
                return 0;
            }
            
            // Record weapon hit
            if (weapon.RequiresLock) {
                attacker.IncrementMissilesHit();
            }
            attacker.IncrementWeaponHits();

            // Determine which system takes damage based on weapon type
            AircraftSystem targetSystem = DetermineTargetSystem(weapon);
            
            // Apply damage to specific system
            int systemDamage = (int)(finalDamage * 0.5);  // 50% damage to specific system (down from 60%)
            int healthDamage = (int)(finalDamage * 0.5);  // 50% damage to overall health (up from 40%)
            
            target.SystemHealth[targetSystem] = Math.Max(0, target.SystemHealth[targetSystem] - systemDamage);

            // Update target's capabilities based on system damage
            UpdateSystemEffects(target);
            
            // Add a damage cap for non-finishing moves when enemy is nearly defeated
            if (target.Health < 25 && !_executingFinishingMove) {
                // Cap damage to prevent instant kills when player declined finishing move
                finalDamage = Math.Min(finalDamage, Math.Max(target.Health - 1, 1));
                finalDamage = Math.Max(5, finalDamage); // Ensure at least 5 damage
            }
            // Realism: Altitude effects
            if (attacker.Altitude < 5000) {
                finalDamage *= 0.8; // Low altitude, less missile effectiveness
            } else if (attacker.Altitude > 40000) {
                finalDamage *= 0.9; // High altitude, less cannon effectiveness
            }
            // Realism: Fuel system (affects damage if low)
            if (attacker is JetFighter jet) {
                if (jet.Fuel < 20) {
                    finalDamage *= 0.7; // Low fuel, less power
                }
            }
            // Realism: Random system failures
            if (target.SystemHealth[AircraftSystem.Engine] < 20 && JetFighter.random.Next(100) < 10) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{target.Name}'s engine sputters! Emergency power lost.");
                Console.ResetColor();
                finalDamage *= 1.2; // Easier to finish off a crippled jet
            }
            return (int)Math.Round(healthDamage > 0 ? finalDamage * 0.5 : 0);
        }

        private static AircraftSystem DetermineTargetSystem(Weapon weapon)
        {
            // Different weapons target different systems
            if (weapon.Type == WeaponType.M61A2_Vulcan || weapon.Type == WeaponType.GSh301)
            {
                // Cannons are more likely to hit external systems
                int rand = JetFighter.random.Next(100); // Use class member variable
                if (rand < 50)
                    return AircraftSystem.FlightControls;
                else
                    return AircraftSystem.WeaponSystems;
            }
            else if (weapon.Type == WeaponType.AIM120_AMRAAM || weapon.Type == WeaponType.R77)
            {
                // Medium range missiles - more likely to cause critical damage
                int rand = JetFighter.random.Next(100);
                if (rand < 40)
                    return AircraftSystem.Engine;
                else if (rand < 70)
                    return AircraftSystem.Radar;
                else
                    return AircraftSystem.WeaponSystems;
            }
            else
            {
                // Short range missiles - varied damage
                int rand = JetFighter.random.Next(4);
                return (AircraftSystem)rand;
            }
        }

        private static void UpdateSystemEffects(JetFighter target)
        {
            // Engine damage reduces maneuverability
            if (target.SystemHealth[AircraftSystem.Engine] < 50)
            {
                double damageFactor = target.SystemHealth[AircraftSystem.Engine] / 100.0;
                target.ManeuverCapability = (int)(target.ManeuverCapability * damageFactor);
            }
            
            // Radar damage reduces detection capability
            if (target.SystemHealth[AircraftSystem.Radar] < 60)
            {
                double damageFactor = target.SystemHealth[AircraftSystem.Radar] / 100.0;
                target.UpdateRadarRange((int)(target.RadarRange * damageFactor)); // Use our new method
            }
            
            // Display system damage notifications if severe
            foreach (var system in target.SystemHealth)
            {
                if (system.Value < 30)
                {
                    Console.WriteLine($"WARNING: {target.Name}'s {system.Key} critically damaged at {system.Value}%!");
                }
                else if (system.Value < 50)
                {
                    Console.WriteLine($"{target.Name}'s {system.Key} damaged at {system.Value}%");
                }
            }
        }

        private static EnemyStrategy DetermineEnemyStrategy(JetFighter enemy, JetFighter player)
        {
            // Make the AI adapt to player's health and its own health
            int playerHealthPercent = player.Health;
            int enemyHealthPercent = enemy.Health;

            // If player is low, AI goes for the kill
            if (playerHealthPercent < 30 && enemyHealthPercent > 20)
                return EnemyStrategy.Aggressive;

            // If AI is low, it will try to evade or defend
            if (enemyHealthPercent < 25)
                return JetFighter.random.Next(0, 100) < 60 ? EnemyStrategy.Evasive : EnemyStrategy.Defensive;

            // If both are healthy, AI mixes strategies
            if (enemy.Distance < 15)
                return JetFighter.random.Next(0, 100) < 70 ? EnemyStrategy.Aggressive : EnemyStrategy.Defensive;
            if (enemy.Distance > 40)
                return JetFighter.random.Next(0, 100) < 60 ? EnemyStrategy.Aggressive : EnemyStrategy.Defensive;

            // If AI has health advantage, be aggressive
            if (enemyHealthPercent > playerHealthPercent + 20)
                return EnemyStrategy.Aggressive;

            // If player has health advantage, be defensive
            if (playerHealthPercent > enemyHealthPercent + 20)
                return EnemyStrategy.Defensive;

            // Otherwise, random mix
            int choice = JetFighter.random.Next(0, 100);
            if (choice < 60) return EnemyStrategy.Aggressive;
            if (choice < 85) return EnemyStrategy.Defensive;
            return EnemyStrategy.Evasive;
        }

        private static void HandleCriticalHit(JetFighter target, Weapon weapon)
        {
            // More balanced critical hit chances
            int baseChance = target.IsPlayer ? 20 : 25; // More balanced (12% → 20% for AI, 30% → 25% for player)
            
            if (weapon.RequiresLock)
                baseChance += 10; // Keep extra 10% for missiles
                
            if (target.Health < 50)
                baseChance += 8; // Increased from 5% to 8% when target is damaged
            
            if (JetFighter.random.Next(100) < baseChance)
            {
                AircraftSystem system = (AircraftSystem)JetFighter.random.Next(4);
                int criticalDamage = JetFighter.random.Next(40, 61); // 40-60% damage (up from 30-50%)
                
                // Apply critical damage
                target.SystemHealth[system] = Math.Max(0, target.SystemHealth[system] - criticalDamage);
                
                // Display critical hit message
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"CRITICAL HIT! {weapon.Name} causes severe damage to {target.Name}'s {system}!");
                Console.ResetColor();
                
                // More significant system effects
                switch (system)
                {
                    case AircraftSystem.Engine:
                        Console.WriteLine($"{target.Name}'s engine is on fire! Maneuverability severely reduced!");
                        target.ManeuverCapability = (int)(target.ManeuverCapability * 0.6); // Greater penalty
                        break;
                    case AircraftSystem.Radar:
                        Console.WriteLine($"{target.Name}'s radar is severely damaged! Detection capability greatly reduced!");
                        target.UpdateRadarRange((int)(target.RadarRange * 0.5)); // Greater penalty
                        break;
                    case AircraftSystem.FlightControls:
                        Console.WriteLine($"{target.Name}'s flight controls are failing! Aircraft is difficult to control!");
                        break;
                    case AircraftSystem.WeaponSystems:
                        Console.WriteLine($"{target.Name}'s weapon systems are compromised! Accuracy reduced!");
                        break;
                }
            }
        }

        private void CheckForRandomEvents()
        {
            if (JetFighter.random.Next(100) < 20) // INCREASED from 15% to 20% chance
            {
                int eventType = JetFighter.random.Next(7); // INCREASED event types
                Console.ForegroundColor = ConsoleColor.Cyan;
                
                switch (eventType)
                {
                    case 0: // Weather change
                        WeatherCondition newWeather = (WeatherCondition)JetFighter.random.Next(4);
                        _weatherSystem.SetWeather(newWeather);
                        Console.WriteLine($"\nWEATHER CHANGE: Conditions shift to {newWeather}!");
                        break;
                        
                    case 1: // System malfunction
                        if (_player.Health > 20) // Only if not already critical
                        {
                            AircraftSystem system = (AircraftSystem)JetFighter.random.Next(4);
                            Console.WriteLine($"\nSYSTEM MALFUNCTION: Your {system} is experiencing temporary issues!");
                            // Temporary effect - lasts one turn
                        }
                        break;
                        
                    case 2: // Radio chatter
                        string[] chatter = [
                            "Command: \"Be advised, additional hostiles inbound to your sector.\"",
                            "Wingman: \"Nice shooting! Keep it up!\"",
                            "AWACS: \"Maintain current heading. You're doing fine.\"",
                            "Tower: \"Weather front moving in. Expect visibility changes.\""
                        ];
                        Console.WriteLine($"\nRADIO: {chatter[JetFighter.random.Next(chatter.Length)]}");
                        break;
                        
                    case 3: // Mechanical advantage
                        if (JetFighter.random.Next(2) == 0) // 50/50 who gets the advantage
                        {
                            Console.WriteLine("\nYour aircraft responds perfectly to your commands! Temporary maneuverability boost.");
                            _player.ManeuverCapability += 10;
                        }
                        else
                        {
                            Console.WriteLine("\nEnemy pilot executes a perfect maneuver! They gain a temporary advantage.");
                            _enemy.ManeuverCapability += 10;
                        }
                        break;
                        
                    case 4: // Situational awareness
                        if (_player.Health < _enemy.Health && JetFighter.random.Next(100) < 60) // Help player if behind
                        {
                            Console.WriteLine("\nYou spot a tactical advantage in the terrain! Next attack +10% accuracy.");
                            // Effect applied in next attack
                        }
                        else
                        {
                            Console.WriteLine("\nEnemy pilot gains tactical advantage from the terrain.");
                            // Effect applied in next attack
                        }
                        break;
                    
                    // ...other cases...
                }
                
                Console.ResetColor();
            }
        }

        private void DisplayCombatProgress()
        {
            int playerDPT = (int)_attackDetails.Where(a => a.Attacker == _player.Name).TakeLast(3).DefaultIfEmpty(new AttackDetail("", "", 0, 0)).Average(a => a.Damage);
            int enemyDPT = (int)_attackDetails.Where(a => a.Attacker == _enemy.Name).TakeLast(3).DefaultIfEmpty(new AttackDetail("", "", 0, 0)).Average(a => a.Damage);
            
            int playerTurnsLeft = playerDPT > 0 ? _enemy.Health / playerDPT : 999;
            int enemyTurnsLeft = enemyDPT > 0 ? _player.Health / enemyDPT : 999;
            
            Console.WriteLine($"\nCombat Estimate: Victory in ~{playerTurnsLeft} turns | Defeat in ~{enemyTurnsLeft} turns");
        }
    }

    class Program
    {
        private static readonly Random random = new();
        private readonly WeatherSystem _weatherSystem = new();

        public static void ShowMenu(JetFighter jetFighter)
        {
            Console.WriteLine("\n========== Combat Information ==========");
            
            // Color code health based on value
            Console.Write($"Your Aircraft: {jetFighter.Name} | Health: ");
            
            if (jetFighter.Health > 60)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (jetFighter.Health > 30)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;
                
            Console.WriteLine($"{jetFighter.Health}");
            Console.ResetColor();
            
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

        public static void DisplayBattleSummary(List<AttackDetail> attackDetails, JetFighter playerJet, JetFighter enemyJet)
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

        private static double CalculateAccuracy(JetFighter jet)
        {
            int totalShots = jet.MissilesFired + (jet.CannonRoundsFired / 10); // Count every 10 cannon rounds as 1 "shot"
            int totalHits = jet.MissilesHit + (jet.WeaponHits - jet.MissilesHit);
            
            if (totalShots == 0) return 0;
            return (totalHits / (double)totalShots) * 100;
        }

        public static void RunGameLoop() // Change from private to public
        {
            // Main game loop logic here
        }

        private static void Main()
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

            // Create pilot for player
            Pilot playerPilot = new("Player Pilot");
            playerJet.AssignPilot(playerPilot);

            Mission mission = new AirSuperiority();

            // Create and initialize the game controller
            GameController gameController = new(playerJet, enemyJet, mission);

            // Initialize weather
            gameController.SetWeather(WeatherCondition.Clear);

            // Run the game loop
            gameController.RunGameLoop();
        }
    }
}
