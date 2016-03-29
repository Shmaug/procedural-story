using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using Procedural_Story;

namespace Procedural_Story.UI
{
    class TextLabel : UIElement {
        public AlignmentType TextAlignment;
        public string Text;
        public Color Color;
        public string Font;
        public float TextScale;

        public TextLabel(UIElement parent, string name, UDim2 position, UDim2 size, string text, string font, Color color) : base(parent, name, position, size) {
            Text = text;
            Font = font;
            Color = color;
            TextScale = 1;
            TextAlignment = AlignmentType.Center;
        }

        public override void Draw(SpriteBatch batch) {
            if (Text == null) {
                base.Draw(batch);
                return;
            }
            Vector2 textSize = Fonts[Font].MeasureString(Text);
            Vector2 textSize2 = textSize * .5f;

            Vector2 offset = Vector2.Zero;
            switch (TextAlignment) {
                case AlignmentType.Center:
                    offset = new Vector2(AbsoluteBounds.Width, AbsoluteBounds.Height) * .5f - textSize2;
                    break;
                case AlignmentType.TopLeft:
                    offset = new Vector2(0, 0);
                    break;
                case AlignmentType.CenterLeft:
                    offset = new Vector2(0, AbsoluteBounds.Height * .5f - textSize2.Y);
                    break;
                case AlignmentType.BottomLeft:
                    offset = new Vector2(0, AbsoluteBounds.Height - textSize.Y);
                    break;
                case AlignmentType.CenterTop:
                    offset = new Vector2(AbsoluteBounds.Width * .5f - textSize2.X, 0);
                    break;
                case AlignmentType.CenterBottom:
                    offset = new Vector2(AbsoluteBounds.Width * .5f - textSize2.X, AbsoluteBounds.Height - textSize.Y);
                    break;
                case AlignmentType.TopRight:
                    offset = new Vector2(AbsoluteBounds.Width - textSize.X, 0);
                    break;
                case AlignmentType.CenterRight:
                    offset = new Vector2(AbsoluteBounds.Width - textSize.X, AbsoluteBounds.Height);
                    break;
                case AlignmentType.BottomRight:
                    offset = new Vector2(AbsoluteBounds.Width - textSize.X, AbsoluteBounds.Height * .5f - textSize2.Y);
                    break;
            }
            batch.DrawString(Fonts[Font], Text, new Vector2(AbsoluteBounds.X, AbsoluteBounds.Y) + offset, Color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            base.Draw(batch);
        }
    }
}
