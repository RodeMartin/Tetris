namespace Tetris.Core
{
    public enum GameMode
    {
        Easy,
        Medium,
        Hard,
        TimeAttack,
        Infinite,
        BombMode
    }

    public sealed class GameModeConfig
    {
        private const int EasyInitialDelayMs = 1000;
        private const int EasyMinDelayMs = 250;
        private const int EasyDelayStepMs = 15;
        private const double EasyScoreMultiplier = 0.8;

        private const int MediumInitialDelayMs = 800;
        private const int MediumMinDelayMs = 100;
        private const int MediumDelayStepMs = 25;
        private const double MediumScoreMultiplier = 1.0;

        private const int HardInitialDelayMs = 450;
        private const int HardMinDelayMs = 60;
        private const int HardDelayStepMs = 35;
        private const double HardScoreMultiplier = 1.5;

        private const int TimeAttackDurationSeconds = 120;
        private const double TimeAttackScoreMultiplier = 1.25;

        private const int InfiniteInitialDelayMs = 700;
        private const int InfiniteMinDelayMs = 150;
        private const int InfiniteDelayStepMs = 10;
        private const double InfiniteScoreMultiplier = 1.0;

        private const double BombModeScoreMultiplier = 1.2;
        private const int NoTimeLimitSeconds = 0;

        public GameMode Mode { get; }
        public int InitialTickDelayMs { get; }
        public int MinTickDelayMs { get; }
        public int DelayDecreasePerStepMs { get; }
        public double BaseScoreMultiplier { get; }
        public int TimeLimitSeconds { get; }
        public bool IsInfinite { get; }
        public bool IsBombMode { get; }

        private GameModeConfig(
            GameMode mode,
            int initialTickDelayMs,
            int minTickDelayMs,
            int delayDecreasePerStepMs,
            double baseScoreMultiplier,
            int timeLimitSeconds,
            bool isInfinite,
            bool isBombMode)
        {
            Mode = mode;
            InitialTickDelayMs = initialTickDelayMs;
            MinTickDelayMs = minTickDelayMs;
            DelayDecreasePerStepMs = delayDecreasePerStepMs;
            BaseScoreMultiplier = baseScoreMultiplier;
            TimeLimitSeconds = timeLimitSeconds;
            IsInfinite = isInfinite;
            IsBombMode = isBombMode;
        }

        public static GameModeConfig FromMode(GameMode mode)
        {
            return mode switch
            {
                GameMode.Easy => new GameModeConfig(
                    mode, EasyInitialDelayMs, EasyMinDelayMs, EasyDelayStepMs,
                    EasyScoreMultiplier, NoTimeLimitSeconds, isInfinite: false, isBombMode: false),

                GameMode.Hard => new GameModeConfig(
                    mode, HardInitialDelayMs, HardMinDelayMs, HardDelayStepMs,
                    HardScoreMultiplier, NoTimeLimitSeconds, isInfinite: false, isBombMode: false),

                GameMode.TimeAttack => new GameModeConfig(
                    mode, MediumInitialDelayMs, MediumMinDelayMs, MediumDelayStepMs,
                    TimeAttackScoreMultiplier, TimeAttackDurationSeconds, isInfinite: false, isBombMode: false),

                GameMode.Infinite => new GameModeConfig(
                    mode, InfiniteInitialDelayMs, InfiniteMinDelayMs, InfiniteDelayStepMs,
                    InfiniteScoreMultiplier, NoTimeLimitSeconds, isInfinite: true, isBombMode: false),

                GameMode.BombMode => new GameModeConfig(
                    mode, MediumInitialDelayMs, MediumMinDelayMs, MediumDelayStepMs,
                    BombModeScoreMultiplier, NoTimeLimitSeconds, isInfinite: false, isBombMode: true),

                _ => new GameModeConfig(
                    GameMode.Medium, MediumInitialDelayMs, MediumMinDelayMs, MediumDelayStepMs,
                    MediumScoreMultiplier, NoTimeLimitSeconds, isInfinite: false, isBombMode: false)
            };
        }
    }
}