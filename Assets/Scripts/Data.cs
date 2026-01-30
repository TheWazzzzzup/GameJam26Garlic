using System;

namespace DefaultNamespace
{
    public static class Data
    {
        public static int AskQuestions;
        public static int TotalQuestions;
        
        public static float MaxHealth = 1000;
        public static float CurrentHealth = MaxHealth;
        public static float EnemyDamage = 200;

        public static float MaxSatisfaction;
        public static float CurrentSatisfaction;

        public static event Action OnDeath;

        public static void RaiseDeath() => OnDeath?.Invoke();
    }
}