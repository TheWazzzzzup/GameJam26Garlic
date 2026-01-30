using System;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public static class Data
    {
        public static int AskQuestions;
        public static int TotalQuestions;

        private static float PlayerMinDamage = 5;
        private static float PlayerMaxDamage = 5;
        
        public static float MaxHealth = 1000;
        public static float CurrentHealth = MaxHealth;
        public static float EnemyDamage = 5;

        public static float MaxSatisfaction;
        public static float CurrentSatisfaction;

        public static event Action OnDeath;

        public static void RaiseDeath() => OnDeath?.Invoke();

        public static float GetPlayerDamage()
        {
            return Random.Range(PlayerMinDamage, PlayerMaxDamage);
        }
        
    }
}