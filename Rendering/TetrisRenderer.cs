using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Tetris.Rendering
{
    public sealed class TetrisRenderer : IDisposable
    {
        private const int VisibleRows = 20;
        private const int VisibleColumns = 10;
        private const int HiddenTopRows = 2;
        private const int FirstVisibleRowIndex = 0;
        private const int FirstColumnIndex = 0;
        private const int EmptyBlockId = 0;

        private const float CellSize = 25.0f;
        private const float CellPadding = 1.0f;
        private const float HalfCellFactor = 0.5f;
        private const float PaddingSidesMultiplier = 2.0f;
        private const float HalfCellOffset = CellSize * HalfCellFactor;
        private const float DoublePadding = CellPadding * PaddingSidesMultiplier;

        private const float BoardOriginX = 0.0f;
        private const float BoardOriginY = 0.0f;
        private const float OrthoNearPlane = -1.0f;
        private const float OrthoFarPlane = 1.0f;
        private const float PlaneDepthZ = 0.0f;
        private const float UnitScaleZ = 1.0f;
        private const float OpaqueAlpha = 1.0f;
        private const float GhostFlagTrue = 1.0f;
        private const float GhostFlagFalse = 0.0f;
        private const float BombShakeIntensity = 14.0f;

        private const float HardDropShakeIntensity = 3.5f;
        private const float LineClearShakeIntensity = 8.5f;
        private const int HardDropSparkCount = 4;
        private const int LineClearSparkCount = 10;
        private const int BombSparkCount = 18;

        private const string VertexShaderPath = "Rendering/Shaders/standard.vert";
        private const string BackgroundFragmentPath = "Rendering/Shaders/grid_bg.frag";
        private const string BlockFragmentPath = "Rendering/Shaders/neon_block.frag";

        private const string UniformProjection = "u_Projection";
        private const string UniformModel = "u_Model";
        private const string UniformTime = "u_Time";
        private const string UniformGridSize = "u_GridSize";
        private const string UniformColor = "u_Color";
        private const string UniformIsGhost = "u_IsGhost";
        
        private static readonly Vector4 BombExplosionColor = new Vector4(1.00f, 0.45f, 0.05f, OpaqueAlpha);

        private static readonly Vector4[] BlockColors =
        {
            Vector4.Zero,                                        // 0: Empty
            new Vector4(0.00f, 0.95f, 1.00f, OpaqueAlpha),       // 1: I - Neon Cyan
            new Vector4(0.15f, 0.35f, 1.00f, OpaqueAlpha),       // 2: J - Electric Blue
            new Vector4(1.00f, 0.55f, 0.00f, OpaqueAlpha),       // 3: L - Neon Orange
            new Vector4(1.00f, 0.90f, 0.05f, OpaqueAlpha),       // 4: O - Laser Yellow
            new Vector4(0.10f, 1.00f, 0.35f, OpaqueAlpha),       // 5: S - Acid Green
            new Vector4(0.75f, 0.15f, 1.00f, OpaqueAlpha),       // 6: T - Synth Purple
            new Vector4(1.00f, 0.12f, 0.30f, OpaqueAlpha),       // 7: Z - Crimson Neon
            new Vector4(1.00f, 0.82f, 0.15f, OpaqueAlpha)        // 8: Bomb - Solar Gold
        };

        private static readonly Vector4 LineClearFlashColor = new Vector4(0.35f, 0.95f, 1.00f, OpaqueAlpha);

        private readonly QuadMesh quadMesh;
        private readonly Shader backgroundShader;
        private readonly Shader blockShader;
        private readonly ParticleSystem particleSystem;
        private readonly CameraShake cameraShake;
        private readonly Matrix4 baseProjectionMatrix;

        public TetrisRenderer()
        {
            quadMesh = new QuadMesh();
            backgroundShader = Shader.FromFiles(VertexShaderPath, BackgroundFragmentPath);
            blockShader = Shader.FromFiles(VertexShaderPath, BlockFragmentPath);
            particleSystem = new ParticleSystem();
            cameraShake = new CameraShake();

            float boardWidth = VisibleColumns * CellSize;
            float boardHeight = VisibleRows * CellSize;

            baseProjectionMatrix = Matrix4.CreateOrthographicOffCenter(
                BoardOriginX,
                boardWidth,
                boardHeight,
                BoardOriginY,
                OrthoNearPlane,
                OrthoFarPlane
            );
        }

        public void OnHardDrop(Block droppedBlock)
        {
            cameraShake.Trigger(HardDropShakeIntensity);
            Vector4 color = BlockColors[droppedBlock.Id];

            foreach (Position position in droppedBlock.TilePositions())
            {
                int visibleRow = position.Row - HiddenTopRows;
                if (visibleRow >= FirstVisibleRowIndex)
                {
                    Vector2 center = GetCellCenter(visibleRow, position.Column);
                    particleSystem.SpawnCellBurst(center, color, HardDropSparkCount);
                }
            }
        }

        public void OnRowsCleared(List<int> clearedGridRows)
        {
            cameraShake.Trigger(LineClearShakeIntensity);

            foreach (int gridRow in clearedGridRows)
            {
                int visibleRow = gridRow - HiddenTopRows;
                if (visibleRow < FirstVisibleRowIndex)
                {
                    continue;
                }

                for (int column = FirstColumnIndex; column < VisibleColumns; column++)
                {
                    Vector2 center = GetCellCenter(visibleRow, column);
                    particleSystem.SpawnCellBurst(center, LineClearFlashColor, LineClearSparkCount);
                }
            }
        }

        public void OnBombDetonated(List<int> blastedGridRows)
        {
            cameraShake.Trigger(BombShakeIntensity);

            foreach (int gridRow in blastedGridRows)
            {
                int visibleRow = gridRow - HiddenTopRows;
                if (visibleRow < FirstVisibleRowIndex)
                {
                    continue;
                }

                for (int column = FirstColumnIndex; column < VisibleColumns; column++)
                {
                    Vector2 center = GetCellCenter(visibleRow, column);
                    particleSystem.SpawnCellBurst(center, BombExplosionColor, BombSparkCount);
                }
            }
        }

        public void ResetEffects()
        {
            particleSystem.Clear();
        }

        public void RenderFrame(GameState gameState, float totalElapsedSeconds, float deltaTime)
        {
            cameraShake.Update(deltaTime);
            particleSystem.Update(deltaTime);

            Matrix4 shakeTranslation = Matrix4.CreateTranslation(
                cameraShake.CurrentOffset.X,
                cameraShake.CurrentOffset.Y,
                PlaneDepthZ
            );
            Matrix4 viewProjection = shakeTranslation * baseProjectionMatrix;

            GL.Clear(ClearBufferMask.ColorBufferBit);

            DrawBackground(totalElapsedSeconds, viewProjection);
            DrawPlacedGrid(gameState.GameGrid, totalElapsedSeconds, viewProjection);

            if (!gameState.GameOver)
            {
                DrawGhostBlock(gameState, totalElapsedSeconds, viewProjection);
                DrawActiveBlock(gameState.CurrentBlock, totalElapsedSeconds, isGhost: false, viewProjection);
            }

            particleSystem.Render(quadMesh, viewProjection);
        }

        private static Vector2 GetCellCenter(int visibleRow, int column)
        {
            return new Vector2(
                (column * CellSize) + HalfCellOffset,
                (visibleRow * CellSize) + HalfCellOffset
            );
        }

        private void DrawBackground(float elapsedSeconds, Matrix4 viewProjection)
        {
            float boardWidth = VisibleColumns * CellSize;
            float boardHeight = VisibleRows * CellSize;

            Matrix4 model = Matrix4.CreateScale(boardWidth, boardHeight, UnitScaleZ);

            backgroundShader.Use();
            backgroundShader.SetMatrix4(UniformProjection, viewProjection);
            backgroundShader.SetMatrix4(UniformModel, model);
            backgroundShader.SetFloat(UniformTime, elapsedSeconds);
            backgroundShader.SetVector2(UniformGridSize, new Vector2(VisibleColumns, VisibleRows));

            quadMesh.Render();
        }

        private void DrawPlacedGrid(GameGrid grid, float elapsedSeconds, Matrix4 viewProjection)
        {
            for (int row = HiddenTopRows; row < grid.Rows; row++)
            {
                for (int column = FirstColumnIndex; column < grid.Columns; column++)
                {
                    int blockId = grid[row, column];
                    if (blockId > EmptyBlockId && blockId < BlockColors.Length)
                    {
                        DrawCell(
                            row - HiddenTopRows,
                            column,
                            BlockColors[blockId],
                            elapsedSeconds,
                            isGhost: false,
                            viewProjection
                        );
                    }
                }
            }
        }

        private void DrawGhostBlock(GameState gameState, float elapsedSeconds, Matrix4 viewProjection)
        {
            int dropDistance = gameState.BlockDropDistance();
            Block currentBlock = gameState.CurrentBlock;
            Vector4 color = BlockColors[currentBlock.Id];

            foreach (Position position in currentBlock.TilePositions())
            {
                int targetRow = position.Row + dropDistance - HiddenTopRows;
                if (targetRow >= FirstVisibleRowIndex)
                {
                    DrawCell(targetRow, position.Column, color, elapsedSeconds, isGhost: true, viewProjection);
                }
            }
        }

        private void DrawActiveBlock(Block block, float elapsedSeconds, bool isGhost, Matrix4 viewProjection)
        {
            Vector4 color = BlockColors[block.Id];

            foreach (Position position in block.TilePositions())
            {
                int visibleRow = position.Row - HiddenTopRows;
                if (visibleRow >= FirstVisibleRowIndex)
                {
                    DrawCell(visibleRow, position.Column, color, elapsedSeconds, isGhost, viewProjection);
                }
            }
        }

        private void DrawCell(
            int visibleRow,
            int column,
            Vector4 color,
            float elapsedSeconds,
            bool isGhost,
            Matrix4 viewProjection)
        {
            float positionX = (column * CellSize) + CellPadding;
            float positionY = (visibleRow * CellSize) + CellPadding;
            float cellInnerSize = CellSize - DoublePadding;

            Matrix4 scale = Matrix4.CreateScale(cellInnerSize, cellInnerSize, UnitScaleZ);
            Matrix4 translation = Matrix4.CreateTranslation(positionX, positionY, PlaneDepthZ);
            Matrix4 model = scale * translation;

            blockShader.Use();
            blockShader.SetMatrix4(UniformProjection, viewProjection);
            blockShader.SetMatrix4(UniformModel, model);
            blockShader.SetVector4(UniformColor, color);
            blockShader.SetFloat(UniformTime, elapsedSeconds);
            blockShader.SetFloat(UniformIsGhost, isGhost ? GhostFlagTrue : GhostFlagFalse);

            quadMesh.Render();
        }

        public void Dispose()
        {
            quadMesh.Dispose();
            backgroundShader.Dispose();
            blockShader.Dispose();
            particleSystem.Dispose();
        }
    }
}