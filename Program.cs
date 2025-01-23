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

        public JetFighter(string name, int health, int attackPower)
        {
            Name = name;
            Health = health;
            AttackPower = attackPower;
            TotalDamageDealt = 0;
        }

        public void Attack(JetFighter target, List<AttackDetail> attackDetails)
        {
            target.Health -= AttackPower;
            TotalDamageDealt += AttackPower;
            attackDetails.Add(new AttackDetail(Name, target.Name, AttackPower, target.Health));
            Console.WriteLine($"{Name} attacks {target.Name} for {AttackPower} damage.");
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
            JetFighter jet1 = new JetFighter("F-35", 100, 20);
            JetFighter jet2 = new JetFighter("Su-35", 100, 15);
            List<AttackDetail> attackDetails = new List<AttackDetail>();

            while (jet1.Health > 0 && jet2.Health > 0)
            {
                jet1.Attack(jet2, attackDetails);
                if (jet2.Health <= 0)
                {
                    Console.WriteLine($"{jet2.Name} is destroyed!");
                    break;
                }

                jet2.Attack(jet1, attackDetails);
                if (jet1.Health <= 0)
                {
                    Console.WriteLine($"{jet1.Name} is destroyed!");
                    break;
                }

                Console.WriteLine($"{jet1.Name} Health: {jet1.Health}, {jet2.Name} Health: {jet2.Health}");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }

            Console.WriteLine("Combat simulation ended.");
            DisplayBattleSummary(attackDetails, jet1, jet2);
        }

        static void DisplayBattleSummary(List<AttackDetail> attackDetails, JetFighter jet1, JetFighter jet2)
        {
            Console.WriteLine("\nBattle Summary:");
            Console.WriteLine("Attacker\tTarget\t\tDamage\tTarget Health After Attack");
            foreach (var detail in attackDetails)
            {
                Console.WriteLine($"{detail.Attacker}\t\t{detail.Target}\t\t{detail.Damage}\t{detail.TargetHealthAfterAttack}");
            }

            Console.WriteLine($"\nTotal Damage Dealt:");
            Console.WriteLine($"{jet1.Name}: {jet1.TotalDamageDealt}");
            Console.WriteLine($"{jet2.Name}: {jet2.TotalDamageDealt}");
        }
    }
}
