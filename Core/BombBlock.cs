namespace Tetris.Core
{
    public sealed class BombBlock : Block
    {
        public const int BombBlockId = 8;

        private const int SpawnRow = 0;
        private const int SpawnColumn = 4;
        private const int LocalOrigin = 0;

        private readonly Position[][] tiles =
        {
            new[] { new Position(LocalOrigin, LocalOrigin) }
        };

        public override int Id => BombBlockId;
        protected override Position StartOffset => new Position(SpawnRow, SpawnColumn);
        protected override Position[][] Tiles => tiles;
    }
}