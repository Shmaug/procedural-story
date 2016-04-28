using System;
using System.Collections.Generic;

using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Procedural_Story.Core.Crops;
using System.Threading;

namespace Procedural_Story.Core.Structures {
    class Farm : Structure {
        public int Width { get; private set; }
        public int Length { get; private set; }

        Random rand;

        public List<Crop> Crops;
        List<int> growing;

        public Farm(Vector3 pos, Area a,  int seed) : base(a, pos) {
            rand = new Random(seed);
            
            Crops = new List<Crop>();
            growing = new List<int>();

            Width = rand.Next(12, 15);
            Length = rand.Next(12, 15);
        }

        public void AddCrop(Crop c) {
            growing.Add(Crops.Count);
            Crops.Add(c);
        }

        public override void PreGenerate() {
            bbox = new BoundingBox(Position - new Vector3(Width, 2, Length) * .5f, Position + new Vector3(Width, 2, Length) * .5f);
            innerbbox = new BoundingBox(bbox.Min + new Vector3(.35f, 0, .35f), bbox.Max - new Vector3(.35f, 0, .35f));
            entrancebbox = new BoundingBox(Position + new Vector3(-1, -1, -Length * .5f - 3), Position + new Vector3(1, 1, 0));
            
        }

        void addFencePost(Vector3 p) {
            addBox(new Color(.47f, .36f, .21f), new BoundingBox(p - new Vector3(.1f, 0, .1f), p + new Vector3(.1f, 1, .1f)));
        }
        public override void BuildGeometry(GraphicsDevice device) {
            addBox(new Color(65, 38, 20), new BoundingBox(new Vector3(-Width * .5f + .05f, -1, -Length * .5f + .05f), new Vector3(Width * .5f - .05f, 0.01f, Length * .5f - .05f)));
            addBox(new Color(.2f, .4f, .3f), new BoundingBox(new Vector3(-Width * .5f, -1, -Length * .5f), new Vector3(Width * .5f, 0, Length * .5f)));

            for (float x = -Width * .5f + 1; x < Width * .5f - 1; x++)
                for (float z = -Length * .5f + 1; z < Length * .5f - 1; z += 3) {
                    Crop c = new CornCrop(rand.Next(), Position + Vector3.Transform(new Vector3(x, 0, z), Orientation), area);
                    c.TimeLeft = 0;
                    Crops.Add(c);
                    ThreadPool.QueueUserWorkItem(new WaitCallback((object d) => { c.UpdateGeometry(d as GraphicsDevice); }), device);
                }

            // corners
            addFencePost(new Vector3(-Width * .5f + .2f, 0, -Length * .5f + .2f));
            addFencePost(new Vector3( Width * .5f - .2f, 0, -Length * .5f + .2f));
            addFencePost(new Vector3(-Width * .5f + .2f, 0,  Length * .5f - .2f));
            addFencePost(new Vector3( Width * .5f - .2f, 0,  Length * .5f - .2f));

            // midpoints
            addFencePost(new Vector3( Width * .5f - .2f, 0, 0));
            addFencePost(new Vector3(-Width * .5f + .2f, 0, 0));
            addFencePost(new Vector3(0, 0, Length * .5f - .2f));

            // entrance posts
            addFencePost(new Vector3(-1, 0, -Length * .5f + .2f));
            addFencePost(new Vector3( 1, 0, -Length * .5f + .2f));

            // fence bars
            float y = .8f;
            Action b = new Action(() => {
                Vector3 c = new Vector3(-Width * .5f + .2f, y, 0),
                    o = new Vector3(.05f, .1f, Length * .5f - .2f);
                addBox(new Color(.47f, .36f, .21f), new BoundingBox(c - o, c + o));

                c = new Vector3(Width * .5f - .2f, y, 0);
                addBox(new Color(.47f, .36f, .21f), new BoundingBox(c - o, c + o));

                c = new Vector3(0, y, Length * .5f - .2f);
                o = new Vector3(Width * .5f - .2f, .1f, .05f);
                addBox(new Color(.47f, .36f, .21f), new BoundingBox(c - o, c + o));

                c = new Vector3(-Width * .25f - .4f, y, -Length * .5f + .2f);
                o = new Vector3(Width * .25f - .6f, .1f, .05f);
                addBox(new Color(.47f, .36f, .21f), new BoundingBox(c - o, c + o));

                c = new Vector3(Width * .25f + .4f, y, -Length * .5f + .2f);
                addBox(new Color(.47f, .36f, .21f), new BoundingBox(c - o, c + o));
            });
            b();
            y = .3f;
            b();

            RigidBody groundBody = new RigidBody(new BoxShape(Width, 1f, Length));
            groundBody.Position = new JVector(0, -.5f, 0f);
            groundBody.Tag = this;
            RigidBodies.Add(groundBody);

            RigidBody f1 = new RigidBody(new BoxShape(Width - .2f, 1f, .2f));
            f1.Position = new JVector(0, .5f, Length * .5f - .2f);
            RigidBodies.Add(f1);

            RigidBody f2 = new RigidBody(new BoxShape(.2f, 1f, Length - .2f));
            f2.Position = new JVector(Width * .5f - .2f, .5f, 0);
            RigidBodies.Add(f2);
            RigidBody f3 = new RigidBody(new BoxShape(.2f, 1f, Length - .2f));
            f3.Position = new JVector(-Width * .5f + .2f, .5f, 0);
            RigidBodies.Add(f3);

            RigidBody f4 = new RigidBody(new BoxShape(Width * .5f - 1.2f, 1f, .2f));
            f4.Position = new JVector(Width * .25f + .4f, .5f, -Length * .5f + .2f);
            RigidBodies.Add(f4);
            RigidBody f5 = new RigidBody(new BoxShape(Width * .5f - 1.2f, 1f, .2f));
            f5.Position = new JVector(-Width * .25f - .4f, .5f, -Length * .5f + .2f);
            RigidBodies.Add(f5);

            base.BuildGeometry(device);
        }

        public override void Update(GameTime gameTime) {
            foreach (Crop c in Crops)
                c.Update(gameTime);

            base.Update(gameTime);
        }
        public override void Draw(GraphicsDevice device) {
            for (int j = 0; j < growing.Count; j++) {
                int i = growing[j];
                Crops[i].UpdateGeometry(device);
                if (Crops[i].TimeLeft <= 0) {
                    growing.RemoveAt(j);
                    j--;
                }
            }

            foreach (Crop c in Crops) {
                c.Draw(device);
            }

            base.Draw(device);
        }
    }
}
