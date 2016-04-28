using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.Core.Crops {
    struct Leaf {
        public Vector3 Position;
        public Matrix Direction;
        public float Drop;

        public Leaf(Vector3 pos, Matrix dir, float s) {
            Position = pos;
            Direction = dir;
            Drop = s;
        }
    }
    struct Branch {
        public Vector3 Start;
        public Matrix Direction;
        public float Length;
        public Vector3 End;

        public Branch(Vector3 pos, Matrix dir, float l) {
            Start = pos;
            Direction = dir;
            Length = l;
            End = pos + dir.Forward * l;
        }
    }
    abstract class Crop : VertexBufferObject {
        public bool Mature
        {
            get { return TimeLeft <= 0; }
        }
        public float TimeToMature { get; internal set; }
        public float TimeLeft { get; internal set; }

        public int Seed { get; internal set; }

        public Crop(int seed, float matureTime, Vector3 pos, Area a) : base(pos, a) {
            Seed = seed;
            TimeToMature = TimeLeft = matureTime;
        }

        public override void Update(GameTime time) {
            TimeLeft -= (float)time.ElapsedGameTime.TotalSeconds;

            base.Update(time);
        }
        
        public override void Draw(GraphicsDevice device) {
            base.Draw(device);
        }

        public abstract void Harvest(out Item[] yield);
        
        public abstract void UpdateGeometry(GraphicsDevice device);
    }
}
