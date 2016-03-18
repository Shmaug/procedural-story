using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.UI
{
    class ImageLabel : UIElement {
        public Texture2D Image;
        public Color Color;

        public ImageLabel(UIElement parent, string name, UDim2 position, UDim2 size, Texture2D image, Color color) : base(parent, name, position, size) {
            Image = image;
            Color = color;
        }

        public override void Draw(SpriteBatch batch) {
            batch.Draw(Image, AbsoluteBounds, null, Color, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            base.Draw(batch);
        }
    }
}
