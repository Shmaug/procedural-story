using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Procedural_Story.UI
{
    class Frame : UIElement
    {
        public Color Background;
        public bool Draggable = false;
        bool dragging = false;

        public Frame(UIElement parent, string name, UDim2 pos, UDim2 size, Color bg) : base(parent, name, pos, size) {
            Background = bg;
        }

        public override void Update(GameTime time) {
            if (Draggable) {
                if (Input.lastms.LeftButton == ButtonState.Released && Input.ms.LeftButton == ButtonState.Pressed)
                    if (Contains(Input.ms.X, Input.ms.Y) && !IntersectsChildren(Input.ms.X, Input.ms.Y))
                        dragging = true;

                if (Input.ms.LeftButton == ButtonState.Released)
                    dragging = false;

                if (dragging && Input.lastms.LeftButton == ButtonState.Pressed)
                    Position.Offset += new Vector2(Input.ms.X, Input.ms.Y) - new Vector2(Input.lastms.X, Input.lastms.Y);
            }

            base.Update(time);
        }

        public override void Draw(SpriteBatch batch) {
            batch.Draw(BlankTexture, AbsoluteBounds, Background);

            base.Draw(batch);
        }
    }
}
