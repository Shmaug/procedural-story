using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace Procedural_Story.World {
    enum Biome {
        Desert,
        Forest,
        Plains
    }

    class Light {
        public Color Color;
        public Vector2 Position;
        public float Radius;
        
        public Light (Color c, Vector2 p, float r) {
            Color = c;
            Position = p;
            Radius = r;
        }
    }

    struct Edge {
        public Vector2 a;
        public Vector2 b;
        public Edge(Vector2 a, Vector2 b) { this.a = a;  this.b = b;  }
    }
    class Cell {
        public Vector2 Point;
        public Vector2 Center;
        public List<Vector2> Verticies;
        public List<Edge> Edges;
        public Vector2 Min;
        public Vector2 Max;

        public Cell(Vector2 pt) {
            Point = pt;
            Verticies = new List<Vector2>();
            Edges = new List<Edge>();
        }
    }

    class Area {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Light[,] Lights;
        public Tile[,] Tiles;
        public int Seed { get; private set; }
        public Biome Biome { get; private set; }
        public float LoadProgress { get; private set; }
        Cell[] VoronoiCells;
        Random rand;

        BenTools.Mathematics.VoronoiGraph graph;

        public Area(Biome biome, int seed) {
            Seed = seed;
            Biome = biome;
        }

        public Cell GetCellFromPos(Vector2 pos) {
            float dist = float.MaxValue;
            int cin = -1;
            for (int i = 0; i < VoronoiCells.Length; i++) {
                float d = Vector2.DistanceSquared(pos, VoronoiCells[i].Point);
                if (d < dist) {
                    dist = d;
                    cin = i;
                }
            }
            if (cin == -1)
                return null;
            return VoronoiCells[cin];
        }

        public void DrawVoronoi(SpriteBatch batch) {
            Cell cIn = GetCellFromPos(Camera.CurrentCamera.Unproject(new Vector2(Input.ms.X, Input.ms.Y)));
            for (int i = 0; i < VoronoiCells.Length; i++) {
                for (int j = 0; j < VoronoiCells[i].Edges.Count; j++)
                    batch.Draw(UI.UIElement.BlankTexture,
                        new Rectangle(
                            (int)VoronoiCells[i].Edges[j].a.X,
                            (int)VoronoiCells[i].Edges[j].a.Y,
                            (int)Vector2.Distance(VoronoiCells[i].Edges[j].a, VoronoiCells[i].Edges[j].b),
                            (int)((cIn == VoronoiCells[i] ? 4 : 1) / Camera.CurrentCamera.Scale)),
                        null, cIn == VoronoiCells[i] ? Color.Blue : Color.Red,
                        (float)Math.Atan2(VoronoiCells[i].Edges[j].b.Y - VoronoiCells[i].Edges[j].a.Y, VoronoiCells[i].Edges[j].b.X - VoronoiCells[i].Edges[j].a.X),
                        Vector2.Zero, SpriteEffects.None, 1 / Camera.CurrentCamera.Scale);

                if (cIn == VoronoiCells[i]) {
                    batch.Draw(UI.UIElement.BlankTexture,
                        new Rectangle(
                            (int)VoronoiCells[i].Min.X,
                            (int)VoronoiCells[i].Min.Y,
                            (int)(VoronoiCells[i].Max.X - VoronoiCells[i].Min.X),
                            (int)(VoronoiCells[i].Max.Y - VoronoiCells[i].Min.Y)),
                        null, Color.White * .25f, 0, Vector2.Zero, SpriteEffects.None, 1 / Camera.CurrentCamera.Scale);
                }
            }
        }

        public void DrawBackground(SpriteBatch batch) {
            float b4 = Camera.CurrentCamera.Scale;

            int sx = Math.Max((int)Camera.CurrentCamera.Unproject(Vector2.Zero).X / Tile.TILE_SIZE - 10, 0);
            int sy = Math.Max((int)Camera.CurrentCamera.Unproject(Vector2.Zero).Y / Tile.TILE_SIZE - 10, 0);
            int ex = Math.Min((int)Camera.CurrentCamera.Unproject(new Vector2(UI.UIElement.ScreenWidth, UI.UIElement.ScreenHeight)).X / Tile.TILE_SIZE + 10, Width - 1);
            int ey = Math.Min((int)Camera.CurrentCamera.Unproject(new Vector2(UI.UIElement.ScreenWidth, UI.UIElement.ScreenHeight)).Y / Tile.TILE_SIZE + 10, Height - 1);
            
            for (int x = sx; x < ex; x++)
                for (int y = sy; y < ey; y++)
                    if (Tiles[x, y] != null)
                        Tiles[x, y].DrawBackground(batch, Camera.CurrentCamera);
        }

        public Light[] visibleLights;
        public void DrawHulls(SpriteBatch batch) {
            visibleLights = new Light[16];
            
            int sx = Math.Max((int)Camera.CurrentCamera.Unproject(Vector2.Zero).X / Tile.TILE_SIZE - 10, 0);
            int sy = Math.Max((int)Camera.CurrentCamera.Unproject(Vector2.Zero).Y / Tile.TILE_SIZE - 10, 0);
            int ex = Math.Min((int)Camera.CurrentCamera.Unproject(new Vector2(UI.UIElement.ScreenWidth, UI.UIElement.ScreenHeight)).X / Tile.TILE_SIZE + 10, Width - 1);
            int ey = Math.Min((int)Camera.CurrentCamera.Unproject(new Vector2(UI.UIElement.ScreenWidth, UI.UIElement.ScreenHeight)).Y / Tile.TILE_SIZE + 10, Height - 1);
            
            for (int x = sx; x < ex; x++)
                for (int y = sy; y < ey; y++) {
                    if (Tiles[x, y] != null && Tiles[x, y].HasHull) {
                        Tiles[x, y].DrawHull(batch, Camera.CurrentCamera);
                    }
                    if (Lights[x, y] != null)
                        for (int i = 0; i < visibleLights.Length; i++)
                            if (visibleLights[i] == null) {
                                visibleLights[i] = Lights[x, y];
                                break;
                            }
                }
        }

        public void Generate() {
            Width = 512;
            Height = 512;
            Tiles = new Tile[Width, Height];
            Lights = new Light[Width, Height];

            switch (Biome) {
                case Biome.Desert:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(genDesert));
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
            BenTools.Mathematics.Vector[] pts = new BenTools.Mathematics.Vector[100];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new BenTools.Mathematics.Vector(rand.Next(0, Width * Tile.TILE_SIZE), rand.Next(0, Height * Tile.TILE_SIZE));
            graph = BenTools.Mathematics.Fortune.ComputeVoronoiGraph(pts);

            BenTools.Mathematics.Vector[] verts = graph.Vertizes.ToArray<BenTools.Mathematics.Vector>();
            BenTools.Mathematics.VoronoiEdge[] edges = graph.Edges.ToArray<BenTools.Mathematics.VoronoiEdge>();

            // compute cells
            VoronoiCells = new Cell[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                VoronoiCells[i] = new Cell(new Vector2((float)pts[i][0], (float)pts[i][1]));

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
                    if (d <= least + 1f) { // add some slop
                        VoronoiCells[j].Verticies.Add(p);
                    }
                }
            }

            // make all the cells' verticies counter-clockwise
            for (int i = 0; i < VoronoiCells.Length; i++) {
                LoadProgress = .9f * i / VoronoiCells.Length;
                Cell c = VoronoiCells[i];
                c.Center = Vector2.Zero;
                foreach (Vector2 p in c.Verticies)
                    c.Center += p;
                c.Center /= c.Verticies.Count;

                bool swap = true;
                while (swap) {
                    swap = false;
                    for (int j = 0; j < c.Verticies.Count - 1; j++) {
                        int j2 = j + 1;
                        if (less(c.Verticies[j], c.Verticies[j2], c.Center)){
                            swap = true;
                            Vector2 t = c.Verticies[j];
                            c.Verticies[j] = c.Verticies[j2];
                            c.Verticies[j2] = t;
                        }
                    }
                }
            }

            for (int i = 0; i < VoronoiCells.Length; i++) {
                VoronoiCells[i].Max = Vector2.Zero;
                VoronoiCells[i].Min = new Vector2(float.MaxValue, float.MaxValue);
                for (int j = 0; j < VoronoiCells[i].Verticies.Count; j++) {
                    if (VoronoiCells[i].Verticies[j].X > VoronoiCells[i].Max.X)
                        VoronoiCells[i].Max.X = VoronoiCells[i].Verticies[j].X;
                    if (VoronoiCells[i].Verticies[j].Y > VoronoiCells[i].Max.Y)
                        VoronoiCells[i].Max.Y = VoronoiCells[i].Verticies[j].Y;

                    if (VoronoiCells[i].Verticies[j].X < VoronoiCells[i].Min.X)
                        VoronoiCells[i].Min.X = VoronoiCells[i].Verticies[j].X;
                    if (VoronoiCells[i].Verticies[j].Y < VoronoiCells[i].Min.Y)
                        VoronoiCells[i].Min.Y = VoronoiCells[i].Verticies[j].Y;

                    VoronoiCells[i].Edges.Add(new Edge(VoronoiCells[i].Verticies[j],
                        VoronoiCells[i].Verticies[j + 1 < VoronoiCells[i].Verticies.Count ? j + 1 : 0]));
                }
            }
        }

        void genDesert(object state = null) {
            rand = new Random(Seed);
            genVoronoi();
            
            float loadcount = 0;
            float total = Width * Height;
            
            // Generate tiles
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tiles[x, y] = new Tile(TileType.DesertGrass, new Point(x * Tile.TILE_SIZE, y * Tile.TILE_SIZE), this);
                    double t = rand.NextDouble();
                    if (t < .5) { // 50% chance of grass
                        Tiles[x, y].HasHull = true;
                        int gt = rand.Next(0, 7);
                        if (gt < 4) {
                            Tiles[x, y].HullSource = new Rectangle(64, 160 + 32 * gt, 32, 32);
                            Tiles[x, y].HullOffset = new Vector2(0, -16);
                            Tiles[x, y].HullZOffset = gt == 2 ? -13 : 0;
                        } else if (gt == 4) {
                            Tiles[x, y].HullSource = new Rectangle(97, 167, 25, 51);
                            Tiles[x, y].HullOffset = new Vector2(0, 0);
                            Tiles[x, y].HullZOffset = 0;
                        } else if (gt == 5) {
                            Tiles[x, y].HullSource = new Rectangle(103, 236, 25, 51);
                            Tiles[x, y].HullOffset = new Vector2(0, 0);
                            Tiles[x, y].HullZOffset = 0;
                        } else if (gt == 6) {
                            Tiles[x, y].HullSource = new Rectangle(3, 172, 55, 51);
                            Tiles[x, y].HullOffset = new Vector2(0, 0);
                            Tiles[x, y].HullZOffset = -28;
                        }
                    } else if (t < .51) { // 1% chance of tree or log
                        if (rand.NextDouble() < .5) {
                            Tiles[x, y].HasHull = true;
                            Tiles[x, y].HullSource = new Rectangle(0, 352, 254, 276);
                            Tiles[x, y].HullOffset = new Vector2(-120, -24);
                            Tiles[x, y].HullZOffset = -10;
                        } else {
                            Tiles[x, y].HasHull = true;
                            Tiles[x, y].HullSource = new Rectangle(96, 0, 100, 72);
                            Tiles[x, y].HullOffset = new Vector2(50, 0);
                            Tiles[x, y].HullZOffset = 0;
                        }
                    }
                    Tiles[x, y].HullZOffset += 5f * x / Width; // to eliminiate flickering
                    loadcount++;
                    LoadProgress = .9f * loadcount / total;
                }
            }

            for (int i = 0; i < VoronoiCells.Length; i++) {
                double t = rand.NextDouble();
                if (t < .15f) {
                } else if (t < .3) { // 15% chance of ?

                } else if (t < .5) { // 15% chance of ?

                } else { // 50% chance of ?

                }
            }

            LoadProgress = 1;
        }
    }
}
