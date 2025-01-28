using System;
using System.Collections.Generic;

namespace JetFighterCombatSim
{
    class JetFighter
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int AttackPower { get; set; }
        public int TotalDamageDealt { get; private set; }
        private static Random random = new Random();

        public JetFighter(string name, int health, int attackPower)
        {
            Name = name;
            Health = health;
            AttackPower = attackPower;
            TotalDamageDealt = 0;
        }

        public void Attack(JetFighter target, List<AttackDetail> attackDetails)
        {
            int damage = random.Next(AttackPower - 5, AttackPower + 6); // Random damage within a range
            target.Health -= damage;
            TotalDamageDealt += damage;
            attackDetails.Add(new AttackDetail(Name, target.Name, damage, target.Health));
            Console.WriteLine($"{Name} attacks {target.Name} for {damage} damage.");
        }

        public void SpecialAttack(JetFighter target, List<AttackDetail> attackDetails)
        {
            int damage = random.Next(AttackPower, AttackPower + 15); // Special attack with higher damage
            target.Health -= damage;
            TotalDamageDealt += damage;
            attackDetails.Add(new AttackDetail(Name, target.Name, damage, target.Health));
            Console.WriteLine($"{Name} uses special attack on {target.Name} for {damage} damage.");
        }
    }

    class AttackDetail
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
        static void Main(string[] args)
        {
            Console.WriteLine("Choose your jet:");
            Console.WriteLine("1. F-35 (Health: 100, Attack Power: 20)");
            Console.WriteLine("2. Su-35 (Health: 100, Attack Power: 15)");
            int choice = int.Parse(Console.ReadLine());

            JetFighter playerJet;
            JetFighter enemyJet;

            if (choice == 1)
            {
                playerJet = new JetFighter("F-35", 100, 20);
                enemyJet = new JetFighter("Su-35", 100, 15);
            }
            else
            {
                playerJet = new JetFighter("Su-35", 100, 15);
                enemyJet = new JetFighter("F-35", 100, 20);
            }

            List<AttackDetail> attackDetails = new List<AttackDetail>();

            while (playerJet.Health > 0 && enemyJet.Health > 0)
            {
                ShowMenu();
                int actionChoice = int.Parse(Console.ReadLine());

                if (actionChoice == 1)
                {
                    playerJet.Attack(enemyJet, attackDetails);
                }
                else if (actionChoice == 2)
                {
                    playerJet.SpecialAttack(enemyJet, attackDetails);
                }

                if (enemyJet.Health <= 0)
                {
                    Console.WriteLine($"{enemyJet.Name} is destroyed!");
                    break;
                }

                enemyJet.Attack(playerJet, attackDetails);
                if (playerJet.Health <= 0)
                {
                    Console.WriteLine($"{playerJet.Name} is destroyed!");
                    break;
                }

                PostEncounterMechanics(playerJet, enemyJet);

                Console.WriteLine($"{playerJet.Name} Health: {playerJet.Health}, {enemyJet.Name} Health: {enemyJet.Health}");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }

            Console.WriteLine("Combat simulation ended.");
            DisplayBattleSummary(attackDetails, playerJet, enemyJet);
        }

        static void ShowMenu()
        {
            Console.WriteLine("Choose an action:");
            Console.WriteLine("1. Attack");
            Console.WriteLine("2. Special Attack");
        }

        static void PostEncounterMechanics(JetFighter playerJet, JetFighter enemyJet)
        {
            Console.WriteLine("Post-Encounter Mechanics:");
            Console.WriteLine($"Your jet: {playerJet.Name}, Health: {playerJet.Health}");
            Console.WriteLine($"Enemy jet: {enemyJet.Name}, Health: {enemyJet.Health}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }

        static void DisplayBattleSummary(List<AttackDetail> attackDetails, JetFighter playerJet, JetFighter enemyJet)
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
        }
    }
}