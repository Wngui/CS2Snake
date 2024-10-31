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
                (1, "NoobMaster69", 400),
                (7, "ThePizzaPope", 300),
                (4, "HeadshotHunter", 150),
                (2, "PotatoAimPro", 120),
                (8, "BananaBlaster", 100),
                (3, "CampingIsLife", 75),
                (5, "LaggingLegend", 50),
                (6, "BaconBandit", 25),
                (9, "007", 7),
                (10, "EpicFailGuy", 0)
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
            WHERE excluded.score > score;",
                new { highscore.SteamId, highscore.Score, highscore.Name });
        }
    }
}
