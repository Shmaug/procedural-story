using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

using Procedural_Story.Core.Structures;

namespace Procedural_Story.Core.Life {
    class ComplexCharacter : Character {
        public string Name;
        public float Aggression;
        public float Willpower;
        public List<Desire> activeDesires;
        public Home home;

        Path currentPath;

        public ComplexCharacter(Area area) : base(area) {
            Name = "Joe";
            Aggression = 0;
            Willpower = 0;
            activeDesires = new List<Desire>();
        }

        public override void Update(GameTime gameTime) {
            //if (Vector3.DistanceSquared(Position, area.Characters[0].Position) < 10 * 10)
            //    currentPath = area.PathSystem.GetPath(Position, area.Characters[0].Position);

            base.Update(gameTime);
        }

        public override void Draw(GraphicsDevice device) {
            currentPath?.DebugDraw(device);

            base.Draw(device);
        }
    }
}
