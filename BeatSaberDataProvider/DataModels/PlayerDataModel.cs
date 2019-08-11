using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeatSaberDataProvider.DataModels
{


    [Serializable]
    public class PlayerData
    {
        public string playerId { get; set; }
        public string playerName { get; set; }
        public bool shouldShowTutorialPrompt { get; set; }
        public bool agreedToEula { get; set; }
        public PlayerGameplayModifiers gameplayModifiers { get; set; }
        public PlayerSpecificSettings playerSpecificSettings { get; set; }
        public Dictionary<string, PlayModeOverallStatsData> playerAllOverallStatsData { get; set; }
        public List<LevelStatsData> levelsStatsData { get; set; }
        public List<MissionStatsData> missionStatsData { get; set; }
        public List<string> showedMissionHelpIds { get; set; }
        public AchievementsData achievementsData { get; set; }

    }
    [Serializable]
    public class LevelStatsData
    {
        public static readonly Regex NewIDPattern = new Regex(@"^custom_level_([0-9a-fA-f]{40})_?(.+)?", RegexOptions.Compiled);
        [JsonIgnore]
        public string Directory { get; set; }
        [JsonIgnore]
        public string Hash { get; set; }
        [JsonIgnore]
        public string SongName { get; set; }
        [JsonIgnore]
        public string AuthorName { get; set; }
        [JsonIgnore]
        private string _levelId;
        public string levelId
        {
            get
            {
                return _levelId;
            }
            set
            {
                Directory = Hash = SongName = AuthorName = string.Empty;
                _levelId = value;
                var match = NewIDPattern.Match(value);
                if (match.Success)
                {
                    Hash = match.Groups[1].Value;
                    Directory = match.Groups[2].Value;
                }
                else
                {
                    string[] parts = value.Split('∎');
                    if (parts.Count() > 3)
                    {
                        Hash = parts[0];
                        SongName = parts[1];
                        AuthorName = parts[3];
                    }
                }


            }
        }
        public int difficulty { get; set; }
        public string beatmapCharacteristicName { get; set; }
        public int highScore { get; set; }
        public int maxCombo { get; set; }
        public bool fullCombo { get; set; }
        public int maxRank { get; set; }
        public bool validScore { get; set; }
        public int playCount { get; set; }

        public override string ToString()
        {
            return levelId;
        }
    }
    [Serializable]
    public class PlayerGameplayModifiers
    {
        public bool energyType { get; set; }
        public bool noFail { get; set; }
        public bool instaFail { get; set; }
        public bool failOnSaberClash { get; set; }
        public int enabledObstacleType { get; set; }
        public bool fastNotes { get; set; }
        public bool strictAngles { get; set; }
        public bool disappearingArrows { get; set; }
        public bool ghostNotes { get; set; }
        public bool noBombs { get; set; }
        public int songSpeed { get; set; }
    }
    [Serializable]
    public class PlayerSpecificSettings
    {
        public bool staticLights { get; set; }
        public bool leftHanded { get; set; }
        public bool swapColors { get; set; }
        public double playerHeight { get; set; }
        public bool disableSFX { get; set; }
        public bool reduceDebris { get; set; }
        public bool noTextsAndHuds { get; set; }
        public bool advancedHud { get; set; }
    }

    [Serializable]
    public class PlayModeOverallStatsData
    {
        public int goodCutsCount { get; set; }
        public int badCutsCount { get; set; }
        public int missedCutsCount { get; set; }
        public long totalScore { get; set; }
        public int playedLevelsCount { get; set; }
        public int cleardLevelsCount { get; set; }
        public int failedLevelsCount { get; set; }
        public int fullComboCount { get; set; }
        public float timePlayed { get; set; }
        public int handDistanceTravelled { get; set; }
        public long cummulativeCutScoreWithoutMultiplier { get; set; }
    }
    [Serializable]
    public class MissionStatsData
    {
        public string missionId { get; set; }
        public bool cleared { get; set; }
    }
    [Serializable]
    public class AchievementsData
    {
        public List<string> unlockedAchievements { get; set; }

        public List<string> unlockedAchievementsToUpload { get; set; }
    }
    [Serializable]
    public class GuestPlayer
    {
        public string playerName { get; set; }
        public PlayerSpecificSettings playerSpecificSettings { get; set; }
}
}
