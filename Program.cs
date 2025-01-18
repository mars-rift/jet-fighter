// See https://aka.ms/new-console-template for more information
using System;

namespace JetFighterCombatSim
{
    class JetFighter
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int AttackPower { get; set; }

        public JetFighter(string name, int health, int attackPower)
        {
            Name = name;
            Health = health;
            AttackPower = attackPower;
        }

        public void Attack(JetFighter target)
        {
            target.Health -= AttackPower;
            Console.WriteLine($"{Name} attacks {target.Name} for {AttackPower} damage.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            JetFighter jet1 = new JetFighter("Falcon", 100, 20);
            JetFighter jet2 = new JetFighter("Eagle", 100, 15);

            while (jet1.Health > 0 && jet2.Health > 0)
            {
                jet1.Attack(jet2);
                if (jet2.Health <= 0)
                {
                    Console.WriteLine($"{jet2.Name} is destroyed!");
                    break;
                }

                jet2.Attack(jet1);
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
        }
    }
}
