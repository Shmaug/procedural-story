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

        public static void GetBoxGeometry(BoundingBox box, out Vector3[] verts, out Vector3[] norms, out int[] inds, int indexOffset = 0) {

            Vector3 size = box.Max - box.Min;

            verts = new Vector3[] {
                // bottom face
                box.Min + size * new Vector3(0, 0, 0),
                box.Min + size * new Vector3(1, 0, 0),
                box.Min + size * new Vector3(0, 0, 1),
                box.Min + size * new Vector3(1, 0, 1),
                
                // top face
                box.Min + size * new Vector3(0, 1, 0),
                box.Min + size * new Vector3(0, 1, 1),
                box.Min + size * new Vector3(1, 1, 0),
                box.Min + size * new Vector3(1, 1, 1),
                
                // left face
                box.Min + size * new Vector3(0, 0, 0),
                box.Min + size * new Vector3(0, 0, 1),
                box.Min + size * new Vector3(0, 1, 0),
                box.Min + size * new Vector3(0, 1, 1),
                
                // right face
                box.Min + size * new Vector3(1, 0, 0),
                box.Min + size * new Vector3(1, 1, 0),
                box.Min + size * new Vector3(1, 0, 1),
                box.Min + size * new Vector3(1, 1, 1),

                // front face
                box.Min + size * new Vector3(0, 0, 0),
                box.Min + size * new Vector3(1, 0, 0),
                box.Min + size * new Vector3(0, 1, 0),
                box.Min + size * new Vector3(1, 1, 0),

                // back face
                box.Min + size * new Vector3(0, 0, 1),
                box.Min + size * new Vector3(0, 1, 1),
                box.Min + size * new Vector3(1, 0, 1),
                box.Min + size * new Vector3(1, 1, 1),
            };

            norms = new Vector3[] {
                Vector3.Down,Vector3.Down,Vector3.Down,Vector3.Down,
                Vector3.Up,Vector3.Up,Vector3.Up,Vector3.Up,
                Vector3.Left,Vector3.Left,Vector3.Left,Vector3.Left,
                Vector3.Right,Vector3.Right,Vector3.Right,Vector3.Right,
                Vector3.Forward,Vector3.Forward,Vector3.Forward,Vector3.Forward,
                Vector3.Backward,Vector3.Backward,Vector3.Backward,Vector3.Backward
            };

            inds = new int[] {
                // bottom
                1, 0, 2,
                3, 1, 2,

                // top
                4, 6, 5,
                6, 7, 5,

                // left
                8, 10, 9,
                9, 10, 11,

                // right
                12, 14, 13,
                14, 15, 13,

                // front
                16, 19, 18,
                19, 16, 17,

                // back
                20, 21, 22,
                21, 23, 22
            };

            for (int i = 0; i < inds.Length; i++)
                inds[i] += indexOffset;
        }
    }
}
