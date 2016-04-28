using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Procedural_Story.Core.Life;
using Jitter.Dynamics;
using Procedural_Story.Core.Structures;

namespace Procedural_Story.Core.Crops {
    class CornSeed : UsableItem {
        public CornSeed() : base(1, ItemIcons["crops"], new Rectangle(0,0,32,32)) {

        }

        public override void Use(Character user, Vector3 atPos, RigidBody onBody) {
            if (onBody != null && onBody.Tag is Farm) {
                CornCrop c = new CornCrop(DateTime.UtcNow.Millisecond, atPos, user.area);
                (onBody.Tag as Farm).AddCrop(c);
                Debug.Log("Corn planted at " + atPos);

                base.Use(user, atPos, onBody);
            }
        }

        public override void getWorldGeometry(GraphicsDevice device, out VertexBuffer VBuffer, out IndexBuffer IBuffer) {
            VBuffer = null;
            IBuffer = null;
        }
    }
    class CornItem : FoodItem {
        public CornItem() : base(0, 40, 10, 4, 10, ItemIcons["crops"], new Rectangle(0, 32, 32, 32)) {

        }

        public override void getWorldGeometry(GraphicsDevice device, out VertexBuffer VBuffer, out IndexBuffer IBuffer) {
            VBuffer = null;
            IBuffer = null;
        }
    }
    class CornCrop : Crop {
        static float Height = 2;

        float height;
        Random rand;

        public CornCrop(int seed, Vector3 pos, Area a) : base(seed, 3, pos, a) {
            rand = new Random(Seed);
            height = Height + ((float)rand.NextDouble() - .5f) * .5f;
        }

        public override void Harvest(out Item[] yield) {
            float t = 1f - TimeLeft / TimeToMature;
            int c = 0;
            if (t > .9f) {
                t -= .9f;
                t *= 10f;
                c = (int)MathHelper.Clamp(t * 5, 1, 5);
            } else {
                yield = null;
                return;
            }

            yield = new Item[c];
            for (int i = 0; i < c; i++)
                yield[i] = new CornItem();
        }
        
        public override void UpdateGeometry(GraphicsDevice device) {
            rand = new Random(Seed);

            List<Branch> branches = new List<Branch>();
            List<Leaf> leaves = new List<Leaf>();

            float step = .4f;

            float max = step + MathHelper.Clamp(1f - TimeLeft / TimeToMature, 0, 1) * (height - step);
            
            branches.Add(new Branch(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), step));
            Vector3 last = new Vector3(0, step, 0);
            float h = step;

            while (h < max) {
                float y = Math.Min(max, h + step) - h;
                for (float i = 0; i < MathHelper.TwoPi;) {
                    leaves.Add(new Leaf(last + new Vector3(0, step * (float)rand.NextDouble(), 0),
                        Matrix.CreateRotationX((float)rand.NextDouble() * MathHelper.PiOver2 * .5f) * Matrix.CreateRotationY(i),
                        .3f + (float)rand.NextDouble() * .3f));

                    i += .5f + (float)rand.NextDouble() * 2;
                }
                Branch b = new Branch(last, Matrix.CreateRotationX(MathHelper.PiOver2), y);
                branches.Add(b);
                last = b.End;
                h += y;
            }
            
            List<VertexPositionColorNormal> verts = new List<VertexPositionColorNormal>();
            List<int> tris = new List<int>();
            float r = .05f;
            int sides = 6;
            #region branch geometry
            foreach (Branch b in branches) {
                float x1 = b.Start.Y / max,
                      x2 = b.End.Y   / max;
                float r1 = r * MathHelper.Clamp(-(float)Math.Pow(x1, 7) + 1, 0, 1);
                float r2 = r * MathHelper.Clamp(-(float)Math.Pow(x2, 7) + 1, 0, 1);

                int bi = verts.Count;
                for (int s = 0; s < sides; s++) {
                    Matrix m = b.Direction * Matrix.CreateRotationY((s / (float)sides) * MathHelper.TwoPi);
                    verts.Add(new VertexPositionColorNormal(b.Start + m.Up * r1, Color.Green, m.Up));
                    verts.Add(new VertexPositionColorNormal(b.End   + m.Up * r2, Color.Green, m.Up));
                }

                // cap on final branch
                if (b.Equals(branches[branches.Count - 1]))
                    verts.Add(new VertexPositionColorNormal(b.End + b.Direction.Forward * .15f, Color.Green, b.Direction.Forward));

                for (int i = 0; i < sides * 2; i += 2) {
                    int i0 = i,
                        i1 = i + 1,
                        i2 = (i + 2) % (sides*2),
                        i3 = (i + 3) % (sides*2);
                    tris.AddRange(new int[] {
                        bi + i0, bi + i3, bi + i2,
                        bi + i0, bi + i1, bi + i3 });

                    // cap on final branch
                    if (b.Equals(branches[branches.Count - 1]))
                        tris.AddRange(new int[] {
                        verts.Count - 1, bi + i3, bi + i1});
                }
            }
            #endregion

            #region leaf generation
            foreach (Leaf l in leaves) {
                if (l.Position.Y < max * .75f) {
                    float scale = MathHelper.Clamp(max / height, 0, 1);

                    // do twice (top & bottom)
                    for (int x = 0; x < 2; x++) {
                        int bi = verts.Count;
                        Matrix m = l.Direction;
                        Vector3 p = l.Position;
                        for (int i = 0; i < 3; i++) {
                            float d = r;
                            if (i == 0) d *= .6f;
                            else if (i == 1) d *= 1.1f;

                            if (x == 0) {
                                verts.Add(new VertexPositionColorNormal(p + m.Left * d * scale, Color.Green, m.Up));
                                verts.Add(new VertexPositionColorNormal(p + m.Right * d * scale, Color.Green, m.Up));
                            } else {
                                verts.Add(new VertexPositionColorNormal(p + m.Right * d * scale, Color.Green, m.Down));
                                verts.Add(new VertexPositionColorNormal(p + m.Left * d * scale, Color.Green, m.Down));
                            }

                            m *= Matrix.CreateRotationX(l.Drop * .3333f * scale);
                            p += m.Forward * 2 * r * scale;
                        }
                        if (x == 0)
                            verts.Add(new VertexPositionColorNormal(p + m.Forward * r * scale, Color.Green, m.Up));
                        else
                            verts.Add(new VertexPositionColorNormal(p + m.Forward * r * scale, Color.Green, m.Down));

                        tris.AddRange(new int[] {
                            bi,     bi + 2, bi + 1, bi + 1, bi + 2, bi + 3,
                            bi + 2, bi + 4, bi + 3, bi + 3, bi + 4, bi + 5,
                            bi + 4, bi + 6, bi + 5
                        });
                    }
                }
            }
            #endregion

            // set data
            //if (VBuffer == null)
            VBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColorNormal), verts.Count, BufferUsage.WriteOnly);
            VBuffer.SetData(verts.ToArray());

            //if (IBuffer == null)
                IBuffer = new DynamicIndexBuffer(device, typeof(int), tris.Count, BufferUsage.WriteOnly);
            IBuffer.SetData(tris.ToArray());
        }
    }
}
