using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Tetris.Core;

namespace Tetris.UI
{
    public static class BlockPreviewRenderer
    {
        private const int EmptyBlockId = 0;
        private const double CanvasSize = 96.0;
        private const double CellStep = 22.0;
        private const double CellInnerSize = 20.0;
        private const double CellCornerRadius = 3.0;
        private const double BorderThickness = 1.8;
        private const double HalfDivisor = 2.0;
        private const double BombRadius = 14.0;
        private const byte CoreFillAlpha = 85;
        private const int SingleTileCount = 1;

        private static readonly Color[] NeonPalette =
        {
            Colors.Transparent,                     // 0: Empty
            Color.FromRgb(0, 242, 255),             // 1: I - Neon Cyan
            Color.FromRgb(38, 89, 255),             // 2: J - Electric Blue
            Color.FromRgb(255, 140, 0),             // 3: L - Neon Orange
            Color.FromRgb(255, 230, 13),            // 4: O - Laser Yellow
            Color.FromRgb(26, 255, 89),             // 5: S - Acid Green
            Color.FromRgb(191, 38, 255),            // 6: T - Synth Purple
            Color.FromRgb(255, 31, 77),             // 7: Z - Crimson Neon
            Color.FromRgb(255, 209, 38)             // 8: Bomb - Solar Gold
        };

        public static ImageSource RenderBlock(Block? block)
        {
            DrawingGroup group = new DrawingGroup();

            // Láthatatlan befoglaló téglalap a fix középre igazításhoz
            group.Children.Add(new GeometryDrawing(
                Brushes.Transparent,
                null,
                new RectangleGeometry(new Rect(0, 0, CanvasSize, CanvasSize))
            ));

            if (block == null || block.Id <= EmptyBlockId || block.Id >= NeonPalette.Length)
            {
                DrawingImage emptyImage = new DrawingImage(group);
                emptyImage.Freeze();
                return emptyImage;
            }

            Color neonColor = NeonPalette[block.Id];
            SolidColorBrush coreBrush = new SolidColorBrush(Color.FromArgb(CoreFillAlpha, neonColor.R, neonColor.G, neonColor.B));
            Pen edgePen = new Pen(new SolidColorBrush(neonColor), BorderThickness);
            coreBrush.Freeze();
            edgePen.Freeze();

            List<Position> positions = block.TilePositions().ToList();
            int minRow = positions.Min(p => p.Row);
            int maxRow = positions.Max(p => p.Row);
            int minCol = positions.Min(p => p.Column);
            int maxCol = positions.Max(p => p.Column);

            int spanRows = (maxRow - minRow) + SingleTileCount;
            int spanCols = (maxCol - minCol) + SingleTileCount;

            double offsetX = (CanvasSize - (spanCols * CellStep)) / HalfDivisor;
            double offsetY = (CanvasSize - (spanRows * CellStep)) / HalfDivisor;

            if (block.Id == BombBlock.BombBlockId)
            {
                double center = CanvasSize / HalfDivisor;
                group.Children.Add(new GeometryDrawing(
                    coreBrush,
                    edgePen,
                    new EllipseGeometry(new Point(center, center), BombRadius, BombRadius)
                ));
            }
            else
            {
                foreach (Position p in positions)
                {
                    double x = offsetX + ((p.Column - minCol) * CellStep);
                    double y = offsetY + ((p.Row - minRow) * CellStep);
                    Rect rect = new Rect(x, y, CellInnerSize, CellInnerSize);

                    group.Children.Add(new GeometryDrawing(
                        coreBrush,
                        edgePen,
                        new RectangleGeometry(rect, CellCornerRadius, CellCornerRadius)
                    ));
                }
            }

            DrawingImage result = new DrawingImage(group);
            result.Freeze();
            return result;
        }
    }
}