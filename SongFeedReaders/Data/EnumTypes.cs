using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Data
{
    // Using schema defined here: https://github.com/lolPants/beatmap-schemas/blob/master/schemas/info.schema.json
    [Flags]
    public enum SongCharacteristics
    {
        None =              0,
        Standard =         (1 << 0),
        OneSaber =         (1 << 1),
        NoArrows =         (1 << 2),
        Lightshow =        (1 << 3),
        Lawless =          (1 << 4),
        ThreeSixtyDegree = (1 << 5),
        NinetyDegree =     (7 << 6)
    }

    public enum SongDifficulty
    {
        Unknown = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3,
        Expert = 4,
        ExpertPlus = 5
    }
}
