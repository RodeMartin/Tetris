using System;
using System.Collections.Generic;
using Tetris.Core;

namespace Tetris
{
    public class BlockQueue
    {
        public const int DefaultPreviewCount = 3;
        private const double BombSpawnProbability = 0.08;
        private const int FirstPreviewIndex = 0;

        private readonly Block[] blocks =
        {
            new IBlock(),
            new JBlock(),
            new LBlock(),
            new OBlock(),
            new SBlock(),
            new TBlock(),
            new ZBlock()
        };

        private readonly List<Block> upcomingBlocks = new List<Block>();
        private readonly Random random = new Random();
        private readonly bool isBombMode;
        private readonly int previewCapacity;

        public Block NextBlock => upcomingBlocks[FirstPreviewIndex];
        public IReadOnlyList<Block> UpcomingBlocks => upcomingBlocks;

        public BlockQueue(bool isBombMode = false, int previewCapacity = DefaultPreviewCount)
        {
            this.isBombMode = isBombMode;
            this.previewCapacity = previewCapacity;
            RefillQueue();
        }

        private void RefillQueue()
        {
            while (upcomingBlocks.Count < previewCapacity)
            {
                Block candidate = GenerateRandomBlock();
                if (upcomingBlocks.Count == 0 || upcomingBlocks[^1].Id != candidate.Id)
                {
                    upcomingBlocks.Add(candidate);
                }
            }
        }

        private Block GenerateRandomBlock()
        {
            if (isBombMode && random.NextDouble() < BombSpawnProbability)
            {
                return new BombBlock();
            }

            return blocks[random.Next(blocks.Length)];
        }

        public Block GetAndUpdate()
        {
            Block next = upcomingBlocks[FirstPreviewIndex];
            upcomingBlocks.RemoveAt(FirstPreviewIndex);
            RefillQueue();
            return next;
        }
    }
}