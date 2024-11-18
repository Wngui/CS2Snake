using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2Snake
{
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 1;

        [JsonPropertyName("ShowHighscorePlaceholders")] public bool ShowHighscorePlaceholders { get; set; } = true;
        [JsonPropertyName("ResetScoresWeekly")] public bool ResetScoresWeekly { get; set; } = false;
        [JsonPropertyName("ResetScoresMonthly")] public bool ResetScoresMonthly { get; set; } = false;
        [JsonPropertyName("GameSpeed")] public int GameSpeed { get; set; } = 100;
    }
}
