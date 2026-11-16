using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using Tetris.Rendering;

namespace Tetris
{
    public partial class MainWindow : Window
    {
        private const int OpenGLMajorVersion = 3;
        private const int OpenGLMinorVersion = 3;
        private const int EmptyBlockImageIndex = 0;

        private const int MaxTickDelayMs = 1000;
        private const int MinTickDelayMs = 75;
        private const int DelayDecreasePerPointMs = 25;
        private const float MinDeltaTimeSeconds = 0.0f;
        private const float MaxDeltaTimeSeconds = 0.05f;

        private readonly ImageSource[] blockImages =
        {
            new BitmapImage(new Uri("Assets/Block-Empty.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-I.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-J.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-L.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-O.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-S.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-T.png", UriKind.Relative)),
            new BitmapImage(new Uri("Assets/Block-Z.png", UriKind.Relative))
        };

        private GameState gameState = new GameState();
        private TetrisRenderer? renderer;
        private readonly Stopwatch renderClock = Stopwatch.StartNew();

        public MainWindow()
        {
            InitializeComponent();
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
            state.BlockHardDropped += HandleBlockHardDropped;
        }

        private void UnsubscribeFromGameEvents(GameState state)
        {
            state.RowsCleared -= HandleRowsCleared;
            state.BlockHardDropped -= HandleBlockHardDropped;
        }

        private void HandleRowsCleared(List<int> clearedRows)
        {
            renderer?.OnRowsCleared(clearedRows);
        }

        private void HandleBlockHardDropped(Block droppedBlock)
        {
            renderer?.OnHardDrop(droppedBlock);
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

        private void DrawNextBlock(BlockQueue blockQueue)
        {
            NextImage.Source = blockImages[blockQueue.NextBlock.Id];
        }

        private void DrawHeldBlock(Block? heldBlock)
        {
            HoldImage.Source = heldBlock == null
                ? blockImages[EmptyBlockImageIndex]
                : blockImages[heldBlock.Id];
        }

        private void UpdateHud(GameState state)
        {
            DrawNextBlock(state.BlockQueue);
            DrawHeldBlock(state.HeldBlock);
            ScoreText.Text = $"SCORE  {state.Score}";
        }

        private async Task GameLoop()
        {
            UpdateHud(gameState);

            while (!gameState.GameOver)
            {
                int delay = Math.Max(MinTickDelayMs, MaxTickDelayMs - (gameState.Score * DelayDecreasePerPointMs));
                await Task.Delay(delay);

                gameState.MoveBlockDown();
                UpdateHud(gameState);
            }

            GameOverMenu.Visibility = Visibility.Visible;
            FinalScoreText.Text = $"SCORE  {gameState.Score}";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameState.GameOver)
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await GameLoop();
        }

        private async void PlayAgain_Click(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromGameEvents(gameState);
            gameState = new GameState();
            SubscribeToGameEvents(gameState);

            renderer?.ResetEffects();
            GameOverMenu.Visibility = Visibility.Hidden;
            await GameLoop();
        }

        protected override void OnClosed(EventArgs e)
        {
            UnsubscribeFromGameEvents(gameState);
            renderer?.Dispose();
            base.OnClosed(e);
        }
    }
}