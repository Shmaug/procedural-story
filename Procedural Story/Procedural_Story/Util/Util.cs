using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Procedural_Story.Util {
    class Util {
        static float sign(Vector2 a, Vector2 b, Vector2 c) {
            return (a.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (a.Y - c.Y);
        }
        public static bool InsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c) {
            bool b1 = sign(point, a, b) < 0f;
            bool b2 = sign(point, b, c) < 0f;
            bool b3 = sign(point, c, a) < 0f;

            return b1 == b2 && b2 == b3;
        }
    }
}
