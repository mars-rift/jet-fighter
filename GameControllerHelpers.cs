
namespace jet_fighter
{
    internal static class GameControllerHelpers
    {
        internal static void AddCombatNarrative(JetFighter player, JetFighter enemy, Weapon weapon, int damage)
        {
            throw new NotImplementedException();
        }

        private static void HandleManeuvers(JetFighter player)
        {
            Console.WriteLine($"\nCurrent Distance: {player.Distance}km | Altitude: {player.Altitude}ft");
        }
    }
}