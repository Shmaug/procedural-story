using System;
using System.Collections.Generic;

using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Procedural_Story.Core.Life;

namespace Procedural_Story.Core.Structures {
    class Home : Structure {
        public struct Room {
            public struct Door {
                public const float DoorWidth = 1.65f;
                public const float DoorHeight = 2.5f;

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

        Random rand;
        public Room[] Rooms;

        public List<ComplexCharacter> Residents;

        public Home(Vector3 pos, Area area, int seed) : base(area, pos) {
            rand = new Random(seed);
            Residents = new List<ComplexCharacter>();
            RigidBodies = new List<RigidBody>();

            float w = (float)(rand.NextDouble() * 5) + 5, l = (float)(rand.NextDouble() * 5) + 5;
            Rooms = new Room[] {
                new Room(Vector3.Zero, w, rand.Next(3, 6), l, new Room.Door(0, (float)rand.NextDouble() * .6f + .2f))
            };
        }

        #region generation
        public override void PreGenerate() {
            for (int i = 0; i < Rooms.Length; i++) {
                Vector3 size = new Vector3(Rooms[i].Width, Rooms[i].Height, Rooms[i].Length);
                Vector3 rmin = Position + Rooms[i].Position - size / 2;
                Vector3 rmax = Position + Rooms[i].Position + size / 2;
                if (i == 0)
                    bbox = new BoundingBox(rmin, rmax);
                else
                    bbox = BoundingBox.CreateMerged(bbox, new BoundingBox(rmin, rmax));
            }
            innerbbox = new BoundingBox(bbox.Min + new Vector3(.35f, 0, .35f), bbox.Max - new Vector3(.35f, .1f, .35f));
        }

        public override void BuildGeometry(GraphicsDevice device) {
            float dh = Room.Door.DoorHeight; // door height
            float dw2 = Room.Door.DoorWidth * .5f; // door width * .5
            float ww = .2f; // wall width
            float rw = .1f; // roof thickness
            float roofSlope = .3f; // slope of line that makes roof
            float rsx = 0; // offset of line that makes roof
            float roofAng = (float)Math.Atan(roofSlope);

            #region geometry generation
            Color pillarColor = new Color(.47f, .36f, .21f);
            Color wallColor = new Color(.57f, .45f, .26f);
            Color roofColor = new Color(.47f, .36f, .21f);

            for (int q = 0; q < Rooms.Length; q++) {
                Room r = Rooms[q];
                Vector3 p = r.Position;
                JVector jp = new JVector(p.X, p.Y, p.Z);

                float w = r.Width * .5f, l = r.Length * .5f;
                Vector3 c0 = p + new Vector3(-w, 0, -l),
                        c1 = p + new Vector3(w, 0, -l),
                        c2 = p + new Vector3(-w, 0, l),
                        c3 = p + new Vector3(w, 0, l);
                rsx = -w;

                // floor
                addFace(wallColor, c0 + new Vector3(0, .1f, 0), c1 + new Vector3(0, .1f, 0), c3 + new Vector3(0, .1f, 0), c2 + new Vector3(0, .1f, 0));
                RigidBody floorBody = new RigidBody(new BoxShape(r.Width, 2, r.Length));
                floorBody.Position = new JVector(p.X, p.Y - .9f, p.Z);
                RigidBodies.Add(floorBody);

                // make walls without doors
                byte doors = 0;
                Vector3 low = new Vector3(0, -2, 0);
                Vector3 high = new Vector3(0, r.Height, 0);
                #region doors
                for (int d = 0; d < r.doors.Length; d++) {
                    Vector3 p1 = c0, p2 = c1;
                    Vector3 dp = Vector3.Zero, ds = Vector3.Zero;
                    Vector3 dsy = new Vector3(0, dh, 0);
                    Vector3 wo = Vector3.Zero;
                    JVector o;
                    RigidBody b1 = null, b2 = null, b3 = null;
                    float f, f2;

                    switch (r.doors[d].Wall) {
                        case 0: // z-
                            doors |= 1;
                            p1 = c0;
                            p2 = c1;
                            dp = Vector3.Lerp(p1, p2, r.doors[d].Location);
                            ds = new Vector3(dw2, 0, 0);
                            wo = new Vector3(0, 0, ww);

                            o = new JVector(dp.X, dp.Y, dp.Z) + new JVector(0, r.Height * .5f, ww * .5f);
                            f = r.Width * r.doors[d].Location - dw2;
                            f2 = r.Width * (1 - r.doors[d].Location) - dw2;
                            b1 = new RigidBody(new BoxShape(f, r.Height, ww)); b1.Position = o + new JVector(-f / 2 - dw2, 0, 0);
                            b2 = new RigidBody(new BoxShape(dw2 * 2, r.Height - dh, ww)); b2.Position = o + new JVector(0, dh * .5f, 0);
                            b3 = new RigidBody(new BoxShape(f2, r.Height, ww)); b3.Position = o + new JVector(f2 / 2 + dw2, 0, 0);

                            entrancebbox = new BoundingBox(dp - ds + new Vector3(0, -1, -2), dp + ds + new Vector3(0, dh, 2));
                            break;
                        case 1: // z+
                            doors |= 2;
                            p1 = c2;
                            p2 = c3;
                            dp = Vector3.Lerp(p1, p2, r.doors[d].Location);
                            ds = new Vector3(dw2, 0, 0);
                            wo = new Vector3(0, 0, -ww);

                            o = new JVector(dp.X, dp.Y, dp.Z) + new JVector(0, r.Height * .5f, -ww * .5f);
                            f = r.Width * r.doors[d].Location - dw2;
                            f2 = r.Width * (1 - r.doors[d].Location) - dw2;
                            b1 = new RigidBody(new BoxShape(f, r.Height, ww)); b1.Position = o + new JVector(-f / 2 - dw2, 0, 0);
                            b2 = new RigidBody(new BoxShape(dw2 * 2, r.Height - dh, ww)); b2.Position = o + new JVector(0, dh * .5f, 0);
                            b3 = new RigidBody(new BoxShape(f2, r.Height, ww)); b3.Position = o + new JVector(f2 / 2 + dw2, 0, 0);

                            entrancebbox = new BoundingBox(dp - ds + new Vector3(0, -1, -2), dp + ds + new Vector3(0, dh, 2));
                            break;
                        case 2: // x-
                            doors |= 4;
                            p1 = c0;
                            p2 = c2;
                            dp = Vector3.Lerp(p1, p2, r.doors[d].Location);
                            ds = new Vector3(0, 0, dw2);
                            wo = new Vector3(ww, 0, 0);

                            o = new JVector(dp.X, dp.Y, dp.Z) + new JVector(ww * .5f, r.Height * .5f, 0);
                            f = r.Length * r.doors[d].Location - dw2;
                            f2 = r.Length * (1 - r.doors[d].Location) - dw2;
                            b1 = new RigidBody(new BoxShape(ww, r.Height, f)); b1.Position = o + new JVector(0, 0, -f / 2 - dw2);
                            b2 = new RigidBody(new BoxShape(ww, r.Height - dh, dw2 * 22)); b2.Position = o + new JVector(0, dh * .5f, 0);
                            b3 = new RigidBody(new BoxShape(ww, r.Height, f2)); b3.Position = o + new JVector(0, 0, f2 / 2 + dw2);

                            entrancebbox = new BoundingBox(dp - ds + new Vector3(-2, -1, 0), dp + ds + new Vector3(2, dh, 0));
                            break;
                        case 3: // x+
                            doors |= 8;
                            p1 = c1;
                            p2 = c3;
                            dp = Vector3.Lerp(p1, p2, r.doors[d].Location);
                            ds = new Vector3(0, 0, dw2);
                            wo = new Vector3(-ww, 0, 0);

                            o = new JVector(dp.X, dp.Y, dp.Z) + new JVector(-ww * .5f, r.Height * .5f, 0);
                            f = r.Length * r.doors[d].Location - dw2;
                            f2 = r.Length * (1 - r.doors[d].Location) - dw2;
                            b1 = new RigidBody(new BoxShape(ww, r.Height, f)); b1.Position = o + new JVector(0, 0, -f / 2 - dw2);
                            b2 = new RigidBody(new BoxShape(ww, r.Height - dh, dw2 * 22)); b2.Position = o + new JVector(0, dh * .5f, 0);
                            b3 = new RigidBody(new BoxShape(ww, r.Height, f2)); b3.Position = o + new JVector(0, 0, f2 / 2 + dw2);

                            entrancebbox = new BoundingBox(dp - ds + new Vector3(-2, -1, 0), dp + ds + new Vector3(2, dh, 0));
                            break;
                    }
                    entrancebbox.Min += Position + r.Position;
                    entrancebbox.Max += Position + r.Position;
                    RigidBodies.Add(b1);
                    RigidBodies.Add(b2);
                    RigidBodies.Add(b3);
                    #region wall polygons
                    // outside wall face
                    addFace(wallColor,
                        new int[] {
                            0, 1, 4,
                            0, 4, 5,
                            0, 5, 6,
                            0, 6, 7,
                            1, 2, 3,
                            1, 3, 4,
                            8, 3, 6,
                            9, 3, 8
                        },
                        p2 + high + new Vector3(0, (p2.X - rsx) * roofSlope, 0),
                        p1 + high + new Vector3(0, (p1.X - rsx) * roofSlope, 0),
                        p1 + low,
                        dp - ds + low,
                        dp - ds + dsy,
                        dp + ds + dsy,
                        dp + ds + low,
                        p2 + low,
                        dp + ds + new Vector3(0, .1f, 0),
                        dp - ds + new Vector3(0, .1f, 0)
                        );
                    // inside wall face
                    addFace(wallColor,
                        new int[] {
                            0, 4, 1,
                            0, 5, 4,
                            0, 6, 5,
                            0, 7, 6,
                            1, 3, 2,
                            1, 4, 3
                        },
                        p2 + high + wo + new Vector3(0, (p2.X - rsx) * roofSlope, 0),
                        p1 + high + wo + new Vector3(0, (p1.X - rsx) * roofSlope, 0),
                        p1 + low + wo,
                        dp - ds + low + wo,
                        dp - ds + dsy + wo,
                        dp + ds + dsy + wo,
                        dp + ds + low + wo,
                        p2 + low + wo
                        );
                    // door insides
                    addFace(wallColor,
                        dp + ds,
                        dp + ds + dsy,
                        dp + ds + wo + dsy,
                        dp + ds + wo);
                    addFace(wallColor,
                        dp - ds + wo,
                        dp - ds + wo + dsy,
                        dp - ds + dsy,
                        dp - ds);
                    addFace(wallColor,
                        dp - ds + dsy + wo,
                        dp + ds + dsy + wo,
                        dp + ds + dsy,
                        dp - ds + dsy);
                    #endregion
                }
                #endregion
                #region walls
                if ((doors & 1) == 0) { // no door on z- side, make a wall
                    addFace(wallColor,
                        c0 + high + new Vector3(0, (c0.X - rsx) * roofSlope, 0),
                        c1 + high + new Vector3(0, (c1.X - rsx) * roofSlope, 0),
                        c1 + low,
                        c0 + low);
                    addFace(wallColor,
                        c1 + high + new Vector3(ww, 0, ww) + new Vector3(0, (c1.X + ww - rsx) * roofSlope, 0),
                        c0 + high + new Vector3(-ww, 0, ww) + new Vector3(0, (c0.X - ww - rsx) * roofSlope, 0),
                        c0 + new Vector3(-ww, 0, ww),
                        c1 + new Vector3(ww, 0, ww));
                    RigidBody wallBody = new RigidBody(new BoxShape(r.Width, r.Height, ww));
                    wallBody.Position = jp + new JVector(0, r.Height * .5f, -(r.Length - ww) * .5f);
                    RigidBodies.Add(wallBody);
                }
                if ((doors & 2) == 0) { // no door on z+ side, make a wall
                    addFace(wallColor,
                        c2 + high + new Vector3(0, (c2.X - rsx) * roofSlope, 0),
                        c3 + high + new Vector3(0, (c3.X - rsx) * roofSlope, 0),
                        c3 + low,
                        c2 + low);
                    addFace(wallColor,
                        c3 + high + new Vector3(-ww, 0, -ww) + new Vector3(0, (c3.X - ww - rsx) * roofSlope, 0),
                        c2 + high + new Vector3(ww, 0, -ww) + new Vector3(0, (c2.X + ww - rsx) * roofSlope, 0),
                        c2 + new Vector3(ww, 0, -ww),
                        c3 + new Vector3(-ww, 0, -ww));
                    RigidBody wallBody = new RigidBody(new BoxShape(r.Width, r.Height, ww));
                    wallBody.Position = jp + new JVector(0, r.Height * .5f, (r.Length - ww) * .5f);
                    RigidBodies.Add(wallBody);
                }
                if ((doors & 4) == 0) { // no door on x- side, make a wall
                    addFace(wallColor,
                        c0 + high + new Vector3(0, (c0.X - rsx) * roofSlope, 0),
                        c2 + high + new Vector3(0, (c2.X - rsx) * roofSlope, 0),
                        c2 + low,
                        c0 + low);
                    addFace(wallColor,
                        c2 + high + new Vector3(ww, 0, -ww) + new Vector3(0, (c2.X + ww - rsx) * roofSlope, 0),
                        c0 + high + new Vector3(ww, 0, ww) + new Vector3(0, (c0.X + ww - rsx) * roofSlope, 0),
                        c0 + new Vector3(ww, 0, ww),
                        c2 + new Vector3(ww, 0, -ww));
                    RigidBody wallBody = new RigidBody(new BoxShape(ww, r.Height, r.Length));
                    wallBody.Position = jp + new JVector(-(r.Width - ww) * .5f, r.Height * .5f, 0);
                    RigidBodies.Add(wallBody);
                }
                if ((doors & 8) == 0) { // no door on x+ side, make a wall
                    addFace(wallColor,
                        c3 + high + new Vector3(0, (c3.X - rsx) * roofSlope, 0),
                        c1 + high + new Vector3(0, (c1.X - rsx) * roofSlope, 0),
                        c1 + low,
                        c3 + low);
                    addFace(wallColor,
                        c1 + high + new Vector3(-ww, 0, ww) + new Vector3(0, (c1.X - ww - rsx) * roofSlope, 0),
                        c3 + high + new Vector3(-ww, 0, -ww) + new Vector3(0, (c3.X - ww - rsx) * roofSlope, 0),
                        c3 + new Vector3(-ww, 0, -ww),
                        c1 + new Vector3(-ww, 0, ww));
                    RigidBody wallBody = new RigidBody(new BoxShape(ww, r.Height, r.Length));
                    wallBody.Position = jp + new JVector((r.Width - ww) * .5f, r.Height * .5f, 0);
                    RigidBodies.Add(wallBody);
                }
                #endregion
                #region roof
                {
                    Vector3 c0r = c0 + high + new Vector3(-.5f, (c0.X - .5f - rsx) * roofSlope + rw, -.5f),
                            c1r = c1 + high + new Vector3(.5f, (c1.X + .5f - rsx) * roofSlope + rw, -.5f),
                            c2r = c2 + high + new Vector3(-.5f, (c2.X - .5f - rsx) * roofSlope + rw, .5f),
                            c3r = c3 + high + new Vector3(.5f, (c3.X + .5f - rsx) * roofSlope + rw, .5f);

                    Vector3 c0rd = c0 + high + new Vector3(-.5f, (c0.X - .5f - rsx) * roofSlope, -.5f),
                            c1rd = c1 + high + new Vector3(.5f, (c1.X + .5f - rsx) * roofSlope, -.5f),
                            c2rd = c2 + high + new Vector3(-.5f, (c2.X - .5f - rsx) * roofSlope, .5f),
                            c3rd = c3 + high + new Vector3(.5f, (c3.X + .5f - rsx) * roofSlope, .5f);
                    // top face
                    addFace(roofColor,
                        c0r,
                        c1r,
                        c3r,
                        c2r);
                    // bottom (inside) face
                    addFace(roofColor,
                        c2rd,
                        c3rd,
                        c1rd,
                        c0rd);
                    // front edge face
                    addFace(roofColor,
                        c0rd,
                        c1rd,
                        c1r,
                        c0r);
                    // back edge face
                    addFace(roofColor,
                        c3rd,
                        c2rd,
                        c2r,
                        c3r);
                    // right edge face
                    addFace(roofColor,
                        c1rd,
                        c3rd,
                        c3r,
                        c1r);
                    // left edge face
                    addFace(roofColor,
                        c2rd,
                        c0rd,
                        c0r,
                        c2r);
                }
                float a = roofSlope * (r.Width + 1 - rsx);
                RigidBody roofBody = new RigidBody(new BoxShape(
                    (float)Math.Sqrt((r.Width + 1) * (r.Width + 1) + a * a) // pythagorean theorem
                    , rw, r.Length + 1));
                roofBody.Orientation = JMatrix.CreateRotationZ(roofAng);
                roofBody.Position = new JVector(0, r.Height + (roofSlope * r.Width + rw) * .5f, 0);
                RigidBodies.Add(roofBody);
                #endregion
            }
            #endregion

            base.BuildGeometry(device);
        }

        public void createResidents() {
            ComplexCharacter c1 = new ComplexCharacter(area);
            c1.home = this;
            c1.Position = Position + new Vector3(0, area.HeightAt(Position) + c1.Height * .6f, 0);
            Residents.Add(c1);
            area.AddCharacter(c1);
        }

        #endregion
    }
}
