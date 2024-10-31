using CounterStrikeSharp.API.Modules.Entities;
using CS2Snake.Model;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CS2Snake
{
    public class Database
    {
        private SqliteConnection _connection;

        public void Initialize(string directory)
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

            var placeholderHighscores = new List<(ulong SteamId, string Name, int Score)>
            {
                (1, "NoobMaster69", 600),   // Moved to first place
                (7, "ThePizzaPope", 550),
                (9, "007", 500),
                (4, "HeadshotHunter", 450),
                (2, "PotatoAimPro", 400),
                (8, "BananaBlaster", 375),
                (3, "CampingIsLife", 350),
                (5, "LaggingLegend", 300),
                (6, "BaconBandit", 250),
                (10, "EpicFailGuy", 225)
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

        public void SaveHighScore(HighScore highscore)
        {
            _connection.Execute(@"
            INSERT INTO `highscores` (steamid, score, name)
            VALUES (@SteamId, @Score, @Name)
            ON CONFLICT(steamid) DO UPDATE SET score = @Score, name = @Name
            WHERE excluded.score < score;",
                new { highscore.SteamId, highscore.Score, highscore.Name });
        }
    }
}
