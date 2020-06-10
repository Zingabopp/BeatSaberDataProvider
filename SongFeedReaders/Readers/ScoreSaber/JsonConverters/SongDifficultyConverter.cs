using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.ScoreSaber.JsonConverters
{
    public class SongDifficultyConverter : JsonConverter
    {
        private static readonly char[] CharacteristicSeparators = new char[] { '_' };
        public override bool CanConvert(Type t) => t == typeof(SongDifficulty) || t == typeof(SongDifficulty?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            SongCharacteristics c = 0;
            if (reader.TokenType == JsonToken.Null) return c;
            string[]? values = serializer.Deserialize<string>(reader)?.Split(CharacteristicSeparators, StringSplitOptions.RemoveEmptyEntries);
            if(values != null)
            {
                for(int i = 0; i < values.Length; i++)
                {

                }
            }
            return c;
        }
        private static Dictionary<string, SongCharacteristics> DifficultyDict = new Dictionary<string, SongCharacteristics>()
        {
            {"ExpertPlus", SongCharacteristics. }
        }
        private SongCharacteristics SetDifficulty(string? diffName)
        {
            if (diffName == null || diffName.Length == 0) return 0;
            
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Diff)untypedValue;
            switch (value)
            {
                case Diff.ExpertPlusSoloStandard:
                    serializer.Serialize(writer, "_ExpertPlus_SoloStandard");
                    return;
                case Diff.ExpertSoloStandard:
                    serializer.Serialize(writer, "_Expert_SoloStandard");
                    return;
            }
            throw new Exception("Cannot marshal type Diff");
        }

        public static readonly DiffConverter Singleton = new DiffConverter();
    }
}
