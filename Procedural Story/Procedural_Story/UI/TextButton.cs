using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.UI
{
    class TextButton : UIElement
    {
        public AlignmentType TextAlignment;
        public string Text;
        public string Font;
        public Color bgColor1;
        public Color bgColor2;
        public Color Color1;
        public Color Color2;
        public float TextScale;
        public Action action;
        float hoverTime;

        public TextButton(UIElement parent, string name, UDim2 position, UDim2 size, string text, string font, Color color1, Color color2, Action action) : base(parent, name, position, size) {
            Text = text;
            this.action = action;
            Font = font;
            TextScale = 1;
            Color1 = color1;
            Color2 = color2;
            TextAlignment = AlignmentType.Center;
            bgColor1 = Color.Black;
            bgColor2 = Color.White;
        }

        public override void Update(GameTime time) {
            if (AbsoluteBounds.Contains(new Point(Input.ms.X, Input.ms.Y))) {
                if (hoverTime == 0)
                    ClickSound.Play();
                hoverTime += (float)time.ElapsedGameTime.TotalSeconds;
            } else
                hoverTime = 0f;
            if (hoverTime > 0 && Input.ms.LeftButton == ButtonState.Released && Input.lastms.LeftButton == ButtonState.Pressed)
                action();

            base.Update(time);
        }

        public override void Draw(SpriteBatch batch) {
            float sc = TextScale * MathHelper.SmoothStep(1f, .95f, MathHelper.Clamp(hoverTime * 10f, 0, 1));
            Color col = hoverTime == 0 ? Color1 : Color2;
            Color bgcol = hoverTime == 0 ? bgColor1 : bgColor2;

            Vector2 textSize2 = Fonts[Font].MeasureString(this.Text) * .5f;
            
            // messy below
            Vector2 offset = Vector2.Zero;
            switch (TextAlignment) {
                case AlignmentType.Center:
                    offset = new Vector2(0, 0);
                    break;
                case AlignmentType.TopLeft:
                    offset = new Vector2(-AbsoluteBounds.Width * .5f, -AbsoluteBounds.Height * .5f) + textSize2;
                    break;
                case AlignmentType.CenterLeft:
                    offset = new Vector2(-AbsoluteBounds.Width * .5f + textSize2.X, -AbsoluteBounds.Height * .5f + textSize2.Y);
                    break;
                case AlignmentType.BottomLeft:
                    offset = new Vector2(-AbsoluteBounds.Width + textSize2.X, AbsoluteBounds.Height * .5f - textSize2.Y);
                    break;
                case AlignmentType.CenterTop:
                    offset = new Vector2(0, -AbsoluteBounds.Height * .5f + textSize2.Y);
                    break;
                case AlignmentType.CenterBottom:
                    offset = new Vector2(0, AbsoluteBounds.Height * .5f - textSize2.Y);
                    break;
                case AlignmentType.TopRight:
                    offset = new Vector2(AbsoluteBounds.Width * .5f - textSize2.X, -AbsoluteBounds.Height * .5f + textSize2.Y);
                    break;
                case AlignmentType.CenterRight:
                    offset = new Vector2(AbsoluteBounds.Width * .5f - textSize2.X, 0);
                    break;
                case AlignmentType.BottomRight:
                    offset = new Vector2(AbsoluteBounds.Width * .5f - textSize2.X, AbsoluteBounds.Height * .5f - textSize2.Y);
                    break;
            }
            
            batch.Draw(BlankTexture, AbsoluteBounds, bgcol);
            batch.DrawString(Fonts[Font], Text, new Vector2(AbsoluteBounds.Center.X, AbsoluteBounds.Center.Y) + offset, col, 0f, textSize2, 1f, SpriteEffects.None, 0f);
            
            base.Draw(batch);
        }
    }
}
