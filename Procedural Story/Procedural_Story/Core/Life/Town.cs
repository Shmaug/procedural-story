using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

using Procedural_Story.Core.Structures;

namespace Procedural_Story.Core.Life {
    class Town {
        public struct Road {
            public Vector3 A;
            public Vector3 B;
            public List<Road> connected;
            public Road(Vector3 a, Vector3 b) {
                A = a; B = b;
                connected = new List<Road>();
            }
        }

        public List<Structure> Structures;
        public List<Road> Roads;
        public Area Area;
        public Vector3 Center;
        public int seed;
        Random rand;

        VertexBufferObject roadObject;

        public BoundingBox bbox;

        public Town(Area area, Vector3 center, int seed) {
            Structures = new List<Structure>();
            Roads = new List<Road>();
            Area = area;
            Center = center;
            rand = new Random(seed);
            this.seed = seed;
        }

        #region obstruction & containment tests
        public ObstructionType Obstructs(BoundingSphere sphere) {
            if (!bbox.Intersects(sphere))
                return ObstructionType.None;

            foreach (Structure h in Structures) {
                ObstructionType t = h.Obstructs(sphere);
                if (t != ObstructionType.None)
                    return t;
            }

            foreach (Road r in Roads) {
                Vector3 d = r.B - r.A;
                d.Normalize();
                Ray ray = new Ray(r.A, d);
                float? f = ray.Intersects(sphere);
                if (f.HasValue && f.Value < Vector3.Distance(r.A, r.B))
                    return ObstructionType.Virtual;
            }

            return ObstructionType.None;
        }
        public ObstructionType Obstructs(BoundingBox box) {
            if (!bbox.Intersects(box))
                return ObstructionType.None;

            foreach (Structure h in Structures) {
                ObstructionType t = h.Obstructs(box);
                if (t != ObstructionType.None)
                    return t;
            }

            foreach (Road r in Roads) {
                Vector3 d = r.B - r.A;
                d.Normalize();
                Ray ray = new Ray(r.A, d);
                float? f = ray.Intersects(box);
                if (f.HasValue && f.Value < Vector3.Distance(r.A, r.B))
                    return ObstructionType.Virtual;
            }

            return ObstructionType.None;
        }
        public ObstructionType Obstructs(BoundingBox box, Matrix transform) {
            foreach (Structure h in Structures) {
                ObstructionType t = h.Obstructs(box, transform);
                if (t != ObstructionType.None)
                    return t;
            }

            foreach (Road r in Roads) {
                Vector3 a = Vector3.Transform(r.A, Matrix.Invert(transform));
                Vector3 b = Vector3.Transform(r.B, Matrix.Invert(transform));
                Vector3 d = b - a;
                d.Normalize();
                Ray ray = new Ray(r.A, d);
                float? f = ray.Intersects(box);
                if (f.HasValue && f.Value < Vector3.Distance(r.A, r.B))
                    return ObstructionType.Virtual;
            }

            return ObstructionType.None;
        }
        public ObstructionType Obstructs(Vector3 point) {
            if (bbox.Contains(point) != ContainmentType.Contains)
                return ObstructionType.None;

            foreach (Structure s in Structures) {
                ObstructionType t = s.Obstructs(point);
                if (t != ObstructionType.None)
                    return t;
            }

            foreach (Road r in Roads) {
                Vector3 mid = (r.A + r.B) * .5f;
                float d = Vector3.Distance(r.B, r.A);
                BoundingBox bbox = new BoundingBox(new Vector3(-2, -1, -d / 2), new Vector3(2, 1, d / 2));

                Vector3 dir = r.B - r.A;
                dir.Normalize();
                Vector3 fxz = r.B - r.A;
                fxz.Y = 0;
                fxz.Normalize();
                Vector3 right = Vector3.Cross(Vector3.Up, fxz);
                Vector3 up = Vector3.Cross(right, dir);
                Matrix rot = Matrix.CreateWorld(Vector3.Zero, dir, up);
                
                Vector3 p2 = Vector3.Transform(point, Matrix.CreateTranslation(-mid) * Matrix.Invert(rot) * Matrix.CreateTranslation(mid));
                if (bbox.Contains(p2) != ContainmentType.Disjoint)
                    return ObstructionType.Virtual;
            }

            return ObstructionType.None;
        }
        #endregion

        public void MakeRoads() {
            if (Roads.Count == 0) {
                // make 2 roads pointing opposite-ish directions
                double a = rand.NextDouble() * Math.PI * 2;
                Road r1 = new Road(Center, Center + new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)) * rand.Next(20, 40));
                a += Math.PI * .5 + (rand.Next(-10, 10) / 30f);
                Road r2 = new Road(Center, Center + new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)) * rand.Next(20, 40));

                r1.A.Y = Area.HeightAt(r1.A); r1.B.Y = Area.HeightAt(r1.B);
                r2.A.Y = Area.HeightAt(r2.A); r2.B.Y = Area.HeightAt(r2.B);
                Roads.Add(r1);
                Roads.Add(r2);
                Roads[0].connected.Add(Roads[1]);
                Roads[1].connected.Add(Roads[0]); // add references for the graph
            }

            List<Road> cur = new List<Road>();
            List<Road> last = new List<Road>();
            last.AddRange(Roads);
            // iteratively branches off the roads from last
            // adding new roads to cur
            // then empties last, adds cur to last, adds cur to the Roads, and clears cur for the next iteration
            for (int asdf = 1; asdf < 3; asdf++) {
                for (int i = 0; i < last.Count; i++) {
                    double a = Math.Atan2(last[i].B.Z - last[i].A.Z, last[i].B.X - last[i].A.X) - Math.PI * .5;
                    for (int j = 0; j < rand.Next(1, 3); j++) {
                        Road r = new Road(last[i].B, last[i].B + new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)) * rand.Next(20, 30) * (MathHelper.Clamp(2f / asdf, 0, 1)));
                        r.A.Y = Area.HeightAt(r.A); r.B.Y = Area.HeightAt(r.B);
                        if (Area.CellAt(r.B) == null) {
                            last[i].connected.Add(r);
                            r.connected.Add(last[i]);
                            cur.Add(r);
                        }
                        a += Math.PI * .5 + (rand.Next(-10, 10) / 30f);
                    }
                }
                Roads.AddRange(cur);
                last.Clear();
                last.AddRange(cur);
                cur.Clear();
            }

            MakeBbox();
        }
        
        public void MakeBuildings() {
            int homes = 0;
            bool farm = true;
            foreach (Road r in Roads) {
                float length = Vector3.Distance(r.A, r.B);
                Vector3 fwd = r.B - r.A;
                fwd.Y = 0;
                fwd.Normalize();
                Matrix roadDir = Matrix.CreateWorld(Vector3.Zero, fwd, Vector3.Up);

                for (float t = 7; t < length - 7; t += 3) {
                    Structure s = null;

                    double rnd = rand.NextDouble();
                    if (homes % 3 == 0 && farm)
                        s = new Farm(Vector3.Zero, Area, rand.Next());
                    else
                        s = new Home(Vector3.Zero, Area, rand.Next());

                    int d = rand.NextDouble() > .5 ? -1 : 1;
                    s.PreGenerate();
                    s.Orientation = roadDir * Matrix.CreateRotationY(MathHelper.PiOver2 * d);
                    s.Position = Vector3.Lerp(r.A, r.B, t / length) + s.Orientation.Backward * ((s.bbox.Max.Z - s.bbox.Min.Z) * .5f + 1);
                    s.PreGenerate();

                    // if the home obstructs another home, try moving it to the other side of the road
                    if (Obstructs(s.bboxfull) == ObstructionType.Physical) {
                        s.Orientation = roadDir * Matrix.CreateRotationY(MathHelper.PiOver2 * -d);
                        s.Position = Vector3.Lerp(r.A, r.B, t / length) + s.Orientation.Backward * ((s.bbox.Max.Z - s.bbox.Min.Z) * .5f + 1);
                        s.PreGenerate();
                    }
                    
                    // only add the home if it doesnt obstruct another home or something
                    if (Obstructs(s.bboxfull) != ObstructionType.Physical) {
                        if (Vector3.DistanceSquared(Area.ClosestCell(new Vector3(s.Position.X, 0, s.Position.Z)).Center, s.Position) > 50 * 50) {
                            Structures.Add(s);

                            if (s is Home) {
                                homes++;
                                farm = true;
                            } else if (s is Farm)
                                farm = false;

                            t += s.bbox.Max.X - s.bbox.Min.X;
                        }
                    }
                }
            }
            MakeBbox();
        }

        public void MakeRoadGeometry(GraphicsDevice device) {
            roadObject = new VertexBufferObject(Center, Area);
            List<VertexPositionColorNormal> roadverts = new List<VertexPositionColorNormal>();
            List<int> roadinds = new List<int>();
            Color roadColor = new Color(87, 59, 12);

            float roadwidth = 1.5f;
            foreach (Road r in Roads) {
                float d = Vector3.Distance(r.A, r.B);
                Vector3 dir = r.B - r.A;
                dir.Normalize();
                Vector3 fxz = r.B - r.A;
                fxz.Y = 0;
                fxz.Normalize();
                Vector3 right = Vector3.Cross(Vector3.Up, fxz) * roadwidth * .5f;

                int ioff = roadverts.Count;
                int vc = 0;
                for (float t = -1; t < d + 1; t += .5f) {
                    Vector3 p = r.A - Center + dir * t;
                    p.Y = Area.HeightAt(p + Center) - Center.Y;
                    Vector3 p1 = p - right;
                    Vector3 p2 = p + right;
                    p1.Y = Area.HeightAt(p1 + Center) - Center.Y + .05f;
                    p2.Y = Area.HeightAt(p2 + Center) - Center.Y + .05f;
                    roadverts.Add(new VertexPositionColorNormal(p1, roadColor, Vector3.Zero));
                    roadverts.Add(new VertexPositionColorNormal(p2, roadColor, Vector3.Zero));
                    vc += 2;
                }
                int ic = roadinds.Count;
                for (int i = 0; i < vc - 2; i += 2) {
                    // top square
                    roadinds.Add(ioff + i);
                    roadinds.Add(ioff + i + 1);
                    roadinds.Add(ioff + i + 2);
                    roadinds.Add(ioff + i + 1);
                    roadinds.Add(ioff + i + 3);
                    roadinds.Add(ioff + i + 2);
                }

                // normals
                for (int i = ic; i < roadinds.Count; i += 3) {
                    VertexPositionColorNormal v1 = roadverts[roadinds[i]], v2 = roadverts[roadinds[i + 1]], v3 = roadverts[roadinds[i + 2]];
                    Vector3 d1 = v1.Position - v2.Position;
                    Vector3 d2 = v3.Position - v1.Position;
                    d1.Normalize(); d2.Normalize();
                    Vector3 normal = Vector3.Cross(d1, d2);
                    v1.Normal += normal; v2.Normal += normal; v3.Normal += normal;
                    roadverts[roadinds[i]] = v1;
                    roadverts[roadinds[i + 1]] = v2;
                    roadverts[roadinds[i + 2]] = v3;
                }
                for (int i = ioff; i < roadverts.Count; i++)
                    roadverts[i].Normal.Normalize();
            }

            // weld some verticies together
            // to form nice edges
            for (int i = 0; i < roadverts.Count; i++) {
                for (int j = 0; j < roadverts.Count; j++) {
                    if (i != j) {
                        float d = Vector3.DistanceSquared(roadverts[i].Position, roadverts[j].Position);
                        if (d < .2f) {
                            Vector3 mid = (roadverts[i].Position + roadverts[j].Position) * .5f;
                            roadverts[i] = new VertexPositionColorNormal(mid, roadverts[i].Color, roadverts[i].Normal);
                            roadverts[j] = new VertexPositionColorNormal(mid, roadverts[j].Color, roadverts[j].Normal);
                        }
                    }
                }
            }

            roadObject.VBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), roadverts.Count, BufferUsage.WriteOnly);
            roadObject.VBuffer.SetData(roadverts.ToArray());
            roadObject.IBuffer = new IndexBuffer(device, typeof(int), roadinds.Count, BufferUsage.WriteOnly);
            roadObject.IBuffer.SetData(roadinds.ToArray());
        }
        public void makeBuildingGeometry(GraphicsDevice device) {
            foreach (Structure h in Structures)
                h.BuildGeometry(device);
        }

        public void createResidents() {
            foreach (Structure h in Structures)
                if (h is Home)
                    (h as Home).createResidents();
        }

        public void MakeBbox() {
            bool set = false;
            foreach (Road r in Roads) {
                BoundingBox b = BoundingBox.CreateFromPoints(new Vector3[] { r.A, r.B });
                if (!set) {
                    bbox = b;
                    set = true;
                } else
                    bbox = BoundingBox.CreateMerged(bbox, b);
            }
            foreach (Structure h in Structures) {
                if (!set) {
                    bbox = h.bboxfull;
                    set = true;
                } else
                    bbox = BoundingBox.CreateMerged(bbox, h.bboxfull);
            }
        }

        public void Draw(GraphicsDevice device) {
            foreach (Structure h in Structures)
                h.Draw(device);
            roadObject?.Draw(device);
        }
        public void Update(GameTime gameTime) {
            foreach (Structure s in Structures)
                s.Update(gameTime);
        }
    }
}
