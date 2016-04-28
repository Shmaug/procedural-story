using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Procedural_Story.Core;

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

        public static T[] MakeArray<T>(T obj, int length) {
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
                array[i] = obj;
            return array;
        }

        /// <summary>
        /// Takes in a mesh that contains verticies in lists, indexes and optimizes them
        /// </summary>
        /// <param name="inVerts"></param>
        /// <param name="inTris"></param>
        /// <param name="verts"></param>
        /// <param name="tris"></param>
        public static void OptimizeMesh(IList<VertexPositionColorNormal> inVerts, out List<VertexPositionColorNormal> verts, out List<int> tris) {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            verts = new List<VertexPositionColorNormal>();
            tris = new List<int>();
            
            #region duplicate vertex removal
            float minNormDiff = .01f * .01f;
            for (int i = 0; i < inVerts.Count; i++) {
                bool add = true;
                for (int j = 0; j < verts.Count; j++) {
                    if (inVerts[i].Position == verts[j].Position &&
                        Vector3.DistanceSquared(inVerts[i].Normal, verts[j].Normal) < minNormDiff &&
                        inVerts[i].Color == verts[j].Color) {
                        // This vertex already added, index it and move on
                        tris.Add(j);
                        add = false;
                        break;
                    }
                }
                if (!add)
                    continue;
                
                // This vertex not added yet, add this vertex and index it
                tris.Add(verts.Count);
                verts.Add(inVerts[i]);
            }
            #endregion
            
            #region degenerate triangle removal
            /*
            float minArea = .25f * .25f;
            for (int i = 0; i < tris.Count; i += 3) {
                VertexPositionColorNormal v1 = verts[tris[i]], v2 = verts[tris[i + 1]], v3 = verts[tris[i + 2]];
                if (Vector3.Cross(v2.Position - v1.Position, v3.Position - v1.Position).LengthSquared() / 4f < minArea) {
                    tris.RemoveAt(i);
                    tris.RemoveAt(i);
                    tris.RemoveAt(i);
                    
                    i -= 3;
                }
            }*/
            #endregion
            

            watch.Stop();
            Debug.Log("Optimized " + inVerts.Count + " to " + verts.Count + " in " + watch.ElapsedMilliseconds + "ms");
        }
    }
}
