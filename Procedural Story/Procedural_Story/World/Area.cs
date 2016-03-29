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

    class Cell {
        public Vector3 Point;
        public Vector3 Center;
        public List<Vector3> Verticies;
        public Vector3 Min;
        public Vector3 Max;

        public bool isLake;
        
        public Cell(Vector3 pt) {
            Point = pt;
            Verticies = new List<Vector3>();
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

    class Chunk {
        public const int ChunkSize = 32;

        public int X;
        public int Z;
        public List<Matrix>[] Grass;
        public List<Matrix>[] Trees;
        public List<RigidBody> treeBodies;
        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;
        public BoundingBox bBox;

        public Chunk(int x, int z) {
            X = x;
            Z = z;
        }
    }

    class Area {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RealWidth {
            get {
                return Width * Chunk.ChunkSize;
            }
        }
        public int RealHeight {
            get {
                return Height * Chunk.ChunkSize;
            }
        }
        public int Seed { get; private set; }
        public Biome Biome { get; private set; }
        public float LoadProgress { get; private set; }
        public string LoadMessage { get; private set; }
        public List<wObject> WorldObjects;
        public List<Character> Characters;
        public List<Home> HouseHolds;
        Random rand;
        
        public float GrassDrawDistance = 100;

        DynamicVertexBuffer GrassVertexBuffer;
        DynamicVertexBuffer TreeVertexBuffer;

        VertexBuffer WaterVertexBuffer;
        IndexBuffer WaterIndexBuffer;
        Cell[] VoronoiCells;

        Chunk[,] Chunks;
        
        public RigidBody TerrainBody;

        float[,] heightMap;
        float heightMapScale = 1;
        float WaterHeight = 0;
        
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
            return p.X >= 0 && p.X <= RealWidth && p.Z >= 0 && p.Z <= RealHeight;
        }

        public Cell CellAt(Vector3 p) {
            return CellAt(p.X, p.Z);
        }
        public Cell CellAt(float x, float z) {
            Cell c = null;
            float d = float.MaxValue;
            for (int i = 0; i < VoronoiCells.Length; i++) {
                float d2 = Vector2.DistanceSquared(new Vector2(x, z), new Vector2(VoronoiCells[i].Point.X, VoronoiCells[i].Point.Z));
                if (d2 < d) {
                    d = d2;
                    c = VoronoiCells[i];
                }
            }
            return c;
        }

        public float HeightAt(Vector3 p) {
            return HeightAt(p.X, p.Z);
        }
        public float HeightAt(float x, float z) {
            int minx = (int)(x / heightMapScale);
            int minz = (int)(z / heightMapScale);
            int maxx = minx + 1;
            int maxz = minz + 1;
            float fx = (x / heightMapScale) - minx;
            float fz = (z / heightMapScale) - minz;
            float a = fx * fz;

            if (minx >= 0 && minz >= 0 && maxx < RealWidth && maxz < RealHeight) {
                Vector3 v1, v2, v3;
                if (a < .5f) {
                    v1 = new Vector3(minx * heightMapScale, heightMap[minx, minz], minz * heightMapScale);
                    v2 = new Vector3((minx + 1) * heightMapScale, heightMap[minx + 1, minz], minz * heightMapScale);
                    v3 = new Vector3(minx * heightMapScale, heightMap[minx, minz + 1], (minz + 1) * heightMapScale);
                } else {
                    v1 = new Vector3((minx + 1) * heightMapScale, heightMap[minx + 1, minz], minz * heightMapScale);
                    v2 = new Vector3((minx + 1) * heightMapScale, heightMap[minx + 1, minz + 1], (minz + 1) * heightMapScale);
                    v3 = new Vector3(minx * heightMapScale, heightMap[minx, minz + 1], (minz + 1) * heightMapScale);
                }

                float det = (v2.Z - v3.Z) * (v1.X - v3.X) + (v3.X - v2.X) * (v1.Z - v3.Z);
                float b1 = ((v2.Z - v3.Z) * (x - v3.X) + (v3.X - v2.X) * (z - v3.Z)) / det;
                float b2 = ((v3.Z - v1.Z) * (x - v3.X) + (v1.X - v3.X) * (z - v3.Z)) / det;
                float b3 = 1 - b1 - b2;

                return b1 * v1.Y + b2 * v2.Y + b3 * v3.Y;
            }
            return 0;
        }
        float generateHeight(float x, float z) {
            Cell c = CellAt(x, z);
            if (c.isLake)
                return WaterHeight - 5 - 2 * (Noise.Generate(x / 30f, z / 30f) * .5f + .5f);

            return WaterHeight + 2 + 2 * Noise.Generate(x / 60f, z / 60f);
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

                    Models.WorldEffect.Parameters["World"].SetValue(transf[m.ParentBone.Index]);
                    Models.WorldEffect.Parameters["Textured"].SetValue(false);
                    Models.WorldEffect.Parameters["MaterialColor"].SetValue((Vector4)mmp.Tag);
                    Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["Instanced"];
                    foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
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

            Models.WorldEffect.Parameters["ViewProj"].SetValue(Camera.CurrentCamera.View * Camera.CurrentCamera.Projection);
            Models.WorldEffect.Parameters["DepthDraw"].SetValue(depth);
            Models.WorldEffect.Parameters["LightDirection"].SetValue(LightDirection);
            Models.WorldEffect.Parameters["LightWVP"].SetValue(getLightProjection(Camera.CurrentCamera.Position));
            Models.WorldEffect.Parameters["SunPos"].SetValue(Camera.CurrentCamera.Position - LightDirection * LightPlaneDistance);

            List<Matrix>[] grassTransforms = new List<Matrix>[Models.GrassModels.Length];
            List<Matrix>[] treeTransforms = new List<Matrix>[Models.TreeModels.Length];
            for (int i = 0; i < grassTransforms.Length; i++) grassTransforms[i] = new List<Matrix>();
            for (int i = 0; i < treeTransforms.Length; i++) treeTransforms[i] = new List<Matrix>();

            #region draw terrain
            Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
            Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["VBO"];
            Models.WorldEffect.Parameters["World"].SetValue(Matrix.Identity);

            int c = 0;
            BoundingFrustum f = Camera.CurrentCamera.Frustum;
            for (int x = 0; x < Width; x++) {
                for (int z = 0; z < Height; z++) {
                    if (f.Intersects(Chunks[x, z].bBox)) {
                        c++;

                        device.SetVertexBuffer(Chunks[x, z].VertexBuffer);
                        device.Indices = Chunks[x, z].IndexBuffer;
                        foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                            p.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Chunks[x, z].VertexBuffer.VertexCount, 0, Chunks[x, z].IndexBuffer.IndexCount / 3);
                        }

                        if (Vector3.DistanceSquared(new Vector3((x + .5f) * Chunk.ChunkSize, 0, (z + .5f) * Chunk.ChunkSize), new Vector3(Camera.CurrentCamera.Position.X, 0, Camera.CurrentCamera.Position.Z)) < GrassDrawDistance*GrassDrawDistance)
                            for (int j = 0; j < Chunks[x, z].Grass.Length; j++)
                                grassTransforms[j].AddRange(Chunks[x, z].Grass[j]);

                        for (int j = 0; j < Chunks[x, z].Trees.Length; j++)
                            treeTransforms[j].AddRange(Chunks[x, z].Trees[j]);
                    }
                }
            }

            if (!depth) {
                device.SetVertexBuffer(WaterVertexBuffer);
                device.Indices = WaterIndexBuffer;
                Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["Water"];
                device.BlendState = BlendState.AlphaBlend;
                foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                    p.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, WaterVertexBuffer.VertexCount, 0, WaterIndexBuffer.IndexCount / 3);
                }
                device.BlendState = BlendState.Opaque;
            }
            #region draw foiliage
            for (int i = 0; i < grassTransforms.Length; i++)
                if (grassTransforms[i].Count > 0)
                    DrawInstanced(device, Models.GrassModels[i], ref GrassVertexBuffer, grassTransforms[i].ToArray());
            for (int i = 0; i < treeTransforms.Length; i++)
                if (treeTransforms[i].Count > 0)
                    DrawInstanced(device, Models.TreeModels[i], ref TreeVertexBuffer, treeTransforms[i].ToArray());
            #endregion
            #endregion

            foreach (wObject o in WorldObjects)
                o.Draw(device);
            foreach (Home h in HouseHolds)
                h.Draw(device);
        }

        public void Generate(GraphicsDevice device) {
            rand = new Random(Seed);
            Width = 20;
            Height = 20;
            Chunks = new Chunk[Width, Height];

            switch (Biome) {
                case Biome.Forest:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(genForest), device);
                    break;
            }
        }
        
        void smoothHeights() {
            for (int x = 1; x < RealWidth; x++) {
                for (int z = 1; z < RealHeight; z++) {
                    heightMap[x, z] = (heightMap[x, z] + heightMap[x - 1, z] + heightMap[x, z - 1] + heightMap[x + 1, z] + heightMap[x, z + 1]) * .2f;
                }
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
            BenTools.Mathematics.Vector[] pts = new BenTools.Mathematics.Vector[RealWidth / 3];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new BenTools.Mathematics.Vector(rand.Next(0, RealWidth), rand.Next(0, RealHeight));
            voronoiGraph = BenTools.Mathematics.Fortune.ComputeVoronoiGraph(pts);

            BenTools.Mathematics.Vector[] verts = voronoiGraph.Vertizes.ToArray();
            BenTools.Mathematics.VoronoiEdge[] edges = voronoiGraph.Edges.ToArray();

            // compute cells from verticies
            List<Cell> rg = new List<Cell>();
            for (int i = 0; i < pts.Length; i++) {
                bool lake = rand.Next(0, 30) == 0;
                Cell r = new Cell(new Vector3((float)pts[i][0], 0, (float)pts[i][1]));
                r.isLake = lake;
                rg.Add(r);
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
                        rg[j].Verticies.Add(new Vector3(p.X, 0, p.Y));
                }
            }

            // make all the cells' verticies counter-clockwise
            for (int i = 0; i < rg.Count; i++) {
                LoadProgress = .99f * i / rg.Count; // don't wanna hit 1
                Cell c = rg[i];
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

            BoundingBox areaBox = new BoundingBox(new Vector3(-RealWidth * .5f, 0, -RealHeight * .5f), new Vector3(RealWidth * .5f, 500, RealHeight * .5f));
            // clamp verticies to be in the bounds of the map
            foreach (Cell cell in rg) {
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
                VoronoiCells[i].Max = new Vector3(0, 0, 0);
                VoronoiCells[i].Min = new Vector3(float.MaxValue, 100, float.MaxValue);
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
            
            LoadProgress = 0;
            
            #region generate heightmap
            LoadMessage = "Generating heightmap...";
            heightMap = new float[RealWidth + 1, RealHeight + 1];
            heightMapScale = 1;
            float li = 0;
            for (int x = 0; x < RealWidth + 1; x++)
                for (int z = 0; z < RealHeight + 1; z++) {
                    heightMap[x, z] = generateHeight(x * heightMapScale, z * heightMapScale);

                    li++;
                    LoadProgress = li / (RealWidth * RealHeight);
                }
            for (int i = 0; i < 3; i++)
                smoothHeights();
            #endregion

            #region generate chunks
            LoadMessage = "Generating world geometry...";
            LoadProgress = 0;
            li = 0;
            for (int cx = 0; cx < Width; cx++) {
                for (int cz = 0; cz < Height; cz++) {
                    int crx = cx * Chunk.ChunkSize;
                    int crz = cz * Chunk.ChunkSize;

                    #region chunk geometry
                    List<VertexPositionColorNormal> verts = new List<VertexPositionColorNormal>();
                    List<int> inds = new List<int>();
                    // verticies
                    for (int x = 0; x < Chunk.ChunkSize + 1; x++) {
                        for (int z = 0; z < Chunk.ChunkSize + 1; z++) {
                            Vector3 v1 = new Vector3(crx + x * heightMapScale, heightMap[crx + x, crz + z], crz + z * heightMapScale);
                            Color c = new Color(.2f, .4f, .3f);
                            if (v1.Y < WaterHeight - 2)
                                c = new Color(0xFF, 0xEB, 0xCD);

                            Vector3 left = Vector3.Zero, right = Vector3.Zero, forward = Vector3.Zero, backward = Vector3.Zero;
                            if (crx + x > 0)
                                left = new Vector3(crx + (x - 1) * heightMapScale, heightMap[crx + x - 1, crz + z], crz + z * heightMapScale) - v1;
                            if (crx + x < RealWidth - 1)
                                right = new Vector3(crx + (x + 1) * heightMapScale, heightMap[crx + x + 1, crz + z], crz + z * heightMapScale) - v1;
                            if (crz + z > 0)
                                forward = new Vector3(crx + x * heightMapScale, heightMap[crx + x, crz + z - 1], crz + (z - 1) * heightMapScale) - v1;
                            if (crz + z < RealHeight - 1)
                                backward = new Vector3(crx + x * heightMapScale, heightMap[crx + x, crz + z + 1], crz + (z + 1) * heightMapScale) - v1;

                            left.Normalize(); right.Normalize(); forward.Normalize(); backward.Normalize();
                            Vector3 n =
                                ((left != Vector3.Zero && forward != Vector3.Zero) ? -Vector3.Cross(left, forward) : Vector3.Zero) +
                                ((right != Vector3.Zero && forward != Vector3.Zero) ? -Vector3.Cross(forward, right) : Vector3.Zero) +
                                ((right != Vector3.Zero && backward != Vector3.Zero) ? -Vector3.Cross(right, backward) : Vector3.Zero) +
                                ((left != Vector3.Zero && backward != Vector3.Zero) ? -Vector3.Cross(backward, left) : Vector3.Zero);
                            n.Normalize();
                            verts.Add(new VertexPositionColorNormal(v1, c, n));
                        }
                    }
                    // indicies
                    int cs = Chunk.ChunkSize+1;
                    for (int x = 0; x < Chunk.ChunkSize; x++) {
                        for (int z = 0; z < Chunk.ChunkSize; z++) {
                            inds.Add(x + z * cs);
                            inds.Add(x + (z + 1) * cs);
                            inds.Add((x + 1) + z * cs);

                            inds.Add((x + 1) + z * cs);
                            inds.Add(x + (z + 1) * cs);
                            inds.Add((x + 1) + (z + 1) * cs);
                        }
                    }
                    #endregion

                    Chunks[cx, cz] = new Chunk(cx, cz);
                    Chunks[cx, cz].VertexBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), verts.Count, BufferUsage.WriteOnly);
                    Chunks[cx, cz].VertexBuffer.SetData(verts.ToArray());
                    Chunks[cx, cz].IndexBuffer = new IndexBuffer(device, typeof(int), inds.Count, BufferUsage.WriteOnly);
                    Chunks[cx, cz].IndexBuffer.SetData(inds.ToArray());
                    Chunks[cx, cz].bBox = new BoundingBox(new Vector3(crx, 0, crz), new Vector3(crx + Chunk.ChunkSize, 100, crz + Chunk.ChunkSize));

                    #region chunk foiliage
                    // grass and trees
                    Chunks[cx, cz].Grass = new List<Matrix>[Models.GrassModels.Length];
                    Chunks[cx, cz].Trees = new List<Matrix>[Models.TreeModels.Length];
                    for (int i = 0; i < Chunks[cx, cz].Grass.Length; i++) Chunks[cx, cz].Grass[i] = new List<Matrix>();
                    for (int i = 0; i < Chunks[cx, cz].Trees.Length; i++) Chunks[cx, cz].Trees[i] = new List<Matrix>();

                    for (int x = cx * Chunk.ChunkSize; x < (cx + 1) * Chunk.ChunkSize; x+=2) {
                        for (int z = cz * Chunk.ChunkSize; z < (cz + 1) * Chunk.ChunkSize; z+=2) {
                            Vector3 pt = new Vector3(x, 0, z) + new Vector3((float)rand.NextDouble() - .5f, 0, (float)rand.NextDouble() - .5f) * 2;
                            if (Contains(pt) && !CellAt(pt).isLake) {
                                pt.Y = HeightAt(pt);

                                Chunks[cx, cz].Grass[rand.Next(0, Chunks[cx, cz].Grass.Length)].Add(
                                    Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi) *
                                    Matrix.CreateTranslation(pt)
                                    );
                            }
                        }
                    }
                    
                    for (int x = cx * Chunk.ChunkSize; x < (cx + 1) * Chunk.ChunkSize; x+=10) {
                        for (int z = cz * Chunk.ChunkSize; z < (cz + 1) * Chunk.ChunkSize; z+=10) {
                            Vector3 pt = new Vector3(x, 0, z) + new Vector3((float)rand.NextDouble() - .5f, 0, (float)rand.NextDouble() - .5f) * 10;
                            if (Contains(pt) && !CellAt(pt).isLake) {
                                pt.Y = HeightAt(pt);

                                int c = rand.Next(0, Chunks[cx, cz].Trees.Length);
                                Matrix m = Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi) * Matrix.CreateTranslation(pt);
                                Chunks[cx, cz].Trees[c].Add(m);

                                RigidBody b = new RigidBody(new CylinderShape(7, .8f));
                                b.Tag = new { c, m };
                                b.Position = new Jitter.LinearMath.JVector(pt.X, pt.Y + 2.5f, pt.Z);
                                b.IsStatic = true;
                                Physics.AddBody(b);
                            }

                        }
                    }
                    #endregion

                    li++;
                    LoadProgress = li / (Width * Height);
                }
            }
            #endregion

            #region water geometry
            List<VertexPositionTexture> wverts = new List<VertexPositionTexture>();
            List<int> winds = new List<int>();
            for (int i = 0; i < VoronoiCells.Length; i++) {
                if (VoronoiCells[i].isLake) {
                    int bi = wverts.Count;
                    Vector3[] vts = new Vector3[VoronoiCells[i].Verticies.Count];
                    vts[0] = new Vector3(VoronoiCells[i].Verticies[0].X, WaterHeight, VoronoiCells[i].Verticies[0].Z);
                    vts[1] = new Vector3(VoronoiCells[i].Verticies[1].X, WaterHeight, VoronoiCells[i].Verticies[1].Z);

                    for (int v = 2; v < VoronoiCells[i].Verticies.Count; v++) {
                        vts[v] = new Vector3(VoronoiCells[i].Verticies[v].X, WaterHeight, VoronoiCells[i].Verticies[v].Z);

                        winds.Add(bi);
                        winds.Add(bi + v - 1);
                        winds.Add(bi + v);
                    }

                    // expand the verticies out a bit
                    for (int v = 0; v < vts.Length; v++) {
                        Vector3 dir = vts[v] - VoronoiCells[i].Center;
                        dir.Normalize();
                        vts[v] += dir * 5;

                        wverts.Add(new VertexPositionTexture(vts[v], Vector2.Zero));
                    }
                }
            }
            WaterVertexBuffer = new VertexBuffer(device, typeof(VertexPositionTexture), wverts.Count, BufferUsage.WriteOnly);
            WaterVertexBuffer.SetData(wverts.ToArray());
            WaterIndexBuffer = new IndexBuffer(device, typeof(int), winds.Count, BufferUsage.WriteOnly);
            WaterIndexBuffer.SetData(winds.ToArray());
            #endregion

            #region generate rigidbody
            TerrainShape s = new TerrainShape(heightMap, heightMapScale, heightMapScale);
            TerrainBody = new RigidBody(s);
            TerrainBody.IsStatic = true;
            Physics.AddBody(TerrainBody);
            #endregion
            
            #region generate life
            LoadMessage = "Generating houses";
            LoadProgress = 0;

            for (int i = rand.Next(6, 13); i < VoronoiCells.Length;) {
                if (!VoronoiCells[i].isLake) {
                    Home home = new Home(new Vector3(VoronoiCells[i].Center.X, HeightAt(VoronoiCells[i].Center), VoronoiCells[i].Center.Z), this, rand.Next());
                    home.Orientation = Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi);
                    home.GenerateFloorPlan();
                    home.BuildGeometry(device);
                    HouseHolds.Add(home);

                    ComplexCharacter c = new ComplexCharacter(this);
                    c.Position = home.Position + new Vector3(0, c.Height, 0);
                    WorldObjects.Add(c);
                    home.Residents.Add(c);
                    Physics.AddBody(c.RigidBody);
                }

                i += rand.Next(6, 13);

                LoadProgress = i / (float)VoronoiCells.Length;
            }
            #endregion

            LoadProgress = 2;
            Console.WriteLine("Done generating");
        }
    }
}
