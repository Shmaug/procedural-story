using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Procedural_Story.Util;

namespace Procedural_Story.World {
    class HouseHold : ModelObject {

        public HouseHold(Vector3 pos, Area area) : base(pos, Models.BoxModel, area) {
            Scale = 5;
        }
    }
}
