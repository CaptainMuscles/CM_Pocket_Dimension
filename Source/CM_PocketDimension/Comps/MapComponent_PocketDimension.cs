using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CM_PocketDimension.Comps
{
    class MapComponent_PocketDimension : MapComponent
    {
        public List<Building_PocketDimensionBox> Building_PocketDimensionBoxes { get; set; } = new List<Building_PocketDimensionBox>();


             
        public MapComponent_PocketDimension(Map map) : base(map)
        {

        }
    }
}
