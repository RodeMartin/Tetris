using System;

namespace Tetris.Core
{
    public sealed class ScoreCalculator
    {
        private const int NoRowsCleared = 0;
        private const int SingleLineCount = 1;
        private const int DoubleLineCount = 2;
        private const int TripleLineCount = 3;
        private const int TetrisLineCount = 4;

        private const int SingleLineBasePoints = 100;
        private const int DoubleLineBasePoints = 300;
        private const int TripleLineBasePoints = 500;
        private const int TetrisLineBasePoints = 800;
        private const int BombRowClearPoints = 250;

        private const int InitialComboStreak = 0;
        private const int ComboIncrement = 1;
        private const double BaseMultiplierFactor = 1.0;
        private const double ComboBonusPerStreak = 0.25;

        private readonly double modeBaseMultiplier;

        public int ComboStreak { get; private set; }
        public int MaxComboStreak { get; private set; }
        public double ActiveMultiplier => modeBaseMultiplier * (BaseMultiplierFactor + (ComboStreak * ComboBonusPerStreak));

        public ScoreCalculator(double modeBaseMultiplier)
        {
            this.modeBaseMultiplier = modeBaseMultiplier;
            ComboStreak = InitialComboStreak;
            MaxComboStreak = InitialComboStreak;
        }

        public int CalculateLineClearScore(int clearedRowsCount)
        {
            if (clearedRowsCount <= NoRowsCleared)
            {
                ComboStreak = InitialComboStreak;
                return NoRowsCleared;
            }

            int basePoints = clearedRowsCount switch
            {
                SingleLineCount => SingleLineBasePoints,
                DoubleLineCount => DoubleLineBasePoints,
                TripleLineCount => TripleLineBasePoints,
                TetrisLineCount => TetrisLineBasePoints,
                _ => clearedRowsCount * BombRowClearPoints
            };

            int earnedScore = (int)Math.Round(basePoints * ActiveMultiplier);
            ComboStreak += ComboIncrement;
            MaxComboStreak = Math.Max(MaxComboStreak, ComboStreak);
            return earnedScore;
        }

        public int CalculateBombExplosionScore(int destroyedRowsCount)
        {
            int rawPoints = destroyedRowsCount * BombRowClearPoints;
            return (int)Math.Round(rawPoints * ActiveMultiplier);
        }
    }
}