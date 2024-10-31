namespace CS2Snake.Model
{
    public class HighScore
    {
        public required ulong SteamId { get; set; }
        public required int Score { get; set; }
        public required string Name { get; set; }
    }
}