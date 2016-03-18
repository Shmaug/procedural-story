using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.UI
{
    public enum AlignmentType {
        Center,
        TopLeft,
        CenterLeft,
        BottomLeft,
        TopRight,
        CenterRight,
        BottomRight,
        CenterTop,
        CenterBottom
    }

    public class UDim2 {
        public Vector2 Scale;
        public Vector2 Offset;

        public UDim2(float sX, float sY, float oX, float oY) {
            Scale = new Vector2(sX, sY);
            Offset = new Vector2(oX, oY);
        }

        public UDim2(Vector2 scale, Vector2 offset) {
            Scale = scale;
            Offset = offset;
        }
    }

    class UIElement {
        public static int ScreenWidth, ScreenHeight;
        public static Dictionary<string, SpriteFont> Fonts;
        public static Texture2D BlankTexture;

        UIElement _parent;
        public UIElement Parent {
            get {
                return _parent;
            }
            set {
                if (value == null && _parent != null)
                    _parent.Children.Remove(this.Name);
                if (value != null && value != _parent)
                    value.Children.Add(Name, this);
                _parent = value;
            }
        }
        public Dictionary<string, UIElement> Children = new Dictionary<string, UIElement>();

        public string Name;
        public UDim2 Position;
        public UDim2 Size;
        public bool Visible = true;
        public Rectangle AbsoluteBounds {
            get {
                if (Parent != null && Parent != this)
                    return new Rectangle(
                        (int)(Parent.AbsoluteBounds.X + Parent.AbsoluteBounds.Width * Position.Scale.X + Position.Offset.X),
                        (int)(Parent.AbsoluteBounds.Y + Parent.AbsoluteBounds.Height * Position.Scale.Y + Position.Offset.Y),
                        (int)(Parent.AbsoluteBounds.Width * Size.Scale.X + Size.Offset.X),
                        (int)(Parent.AbsoluteBounds.Height * Size.Scale.Y + Size.Offset.Y)
                    );
                else
                    return new Rectangle(
                        (int)(ScreenWidth * Position.Scale.X + Position.Offset.X),
                        (int)(ScreenHeight * Position.Scale.Y + Position.Offset.Y),
                        (int)(ScreenWidth * Size.Scale.X + Size.Offset.X),
                        (int)(ScreenHeight * Size.Scale.Y + Size.Offset.Y)
                    );
            }
        }

        internal UIElement(UIElement parent, string name, UDim2 position, UDim2 size) {
            Name = name;
            Position = position;
            Size = size;
            Parent = parent;
        }

        public virtual void Update(GameTime time) {
            if (Visible)
                foreach (KeyValuePair<String, UIElement> c in Children)
                    if (c.Value != this && c.Value != Parent) // this should NEVER happen, but check anyway
                        if (c.Value.Visible)
                            c.Value.Update(time);
        }
        public virtual void Draw(SpriteBatch batch) {
            if (Visible)
                foreach (KeyValuePair<String, UIElement> c in Children)
                    if (c.Value != this && c.Value != Parent) // this should NEVER happen, but check anyway
                        if (c.Value.Visible)
                            c.Value.Draw(batch);
        }
    }
}
