using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Procedural_Story.World.Life {
    class ComplexCharacter : Character {
        public string Name;
        public float Aggression;
        public float Willpower;

        public ComplexCharacter(Area area) : base(area) {

        }
    }
}
