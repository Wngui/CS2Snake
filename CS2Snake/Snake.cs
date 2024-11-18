using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Snake.Menu;
using static System.Formats.Asn1.AsnWriter;

namespace CS2Snake
{
    public class Snake : BasePlugin, IPluginConfig<Config>
    {
        private Database _database;
        private SnakeGame _snakeGame;

        public override string ModuleName => "Snake";
        public override string ModuleVersion => "2.0.0";
        public override string ModuleAuthor => "BoinK";
        public override string ModuleDescription => "Play snake!";

        public static Snake? Instance { get; set; }
        public Config Config { get; set; } = null!;

        public override void Load(bool hotReload)
        {
            Instance ??= this;

            _database = new Database();

            _snakeGame = new SnakeGame();
            _snakeGame.Load(this, hotReload, _database, Config);

            _database.Initialize(ModuleDirectory, Config.ShowHighscorePlaceholders);

            if (Config.ResetScoresWeekly || Config.ResetScoresMonthly)
            {
                Instance.AddTimer(600, ShouldResetScore);
            }

            //Debug
            //Utilities.GetPlayers().ForEach(x => x.ExecuteClientCommandFromServer("css_snake"));
        }

        private void ShouldResetScore()
        {
            throw new NotImplementedException();
        }

        [ConsoleCommand("css_snake")]
        public void SnakeCommand(CCSPlayerController? player, CommandInfo? info)
        {
            SnakeGame.Start(player);
        }

        [ConsoleCommand("css_topsnake")]
        [ConsoleCommand("css_snaketop")]
        [ConsoleCommand("css_snakehighscore")]
        [ConsoleCommand("css_snakescore")]
        public void TopSnakeCommand(CCSPlayerController? player, CommandInfo? info)
        {
            SnakeGame.ShowHighscores(player);
        }

        [RequiresPermissions("@css/resetsnake")]
        [ConsoleCommand("css_resetsnake")]
        [ConsoleCommand("css_snakereset")]
        public void ResetSnakeCommand(CCSPlayerController? player, CommandInfo? info)
        {
            _database.ResetHighScores(Config.ShowHighscorePlaceholders);

            if (!player.IsValid) return;
            player.PrintToChat($" [{ChatColors.Green}Snake{ChatColors.Default}] {ChatColors.Red}All highscores have been reset{ChatColors.Default}!");
        }

        [ConsoleCommand("css_nextsnakereset")]
        [ConsoleCommand("css_snakeresetnext")]
        public void NextSnakeResetCommand(CCSPlayerController? player, CommandInfo? info)
        {
            if (!player.IsValid) return;

            var lastSnakeReset = _database.GetLastSnakeReset() ?? DateTime.Now;

            DateTime nextReset;
            if (Config.ResetScoresMonthly)
            {
                nextReset = lastSnakeReset.AddMonths(1);
            }
            else if (Config.ResetScoresWeekly)
            {
                nextReset = lastSnakeReset.AddDays(7);
            }
            else
            {
                player.PrintToChat($" [{ChatColors.Green}Snake{ChatColors.Default}] {ChatColors.LightBlue}Highscores don't reset.");
                return;
            }

            var timeRemaining = nextReset - DateTime.Now;
            player.PrintToChat($" [{ChatColors.Green}Snake{ChatColors.Default}] {ChatColors.LightBlue}Next reset in: {ChatColors.Default}{ChatColors.Gold}{timeRemaining.TotalDays} days, {timeRemaining.Hours} hours, {timeRemaining.Minutes} minutes");
        }

        public void OnConfigParsed(Config config)
        {
            Config = config;
        }
    }
}