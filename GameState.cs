using System;
using System.Collections.Generic;
using Tetris.Core;

namespace Tetris
{
    public class GameState
    {
        private const int GridRows = 22;
        private const int GridColumns = 10;
        private const int TopHiddenRowIndex = 0;
        private const int SecondHiddenRowIndex = 1;
        private const int FirstRowIndex = 0;
        private const int EmptyCount = 0;
        private const int SpawnNudgeSteps = 2;
        private const int SingleStep = 1;
        private const int ReverseStep = -1;
        private const int ZeroStep = 0;
        private const int BombBlastRadiusRows = 1;
        private const int HalfDivisor = 2;

        private const int InitialLevel = 1;
        private const int LinesPerLevel = 10;
        private const int DoubleLineCount = 2;
        private const int TripleLineCount = 3;
        private const int TetrisLineCount = 4;
        private const int MinimumComboToDisplay = 1;

        private Block currentBlock = null!;

        public Block CurrentBlock
        {
            get => currentBlock;
            private set
            {
                currentBlock = value;
                currentBlock.Reset();

                for (int i = 0; i < SpawnNudgeSteps; i++)
                {
                    currentBlock.Move(SingleStep, ZeroStep);

                    if (!BlockFits())
                    {
                        currentBlock.Move(ReverseStep, ZeroStep);
                    }
                }
            }
        }

        public event Action<List<int>>? RowsCleared;
        public event Action<List<int>>? BombDetonated;
        public event Action<Block>? BlockHardDropped;
        public event Action<BonusNotification>? BonusEarned;
        public event Action? PieceMoved;
        public event Action? PieceRotated;

        public GameGrid GameGrid { get; }
        public BlockQueue BlockQueue { get; }
        public GameModeConfig ModeConfig { get; }
        public ScoreCalculator ScoreCalculator { get; }
        public GameStatisticsRepository StatisticsRepository { get; }

        public GameSessionState SessionState { get; private set; }
        public bool GameOver => SessionState == GameSessionState.GameOver;
        public bool IsPaused => SessionState == GameSessionState.Paused;
        public bool IsNewHighScore { get; private set; }

        public int Score { get; private set; }
        public int TotalLinesCleared { get; private set; }
        public int Level => (TotalLinesCleared / LinesPerLevel) + InitialLevel;
        public Block? HeldBlock { get; private set; }
        public bool CanHold { get; private set; }
        public TimeSpan RemainingTime { get; private set; }

        public GameState(
            GameMode mode = GameMode.Medium,
            GameStatisticsRepository? statisticsRepository = null,
            GameSessionState initialSessionState = GameSessionState.Playing)
        {
            ModeConfig = GameModeConfig.FromMode(mode);
            ScoreCalculator = new ScoreCalculator(ModeConfig.BaseScoreMultiplier);
            StatisticsRepository = statisticsRepository ?? new GameStatisticsRepository();
            RemainingTime = TimeSpan.FromSeconds(ModeConfig.TimeLimitSeconds);
            SessionState = initialSessionState;

            GameGrid = new GameGrid(GridRows, GridColumns);
            BlockQueue = new BlockQueue(ModeConfig.IsBombMode);
            CurrentBlock = BlockQueue.GetAndUpdate();
            CanHold = true;
        }

        public void SetModeSelectionState()
        {
            SessionState = GameSessionState.ModeSelection;
        }

        public void TogglePause()
        {
            if (SessionState == GameSessionState.Playing)
            {
                SessionState = GameSessionState.Paused;
            }
            else if (SessionState == GameSessionState.Paused)
            {
                SessionState = GameSessionState.Playing;
            }
        }

        public void TickTimer(TimeSpan elapsed)
        {
            if (SessionState != GameSessionState.Playing || ModeConfig.TimeLimitSeconds <= EmptyCount)
            {
                return;
            }

            RemainingTime -= elapsed;
            if (RemainingTime <= TimeSpan.Zero)
            {
                RemainingTime = TimeSpan.Zero;
                FinishGameSession();
            }
        }

        private bool BlockFits()
        {
            foreach (Position p in CurrentBlock.TilePositions())
            {
                if (!GameGrid.IsEmpty(p.Row, p.Column))
                {
                    return false;
                }
            }

            return true;
        }

        public void HoldBlock()
        {
            if (SessionState != GameSessionState.Playing || !CanHold)
            {
                return;
            }

            if (HeldBlock == null)
            {
                HeldBlock = CurrentBlock;
                CurrentBlock = BlockQueue.GetAndUpdate();
            }
            else
            {
                Block tmp = CurrentBlock;
                CurrentBlock = HeldBlock;
                HeldBlock = tmp;
            }

            CanHold = false;
            PieceRotated?.Invoke();
        }

        public void RotateBlockCW()
        {
            if (SessionState != GameSessionState.Playing)
            {
                return;
            }

            TryRotateWithWallKicks(CurrentBlock.RotateCW, CurrentBlock.RotateCCW);
        }

        public void RotateBlockCCW()
        {
            if (SessionState != GameSessionState.Playing)
            {
                return;
            }

            TryRotateWithWallKicks(CurrentBlock.RotateCCW, CurrentBlock.RotateCW);
        }

        private void TryRotateWithWallKicks(Action rotateForward, Action rotateBackward)
        {
            int previousRotationState = CurrentBlock.RotationState;
            rotateForward();
            int nextRotationState = CurrentBlock.RotationState;

            (int RowOffset, int ColumnOffset)[] kicks = WallKickData.GetKickOffsets(
                CurrentBlock.Id,
                previousRotationState,
                nextRotationState
            );

            foreach ((int rowOffset, int columnOffset) in kicks)
            {
                CurrentBlock.Move(rowOffset, columnOffset);
                if (BlockFits())
                {
                    PieceRotated?.Invoke();
                    return;
                }

                CurrentBlock.Move(-rowOffset, -columnOffset);
            }

            rotateBackward();
        }

        public void MoveBlockLeft()
        {
            if (SessionState != GameSessionState.Playing)
            {
                return;
            }

            CurrentBlock.Move(ZeroStep, ReverseStep);
            if (!BlockFits())
            {
                CurrentBlock.Move(ZeroStep, SingleStep);
            }
            else
            {
                PieceMoved?.Invoke();
            }
        }

        public void MoveBlockRight()
        {
            if (SessionState != GameSessionState.Playing)
            {
                return;
            }

            CurrentBlock.Move(ZeroStep, SingleStep);
            if (!BlockFits())
            {
                CurrentBlock.Move(ZeroStep, ReverseStep);
            }
            else
            {
                PieceMoved?.Invoke();
            }
        }

        private bool IsGridToppedOut()
        {
            return !(GameGrid.IsRowEmpty(TopHiddenRowIndex) && GameGrid.IsRowEmpty(SecondHiddenRowIndex));
        }

        private void PlaceBlock()
        {
            if (CurrentBlock is BombBlock)
            {
                DetonateBomb();
                return;
            }

            foreach (Position p in CurrentBlock.TilePositions())
            {
                GameGrid[p.Row, p.Column] = CurrentBlock.Id;
            }

            List<int> fullRows = new List<int>();
            for (int row = FirstRowIndex; row < GameGrid.Rows; row++)
            {
                if (GameGrid.IsRowFull(row))
                {
                    fullRows.Add(row);
                }
            }

            int previousLevel = Level;
            int cleared = GameGrid.ClearFullRows();
            TotalLinesCleared += cleared;

            double multiplierBeforeClear = ScoreCalculator.ActiveMultiplier;
            int earnedPoints = ScoreCalculator.CalculateLineClearScore(cleared);
            Score += earnedPoints;

            if (fullRows.Count > EmptyCount)
            {
                RowsCleared?.Invoke(fullRows);
                NotifyLineClearBonus(cleared, earnedPoints, multiplierBeforeClear, Level > previousLevel);
            }

            if (IsGridToppedOut())
            {
                if (ModeConfig.IsInfinite)
                {
                    ClearTopHalfForInfiniteMode();
                }
                else
                {
                    FinishGameSession();
                    return;
                }
            }

            CurrentBlock = BlockQueue.GetAndUpdate();
            CanHold = true;
        }

        private void NotifyLineClearBonus(int clearedRows, int earnedPoints, double appliedMultiplier, bool leveledUp)
        {
            string clearLabel = clearedRows switch
            {
                TetrisLineCount => "TETRIS!",
                TripleLineCount => "TRIPLE!",
                DoubleLineCount => "DOUBLE!",
                _ => "LINE CLEAR"
            };

            string title = $"{clearLabel}  +{earnedPoints}";
            string subtitle = leveledUp
                ? $"LEVEL UP! LVL {Level}  •  {appliedMultiplier:0.00}x"
                : ScoreCalculator.ComboStreak > MinimumComboToDisplay
                    ? $"COMBO STREAK x{ScoreCalculator.ComboStreak}  •  {appliedMultiplier:0.00}x"
                    : $"MULTIPLIER {appliedMultiplier:0.00}x";

            BonusEarned?.Invoke(new BonusNotification(title, subtitle));
        }

        private void DetonateBomb()
        {
            HashSet<int> affectedRows = new HashSet<int>();
            foreach (Position p in CurrentBlock.TilePositions())
            {
                for (int offset = -BombBlastRadiusRows; offset <= BombBlastRadiusRows; offset++)
                {
                    int targetRow = p.Row + offset;
                    if (targetRow >= FirstRowIndex && targetRow < GameGrid.Rows)
                    {
                        affectedRows.Add(targetRow);
                    }
                }
            }

            List<int> sortedRows = new List<int>(affectedRows);
            sortedRows.Sort();

            foreach (int row in sortedRows)
            {
                GameGrid.DeleteRowAndCollapse(row);
            }

            TotalLinesCleared += sortedRows.Count;
            int earnedPoints = ScoreCalculator.CalculateBombExplosionScore(sortedRows.Count);
            Score += earnedPoints;

            BombDetonated?.Invoke(sortedRows);
            BonusEarned?.Invoke(new BonusNotification($"BOMB BLAST!  +{earnedPoints}", $"{sortedRows.Count} ROWS OBLITERATED"));

            CurrentBlock = BlockQueue.GetAndUpdate();
            CanHold = true;
        }

        private void ClearTopHalfForInfiniteMode()
        {
            int halfRows = GameGrid.Rows / HalfDivisor;
            List<int> clearedTopRows = new List<int>();

            for (int row = FirstRowIndex; row < halfRows; row++)
            {
                GameGrid.DeleteRowAndCollapse(row);
                clearedTopRows.Add(row);
            }

            RowsCleared?.Invoke(clearedTopRows);
            BonusEarned?.Invoke(new BonusNotification("OVERFLOW PURGE!", "INFINITE MODE CONTINUES"));
        }

        private void FinishGameSession()
        {
            SessionState = GameSessionState.GameOver;
            IsNewHighScore = StatisticsRepository.RecordFinishedSession(
                ModeConfig.Mode,
                Score,
                ScoreCalculator.MaxComboStreak,
                TotalLinesCleared
            );
        }

        public void MoveBlockDown()
        {
            if (SessionState != GameSessionState.Playing)
            {
                return;
            }

            CurrentBlock.Move(SingleStep, ZeroStep);

            if (!BlockFits())
            {
                CurrentBlock.Move(ReverseStep, ZeroStep);
                PlaceBlock();
            }
        }

        private int TileDropDistance(Position p)
        {
            int drop = EmptyCount;
            while (GameGrid.IsEmpty(p.Row + drop + SingleStep, p.Column))
            {
                drop++;
            }

            return drop;
        }

        public int BlockDropDistance()
        {
            int drop = GameGrid.Rows;
            foreach (Position p in CurrentBlock.TilePositions())
            {
                drop = Math.Min(drop, TileDropDistance(p));
            }

            return drop;
        }

        public void DropBlock()
        {
            if (SessionState != GameSessionState.Playing)
            {
                return;
            }

            CurrentBlock.Move(BlockDropDistance(), ZeroStep);
            BlockHardDropped?.Invoke(CurrentBlock);
            PlaceBlock();
        }
    }
}