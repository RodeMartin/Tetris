namespace Tetris.Core
{
    public static class WallKickData
    {
        private const int IBlockId = 1;
        private const int OBlockId = 4;

        private const int State0 = 0;
        private const int StateR = 1;
        private const int State2 = 2;
        private const int StateL = 3;

        private const int Zero = 0;
        private const int PlusOne = 1;
        private const int MinusOne = -1;
        private const int PlusTwo = 2;
        private const int MinusTwo = -2;

        private static readonly (int RowOffset, int ColumnOffset)[] NoKickOffsets =
        {
            (Zero, Zero)
        };

        // SRS push offsets for standard blocks (J, L, S, T, Z)
        private static readonly (int RowOffset, int ColumnOffset)[] Standard0ToR =
        {
            (Zero, Zero), (Zero, MinusOne), (MinusOne, MinusOne), (PlusTwo, Zero), (PlusTwo, MinusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] StandardRTo0 =
        {
            (Zero, Zero), (Zero, PlusOne), (PlusOne, PlusOne), (MinusTwo, Zero), (MinusTwo, PlusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] StandardRTo2 =
        {
            (Zero, Zero), (Zero, PlusOne), (PlusOne, PlusOne), (MinusTwo, Zero), (MinusTwo, PlusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] Standard2ToR =
        {
            (Zero, Zero), (Zero, MinusOne), (MinusOne, MinusOne), (PlusTwo, Zero), (PlusTwo, MinusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] Standard2ToL =
        {
            (Zero, Zero), (Zero, PlusOne), (MinusOne, PlusOne), (PlusTwo, Zero), (PlusTwo, PlusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] StandardLTo2 =
        {
            (Zero, Zero), (Zero, MinusOne), (PlusOne, MinusOne), (MinusTwo, Zero), (MinusTwo, MinusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] StandardLTo0 =
        {
            (Zero, Zero), (Zero, MinusOne), (PlusOne, MinusOne), (MinusTwo, Zero), (MinusTwo, MinusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] Standard0ToL =
        {
            (Zero, Zero), (Zero, PlusOne), (MinusOne, PlusOne), (PlusTwo, Zero), (PlusTwo, PlusOne)
        };

        // Special SRS push offsets for the I block
        private static readonly (int RowOffset, int ColumnOffset)[] IBlock0ToR =
        {
            (Zero, Zero), (Zero, MinusTwo), (Zero, PlusOne), (PlusOne, MinusTwo), (MinusTwo, PlusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlockRTo0 =
        {
            (Zero, Zero), (Zero, PlusTwo), (Zero, MinusOne), (MinusOne, PlusTwo), (PlusTwo, MinusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlockRTo2 =
        {
            (Zero, Zero), (Zero, MinusOne), (Zero, PlusTwo), (MinusTwo, MinusOne), (PlusOne, PlusTwo)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlock2ToR =
        {
            (Zero, Zero), (Zero, PlusOne), (Zero, MinusTwo), (PlusTwo, PlusOne), (MinusOne, MinusTwo)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlock2ToL =
        {
            (Zero, Zero), (Zero, PlusTwo), (Zero, MinusOne), (MinusOne, PlusTwo), (PlusTwo, MinusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlockLTo2 =
        {
            (Zero, Zero), (Zero, MinusTwo), (Zero, PlusOne), (PlusOne, MinusTwo), (MinusTwo, PlusOne)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlockLTo0 =
        {
            (Zero, Zero), (Zero, PlusOne), (Zero, MinusTwo), (PlusTwo, PlusOne), (MinusOne, MinusTwo)
        };

        private static readonly (int RowOffset, int ColumnOffset)[] IBlock0ToL =
        {
            (Zero, Zero), (Zero, MinusOne), (Zero, PlusTwo), (MinusTwo, MinusOne), (PlusOne, PlusTwo)
        };

        public static (int RowOffset, int ColumnOffset)[] GetKickOffsets(int blockId, int fromState, int toState)
        {
            if (blockId == OBlockId || blockId == BombBlock.BombBlockId)
            {
                return NoKickOffsets;
            }

            if (blockId == IBlockId)
            {
                return (fromState, toState) switch
                {
                    (State0, StateR) => IBlock0ToR,
                    (StateR, State0) => IBlockRTo0,
                    (StateR, State2) => IBlockRTo2,
                    (State2, StateR) => IBlock2ToR,
                    (State2, StateL) => IBlock2ToL,
                    (StateL, State2) => IBlockLTo2,
                    (StateL, State0) => IBlockLTo0,
                    (State0, StateL) => IBlock0ToL,
                    _ => NoKickOffsets
                };
            }

            return (fromState, toState) switch
            {
                (State0, StateR) => Standard0ToR,
                (StateR, State0) => StandardRTo0,
                (StateR, State2) => StandardRTo2,
                (State2, StateR) => Standard2ToR,
                (State2, StateL) => Standard2ToL,
                (StateL, State2) => StandardLTo2,
                (StateL, State0) => StandardLTo0,
                (State0, StateL) => Standard0ToL,
                _ => NoKickOffsets
            };
        }
    }
}