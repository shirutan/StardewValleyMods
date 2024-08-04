using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceShared;

namespace SpaceCore.Dungeons
{
    public class DungeonData
    {
        public class LevelRange
        {
            public int Begin { get; set; }
            public int End { get; set; } // inclusive
        }

        public class DungeonRegion
        {
            public LevelRange LevelRange { get; set; }
            public List<Weighted<string>> MapPool { get; set; } = new();
            public List<string> TriggerActionsOnEntry { get; set; } = new();
        }

        // Might need something for defining the world map region to show up at?

        public Dictionary<string, DungeonRegion> Regions { get; set; }

        public bool SpawnStaircases { get; set; } = true;
        public bool SpawnMineshafts { get; set; } = false;
    }
}
