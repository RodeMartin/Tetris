namespace Tetris.Core
{
    public sealed class BonusNotification
    {
        public string Title { get; }
        public string Subtitle { get; }

        public BonusNotification(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
        }
    }
}