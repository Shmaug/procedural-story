using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

using Jitter.Dynamics;
using Jitter.Collision;
using Jitter.Collision.Shapes;

using Procedural_Story.Util;
using Procedural_Story.World.Life;

namespace Procedural_Story.World {
    enum Biome {
        Forest
    }

    class Region {
        public Vector3 Point;
        public Vector3 Center;
        public List<Vector3> Verticies;
        public Vector3 Min;
        public Vector3 Max;

        public float Elevation;

        public float GrassDensity;
        public float TreeDensity;
        public float SurfaceArea;

        public List<Matrix>[] Grass;
        public List<Matrix>[] Trees;
        
        public List<RigidBody> treeBodies;

        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;

        public RigidBody RigidBody;

        public Region(Vector3 pt) {
            Elevation = pt.Y;
            Point = pt;
            Verticies = new List<Vector3>();
            treeBodies = new List<RigidBody>();
        }

        public bool Contains(Vector3 pt) {
            for (int i = 1; i < Verticies.Count - 1; i++) {
                Vector3 A = Verticies[0];
                Vector3 B = Verticies[i];
                Vector3 C = Verticies[i + 1];
                if (Util.Util.InsideTriangle(new Vector2(pt.X, pt.Z), new Vector2(A.X, A.Z), new Vector2(B.X, B.Z), new Vector2(C.X, C.Z)))
                    return true;
            }
            return false;
        }
    }

    class Area {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Seed { get; private set; }
        public Biome Biome { get; private set; }
        public float LoadProgress { get; private set; }
        public string LoadMessage { get; private set; }
        public List<wObject> WorldObjects;
        public List<Character> Characters;
        public List<Home> HouseHolds;
        Region[] VoronoiCells;
        Random rand;

        DynamicVertexBuffer GrassVertexBuffer;
        DynamicVertexBuffer TreeVertexBuffer;

        BenTools.Mathematics.VoronoiGraph voronoiGraph;

        public CollisionSystem CollisionSystem;
        public Jitter.World Physics;

        public Vector3 LightDirection = new Vector3(-.6f, -.4f, 0);
        public float LightPlaneDistance = 50;

        JitterDrawer jDrawer;
        
        public Area(Biome biome, int seed) {
            Seed = seed;
            Biome = biome;
            WorldObjects = new List<wObject>();
            HouseHolds = new List<Home>();
            CollisionSystem = new CollisionSystemSAP();
            Physics = new Jitter.World(CollisionSystem);
            Physics.Gravity = new Jitter.LinearMath.JVector(0, -20, 0);
        }

        public Matrix getLightProjection(Vector3 center) {
            Matrix v = Matrix.CreateLookAt(center - LightDirection * LightPlaneDistance, center, Vector3.Cross(LightDirection, Vector3.Backward));
            return v * Matrix.CreateOrthographic(40, 40, 1, 200);
        }
        
        public bool Contains(Vector3 p) {
            return p.X >= -Width * .5f && p.X <= Width * .5f && p.Z >= -Height * .5f && p.Z <= Height * .5f;
        }
        
        public Region CellAt(Vector3 pos) {
            float dist = float.MaxValue;
            int cin = -1;
            for (int i = 0; i < VoronoiCells.Length; i++) {
                float d = Vector2.DistanceSquared(new Vector2(pos.X, pos.Z), new Vector2(VoronoiCells[i].Point.X, VoronoiCells[i].Point.Z));
                if (d < dist) {
                    dist = d;
                    cin = i;
                }
            }
            if (cin == -1)
                return null;
            return VoronoiCells[cin];
        }
        
        void DrawInstanced(GraphicsDevice device, Model model, ref DynamicVertexBuffer vbuffer, Matrix[] transforms) {
            if (vbuffer == null || transforms.Length > vbuffer.VertexCount) {
                if (vbuffer != null)
                    vbuffer.Dispose();

                vbuffer = new DynamicVertexBuffer(device, VertexInstanced.instanceVertexDeclaration, transforms.Length, BufferUsage.WriteOnly);
            }
            vbuffer.SetData(transforms, 0, transforms.Length, SetDataOptions.Discard);

            Matrix[] transf = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transf);
            foreach (ModelMesh m in model.Meshes) {
                foreach (ModelMeshPart mmp in m.MeshParts) {
                    device.SetVertexBuffers(new VertexBufferBinding(mmp.VertexBuffer, mmp.VertexOffset, 0), new VertexBufferBinding(vbuffer, 0, 1));
                    device.Indices = mmp.IndexBuffer;

                    Models.SceneEffect.Parameters["World"].SetValue(transf[m.ParentBone.Index]);
                    Models.SceneEffect.Parameters["Textured"].SetValue(false);
                    Models.SceneEffect.Parameters["MaterialColor"].SetValue((Vector4)mmp.Tag);
                    Models.SceneEffect.CurrentTechnique = Models.SceneEffect.Techniques["Instanced"];
                    foreach (EffectPass p in Models.SceneEffect.CurrentTechnique.Passes) {
                        p.Apply();
                        device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, mmp.NumVertices, mmp.StartIndex, mmp.PrimitiveCount, transforms.Length);
                    }
                }
            }
        }

        public void Update(GameTime gameTime) {
            foreach (wObject o in WorldObjects)
                o.Update(gameTime);
            foreach (Home h in HouseHolds)
                h.Update(gameTime);

            Physics.Step((float)gameTime.ElapsedGameTime.TotalSeconds, true);

            foreach (wObject o in WorldObjects)
                o.PostUpdate();
            foreach (Home h in HouseHolds)
                h.PostUpdate();
        }

        public void Draw(GraphicsDevice device, bool depth) {
            if (jDrawer == null)
                jDrawer = new JitterDrawer(device);

            Models.SceneEffect.Parameters["ViewProj"].SetValue(Camera.CurrentCamera.View * Camera.CurrentCamera.Projection);
            Models.SceneEffect.Parameters["DepthDraw"].SetValue(depth);
            Models.SceneEffect.Parameters["LightDirection"].SetValue(LightDirection);
            Models.SceneEffect.Parameters["LightWVP"].SetValue(getLightProjection(Camera.CurrentCamera.Position));
            Models.SceneEffect.Parameters["SunPos"].SetValue(Camera.CurrentCamera.Position - LightDirection * LightPlaneDistance);

            List<Matrix>[] grassTransforms = new List<Matrix>[Models.GrassModels.Length];
            List<Matrix>[] treeTransforms = new List<Matrix>[Models.TreeModels.Length];
            for (int i = 0; i < grassTransforms.Length; i++) grassTransforms[i] = new List<Matrix>();
            for (int i = 0; i < treeTransforms.Length; i++) treeTransforms[i] = new List<Matrix>();

            #region draw ground

            Models.SceneEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
            Models.SceneEffect.CurrentTechnique = Models.SceneEffect.Techniques["VBO"];
            for (int i = 0; i < VoronoiCells.Length; i++) {
                Region r = VoronoiCells[i];
                if (Camera.CurrentCamera.Frustum.Intersects(new BoundingBox(r.Min, r.Max))){
                    Matrix W = Matrix.CreateTranslation(r.Point);
                    Models.SceneEffect.Parameters["World"].SetValue(W);

                    device.SetVertexBuffer(r.VertexBuffer);
                    device.Indices = r.IndexBuffer;

                    foreach (EffectPass p in Models.SceneEffect.CurrentTechnique.Passes) {
                        p.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, r.VertexBuffer.VertexCount, 0, r.IndexBuffer.IndexCount / 3);
                    }

                    for (int j = 0; j < r.Grass.Length; j++)
                        grassTransforms[j].AddRange(r.Grass[j]);
                    for (int j = 0; j < r.Trees.Length; j++)
                        treeTransforms[j].AddRange(r.Trees[j]);
                }
            }
            #endregion
            
            foreach (wObject o in WorldObjects)
                o.Draw(device);
            foreach (Home h in HouseHolds)
                h.Draw(device);

            #region draw foiliage
            for (int i = 0; i < grassTransforms.Length; i++)
                if (grassTransforms[i].Count > 0)
                    DrawInstanced(device, Models.GrassModels[i], ref GrassVertexBuffer, grassTransforms[i].ToArray());
            for (int i = 0; i < treeTransforms.Length; i++)
                if (treeTransforms[i].Count > 0)
                    DrawInstanced(device, Models.TreeModels[i], ref TreeVertexBuffer, treeTransforms[i].ToArray());
            #endregion
        }

        public void Generate(GraphicsDevice device) {
            rand = new Random(Seed);
            Width = 600;
            Height = 600;

            switch (Biome) {
                case Biome.Forest:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(genForest), device);
                    break;
            }
        }
        
        /// <summary>
        /// For determining order of verticies in a polygon
        /// </summary>
        /// <param name="a">A point</param>
        /// <param name="b">B point</param>
        /// <param name="c">Center of polygon</param>
        /// <returns>Whether or not a->b is clockwise</returns>
        bool less(Vector2 a, Vector2 b, Vector2 c) {
            if (a.X - c.X >= 0 && b.X - c.X < 0)
                return true;
            if (a.X - c.X < 0 && b.X - c.X >= 0)
                return false;
            if (a.X - c.X == 0 && b.X - c.X == 0) {
                if (a.Y - c.Y >= 0 || b.Y - c.Y >= 0)
                    return a.Y > b.Y;
                return b.Y > a.Y;
            }

            // compute the cross product of vectors (c -> a) X (c -> b)
            float det = (a.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (a.Y - c.Y);
            if (det < 0)
                return true;
            if (det > 0)
                return false;

            // points a and b are on the same line from the c
            // check which point is closer to the c
            float d1 = (a.X - c.X) * (a.X - c.X) + (a.Y - c.Y) * (a.Y - c.Y);
            float d2 = (b.X - c.X) * (b.X - c.X) + (b.Y - c.Y) * (b.Y - c.Y);
            return d1 > d2;
        }
        void genVoronoi() {

            // Thanks Ben
            BenTools.Mathematics.Vector[] pts = new BenTools.Mathematics.Vector[Width / 2];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new BenTools.Mathematics.Vector(rand.Next(-Width / 2, Width / 2), rand.Next(-Height / 2, Height / 2));
            voronoiGraph = BenTools.Mathematics.Fortune.ComputeVoronoiGraph(pts);

            BenTools.Mathematics.Vector[] verts = voronoiGraph.Vertizes.ToArray();
            BenTools.Mathematics.VoronoiEdge[] edges = voronoiGraph.Edges.ToArray();

            // compute cells from verticies
            List<Region> rg = new List<Region>();
            for (int i = 0; i < pts.Length; i++) {
                rg.Add(
                    new Region(
                        new Vector3((float)pts[i][0],
                        (Noise.Generate((float)pts[i][0] / 600f + Width, (float)pts[i][1] / 600f + Height) + 1) * 20,
                        (float)pts[i][1])));
            }

            for (int i = 0; i < verts.Length; i++) {
                Vector2 p = new Vector2((float)verts[i][0], (float)verts[i][1]);

                float least = float.MaxValue;

                // calculate the smallest distance from this vertex to a site
                for (int j = 0; j < pts.Length; j++) {
                    Vector2 p2 = new Vector2((float)pts[j][0], (float)pts[j][1]);
                    float d = Vector2.DistanceSquared(p, p2);
                    if (d < least)
                        least = d;
                }

                // find the 3 sites that are equidistant from this vertex
                for (int j = 0; j < pts.Length; j++) {
                    Vector2 p2 = new Vector2((float)pts[j][0], (float)pts[j][1]);
                    float d = Vector2.DistanceSquared(p, p2);
                    if (d <= least + 1f) // add some slop
                        rg[j].Verticies.Add(new Vector3(p.X, rg[j].Elevation, p.Y));
                }
            }

            // make all the cells' verticies counter-clockwise
            for (int i = 0; i < rg.Count; i++) {
                LoadProgress = .99f * i / rg.Count; // don't wanna hit 1
                Region c = rg[i];
                c.Center = Vector3.Zero;
                foreach (Vector3 p in c.Verticies)
                    c.Center += p;
                c.Center /= c.Verticies.Count;

                bool swap = true;
                while (swap) {
                    swap = false;
                    for (int j = 0; j < c.Verticies.Count - 1; j++) {
                        int j2 = j + 1;
                        if (less(new Vector2(c.Verticies[j].X, c.Verticies[j].Z), new Vector2(c.Verticies[j2].X, c.Verticies[j2].Z), new Vector2(c.Center.X, c.Center.Z))) {
                            swap = true;
                            Vector3 t = c.Verticies[j];
                            c.Verticies[j] = c.Verticies[j2];
                            c.Verticies[j2] = t;
                        }
                    }
                }
            }

            // remove the cells that are malformed
            for (int i = 0; i < rg.Count; i++) {
                if (rg[i].Verticies.Count < 3) {
                    rg.Remove(rg[i]);
                    i--;
                }
            }

            BoundingBox areaBox = new BoundingBox(new Vector3(-Width * .5f, 0, -Height * .5f), new Vector3(Width * .5f, 500, Height * .5f));
            // clamp verticies to be in the bounds of the map
            foreach (Region cell in rg) {
                for (int i = 0; i < cell.Verticies.Count; i++) {
                    if (!Contains(cell.Verticies[i])) {
                        Vector3 a = cell.Verticies[i];
                        Vector3 prev = cell.Verticies[i - 1 >= 0 ? i - 1 : cell.Verticies.Count - 1];
                        Vector3 next = cell.Verticies[(i + 1) % cell.Verticies.Count];
                        if (Contains(prev) && Contains(next)) {
                            Vector3 b = prev;
                            Vector3 m = b - a;
                            m.Normalize();
                            Ray r = new Ray(a, m);
                            float? d = r.Intersects(areaBox);

                            Vector3 c = next;
                            Vector3 m2 = c - a;
                            m2.Normalize();
                            Ray r2 = new Ray(a, m2);
                            float? d2 = r2.Intersects(areaBox);

                            if (d != null)
                                cell.Verticies[i] = a + m * d.Value;
                            else {
                                cell.Verticies.Remove(cell.Verticies[i]);
                                i--;
                            }
                            if (d2 != null)
                                cell.Verticies.Insert(i + 1, a + m2 * d2.Value);
                            i++;

                        } else {
                            Vector3 b = Contains(prev) ? prev : next;
                            // move a to point c along line ab
                            Vector3 m = b - a;
                            m.Normalize();
                            Ray r = new Ray(a, m);
                            float? d = r.Intersects(areaBox);
                            if (d != null) 
                                cell.Verticies[i] = a + m * d.Value;
                            else {
                                cell.Verticies.Remove(cell.Verticies[i]);
                                i--;
                            }
                        }
                    }
                }
            }

            VoronoiCells = rg.ToArray();

            // calculate min/max
            for (int i = 0; i < VoronoiCells.Length; i++) {
                VoronoiCells[i].Max = new Vector3(0, VoronoiCells[i].Elevation, 0);
                VoronoiCells[i].Min = new Vector3(float.MaxValue, 0, float.MaxValue);
                for (int j = 0; j < VoronoiCells[i].Verticies.Count; j++) {
                    if (VoronoiCells[i].Verticies[j].X > VoronoiCells[i].Max.X)
                        VoronoiCells[i].Max.X = VoronoiCells[i].Verticies[j].X;
                    if (VoronoiCells[i].Verticies[j].Z > VoronoiCells[i].Max.Z)
                        VoronoiCells[i].Max.Z = VoronoiCells[i].Verticies[j].Z;

                    if (VoronoiCells[i].Verticies[j].X < VoronoiCells[i].Min.X)
                        VoronoiCells[i].Min.X = VoronoiCells[i].Verticies[j].X;
                    if (VoronoiCells[i].Verticies[j].Z < VoronoiCells[i].Min.Z)
                        VoronoiCells[i].Min.Z = VoronoiCells[i].Verticies[j].Z;
                }
            }

        }

        void genForest(object state = null) {
            GraphicsDevice device = (GraphicsDevice)state;
            LoadMessage = "Generating cells...";
            genVoronoi();

            #region Generate cell geometry and foiliage
            LoadMessage = "Generating world geometry...";
            LoadProgress = 0;
            for (int ri = 0; ri < VoronoiCells.Length; ri++) {
                Region cell = VoronoiCells[ri];
                List<VertexPositionColorNormal> verts = new List<VertexPositionColorNormal>();
                List<int> inds = new List<int>();

                cell.SurfaceArea = 0;
                cell.GrassDensity = (float)rand.NextDouble();
                cell.TreeDensity = (float)rand.NextDouble();

                #region generate geometry
                Color col = new Color(0.30f, 0.43f + .15f * cell.GrassDensity, 0.43f);
                int basei = verts.Count;
                // Add verticies for the cell's geometry
                for (int i = 0; i < cell.Verticies.Count; i++) {
                    Vector3 p = cell.Verticies[i] - cell.Point;
                    verts.Add(new VertexPositionColorNormal(p, col, Vector3.Up)); // top
                }
                for (int i = 1; i < cell.Verticies.Count - 1; i++) {
                    // Add a triangle for the surface
                    int i2 = i + 1;
                    inds.Add(basei);
                    inds.Add(basei + i);
                    inds.Add(basei + i2);

                    Vector3 a = cell.Verticies[0];
                    Vector3 b = cell.Verticies[i];
                    Vector3 c = cell.Verticies[i2];
                    cell.SurfaceArea += (a.X * (b.Z - c.Z) + b.X * (c.Z - a.Z) + c.X * (a.Z - b.Z)) * .5f; // surface area of a triangle
                }
                for (int i = 0; i < cell.Verticies.Count; i++) {
                    Vector3 p1 = cell.Verticies[i] - cell.Point;
                    Vector3 p2 = cell.Verticies[(i + 1) % cell.Verticies.Count] - cell.Point;
                    Vector3 d = p1 - p2;
                    d.Normalize();
                    Vector3 normal = new Vector3(-d.Z, 0, d.X);

                    int b = verts.Count;

                    verts.Add(new VertexPositionColorNormal(p1, Color.Gray, normal));
                    verts.Add(new VertexPositionColorNormal(p2, Color.Gray, normal));
                    verts.Add(new VertexPositionColorNormal(new Vector3(p1.X, -cell.Elevation, p1.Z), Color.Gray, normal));
                    verts.Add(new VertexPositionColorNormal(new Vector3(p2.X, -cell.Elevation, p2.Z), Color.Gray, normal));

                    inds.Add(b);
                    inds.Add(b + 3);
                    inds.Add(b + 1);
                    inds.Add(b);
                    inds.Add(b + 2);
                    inds.Add(b + 3);
                }

                cell.VertexBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), verts.Count, BufferUsage.WriteOnly);
                cell.VertexBuffer.SetData(verts.ToArray());
                cell.IndexBuffer = new IndexBuffer(device, typeof(int), inds.Count, BufferUsage.WriteOnly);
                cell.IndexBuffer.SetData<int>(inds.ToArray());
                #endregion
                #region generate rigidbody
                List<Jitter.LinearMath.JVector> vts = new List<Jitter.LinearMath.JVector>();
                for (int i = 0; i < verts.Count; i++)
                    vts.Add(new Jitter.LinearMath.JVector(verts[i].Position.X, verts[i].Position.Y, verts[i].Position.Z));
                List<TriangleVertexIndices> ind = new List<TriangleVertexIndices>();
                for (int i = 0; i < inds.Count; i+=3)
                    ind.Add(new TriangleVertexIndices(inds[i], inds[i + 1], inds[i + 2]));

                Octree o = new Octree(vts, ind);
                o.BuildOctree();
                TriangleMeshShape s = new TriangleMeshShape(o);
                cell.RigidBody = new RigidBody(s);
                cell.RigidBody.Position = new Jitter.LinearMath.JVector(cell.Point.X, cell.Point.Y, cell.Point.Z);
                cell.RigidBody.IsStatic = true;
                Physics.AddBody(cell.RigidBody);
                #endregion
                #region generate foiliage
                cell.Grass = new List<Matrix>[Models.GrassModels.Length];
                cell.Trees = new List<Matrix>[Models.TreeModels.Length];
                for (int i = 0; i < cell.Grass.Length; i++) cell.Grass[i] = new List<Matrix>();
                for (int i = 0; i < cell.Trees.Length; i++) cell.Trees[i] = new List<Matrix>();

                for (float x = cell.Min.X; x < cell.Max.X; x += 2f / cell.GrassDensity) {
                    for (float z = cell.Min.Z; z < cell.Max.Z; z += 2f / cell.GrassDensity) {
                        Vector3 pt = new Vector3(x, cell.Elevation, z) + new Vector3((float)rand.NextDouble() - .5f, 0, (float)rand.NextDouble() - .5f) * 3;

                        if (CellAt(pt) == cell)
                            cell.Grass[rand.Next(0, cell.Grass.Length)].Add(
                                Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi) *
                                Matrix.CreateTranslation(pt)
                                );
                    }
                }

                for (float x = cell.Min.X; x < cell.Max.X; x += 5f / cell.TreeDensity) {
                    for (float z = cell.Min.Z; z < cell.Max.Z; z += 5f / cell.TreeDensity) {
                        Vector3 pt = new Vector3(x, cell.Elevation, z) + new Vector3((float)rand.NextDouble() - .5f, 0, (float)rand.NextDouble() - .5f) * 3;

                        if (CellAt(pt) == cell) {
                            int c = rand.Next(0, cell.Trees.Length);
                            Matrix m = Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi) * Matrix.CreateTranslation(pt);
                            cell.Trees[c].Add(m);

                            RigidBody b = new RigidBody(new CylinderShape(7, .8f));
                            b.Tag = new { c, m };
                            b.Position = new Jitter.LinearMath.JVector(pt.X, pt.Y + 2.5f, pt.Z);
                            b.IsStatic = true;
                            //cell.treeBodies.Add(b);
                            Physics.AddBody(b);
                        }
                    }
                }
                #endregion

                LoadProgress = ri / (float)VoronoiCells.Length;
            }
            #endregion

            LoadMessage = "Generating houses";
            LoadProgress = 0;

            for (int i = rand.Next(0, 20); i < VoronoiCells.Length;) {
                Home home = new Home(VoronoiCells[i].Center, this, rand.Next());
                home.Orientation = Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi);
                home.GenerateFloorPlan();
                home.BuildGeometry(device);
                HouseHolds.Add(home);

                ComplexCharacter c = new ComplexCharacter(this);
                c.Position = home.Position + new Vector3(0, c.Height, 0);
                WorldObjects.Add(c);
                home.Residents.Add(c);
                Physics.AddBody(c.RigidBody);

                // get rid of the grass and trees at this cell
                for (int g = 0; g < VoronoiCells[i].Grass.Length; g++)
                    VoronoiCells[i].Grass[g].Clear();
                for (int t = 0; t < VoronoiCells[i].Trees.Length; t++)
                    VoronoiCells[i].Trees[t].Clear();

                i += rand.Next(10, 20);
            }

            LoadProgress = 1;
        }
    }
}
