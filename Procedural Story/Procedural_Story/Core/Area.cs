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
using Procedural_Story.Core.Life;
using Procedural_Story.Core.Structures;
using Jitter.LinearMath;

namespace Procedural_Story.Core {
    public enum ObstructionType {
        None,
        /// <summary>
        /// Obstructions such as roads; not a physical barrier but things shouldn't generally be here
        /// </summary>
        Virtual,
        /// <summary>
        /// Physical obstructions such as buildings and trees
        /// </summary>
        Physical
    }
    public enum Biome {
        Forest
    }
    class Cell {
        public Vector3 Point;
        public Vector3 Center;
        public List<Vector3> Verticies;
        public Vector3 Min;
        public Vector3 Max;

        public bool isLake;
        public bool isEdge;

        public Cell(Vector3 pt) {
            Point = pt;
            isEdge = false;
            isLake = false;
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
        public Area area;

        public Chunk(int x, int z, Area a) {
            X = x;
            Z = z;
            area = a;
        }

        /// <summary>
        /// Check for intersection with trees
        /// </summary>
        public ObstructionType Obstructed(BoundingSphere bsphere) {
            if (!bBox.Intersects(bsphere))
                return ObstructionType.None;

            foreach (RigidBody b in treeBodies)
                if (bsphere.Intersects(
                    new BoundingBox(
                        new Vector3(b.Position.X - .8f, b.Position.Y - 5, b.Position.Z - .8f),
                        new Vector3(b.Position.X + .8f, b.Position.Y + 5, b.Position.Z + .8f)))) {
                    return ObstructionType.Physical;
                }
            return ObstructionType.None;
        }
        /// <summary>
        /// Check for intersection with trees
        /// </summary>
        public ObstructionType Obstructed(BoundingBox bbox) {
            if (!bBox.Intersects(bbox))
                return ObstructionType.None;

            foreach (RigidBody b in treeBodies)
                if (bbox.Intersects(
                    new BoundingBox(
                        new Vector3(b.Position.X - .8f, b.Position.Y - 5, b.Position.Z - .8f),
                        new Vector3(b.Position.X + .8f, b.Position.Y + 5, b.Position.Z + .8f)))) {
                    return ObstructionType.Physical;
                }
            return ObstructionType.None;
        }
        /// <summary>
        /// Check for intersection with trees
        /// </summary>
        public ObstructionType Obstructed(Vector3 p) {
            if (bBox.Contains(p) == ContainmentType.Disjoint)
                return ObstructionType.None;

            foreach (RigidBody b in treeBodies)
                    if (new BoundingBox(
                        new Vector3(b.Position.X - .8f, b.Position.Y - 5, b.Position.Z - .8f),
                        new Vector3(b.Position.X + .8f, b.Position.Y + 5, b.Position.Z + .8f)).Contains(p) != ContainmentType.Disjoint) {
                    return ObstructionType.Physical;
                }
            return ObstructionType.None;
        }
    }
    class Area {
        public double ElapsedTime;

        public int Width { get; private set; }
        public int Length { get; private set; }
        public int RealWidth {
            get {
                return Width * Chunk.ChunkSize;
            }
        }
        public int RealLength {
            get {
                return Length * Chunk.ChunkSize;
            }
        }
        public Biome Biome { get; private set; }

        #region generation variables
        public int Seed { get; private set; }
        public float LoadProgress;
        public string LoadMessage;
        Random rand;
        float[,] heightMap;
        uint[,] colorMap;
        float heightMapScale = 1;
        public float WaterHeight { get; private set; }
        BenTools.Mathematics.VoronoiGraph voronoiGraph;
        #endregion

        public Chunk[,] Chunks { get; private set; }
        public Cell[] VoronoiCells { get; private set; }

        public List<wObject> WorldObjects;
        public List<Character> Characters;
        public List<Town> Towns;

        public float GrassDrawDistance = 100;
        public float TreeDrawDistance = 200;

        DynamicVertexBuffer GrassVertexBuffer;
        DynamicVertexBuffer TreeVertexBuffer;

        VertexBuffer WaterVertexBuffer;
        IndexBuffer WaterIndexBuffer;
        
        public RigidBody TerrainBody;
        
        public CollisionSystem CollisionSystem;
        public Jitter.World Physics;
        public PathSystem PathSystem;

        public Vector3 LightDirection = new Vector3(-.6f, -.4f, 0);
        public float LightPlaneDistance = 50;

        public JitterDrawer jDrawer;
        
        public Area(Biome biome, int seed) {
            Seed = seed;
            Biome = biome;
            WorldObjects = new List<wObject>();
            Towns = new List<Town>();
            Characters = new List<Character>();
            CollisionSystem = new CollisionSystemSAP();
            CollisionSystem.UseTriangleMeshNormal = true;
            Physics = new Jitter.World(CollisionSystem);
            Physics.Gravity = new JVector(0, -20, 0);
        }

        public void AddCharacter(Character c) {
            Characters.Add(c);
            Physics.AddBody(c.RigidBody);
        }
        
        #region heightmap generation and manipulation
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

            if (minx >= 0 && minz >= 0 && maxx < RealWidth && maxz < RealLength) {
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
        public float HeightAt(Vector3 p, out Vector3 normal) {
            return HeightAt(p.X, p.Z, out normal);
        }
        public float HeightAt(float x, float z, out Vector3 normal) {
            int minx = (int)(x / heightMapScale);
            int minz = (int)(z / heightMapScale);
            int maxx = minx + 1;
            int maxz = minz + 1;
            float fx = (x / heightMapScale) - minx;
            float fz = (z / heightMapScale) - minz;
            float a = fx * fz;

            if (minx >= 0 && minz >= 0 && maxx < RealWidth && maxz < RealLength) {
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

                normal = Vector3.Cross(v3 - v1, v2 - v1);
                normal.Normalize();

                return b1 * v1.Y + b2 * v2.Y + b3 * v3.Y;
            }

            normal = Vector3.Zero;
            return 0;
        }
        float generateHeight(float x, float z) {
            float h = WaterHeight + 2 + 2 * Noise.Generate(x / 60f, z / 60f);

            Cell c = CellAt(x, z);
            if (c != null) {
                if (c.isLake)
                    h = WaterHeight - 5 - 2 * (Noise.Generate(x / 30f, z / 30f) * .5f + .5f);
                if (c.isEdge)
                    h = WaterHeight + 15 + 5 * Noise.Generate(x / 40f, z / 40f);
            }

            return h;
        }
        void smoothHeights() {
            for (int x = 1; x < RealWidth; x++) {
                for (int z = 1; z < RealLength; z++) {
                    heightMap[x, z] = (heightMap[x, z] + heightMap[x - 1, z] + heightMap[x, z - 1] + heightMap[x + 1, z] + heightMap[x, z + 1]) * .2f;
                }
            }
        }
        #endregion

        #region draw & update
        public void Update(GameTime gameTime) {
            foreach (wObject o in WorldObjects)
                o.Update(gameTime);
            foreach (Character c in Characters)
                c.Update(gameTime);
            foreach (Town t in Towns)
                t.Update(gameTime);

            Physics.Step((float)gameTime.ElapsedGameTime.TotalSeconds, true);

            foreach (wObject o in WorldObjects)
                o.PostUpdate();
            foreach (Character c in Characters)
                c.PostUpdate();

            ElapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
        }

        public Matrix getLightProjection(Vector3 center) {
            Matrix v = Matrix.CreateLookAt(center - LightDirection * LightPlaneDistance, center, Vector3.Cross(LightDirection, Vector3.Backward));
            return v * Matrix.CreateOrthographic(40, 40, 1, 200);
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

        public void Draw(GraphicsDevice device, bool depth) {
            if (jDrawer == null)
                jDrawer = new JitterDrawer(device, Models.WorldEffect);

            #region parameters
            Models.WorldEffect.Parameters["ViewProj"].SetValue(Camera.CurrentCamera.View * Camera.CurrentCamera.Projection);
            Models.WorldEffect.Parameters["LightDirection"].SetValue(LightDirection);
            Models.WorldEffect.Parameters["LightWVP"].SetValue(getLightProjection(Camera.CurrentCamera.Position));
            Models.WorldEffect.Parameters["SunPos"].SetValue(Camera.CurrentCamera.Position - LightDirection * LightPlaneDistance);
            Cell cell = CellAt(Camera.CurrentCamera.Position);
            Models.WorldEffect.Parameters["CameraInWater"].SetValue(cell != null && cell.isLake && Camera.CurrentCamera.Position.Y < WaterHeight);
            #endregion

            List<Matrix>[] grassTransforms = new List<Matrix>[Models.GrassModels.Length];
            List<Matrix>[] treeTransforms = new List<Matrix>[Models.TreeModels.Length];
            for (int i = 0; i < grassTransforms.Length; i++) grassTransforms[i] = new List<Matrix>();
            for (int i = 0; i < treeTransforms.Length; i++) treeTransforms[i] = new List<Matrix>();
            if (Debug.DrawTerrain) {
                #region draw terrain
                Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
                Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["VBO"];
                Models.WorldEffect.Parameters["World"].SetValue(Matrix.Identity);
                int c = 0;
                BoundingFrustum f = Camera.CurrentCamera.Frustum;
                for (int x = 0; x < Width; x++) {
                    for (int z = 0; z < Length; z++) {
                        if (f.Intersects(Chunks[x, z].bBox)) {
                            c++;

                            device.SetVertexBuffer(Chunks[x, z].VertexBuffer);
                            device.Indices = Chunks[x, z].IndexBuffer;
                            foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                                p.Apply();
                                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Chunks[x, z].VertexBuffer.VertexCount, 0, Chunks[x, z].IndexBuffer.IndexCount / 3);
                            }

                            float chunkdist = Vector3.DistanceSquared(new Vector3((x + .5f) * Chunk.ChunkSize, 0, (z + .5f) * Chunk.ChunkSize), new Vector3(Camera.CurrentCamera.Position.X, 0, Camera.CurrentCamera.Position.Z));
                            if (chunkdist < GrassDrawDistance * GrassDrawDistance)
                                for (int j = 0; j < Chunks[x, z].Grass.Length; j++)
                                    grassTransforms[j].AddRange(Chunks[x, z].Grass[j]);
                            if (chunkdist < TreeDrawDistance * TreeDrawDistance)
                                for (int j = 0; j < Chunks[x, z].Trees.Length; j++)
                                    treeTransforms[j].AddRange(Chunks[x, z].Trees[j]);
                        }
                    }
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
            }
            foreach (wObject o in WorldObjects)
                o.Draw(device);
            foreach (Character ch in Characters)
                ch.Draw(device);

            if (Debug.DrawSettlements)
                foreach (Town t in Towns)
                    t.Draw(device);
            
            #region draw water
            if (!depth && Debug.DrawTerrain) {
                RasterizerState b4 = device.RasterizerState;
                device.RasterizerState = new RasterizerState() { CullMode = CullMode.None, FillMode = b4.FillMode };
                Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
                Models.WorldEffect.Parameters["World"].SetValue(Matrix.Identity);

                device.SetVertexBuffer(WaterVertexBuffer);
                device.Indices = WaterIndexBuffer;
                Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["Water"];
                device.BlendState = BlendState.AlphaBlend;
                foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                    p.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, WaterVertexBuffer.VertexCount, 0, WaterIndexBuffer.IndexCount / 3);
                }
                device.BlendState = BlendState.Opaque;
                device.RasterizerState = b4;
            }
            #endregion

            if (Debug.DrawPaths)
                PathSystem.DebugDraw(device);
        }
        #endregion

        #region obstruction and containment tests
        public bool Contains(Vector3 p) {
            return p.X >= 0 && p.X <= RealWidth && p.Z >= 0 && p.Z <= RealLength;
        }
        public bool Contains(BoundingBox box) {
            BoundingBox abox = new BoundingBox(Vector3.Zero, new Vector3(RealLength, 1000, RealLength));
            return abox.Contains(box) == ContainmentType.Contains;
        }

        public Cell CellAt(Vector3 p) {
            return CellAt(p.X, p.Z);
        }
        public Cell CellAt(float x, float z) {
            if (!Contains(new Vector3(x, 0, z)))
                return null;

            Cell c = null;
            float d = float.MaxValue;
            for (int i = 0; i < VoronoiCells.Length; i++) {
                float d2 = Vector2.DistanceSquared(new Vector2(x, z), new Vector2(VoronoiCells[i].Point.X, VoronoiCells[i].Point.Z));
                if (d2 < d) {
                    d = d2;
                    c = VoronoiCells[i];
                }
            }
            if (c.Contains(new Vector3(x, 0, z)))
                return c;
            return null;
        }

        public Cell ClosestCell(Vector3 p) {
            Cell c = null;
            float d = float.MaxValue;
            for (int i = 0; i < VoronoiCells.Length; i++) {
                float d2 = Vector2.DistanceSquared(new Vector2(p.X, p.Z), new Vector2(VoronoiCells[i].Point.X, VoronoiCells[i].Point.Z));
                if (d2 < d) {
                    d = d2;
                    c = VoronoiCells[i];
                }
            }
            return c;
        }
        public ObstructionType Obstructed(BoundingSphere bsphere) {
            if (!Contains(bsphere.Center))
                return ObstructionType.None;
            foreach (Town s in Towns) {
                ObstructionType t = s.Obstructs(bsphere);
                if (t != ObstructionType.None)
                    return t;
            }

            if (Chunks != null) {
                int cx = (int)MathHelper.Clamp((bsphere.Center.X - bsphere.Radius) / Chunk.ChunkSize, 0, Width - 1);
                int cx2 = (int)MathHelper.Clamp((bsphere.Center.X + bsphere.Radius) / Chunk.ChunkSize, 0, Width - 1);
                int cz = (int)MathHelper.Clamp((bsphere.Center.Z - bsphere.Radius) / Chunk.ChunkSize, 0, Length - 1);
                int cz2 = (int)MathHelper.Clamp((bsphere.Center.Z + bsphere.Radius) / Chunk.ChunkSize, 0, Length - 1);
                do {
                    do {
                        if (Chunks[cx, cz] != null) {
                            ObstructionType t = Chunks[cx, cz].Obstructed(bsphere);
                            if (t != ObstructionType.None)
                                return t;
                        }
                        cz++;
                    } while (cz < cz2);
                    cx++;
                } while (cx < cx2);
            }

            return ObstructionType.None;
        }
        public ObstructionType Obstructed(BoundingBox bbox) {
            if (!Contains(bbox))
                return ObstructionType.None;
            foreach (Town s in Towns) {
                ObstructionType t = s.Obstructs(bbox);
                if (t != ObstructionType.None)
                    return t;
            }

            if (Chunks != null) {
                int cx = (int)MathHelper.Clamp((bbox.Min.X) / Chunk.ChunkSize, 0, Width - 1);
                int cx2 = (int)MathHelper.Clamp((bbox.Max.X) / Chunk.ChunkSize, 0, Width - 1);
                int cz = (int)MathHelper.Clamp((bbox.Min.Z) / Chunk.ChunkSize, 0, Length - 1);
                int cz2 = (int)MathHelper.Clamp((bbox.Max.Z) / Chunk.ChunkSize, 0, Length - 1);
                do {
                    do {
                        if (Chunks[cx, cz] != null) {
                            ObstructionType t = Chunks[cx, cz].Obstructed(bbox);
                            if (t != ObstructionType.None)
                                return t;
                        }
                        cz++;
                    } while (cz < cz2);
                    cx++;
                } while (cx < cx2);
            }

            return ObstructionType.None;
        }
        public ObstructionType Obstructed(Vector3 p) {
            if (!Contains(p))
                return ObstructionType.None;

            // check towns
            foreach (Town s in Towns) {
                ObstructionType ot = s.Obstructs(p);
                if (ot != ObstructionType.None)
                    return ot;
            }

            // check trees
            int cx = (int)p.X / Chunk.ChunkSize, cz = (int)p.Z / Chunk.ChunkSize;
            if (Chunks != null && Chunks[cx, cz] != null) {
                ObstructionType t = Chunks[cx, cz].Obstructed(p);
                if (t != ObstructionType.None)
                    return t;
            }

            return ObstructionType.None;
        }
        #endregion
        
        #region world generation
        public void Generate(GraphicsDevice device) {
            rand = new Random(Seed);
            Width = 20;
            Length = 20;
            Chunks = new Chunk[Width, Length];

            switch (Biome) {
                case Biome.Forest:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(genForest), device);
                    break;
            }
        }
        public void FlattenUnder(Structure structure) {
            Matrix mat = Matrix.CreateTranslation(-structure.Position) * Matrix.Invert(structure.Orientation) * Matrix.CreateTranslation(structure.Position);
            for (int x = (int)structure.bboxfull.Min.X-1; x < structure.bboxfull.Max.X+1; x++) {
                for (int z = (int)structure.bboxfull.Min.Z-1; z < structure.bboxfull.Max.Z+1; z++) {
                    if (structure.bbox.Intersects(new BoundingSphere(Vector3.Transform(new Vector3(x, HeightAt(x, z), z), mat), 1)))
                        heightMap[x, z] = structure.Position.Y;
                }
            }
        }
        public void RaiseTo(Structure structure) {
            float y = 0;
            Matrix mat = Matrix.CreateTranslation(-structure.Position) * Matrix.Invert(structure.Orientation) * Matrix.CreateTranslation(structure.Position);
            for (int x = (int)structure.bboxfull.Min.X; x < structure.bboxfull.Max.X; x++) {
                for (int z = (int)structure.bboxfull.Min.Z; z < structure.bboxfull.Max.Z; z++) {
                    if (structure.bbox.Contains(Vector3.Transform(new Vector3(x, HeightAt(x, z), z), mat)) == ContainmentType.Contains)
                        y = Math.Max(heightMap[x, z], y);
                }
            }
            structure.Position = new Vector3(structure.Position.X, y, structure.Position.Z);
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
                pts[i] = new BenTools.Mathematics.Vector(rand.Next(0, RealWidth), rand.Next(0, RealLength));
            voronoiGraph = BenTools.Mathematics.Fortune.ComputeVoronoiGraph(pts);

            BenTools.Mathematics.Vector[] verts = voronoiGraph.Vertizes.ToArray();
            BenTools.Mathematics.VoronoiEdge[] edges = voronoiGraph.Edges.ToArray();

            // compute cells from verticies
            List<Cell> rg = new List<Cell>();
            for (int i = 0; i < pts.Length; i++) {
                Cell r = new Cell(new Vector3((float)pts[i][0], 0, (float)pts[i][1]));
                r.isLake = rand.Next(0, 25) == 3;
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
                LoadProgress = i / rg.Count;
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
                    rg.RemoveAt(i);
                    i--;
                }
            }

            BoundingBox areaBox = new BoundingBox(new Vector3(-RealWidth * .5f, 0, -RealLength * .5f), new Vector3(RealWidth * .5f, 500, RealLength * .5f));
            // clamp verticies to be in the bounds of the map
            foreach (Cell cell in rg) {
                for (int i = 0; i < cell.Verticies.Count; i++) {
                    if (!Contains(cell.Verticies[i])) {
                        cell.isLake = false;
                        Vector3 a = cell.Verticies[i];
                        Vector3 prev = cell.Verticies[i - 1 >= 0 ? i - 1 : cell.Verticies.Count - 1];
                        Vector3 next = cell.Verticies[(i + 1) % cell.Verticies.Count];
                        if (Contains(prev) && Contains(next)) {
                            // surrounding 2 verticies are inside; move this vertex and add another to make an edge along the boundary
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
                                cell.Verticies.RemoveAt(i);
                                i--;
                            }
                            if (d2 != null)
                                cell.Verticies.Insert(i + 1, a + m2 * d2.Value);
                            i++;

                        } else {
                            // only 1 of the surrounding verticies is inside; just move this one to the edge along boundary
                            Vector3 b = Contains(prev) ? prev : next;
                            Vector3 m = b - a;
                            m.Normalize();
                            Ray r = new Ray(a, m);
                            float? d = r.Intersects(areaBox);
                            if (d != null) 
                                cell.Verticies[i] = a + m * d.Value;
                            else {
                                cell.Verticies.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }

            // remove non-lake and non-edge cells to speed up searching later on
            // as they aren't used for anything else
            for (int i = 0; i < rg.Count; i++) {
                if (!rg[i].isEdge && !rg[i].isLake) {
                    rg.RemoveAt(i);
                    i--;
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
            
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            #region generate heightmap
            LoadMessage = "Generating heightmap...";
            heightMap = new float[RealWidth + 1, RealLength + 1];
            heightMapScale = 1;
            colorMap = new uint[RealWidth + 1, RealLength + 1];
            Color groundColor = new Color(.2f, .4f, .3f);
            float li = 0;
            for (int x = 0; x < RealWidth + 1; x++) {
                for (int z = 0; z < RealLength + 1; z++) {
                    heightMap[x, z] = generateHeight(x * heightMapScale, z * heightMapScale);
                    colorMap[x, z] = groundColor.PackedValue;

                    Cell c = CellAt(x, z);
                    if (c != null && c.isLake) {
                        // shrink the lake to try to avoid colors going everywhere
                        Vector3[] verts = c.Verticies.ToArray();
                        for (int i = 0; i < verts.Length; i++) {
                            Vector3 d = verts[i] - c.Center;
                            d.Normalize();
                            verts[i] += d * 10;
                        }
                        Cell cell = new Cell(c.Point);
                        cell.Verticies = new List<Vector3>(verts);
                        if (cell.Contains(new Vector3(x, 0, z)))
                            colorMap[x, z] = new Color(1f, .92f, .8f).PackedValue;
                    }

                    li++;
                    LoadProgress = li / (RealWidth * RealLength);
                }
            }
            Action<int, int, Color> paintHeightmap = (int x, int y, Color color) => {
                colorMap[x, y] = color.PackedValue;
                colorMap[x - 1, y] = color.PackedValue;
                colorMap[x + 1, y] = color.PackedValue;
                colorMap[x, y - 1] = color.PackedValue;
                colorMap[x, y + 1] = color.PackedValue;
                colorMap[x - 1, y - 1] = color.PackedValue;
                colorMap[x - 1, y + 1] = color.PackedValue;
                colorMap[x + 1, y - 1] = color.PackedValue;
                colorMap[x + 1, y + 1] = color.PackedValue;
            };
            smoothHeights();
            smoothHeights();
            #endregion
            timer.Stop();
            Debug.Log("Generated " + RealWidth * RealLength + " heights in " + Math.Round(timer.Elapsed.TotalSeconds, 2) + " seconds");

            #region generate settlements
            LoadMessage = "Generating settlements";
            LoadProgress = 0;

            int townCount = rand.Next(2, 5);
            for (int i = 0; i < townCount; i++) {
                // pick a cell to be the city center
                Vector3 center = new Vector3(rand.Next(100, RealWidth - 100), 0, rand.Next(100, RealLength - 100));
                Cell close = ClosestCell(center);
                float d = Vector3.DistanceSquared(close.Center, center);
                while (d < 300 * 300) {
                    center = new Vector3(rand.Next(100, RealWidth - 100), 0, rand.Next(100, RealLength - 100));
                    close = ClosestCell(center);
                    d = Vector3.DistanceSquared(close.Center, center);
                }
                
                float townheight = HeightAt(center);
                
                Town town = new Town(this, new Vector3(center.X, townheight, center.Z), rand.Next());
                town.MakeRoads();
                town.MakeBuildings();
                bool flag = false;
                foreach (Town t in Towns)
                    if (t.bbox.Intersects(town.bbox))
                        flag = true;
                if (!flag)
                    Towns.Add(town);

                LoadProgress = i / (float)townCount;
            }
            
            #region flatten terrain under buildings
            foreach (Town t in Towns)
                foreach (Structure st in t.Structures)
                    FlattenUnder(st);
            smoothHeights();
            smoothHeights();
            foreach (Town t in Towns)
                foreach (Structure st in t.Structures)
                    RaiseTo(st);
            #endregion

            LoadProgress = 0;
            li = 0;
            foreach (Town t in Towns) {
                t.MakeRoadGeometry(device);
                t.makeBuildingGeometry(device);
                t.createResidents();

                LoadProgress = li / Towns.Count;
                li++;
            }
            #endregion

            timer.Restart();
            int vertexCount = 0;
            int triCount = 0;
            #region generate chunks
            LoadMessage = "Generating world geometry...";
            LoadProgress = 0;
            li = 0;
            for (int cx = 0; cx < Width; cx++) {
                for (int cz = 0; cz < Length; cz++) {
                    int crx = cx * Chunk.ChunkSize;
                    int crz = cz * Chunk.ChunkSize;
                    Chunks[cx, cz] = new Chunk(cx, cz, this);
                    Chunks[cx, cz].bBox = new BoundingBox(new Vector3(crx, 0, crz), new Vector3(crx + Chunk.ChunkSize, 100, crz + Chunk.ChunkSize));

                    #region chunk geometry
                    List<VertexPositionColorNormal> verts = new List<VertexPositionColorNormal>();
                    List<int> inds = new List<int>();
                    // verticies
                    for (int x = 0; x < Chunk.ChunkSize + 1; x++) {
                        for (int z = 0; z < Chunk.ChunkSize + 1; z++) {
                            Vector3 v1 = new Vector3(crx + x * heightMapScale, heightMap[crx + x, crz + z], crz + z * heightMapScale);

                            Vector3 left = Vector3.Zero, right = Vector3.Zero, forward = Vector3.Zero, backward = Vector3.Zero;
                            if (crx + x > 0)
                                left = new Vector3(crx + (x - 1) * heightMapScale, heightMap[crx + x - 1, crz + z], crz + z * heightMapScale) - v1;
                            if (crx + x < RealWidth - 1)
                                right = new Vector3(crx + (x + 1) * heightMapScale, heightMap[crx + x + 1, crz + z], crz + z * heightMapScale) - v1;
                            if (crz + z > 0)
                                forward = new Vector3(crx + x * heightMapScale, heightMap[crx + x, crz + z - 1], crz + (z - 1) * heightMapScale) - v1;
                            if (crz + z < RealLength - 1)
                                backward = new Vector3(crx + x * heightMapScale, heightMap[crx + x, crz + z + 1], crz + (z + 1) * heightMapScale) - v1;

                            left.Normalize(); right.Normalize(); forward.Normalize(); backward.Normalize();
                            Vector3 n =
                                ((left != Vector3.Zero && forward != Vector3.Zero) ? -Vector3.Cross(left, forward) : Vector3.Zero) +
                                ((right != Vector3.Zero && forward != Vector3.Zero) ? -Vector3.Cross(forward, right) : Vector3.Zero) +
                                ((right != Vector3.Zero && backward != Vector3.Zero) ? -Vector3.Cross(right, backward) : Vector3.Zero) +
                                ((left != Vector3.Zero && backward != Vector3.Zero) ? -Vector3.Cross(backward, left) : Vector3.Zero);
                            n.Normalize();
                            verts.Add(new VertexPositionColorNormal(v1, new Color() { PackedValue = colorMap[crx + x, crz + z] }, n));
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
                    vertexCount += verts.Count;
                    triCount += inds.Count / 3;

                    Chunks[cx, cz].VertexBuffer = new VertexBuffer(device, typeof(VertexPositionColorNormal), verts.Count, BufferUsage.WriteOnly);
                    Chunks[cx, cz].VertexBuffer.SetData(verts.ToArray());
                    Chunks[cx, cz].IndexBuffer = new IndexBuffer(device, typeof(int), inds.Count, BufferUsage.WriteOnly);
                    Chunks[cx, cz].IndexBuffer.SetData(inds.ToArray());
                    #endregion

                    #region chunk foiliage
                        Chunks[cx, cz].treeBodies = new List<RigidBody>();
                        Chunks[cx, cz].Grass = new List<Matrix>[Models.GrassModels.Length];
                        Chunks[cx, cz].Trees = new List<Matrix>[Models.TreeModels.Length];
                        for (int i = 0; i < Chunks[cx, cz].Grass.Length; i++) Chunks[cx, cz].Grass[i] = new List<Matrix>();
                        for (int i = 0; i < Chunks[cx, cz].Trees.Length; i++) Chunks[cx, cz].Trees[i] = new List<Matrix>();

                        // generate grass
                        for (int x = cx * Chunk.ChunkSize; x < (cx + 1) * Chunk.ChunkSize; x+=2) {
                            for (int z = cz * Chunk.ChunkSize; z < (cz + 1) * Chunk.ChunkSize; z+=2) {
                                Vector3 pt = new Vector3(x, 0, z) + new Vector3((float)rand.NextDouble() - .5f, 0, (float)rand.NextDouble() - .5f) * 1.5f;
                                if (Contains(pt) && CellAt(pt) == null &&
                                    colorMap[(int)pt.X, (int)pt.Z] == groundColor.PackedValue &&
                                    colorMap[(int)pt.X+1, (int)pt.Z] == groundColor.PackedValue &&
                                    colorMap[(int)pt.X, (int)pt.Z+1] == groundColor.PackedValue) {
                                    Vector3 n;
                                    pt.Y = HeightAt(pt, out n);

                                    BoundingSphere psphere = new BoundingSphere(pt, 1);
                                    if (n.Y > .7f && Obstructed(psphere) == ObstructionType.None) {
                                        Chunks[cx, cz].Grass[rand.Next(0, Chunks[cx, cz].Grass.Length)].Add(
                                            Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi) *
                                            Matrix.CreateTranslation(pt)
                                            );
                                    }
                                }
                            }
                        }
                    
                        // generate trees
                        for (int x = cx * Chunk.ChunkSize; x < (cx + 1) * Chunk.ChunkSize; x+=10) {
                            for (int z = cz * Chunk.ChunkSize; z < (cz + 1) * Chunk.ChunkSize; z+=10) {
                                Vector3 pt = new Vector3(x, 0, z) + new Vector3((int)((rand.NextDouble() - .5f) * 10), 0, (int)((rand.NextDouble() - .5f) * 10)); // round off to int so that navmeshes work good
                                pt.X = MathHelper.Clamp(pt.X, 0, RealWidth - 1);
                                pt.Z = MathHelper.Clamp(pt.Z, 0, RealLength - 1);
                                if (Contains(pt) && CellAt(pt) == null &&
                                    colorMap[(int)pt.X, (int)pt.Z] == groundColor.PackedValue &&
                                    colorMap[(int)pt.X + 1, (int)pt.Z] == groundColor.PackedValue &&
                                    colorMap[(int)pt.X, (int)pt.Z + 1] == groundColor.PackedValue) {
                                    Vector3 n;
                                    pt.Y = HeightAt(pt, out n);

                                    BoundingSphere psphere = new BoundingSphere(pt, 1);
                                    if (n.Y > .7f && Obstructed(psphere) == ObstructionType.None && Chunks[cx, cz].bBox.Contains(pt) == ContainmentType.Contains) {
                                        int c = rand.Next(0, Chunks[cx, cz].Trees.Length);
                                        Matrix m = Matrix.CreateRotationY((float)rand.NextDouble() * MathHelper.TwoPi) * Matrix.CreateTranslation(pt);
                                        Chunks[cx, cz].Trees[c].Add(m);

                                        RigidBody b = new RigidBody(new CylinderShape(7, .8f));
                                        b.Tag = new { c, m };
                                        b.Position = new JVector(pt.X, pt.Y + 2.5f, pt.Z);
                                        b.IsStatic = true;
                                        Physics.AddBody(b);
                                        Chunks[cx, cz].treeBodies.Add(b);
                                    }
                                }

                            }
                        }
                        #endregion
                    li++;
                    LoadProgress = li / (Width * Length);
                }
            }
            #endregion
            timer.Stop();
            Debug.Log("Generated " + vertexCount + " verticies (" + triCount + " triangles) in " + Math.Round(timer.Elapsed.TotalSeconds, 2) + " seconds");

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
            TerrainBody.Material.StaticFriction = 2;
            Physics.AddBody(TerrainBody);
            #endregion

            #region generate navmesh
            LoadMessage = "Generating navigatable mesh...";
            timer.Restart();
            PathSystem = new PathSystem(this);
            PathSystem.BuildGraph();
            timer.Stop();
            Debug.Log("Generated navmesh in " + Math.Round(timer.Elapsed.TotalSeconds, 2) + " seconds");
            #endregion

            LoadProgress = 2; // signifies that it's done
        }
        #endregion
    }
}
