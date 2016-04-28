using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Procedural_Story.Core.Life;

namespace Procedural_Story.Core {
    class Path {
        public Vector3[] Points;

        public Vector3 this[int i]
        {
            get { return Points[i]; }
        }

        public Path(params Vector3[] pts) {
            Points = pts;
        }
        
        public void DebugDraw(GraphicsDevice device) {
            VertexPositionColor[] verts = new VertexPositionColor[Points.Length];
            for (int i = 0; i < Points.Length; i++)
                verts[i] = new VertexPositionColor(Points[i] + Vector3.Up, Color.Red);
            Util.Models.WorldEffect.Parameters["Textured"].SetValue(false);
            Util.Models.WorldEffect.CurrentTechnique = Util.Models.WorldEffect.Techniques["Debug"];
            Util.Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
            foreach (EffectPass p in Util.Models.WorldEffect.CurrentTechnique.Passes) {
                p.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineStrip, verts, 0, Points.Length - 1);
            }
        }
    }
    class NavSurface {
        public struct Node {
            public int Index;
            public Vector3 Position;
            public List<int> Connections;

            public Node(Vector3 pos, int i) {
                Position = pos;
                Index = i;
                Connections = new List<int>();
            }
            public Node(Vector3 pos, int i, params int[] connections) {
                Position = pos;
                Index = i;
                Connections = new List<int>(connections);
            }
        }

        public int Width { get; private set; }
        public int Length { get; private set; }
        public List<Node> Graph { get; private set; }
        /// <summary>
        /// index of a vertex at [x, z], or -1 if none exist
        /// </summary>
        public int[,] graphCache { get; private set; }

        public NavSurface() {
            Graph = new List<Node>();
        }

        public void Generate(Area area) {
            Width = area.RealWidth;
            Length = area.RealLength;

            Graph = new List<Node>();

            float max = area.RealWidth * area.RealLength;
            // Make graph on open terrain
            graphCache = new int[area.RealWidth, area.RealLength];
            for (int x = 0; x < area.RealWidth; x++)
                for (int z = 0; z < area.RealLength; z++) {
                    Vector3 n;
                    Vector3 p = new Vector3(x, area.HeightAt(x, z, out n) + .1f, z);
                    Cell c = area.CellAt(p);
                    if (n.Y > .7f &&
                        area.Obstructed(new BoundingSphere(p, .35f)) != ObstructionType.Physical &&
                        !(c != null && (c.isLake || c.isEdge))) {
                        graphCache[x, z] = Graph.Count;
                        Graph.Add(new Node(p, graphCache[x, z]));
                    } else
                        graphCache[x, z] = -1;
                    area.LoadProgress = .9f * (z + x * area.RealWidth) / max;
                }

            // connect nodes
            for (int x = 0; x < area.RealWidth - 1; x++) {
                for (int z = 0; z < area.RealLength - 1; z++) {
                    // graph connections
                    if (graphCache[x, z] != -1) {
                        if (graphCache[x + 1, z] != -1) {
                            Graph[graphCache[x, z]].Connections.Add(graphCache[x + 1, z]);
                            Graph[graphCache[x + 1, z]].Connections.Add(graphCache[x, z]);
                        }
                        if (graphCache[x, z + 1] != -1) {
                            Graph[graphCache[x, z]].Connections.Add(graphCache[x, z + 1]);
                            Graph[graphCache[x, z + 1]].Connections.Add(graphCache[x, z]);
                        }

                        if (graphCache[x + 1, z] != -1 && graphCache[x, z + 1] != -1 && graphCache[x + 1, z + 1] != -1) { // if both verticies are connected, a square is formed;
                            Graph[graphCache[x, z]].Connections.Add(graphCache[x + 1, z + 1]); // add a diagonal connection
                            Graph[graphCache[x + 1, z + 1]].Connections.Add(graphCache[x, z]);

                            Graph[graphCache[x + 1, z]].Connections.Add(graphCache[x, z + 1]); // add a diagonal connection
                            Graph[graphCache[x, z + 1]].Connections.Add(graphCache[x + 1, z]);
                        }
                    }
                    area.LoadProgress = .9f + .1f * (z + x * area.RealWidth) / max;
                }
            }
            
            Debug.Log("NAVMESH:" + Graph.Count + " verticies");
        }
        
        public Node FindNearest(Vector3 pt) {
            int sx = (int)MathHelper.Clamp(pt.X, 0, Width - 1);
            int sz = (int)MathHelper.Clamp(pt.Z, 0, Length - 1);
            if (graphCache[sx, sz] == -1) {
                int radius = 10;
                bool found = false;
                float small = float.MaxValue;
                Node close = new Node();
                // keep increasing radius until we find the closest node
                while (!found) {
                    for (int x = Math.Max(sx - radius, 0); x < Math.Min(sx + radius, Width-1); x++) {
                        for (int z = Math.Max(sz - radius, 0); z < Math.Min(sz + radius, Length-1); z++) {
                            if (graphCache[x, z] != -1) {
                                float d = Vector3.DistanceSquared(Graph[graphCache[x, z]].Position, pt);
                                if (d < small) {
                                    small = d;
                                    close = Graph[graphCache[x, z]];
                                    found = true;
                                }
                            }
                        }
                    }
                    radius += 10;
                }

                return close;
            } else
                return Graph[graphCache[sx, sz]];
        }

        const int vbuffersize = 16384;
        VertexBuffer[] vbuffers;
        public void DebugDraw(GraphicsDevice device) {
            if (vbuffers == null) {
                List<VertexPositionColor> vts = new List<VertexPositionColor>();
                for (int i = 0; i < Graph.Count; i++) {
                    for (int j = 0; j < Graph[i].Connections.Count; j++) {
                        vts.Add(new VertexPositionColor(Graph[i].Position, Color.Blue));
                        vts.Add(new VertexPositionColor(Graph[Graph[i].Connections[j]].Position, Color.Blue));
                    }
                }

                int total = 0;
                VertexPositionColor[] vArray = vts.ToArray();
                vbuffers = new VertexBuffer[vts.Count / vbuffersize + Math.Sign(vts.Count % vbuffersize)];
                for (int i = 0; i < vbuffers.Length; i++) {
                    int size = i == vbuffers.Length - 1 ? vts.Count % vbuffersize : vbuffersize;
                    vbuffers[i] = new VertexBuffer(device, typeof(VertexPositionColor), size, BufferUsage.WriteOnly);
                    vbuffers[i].SetData(vArray, i * vbuffersize, size);
                    total += size;
                }
            }
            
            Util.Models.WorldEffect.CurrentTechnique = Util.Models.WorldEffect.Techniques["Debug"];
            Util.Models.WorldEffect.Parameters["World"].SetValue(Matrix.Identity);
            Util.Models.WorldEffect.Parameters["Textured"].SetValue(false);
            Util.Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
            foreach (EffectPass p in Util.Models.WorldEffect.CurrentTechnique.Passes) {
                p.Apply();

                // draw in steps b/c u cant draw a lot of primitives, rip
                for (int i = 0; i < vbuffers.Length; i++) {
                    device.SetVertexBuffer(vbuffers[i]);
                    device.DrawPrimitives(PrimitiveType.LineList, 0, vbuffers[i].VertexCount);
                }
            }
        }
    }
    class PathSystem {
        Area area;

        public NavSurface surface { get; private set; }

        List<Path> debugPaths;

        public PathSystem(Area area) {
            this.area = area;
            surface = new NavSurface();
        }

        class pNode {
            public pNode prev;
            public int index;

            public pNode(pNode prev, int index) {
                this.prev = prev;
                this.index = index;
            }
        }
        Path traceBack(Vector3 a, Vector3 b, pNode node) {
            List<Vector3> p = new List<Vector3>();
            p.Add(b);
            while (node.prev != null) {
                p.Add(surface.Graph[node.index].Position);
                node = node.prev;
            }
            p.Reverse();
            return new Path(p.ToArray());
        }
        public Path GetPath(Vector3 a, Vector3 b) {
            debugPaths = new List<Path>();

            NavSurface.Node start = surface.FindNearest(a);
            NavSurface.Node end = surface.FindNearest(b);
            bool[] searched = new bool[surface.Graph.Count];
            List<pNode> cur = new List<pNode>(); // current search "leads"
            cur.Add(new pNode(null, start.Index));
            bool found = false;
            while (!found) {
                List<pNode> l = new List<pNode>();
                foreach (pNode n in cur) {
                    foreach (int i in surface.Graph[n.index].Connections) {
                        if (!searched[i]) {
                            searched[i] = true;
                            if (end.Index == i) {
                                found = true;
                                // trace back
                                Path p = traceBack(a, b, new pNode(n, i));
                                debugPaths.Add(p);
                                return p;
                            }
                            l.Add(new pNode(n, i));
                        }
                    }
                }
                cur = l;
            }
            
            return new Path(a, start.Position, end.Position, b);
        }

        public void BuildGraph() {
            surface.Generate(area);
        }

        public void DebugDraw(GraphicsDevice device) {
            Util.Models.WorldEffect.Parameters["World"].SetValue(Matrix.Identity);
            if (debugPaths != null) {
                foreach (Path p in debugPaths)
                    p.DebugDraw(device);
            }
            surface?.DebugDraw(device);
        }
    }
}
