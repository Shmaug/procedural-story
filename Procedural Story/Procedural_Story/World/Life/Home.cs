using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Procedural_Story.Util;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.World.Life {
    struct Room {
        public struct Door {
            public byte Wall; // 0:z- 1:z+ 2:x- 3:x+
            public float Location; // 0-1

            public Door(byte w, float o) {
                Wall = w;
                Location = o;
            }
        }
        public Vector3 Position;
        public float Width, Height, Length;
        public Door[] doors;

        public Room(Vector3 p, float w, float h, float l, params Door[] doors) {
            Position = p;
            Width = w;
            Height = h;
            Length = l;
            this.doors = doors;
        }
    }
    class Home : VertexBufferObject {
        Random rand;
        Room[] Rooms;

        public List<ComplexCharacter> Residents;

        List<VertexPositionColorNormal> verts;
        List<int> inds;

        public Home(Vector3 pos, Area area, int seed) : base(pos, area) {
            rand = new Random(seed);
            Residents = new List<ComplexCharacter>();
        }

        public void GenerateFloorPlan() {
            int nr = 1;// rand.Next(1, 4);
            Rooms = new Room[nr];
            float w = (float)(rand.NextDouble() * 5) + 5, l = (float)(rand.NextDouble() * 5) + 5;
            Rooms[0] = new Room(Position, w, rand.Next(3, 6), l, new Room.Door((byte)rand.Next(0, 4), (float)rand.NextDouble() * .6f + .2f));

            for (int i = 1; i < nr; i++) {
                //Rooms[i] = new Room();
            }
        }

        public void BuildGeometry(GraphicsDevice device) {
            float dh = 2.25f; // door height
            float dw = 1.65f * .5f; // door width * .5
            float ww = .125f;        // wall width  
            float ww2 = ww * .5f;

            verts = new List<VertexPositionColorNormal>();
            inds = new List<int>();

            for (int i = 0; i < Rooms.Length; i++) {
                Room r = Rooms[i];
                Vector3 p = r.Position - Position;

                float w = r.Width * .5f, l = r.Length * .5f;
                Vector3 c0 = p + new Vector3(-w + ww2, 0, -l + ww2),
                        c1 = p + new Vector3(w - ww2, 0, -l + ww2),
                        c2 = p + new Vector3(-w + ww2, 0, l - ww2),
                        c3 = p + new Vector3(w - ww2, 0, l - ww2);

                byte wallsWithDoors = 0;
                for (int d = 0; d < r.doors.Length; d++) {
                    wallsWithDoors |= (byte)Math.Pow(2, r.doors[d].Wall);

                    Vector3 p1 = c0, p2 = c1;
                    Vector3 o = Vector3.Zero;
                    switch (r.doors[d].Wall) {
                        case 0:
                            p1 = c0; p2 = c1;
                            o = new Vector3(dw, 0, 0);
                            break;
                        case 1:
                            p1 = c2; p2 = c3;
                            o = new Vector3(dw, 0, 0);
                            break;
                        case 2:
                            p1 = c0; p2 = c2;
                            o = new Vector3(0, 0, dw);
                            break;
                        case 3:
                            p1 = c1; p2 = c3;
                            o = new Vector3(0, 0, dw);
                            break;
                    }

                    // build walls with a hole for the door
                    Vector3 dc = Vector3.Lerp(p1, p2, r.doors[d].Location);
                    makeWall(p1, dc - o, ww, r.Height, new Color(.57f, .45f, .26f));
                    makeWall(dc + o, p2, ww, r.Height, new Color(.57f, .45f, .26f));
                    makeWall(dc - o + new Vector3(0, dh, 0), dc + o + new Vector3(0, dh, 0), ww, r.Height - dh, new Color(.57f, .45f, .26f));

                    // door pillars
                    Vector3 dp1 = dc - o;
                    Vector3 dp2 = dc + o;
                    addBox(new BoundingBox(dp1 + new Vector3(-ww, 0, -ww), dp1 + new Vector3(ww, dh, ww)), new Color(.57f, .45f, .26f) * .8f);
                    addBox(new BoundingBox(dp2 + new Vector3(-ww, 0, -ww), dp2 + new Vector3(ww, dh, ww)), new Color(.57f, .45f, .26f) * .8f);
                    addBox(new BoundingBox(dp1 + new Vector3(-ww, dh - ww, -ww), dp2 + new Vector3(ww, dh + ww, ww)), new Color(.57f, .45f, .26f) * .8f);
                }

                // floor
                addBox(
                    new BoundingBox(c0, c3 + new Vector3(0, .1f, 0)),
                    new Color(.57f, .45f, .26f));

                // front wall
                if ((wallsWithDoors & 1) == 0)
                    makeWall(c0, c1, ww, r.Height, new Color(.57f, .45f, .26f));
                // back wall
                if ((wallsWithDoors & 2) == 0)
                    makeWall(c2, c3, ww, r.Height, new Color(.57f, .45f, .26f));
                // left wall
                if ((wallsWithDoors & 4) == 0)
                    makeWall(c0, c2, ww, r.Height, new Color(.57f, .45f, .26f));
                // right wall
                if ((wallsWithDoors & 8) == 0)
                    makeWall(c1, c3, ww, r.Height, new Color(.57f, .45f, .26f));
                
                // wall pillars
                float pw = ww * 2;
                addBox(new BoundingBox(c0 + new Vector3(-pw, 0, -pw), c0 + new Vector3(pw, r.Height + pw, pw)), new Color(.57f, .45f, .26f) * .8f);
                addBox(new BoundingBox(c1 + new Vector3(-pw, 0, -pw), c1 + new Vector3(pw, r.Height + pw, pw)), new Color(.57f, .45f, .26f) * .8f);
                addBox(new BoundingBox(c2 + new Vector3(-pw, 0, -pw), c2 + new Vector3(pw, r.Height + pw, pw)), new Color(.57f, .45f, .26f) * .8f);
                addBox(new BoundingBox(c3 + new Vector3(-pw, 0, -pw), c3 + new Vector3(pw, r.Height + pw, pw)), new Color(.57f, .45f, .26f) * .8f);
            }

            VBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), verts.Count, BufferUsage.WriteOnly);
            VBuffer.SetData(verts.ToArray());
            IBuffer = new IndexBuffer(device, typeof(int), inds.Count, BufferUsage.WriteOnly);
            IBuffer.SetData(inds.ToArray());
        }

        void makeWall(Vector3 p1, Vector3 p2, float width, float height, Color col) {
            float w2 = width * .5f;
            addBox(new BoundingBox(p1 - new Vector3(w2, 0, w2), p2 + new Vector3(w2, height, w2)),
                new Color(.57f, .45f, .26f));
        }

        void addFace(Vector3 min, Vector3 max, Vector3 norm, Color col) {
            // TODO: implement this and optimize building generation
        }

        void addBox(BoundingBox b, Color col) {
            int bi = verts.Count;
            Vector3[] bverts;
            Vector3[] bnorms;
            int[] binds;

            Util.Util.GetBoxGeometry(b, out bverts, out bnorms, out binds, bi);
            for (int v = 0; v < bverts.Length; v++)
                verts.Add(new VertexPositionColorNormal(bverts[v], col, bnorms[v]));
            inds.AddRange(binds);
        }
    }
}
