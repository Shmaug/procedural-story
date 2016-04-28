using Jitter.Dynamics;
using Jitter.Collision.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Procedural_Story.Core.Life;

namespace Procedural_Story.Core.Structures {
    class Crate : ModelObject {
        public Inventory Inventory;

        public Crate(Vector3 pos, Area a) : base(pos, Util.Models.BoxModel, a) {
            Inventory = new Inventory(8, 8);
            RigidBody = new RigidBody(new BoxShape(1, 1, 1));
            Position = pos;
        }
    }
}
