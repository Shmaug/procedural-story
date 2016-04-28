using System;
using System.Collections.Generic;

using Procedural_Story.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Procedural_Story.Core.Life;

namespace Procedural_Story.Core {
    class Inventory : UIElement {
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public int Capacity
        {
            get { return GridWidth * GridHeight; }
        }
        internal Item[] items;
        public Character owner;

        public Inventory(int width, int height) : base(null, "inventory", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0)) {
            GridWidth = width;
            GridHeight = height;
            items = new Item[Capacity];
        }

        public override void Draw(SpriteBatch batch) {
            int w = (int)(AbsoluteBounds.Width / (float)GridWidth);
            int h = (int)(AbsoluteBounds.Height / (float)GridHeight);
            for (int x = 0; x < GridWidth; x++)
                for (int y = 0; y < GridHeight; y++) {
                    Item i = this[x, y];
                    Color c = Color.Gray;
                    if (owner != null && owner.Equipped == i && i != null)
                        c = Color.White;
                    batch.Draw(BlankTexture, new Rectangle(AbsoluteBounds.X + w * x + 3, AbsoluteBounds.Y + h * y + 3, w - 6, h - 6), c);
                    if (i != null)
                        batch.Draw(i.Icon,   new Rectangle(AbsoluteBounds.X + w * x + 3, AbsoluteBounds.Y + h * y + 3, w - 6, h - 6), i.Src, Color.White);
                }
        }

        public bool getSelected(out Item i) {
            if (!Contains(Input.ms.X, Input.ms.Y)) {
                i = null;
                return false;
            }

            int w = (int)(AbsoluteBounds.Width / (float)GridWidth);
            int h = (int)(AbsoluteBounds.Height / (float)GridHeight);
            for (int x = 0; x < GridWidth; x++)
                for (int y = 0; y < GridHeight; y++) {
                    Rectangle r = new Rectangle(AbsoluteBounds.X + w * x + 3, AbsoluteBounds.Y + h * y + 3, w - 6, h - 6);
                    if (r.Contains(Input.ms.X, Input.ms.Y)) {
                        i = this[x, y];
                        return true;
                    }
                }

            i = null;
            return false;
        }

        public bool Add(Item item) {
            for (int i = 0; i < Capacity; i++)
                if (items[i] == null) {
                    items[i] = item;
                    return true;
                }
            return false;
        }
        public bool Remove(Item item) {
            for (int i = 0; i < Capacity; i++)
                if (items[i] == item) {
                    items[i] = null;
                    return true;
                }
            return false;
        }

        public Item this[int i]
        {
            get
            {
                return items[i];
            }
            set
            {
                items[i] = value;
            }
        }
        public Item this[int x, int y]
        {
            get
            {
                int i = y * GridWidth + x;
                if (i < items.Length)
                    return items[i];
                return null;
            }
            set
            {
                int i = y * GridWidth + x;
                if (i < items.Length)
                    items[i] = value;
            }
        }
    }
}
