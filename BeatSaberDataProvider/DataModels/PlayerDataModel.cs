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
        public string playerId;
        public string playerName;
        public bool shouldShowTutorialPrompt;
        public bool agreedToEula;
        public PlayerGameplayModifiers gameplayModifiers;
        public PlayerSpecificSettings playerSpecificSettings;
        public Dictionary<string, PlayModeOverallStatsData> playerAllOverallStatsData;
        public List<LevelStatsData> levelsStatsData;
        public List<MissionStatsData> missionStatsData;
        public List<string> showedMissionHelpIds;
        public AchievementsData achievementsData;

    }
    [Serializable]
    public class LevelStatsData
    {
        public static readonly Regex NewIDPattern = new Regex(@"^custom_level_([0-9a-fA-f]{40})_?(.+)?", RegexOptions.Compiled);
        [JsonIgnore]
        public string directory;
        [JsonIgnore]
        public string hash;
        [JsonIgnore]
        public string songName;
        [JsonIgnore]
        public string authorName;
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
                directory = hash = songName = authorName = string.Empty;
                _levelId = value;
                var match = NewIDPattern.Match(value);
                if (match.Success)
                {
                    hash = match.Groups[1].Value;
                    directory = match.Groups[2].Value;
                }
                string[] parts = value.Split('∎');
                if (parts.Count() > 3)
                {
                    hash = parts[0];
                    songName = parts[1];
                    authorName = parts[3];
                }


            }
        }
        public int difficulty;
        public string beatmapCharacteristicName;
        public int highScore;
        public int maxCombo;
        public bool fullCombo;
        public int maxRank;
        public bool validScore;
        public int playCount;

        public override string ToString()
        {
            return levelId;
        }
    }
    [Serializable]
    public class PlayerGameplayModifiers
    {
        public bool energyType;
        public bool noFail;
        public bool instaFail;
        public bool failOnSaberClash;
        public int enabledObstacleType;
        public bool fastNotes;
        public bool strictAngles;
        public bool disappearingArrows;
        public bool ghostNotes;
        public bool noBombs;
        public int songSpeed;
    }
    [Serializable]
    public class PlayerSpecificSettings
    {
        public bool staticLights;
        public bool leftHanded;
        public bool swapColors;
        public double playerHeight;
        public bool disableSFX;
        public bool reduceDebris;
        public bool noTextsAndHuds;
        public bool advancedHud;
    }

    [Serializable]
    public class PlayModeOverallStatsData
    {
        public int goodCutsCount;
        public int badCutsCount;
        public int missedCutsCount;
        public long totalScore;
        public int playedLevelsCount;
        public int cleardLevelsCount;
        public int failedLevelsCount;
        public int fullComboCount;
        public float timePlayed;
        public int handDistanceTravelled;
        public long cummulativeCutScoreWithoutMultiplier;
    }
    [Serializable]
    public class MissionStatsData
    {
        public string missionId;
        public bool cleared;
    }
    [Serializable]
    public class AchievementsData
    {
        public List<string> unlockedAchievements;

        public List<string> unlockedAchievementsToUpload;
    }
    [Serializable]
    public class GuestPlayer
    {
        public string playerName;
        public PlayerSpecificSettings playerSpecificSettings;
    }
}
