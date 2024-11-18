using System.Drawing;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Snake.Model;

namespace CS2Snake.Menu;

public class GameState
{
    public CCSPlayerController _player { get; set; }
    public PlayerButtons Buttons { get; set; }
    public string? CenterHtml { get; private set; }
    public bool GameRunning { get; set; } = false;
    public Database Database { get; init; }

    private List<(int X, int Y)> snake;
    private (int X, int Y) direction;
    private (int X, int Y) food;
    private readonly int gridWidth = 19;
    private readonly int gridHeight = 6;
    private Random random;
    private bool gameOver = false;
    private bool showingHighscores = false;
    private int _score;


    public GameState()
    {
        random = new Random();
        ResetGame();
    }

    // Initialize the game
    private void ResetGame()
    {
        int startX = gridWidth / 2;
        int startY = gridHeight / 2;

        // Initialize the snake with 3 parts, moving left
        snake =
        [
            (startX, startY),       // Head (center)
            (startX + 1, startY),   // Second part (right of the head)
            (startX + 2, startY)    // Third part (right of the second part)
        ];

        direction = (-1, 0); // Start moving left
        food = GenerateFoodPosition();
        gameOver = false;
        showingHighscores = false;
        _score = 0; // Reset score
    }


    private (int X, int Y) GenerateFoodPosition()
    {
        int foodX, foodY;
        do
        {
            foodX = random.Next(0, gridWidth);
            foodY = random.Next(0, gridHeight);
        } while (snake.Contains((foodX, foodY))); // Ensure food isn't placed on the snake
        return (foodX, foodY);
    }

    public void CloseGame()
    {
        GameRunning = false;
        _player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(_player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
        Utilities.SetStateChanged(_player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
    }

    // Called when the player opens the menu
    public void StartGame()
    {
        ResetGame();
        if (_player.PlayerPawn.Value != null && _player.PlayerPawn.Value.IsValid)
        {
            _player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
            Schema.SetSchemaValue(_player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
            Utilities.SetStateChanged(_player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
        }

        GameRunning = true;
        UpdateCenterHtml();
    }

    public void Down()
    {
        if (direction != (0, -1)) // Prevent reversing direction
            direction = (0, 1);
    }

    public void Up()
    {
        if (direction != (0, 1))
            direction = (0, -1);
    }

    public void Left()
    {
        if (direction != (1, 0))
            direction = (-1, 0);
    }

    public void Right()
    {
        if (direction != (-1, 0))
            direction = (1, 0);
    }

    internal void Retry()
    {
        StartGame();
    }

    // This function updates the game state and renders the grid
    public void UpdateCenterHtml()
    {
        if (showingHighscores) return;
        if (gameOver)
        {
            ShowGameOver();
            return;
        }

        MoveSnake();

        StringBuilder builder = new();
        builder.AppendLine($"<font class='{FontSizes.FontSizeM}' color='lightgreen'>Snake</font><font class='{FontSizes.FontSizeSm}' color='white'> - Score: {_score}</font>"); // Updated to display score

        // Render the grid
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (snake.Contains((x, y)))
                {
                    builder.Append($"<font class='{FontSizes.FontSizeL}' color='{Color.LightGreen.Name}'>█</font>");
                }
                else if (food == (x, y))
                {
                    builder.Append($"<font class='{FontSizes.FontSizeL}' color='{Color.Red.Name}'>█</font>");
                }
                else
                {
                    builder.Append($"<font class='{FontSizes.FontSizeL}' color='{Color.DimGray.Name}'>█</font>");
                }
            }
            builder.AppendLine();

            if (y == gridHeight - 1) { builder.Append('_'); } // Fixes a layout issue
        }
        CenterHtml = builder.ToString();
    }

    private void ShowGameOver()
    {
        if (_player.IsValid)
        {
            //Save Highscore
            Database.SaveHighScore(new HighScore() { SteamId = _player.SteamID, Score = _score, Name = _player.PlayerName });
        }

        //List player score
        ShowHighscores(isGameover: true);

        PrintScoreToChat();
    }

    public void ShowHighscores(bool isGameover = false)
    {
        showingHighscores = true;

        StringBuilder builder = new();
        if (isGameover)
        {
            builder.Append($"<font color='red'>Game Over</font><br><font class='{FontSizes.FontSizeSm}' color='white'>Your Score: </font><font class='{FontSizes.FontSizeSm}' color='gold'>{_score}</font>");
            builder.Append("<br>");
        }
        else
        {
            var highscore = Database.GetHighScore(_player.SteamID);
            builder.Append($"<font color='red'>Highscores</font><br><font class='{FontSizes.FontSizeSm}' color='white'>Your Best Score: </font><font class='{FontSizes.FontSizeSm}' color='gold'>{highscore?.Score ?? 0}</font>");
            builder.Append("<br>");
        }

        var highScores = Database.GetHighScores();

        int maxDisplayedScores = 8;
        int maxNameLength = 20;     // Maximum allowed name length
        int rank = 1;               // Start rank at 1

        builder.Append($"<font class='{FontSizes.FontSizeS}'>ㅤ</font>");

        if (highScores.Any())
        {
            // Loop through and display each score up to maxDisplayedScores
            for (int i = 0; i < highScores.Count && i < maxDisplayedScores; i++)
            {
                var player = highScores[i];

                // Trim player name if it exceeds maxNameLength
                string playerName = player.Name.Length > maxNameLength
                    ? player.Name[..maxNameLength]
                    : player.Name;

                var playerColor = player.SteamId == _player.SteamID ? Color.Orange : Color.White;

                // Append formatted score information
                builder.Append("<font>");
                builder.Append($"<font color='lightgreen' class='{FontSizes.FontSizeS}'>{rank}. </font>");
                builder.Append($"<font class='{FontSizes.FontSizeS}' color='{playerColor.Name}'>{playerName}: </font>");
                builder.Append($"<font class='{FontSizes.FontSizeS}' color='gold'>{player.Score}</font>");
                builder.Append("</font><br>"); // New line after each player

                rank++; // Increment rank for the next player
            }
        }
        else
        {
            builder.Append("<font>");
            builder.Append($"<font color='white' class='{FontSizes.FontSizeS}'>No highscores</font>");
            builder.Append("</font><br>"); // New line after each player
        }

        // Footer for retry/exit options
        builder.Append("<center><font color='green' class='fontSize-sm'>Retry: </font><font color='orange' class='fontSize-s'>[R]</font><font color='white' class='fontSize-sm'> | </font><font color='red' class='fontSize-sm'>Exit: </font><font color='orange' class='fontSize-s'>[Tab]</font></center>");
        CenterHtml = builder.ToString();
    }

    private void PrintScoreToChat()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot) continue;
            player.PrintToChat($" [{ChatColors.Green}Snake{ChatColors.Default}] {ChatColors.LightBlue}{_player.PlayerName}{ChatColors.Default} scored {ChatColors.Gold}{_score}{ChatColors.Default} points!");
        }
    }

    // Moves the snake based on the current direction
    private void MoveSnake()
    {
        if (gameOver) return;

        // Calculate new head position with wrapping
        (int X, int Y) newHead = (snake[0].X + direction.X, snake[0].Y + direction.Y);

        // Wrap around horizontally (left/right)
        if (newHead.X < 0) newHead.X = gridWidth - 1; // Wrap from left to right
        if (newHead.X >= gridWidth) newHead.X = 0;    // Wrap from right to left

        // Wrap around vertically (top/bottom)
        if (newHead.Y < 0) newHead.Y = gridHeight - 1; // Wrap from top to bottom
        if (newHead.Y >= gridHeight) newHead.Y = 0;    // Wrap from bottom to top

        // Check for collision with self
        if (snake.Contains(newHead))
        {
            gameOver = true;
            return;
        }

        // Move the snake's body
        snake.Insert(0, newHead); // Add the new head
        if (newHead == food)
        {
            // Snake ate the food, increase the score
            _score += 10; // Increment the score
            // Generate new food and don't remove the tail (snake grows)
            food = GenerateFoodPosition();
        }
        else
        {
            snake.RemoveAt(snake.Count - 1); // Remove the tail (move forward)
        }
    }
}
