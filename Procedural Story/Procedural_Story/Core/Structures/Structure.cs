using System.Collections.Generic;

using Jitter.Dynamics;
using Jitter.LinearMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.Core.Structures {
    abstract class Structure : VertexBufferObject {
        public BoundingBox entrancebbox;
        public BoundingBox bbox;
        public BoundingBox innerbbox;
        public BoundingBox bboxfull
        {
            get
            {
                Vector3[] c = bbox.GetCorners();
                for (int i = 0; i < c.Length; i++) {
                    c[i] -= Position;
                    c[i] = Vector3.Transform(c[i], Matrix.CreateScale((c[i].Length() + 1) / c[i].Length()) * Orientation);
                    c[i] += Position;
                }
                return BoundingBox.CreateFromPoints(c);
            }
        }

        public List<RigidBody> RigidBodies;

        internal List<VertexPositionColorNormal> verts;
        internal List<int> inds;

        public Structure(Area area, Vector3 pos) : base(pos, area) {
            verts = new List<VertexPositionColorNormal>();
            inds = new List<int>();
            RigidBodies = new List<RigidBody>();
        }

        #region containment tests
        /// <summary>
        /// Test whether a sphere is obstructed by any objects (homes, roads, etc)
        /// </summary>
        public ObstructionType Obstructs(BoundingSphere sphere) {
            // transform sphere to fit with boundingbox's orientation, then check
            BoundingSphere sphere2 = new BoundingSphere(Vector3.Transform(sphere.Center,
            Matrix.CreateTranslation(-Position) * Matrix.Invert(Orientation) * Matrix.CreateTranslation(Position)
            ), sphere.Radius);
            if (entrancebbox.Intersects(sphere2) || innerbbox.Contains(sphere2) == ContainmentType.Contains)
                return ObstructionType.Virtual;
            if (bbox.Intersects(sphere2))
                return ObstructionType.Physical;

            return ObstructionType.None;
        }
        /// <summary>
        /// Test whether a box is obstructed by any objects (homes, roads, etc)
        /// </summary>
        public ObstructionType Obstructs(BoundingBox box) {
            if (box.Intersects(bboxfull))
                return ObstructionType.Physical;
            return ObstructionType.None;
        }
        /// <summary>
        /// Test whether a box is obstructed by any objects (homes, roads, etc)
        /// </summary>
        public ObstructionType Obstructs(BoundingBox box, Matrix matrix) {
            Vector3 p1 = Vector3.Transform(box.Min, matrix);
            Vector3 p2 = Vector3.Transform(box.Max, matrix);
            if (BoundingBox.CreateFromPoints(new Vector3[] { p1, p2 }).Intersects(bboxfull))
                return ObstructionType.Physical;
            return ObstructionType.None;
        }
        /// <summary>
        /// Test whether a point is obstructed by any objects (homes, roads, etc)
        /// </summary>
        public ObstructionType Obstructs(Vector3 p) {
            // transform sphere to fit with boundingbox's orientation, then check
            p = Vector3.Transform(p, Matrix.CreateTranslation(-Position) * Matrix.Invert(Orientation) * Matrix.CreateTranslation(Position));
            if (entrancebbox.Contains(p) == ContainmentType.Contains || innerbbox.Contains(p) == ContainmentType.Contains)
                return ObstructionType.Virtual;
            if (bbox.Contains(p) == ContainmentType.Contains)
                return ObstructionType.Physical;

            return ObstructionType.None;
        }
        #endregion

        public virtual void PreGenerate() { }
        public virtual void BuildGeometry(GraphicsDevice device) {
            // generate vertex buffer
            VBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), verts.Count, BufferUsage.WriteOnly);
            VBuffer.SetData(verts.ToArray());
            IBuffer = new IndexBuffer(device, typeof(int), inds.Count, BufferUsage.WriteOnly);
            IBuffer.SetData(inds.ToArray());
            
            for (int i = 0; i < RigidBodies.Count; i++) {
                RigidBodies[i].Orientation *= RigidBody.Orientation;
                RigidBodies[i].Position = JVector.Transform(RigidBodies[i].Position, RigidBody.Orientation) + RigidBody.Position;
                RigidBodies[i].IsStatic = true;
                area.Physics.AddBody(RigidBodies[i]);
            }
        }

        internal void addFace(Color color, params Vector3[] pts) {
            addFace(color, null, pts);
        }
        internal void addFace(Color color, int[] tris, params Vector3[] pts) {
            int bi = verts.Count;
            Vector3 n = Vector3.Cross(pts[2] - pts[0], pts[1] - pts[0]);
            n.Normalize();
            if (tris != null)
                for (int i = 0; i < tris.Length; i++)
                    inds.Add(bi + tris[i]);
            else
                for (int i = 2; i < pts.Length; i++) {
                    inds.Add(bi);
                    inds.Add(bi + i - 1);
                    inds.Add(bi + i);
                }
            for (int i = 0; i < pts.Length; i++)
                verts.Add(new VertexPositionColorNormal(pts[i], color, n));
        }

        internal void addBox(Color color, BoundingBox bbox) {
            Vector3 min = bbox.Min, max = bbox.Max;
            Vector3
                v1 = new Vector3(min.X, max.Y, min.Z),
                v2 = new Vector3(max.X, max.Y, min.Z),
                v3 = new Vector3(min.X, max.Y, max.Z),
                v4 = new Vector3(max.X, max.Y, max.Z),

                v5 = new Vector3(min.X, min.Y, min.Z),
                v6 = new Vector3(max.X, min.Y, min.Z),
                v7 = new Vector3(min.X, min.Y, max.Z),
                v8 = new Vector3(max.X, min.Y, max.Z);

            // top face
            int bi = verts.Count;
            verts.Add(new VertexPositionColorNormal(v1, color, Vector3.Up));
            verts.Add(new VertexPositionColorNormal(v2, color, Vector3.Up));
            verts.Add(new VertexPositionColorNormal(v3, color, Vector3.Up));
            verts.Add(new VertexPositionColorNormal(v4, color, Vector3.Up));
            inds.AddRange(new int[] {
                bi, bi + 1, bi + 3,
                bi, bi + 3, bi + 2,
            });

            // bottom face
            bi = verts.Count;
            verts.Add(new VertexPositionColorNormal(v7, color, Vector3.Down));
            verts.Add(new VertexPositionColorNormal(v8, color, Vector3.Down));
            verts.Add(new VertexPositionColorNormal(v5, color, Vector3.Down));
            verts.Add(new VertexPositionColorNormal(v6, color, Vector3.Down));
            inds.AddRange(new int[] {
                bi, bi + 1, bi + 3,
                bi, bi + 3, bi + 2,
            });

            // right face
            bi = verts.Count;
            verts.Add(new VertexPositionColorNormal(v4, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v2, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v6, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v8, color, Vector3.Right));
            inds.AddRange(new int[] {
                bi, bi + 1, bi + 2,
                bi, bi + 2, bi + 3,
            });

            // left face
            bi = verts.Count;
            verts.Add(new VertexPositionColorNormal(v7, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v5, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v1, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v3, color, Vector3.Right));
            inds.AddRange(new int[] {
                bi, bi + 1, bi + 2,
                bi, bi + 2, bi + 3,
            });

            // front face
            bi = verts.Count;
            verts.Add(new VertexPositionColorNormal(v2, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v1, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v5, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v6, color, Vector3.Right));
            inds.AddRange(new int[] {
                bi, bi + 1, bi + 2,
                bi, bi + 2, bi + 3,
            });

            // back face
            bi = verts.Count;
            verts.Add(new VertexPositionColorNormal(v3, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v4, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v8, color, Vector3.Right));
            verts.Add(new VertexPositionColorNormal(v7, color, Vector3.Right));
            inds.AddRange(new int[] {
                bi, bi + 1, bi + 2,
                bi, bi + 2, bi + 3,
            });
        }
    }

}
