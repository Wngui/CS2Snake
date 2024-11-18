using CS2Snake.Model;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CS2Snake
{
    public class Database
    {
        private SqliteConnection _connection;

        public void Initialize(string directory, bool includePlaceholders = true)
        {
            _connection =
                new SqliteConnection(
                    $"Data Source={Path.Join(directory, "database.db")}");

            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS `highscores` (
                    `steamid` UNSIGNED BIG INT NOT NULL,
                    `score` INT NOT NULL DEFAULT '0',
                    `name` VARCHAR(64),
	                PRIMARY KEY (`steamid`));");

            InitMetadata();

            if (includePlaceholders)
            {
                InsertPlaceholders();
            }
        }

        private void InitMetadata()
        {
            // Create the 'meta' table if it doesn't exist
            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS `meta` (
                    `lastReset` DATETIME DEFAULT CURRENT_TIMESTAMP
                );");

            // Insert a row into the 'meta' table if it does not already exist
            var rowExists = _connection.ExecuteScalar<int>(@"SELECT COUNT(1) FROM `meta`;");

            if (rowExists == 0)
            {
                _connection.Execute(@"INSERT INTO `meta` (`lastReset`) VALUES (CURRENT_TIMESTAMP);");
            }
        }

        private void InsertPlaceholders()
        {
            var placeholderHighscores = new List<(ulong SteamId, string Name, int Score)>
                {
                    (1, "NoobMaster69", 400),
                    (2, "ThePizzaPope", 300),
                    (3, "HeadshotHunter", 150),
                    (4, "PotatoAimPro", 120),
                    (5, "BananaBlaster", 100),
                    (6, "CampingIsLife", 75),
                    (7, "LaggingLegend", 50),
                    (8, "EpicFailGuy", 0),
                };

            foreach (var (steamId, name, score) in placeholderHighscores)
            {
                _connection.Execute(@"
                INSERT INTO `highscores` (steamid, score, name)
                VALUES (@SteamId, @Score, @Name)
                ON CONFLICT(steamid) DO NOTHING;",
                new { SteamId = steamId, Name = name, Score = score });
            }
        }

        public List<HighScore> GetHighScores()
        {
            var highscores = _connection.Query<HighScore>("SELECT * FROM `highscores` ORDER BY score DESC LIMIT 10;");
            return highscores.ToList();
        }

        public HighScore GetHighScore(ulong steamId)
        {
            var highscore = _connection.QueryFirstOrDefault<HighScore>(
                "SELECT * FROM `highscores` WHERE steamId = @SteamId ORDER BY score DESC LIMIT 1;",
                new { SteamId = steamId });
            return highscore;
        }

        public void SaveHighScore(HighScore highscore)
        {
            _connection.Execute(@"
            INSERT INTO `highscores` (steamid, score, name)
            VALUES (@SteamId, @Score, @Name)
            ON CONFLICT(steamid) DO UPDATE SET score = @Score, name = @Name
            WHERE excluded.score > score;",
                new { highscore.SteamId, highscore.Score, highscore.Name });
        }

        public void ResetHighScores(bool includePlaceholders = true)
        {
            _connection.Execute("DELETE FROM `highscores`;");

            _connection.Execute(@"UPDATE `meta` SET `lastReset` = CURRENT_TIMESTAMP;");

            if (includePlaceholders) InsertPlaceholders();
        }

        public DateTime? GetLastSnakeReset()
        {
            var result = _connection.ExecuteScalar<DateTime?>(@"SELECT `lastReset` FROM `meta` LIMIT 1;");
            return result;
        }

    }
}
