using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.UI
{
    class ImageButton : UIElement
    {
        public Texture2D Icon;
        public Color Color1;
        public Color Color2;
        public Action action;
        public Rectangle SrcRect;
        float hoverTime;

        public ImageButton(UIElement parent, string name, UDim2 position, UDim2 size, Texture2D icon, Rectangle? src, Color c1, Color c2, Action action) : base(parent, name, position, size) {
            this.action = action;
            Icon = icon;
            Color1 = c1;
            Color2 = c2;
            if (src.HasValue)
                SrcRect = src.Value;
            else {
                if (icon != null)
                    SrcRect = new Rectangle(0, 0, icon.Width, icon.Height);
                else
                    SrcRect = new Rectangle(0, 0, 0, 0);
            }
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
            Color col = hoverTime == 0 ? Color1 : Color2;

            if (Icon != null)
                batch.Draw(Icon, AbsoluteBounds, SrcRect, col, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            else
                batch.Draw(BlankTexture, AbsoluteBounds, SrcRect, col, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            base.Draw(batch);
        }
    }
}
