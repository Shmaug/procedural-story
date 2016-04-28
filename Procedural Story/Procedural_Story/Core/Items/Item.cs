using Jitter.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Procedural_Story.Core.Life;
using System.Collections.Generic;

namespace Procedural_Story.Core {
    /// <summary>
    /// Represents an item that is in the world
    /// </summary>
    abstract class WorldItem : VertexBufferObject {
        public Item item;

        public WorldItem(Item i, Vector3 pos, Area a) : base(pos, a) {
            item = i;
        }

        public override void Draw(GraphicsDevice device) {
            item.getWorldGeometry(device, out VBuffer, out IBuffer);

            base.Draw(device);
        }
    }

    abstract class Item {
        public static Dictionary<uint, Item> ClassIDs;
        public static Dictionary<string, Texture2D> ItemIcons;

        public uint ID;
        public int Stack;
        public Texture2D Icon;
        public Rectangle Src;

        public Item(uint id) {
            ID = id;
            Stack = 0;
            Icon = null;
        }
        public Item(uint id, Texture2D icon, Rectangle src) {
            ID = id;
            Stack = 0;
            Icon = icon;
            Src = src;
        }

        public Item(int stack) {
            Stack = stack;
        }

        public abstract void getWorldGeometry(GraphicsDevice device, out VertexBuffer VBuffer, out IndexBuffer IBuffer);

        public static Item FromID(uint id) {
            switch (id) {
                case 0:
                    return new Crops.CornItem();
                case 1:
                    return new Crops.CornSeed();
                default:
                    return null;
            }
        }
    }
    abstract class UsableItem : Item {
        public int Uses;

        public UsableItem(uint id) : base(id) { }
        public UsableItem(uint id, Texture2D icon, Rectangle src) : base(id, icon, src) { }
        
        public virtual void Use(Character user, Vector3 atPos, RigidBody onBody) {
            Uses--;

            if (Uses <= 0) {
                user.Inventory.Remove(this);
                if (user.Equipped == this)
                    user.Equipped = null;
            }
        }
    }
    abstract class FoodItem : UsableItem {
        public float Hunger { get; internal set; }
        public float Thirst { get; internal set; }
        public float Energy { get; internal set; }
        public float Health { get; internal set; }

        public FoodItem(uint id, float hunger, float thirst, float energy, float health) : base(id) {
            Hunger = hunger;
            Thirst = thirst;
            Energy = energy;
            Health = health;
        }

        public FoodItem(uint id, float hunger, float thirst, float energy, float health, Texture2D icon, Rectangle src) : base(id, icon, src) {
            Hunger = hunger;
            Thirst = thirst;
            Energy = energy;
            Health = health;
        }

        public override void Use(Character user, Vector3 atPos, RigidBody onBody) {
            user.Hunger += Hunger;
            user.Thirst += Thirst;
            user.Health += Health;
            user.Energy += Energy;

            base.Use(user, atPos, onBody);
        }
    }
}
