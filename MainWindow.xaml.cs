using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using Tetris.Audio;
using Tetris.Core;
using Tetris.Rendering;
using Tetris.UI;

namespace Tetris
{
    public partial class MainWindow : Window
    {
        private const int OpenGLMajorVersion = 3;
        private const int OpenGLMinorVersion = 3;
        private const int NoTimeLimit = 0;
        private const int FirstLevelIndex = 1;
        private const string BonusStoryboardKey = "BonusRainbowStoryboard";

        private const float MinDeltaTimeSeconds = 0.0f;
        private const float MaxDeltaTimeSeconds = 0.05f;

        private readonly SoundEngine soundEngine = new SoundEngine();
        private readonly GameStatisticsRepository statisticsRepository = new GameStatisticsRepository();
        private readonly Stopwatch renderClock = Stopwatch.StartNew();

        private GameState gameState;
        private GameMode selectedMode = GameMode.Medium;
        private TetrisRenderer? renderer;
        private CancellationTokenSource? gameLoopCts;
        private bool isFullscreen;

        public MainWindow()
        {
            InitializeComponent();
            gameState = new GameState(GameMode.Medium, statisticsRepository, GameSessionState.ModeSelection);
            SubscribeToGameEvents(gameState);

            GLWpfControlSettings glSettings = new GLWpfControlSettings
            {
                MajorVersion = OpenGLMajorVersion,
                MinorVersion = OpenGLMinorVersion
            };
            OpenGLControl.Start(glSettings);
        }

        private void SubscribeToGameEvents(GameState state)
        {
            state.RowsCleared += HandleRowsCleared;
            state.BombDetonated += HandleBombDetonated;
            state.BlockHardDropped += HandleBlockHardDropped;
            state.BonusEarned += HandleBonusEarned;
            state.PieceMoved += soundEngine.PlayMove;
            state.PieceRotated += soundEngine.PlayRotate;
        }

        private void UnsubscribeFromGameEvents(GameState state)
        {
            state.RowsCleared -= HandleRowsCleared;
            state.BombDetonated -= HandleBombDetonated;
            state.BlockHardDropped -= HandleBlockHardDropped;
            state.BonusEarned -= HandleBonusEarned;
            state.PieceMoved -= soundEngine.PlayMove;
            state.PieceRotated -= soundEngine.PlayRotate;
        }

        private void HandleRowsCleared(List<int> clearedRows)
        {
            soundEngine.PlayLineClear();
            renderer?.OnRowsCleared(clearedRows);
        }

        private void HandleBombDetonated(List<int> blastedRows)
        {
            soundEngine.PlayBombExplosion();
            renderer?.OnBombDetonated(blastedRows);
        }

        private void HandleBlockHardDropped(Block droppedBlock)
        {
            soundEngine.PlayHardDrop();
            renderer?.OnHardDrop(droppedBlock);
        }

        private void HandleBonusEarned(BonusNotification bonus)
        {
            BonusTitleText.Text = bonus.Title;
            BonusSubtitleText.Text = bonus.Subtitle;

            if (Resources[BonusStoryboardKey] is Storyboard storyboard)
            {
                storyboard.Begin(this, isControllable: true);
            }
        }

        private void OpenGLControl_Ready()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            renderer = new TetrisRenderer();
        }

        private void OpenGLControl_Render(TimeSpan delta)
        {
            if (renderer == null)
            {
                return;
            }

            float elapsedSeconds = (float)renderClock.Elapsed.TotalSeconds;
            float clampedDelta = Math.Clamp((float)delta.TotalSeconds, MinDeltaTimeSeconds, MaxDeltaTimeSeconds);

            renderer.RenderFrame(gameState, elapsedSeconds, clampedDelta);
        }

        private void UpdateHud(GameState state)
        {
            HoldImage.Source = BlockPreviewRenderer.RenderBlock(state.HeldBlock);
            NextQueueList.ItemsSource = state.BlockQueue.UpcomingBlocks
                .Select(BlockPreviewRenderer.RenderBlock)
                .ToList();

            ModeStatistics stats = statisticsRepository.GetStats(state.ModeConfig.Mode);
            int displayedBestScore = Math.Max(stats.BestScore, state.Score);

            ScoreText.Text = $"SCORE  {state.Score}";
            MultiplierText.Text = $"{state.ScoreCalculator.ActiveMultiplier:0.00}x";
            BestScoreText.Text = $"BEST  {displayedBestScore}";
            ModeBadgeText.Text = state.ModeConfig.Mode.ToString().ToUpperInvariant();
            LevelText.Text = $"LEVEL: {state.Level}";

            TimerText.Text = state.ModeConfig.TimeLimitSeconds > NoTimeLimit
                ? $"TIME: {state.RemainingTime:mm\\:ss}"
                : "TIME: ∞";

            SyncOverlaysWithSessionState(state.SessionState);
        }

        private void SyncOverlaysWithSessionState(GameSessionState sessionState)
        {
            ModeMenuOverlay.Visibility = sessionState == GameSessionState.ModeSelection
                ? Visibility.Visible
                : Visibility.Hidden;

            PauseOverlay.Visibility = sessionState == GameSessionState.Paused
                ? Visibility.Visible
                : Visibility.Hidden;

            GameOverMenu.Visibility = sessionState == GameSessionState.GameOver
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private void StopCurrentGameLoop()
        {
            if (gameLoopCts != null)
            {
                gameLoopCts.Cancel();
                gameLoopCts.Dispose();
                gameLoopCts = null;
            }
        }

        private async Task StartNewGame(GameMode mode)
        {
            StopCurrentGameLoop();

            selectedMode = mode;
            UnsubscribeFromGameEvents(gameState);
            gameState = new GameState(mode, statisticsRepository, GameSessionState.Playing);
            SubscribeToGameEvents(gameState);

            renderer?.ResetEffects();
            NewRecordBadge.Visibility = Visibility.Collapsed;

            gameLoopCts = new CancellationTokenSource();
            await RunGameLoopAsync(gameLoopCts.Token);
        }

        private async Task RunGameLoopAsync(CancellationToken token)
        {
            UpdateHud(gameState);

            try
            {
                while (gameState.SessionState != GameSessionState.GameOver
                       && gameState.SessionState != GameSessionState.ModeSelection
                       && !token.IsCancellationRequested)
                {
                    int levelSteps = gameState.Level - FirstLevelIndex;
                    int delay = Math.Max(
                        gameState.ModeConfig.MinTickDelayMs,
                        gameState.ModeConfig.InitialTickDelayMs - (levelSteps * gameState.ModeConfig.DelayDecreasePerStepMs)
                    );

                    await Task.Delay(delay, token);

                    if (gameState.SessionState == GameSessionState.Playing)
                    {
                        gameState.TickTimer(TimeSpan.FromMilliseconds(delay));
                        if (gameState.SessionState == GameSessionState.Playing)
                        {
                            gameState.MoveBlockDown();
                        }
                    }

                    UpdateHud(gameState);
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (gameState.SessionState == GameSessionState.GameOver && !token.IsCancellationRequested)
            {
                soundEngine.PlayGameOver();
                FinalScoreText.Text = $"SCORE  {gameState.Score}";
                NewRecordBadge.Visibility = gameState.IsNewHighScore ? Visibility.Visible : Visibility.Collapsed;
                UpdateHud(gameState);
            }
        }

        private void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            if (isFullscreen)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
            }
        }

        private void ToggleMute()
        {
            soundEngine.IsMuted = !soundEngine.IsMuted;
            AudioStatusText.Text = soundEngine.IsMuted ? "AUDIO: MUTED (M)" : "AUDIO: ON (M)";
            AudioStatusText.Foreground = soundEngine.IsMuted ? Brushes.OrangeRed : Brushes.Cyan;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                return;
            }

            if (e.Key == Key.M)
            {
                ToggleMute();
                return;
            }

            if (e.Key == Key.P)
            {
                gameState.TogglePause();
                UpdateHud(gameState);
                return;
            }

            if (e.Key == Key.Escape)
            {
                StopCurrentGameLoop();
                gameState.SetModeSelectionState();
                UpdateHud(gameState);
                return;
            }

            if (gameState.SessionState != GameSessionState.Playing)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                    gameState.MoveBlockLeft();
                    break;
                case Key.Right:
                    gameState.MoveBlockRight();
                    break;
                case Key.Down:
                    gameState.MoveBlockDown();
                    break;
                case Key.E:
                    gameState.RotateBlockCW();
                    break;
                case Key.Q:
                    gameState.RotateBlockCCW();
                    break;
                case Key.W:
                    gameState.HoldBlock();
                    break;
                case Key.Space:
                    gameState.DropBlock();
                    break;
                default:
                    return;
            }

            UpdateHud(gameState);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateHud(gameState);
        }

        private async void SelectMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modeName && Enum.TryParse(modeName, out GameMode mode))
            {
                await StartNewGame(mode);
            }
        }

        private async void PlayAgain_Click(object sender, RoutedEventArgs e)
        {
            await StartNewGame(selectedMode);
        }

        private void OpenModeMenu_Click(object sender, RoutedEventArgs e)
        {
            StopCurrentGameLoop();
            gameState.SetModeSelectionState();
            UpdateHud(gameState);
        }

        protected override void OnClosed(EventArgs e)
        {
            StopCurrentGameLoop();
            UnsubscribeFromGameEvents(gameState);
            soundEngine.Dispose();
            renderer?.Dispose();
            base.OnClosed(e);
        }
    }
}