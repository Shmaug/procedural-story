using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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

    class UDim2 {
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
        public static SoundEffect ClickSound;

        UIElement _parent;
        public UIElement Parent {
            get {
                return _parent;
            }
            set {
                if (value != this) {
                    if (value == null && _parent != null)
                        _parent.Children.Remove(this);
                    if (value != null && value != _parent)
                        value.Children.Add(this);
                    _parent = value;
                }
            }
        }
        public List<UIElement> Children = new List<UIElement>();

        public object Tag;

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

        public bool IntersectsChildren(Vector2 pt) {
            return IntersectsChildren((int)pt.X, (int)pt.Y);
        }
        public bool IntersectsChildren(int x, int y) {
            if (!Contains(x, y)) return false;
            if (Children.Count == 0) return false;
            for (int i = 0; i < Children.Count; i++)
                if (Children[i] != this && Children[i] != Parent) // this should NEVER happen, but check anyway
                    if (Children[i].Visible && (Children[i].Contains(x, y) || Children[i].IntersectsChildren(x, y)))
                        return true;
            return false;
        }

        public UIElement this[string n] {
            get
            {
                for (int i = 0; i < Children.Count; i++)
                    if (Children[i].Name == n)
                        return Children[i];
                return null;
            }
        }

        public bool Contains(Vector2 pt) {
            return Contains((int)pt.X, (int)pt.Y);
        }
        public bool Contains(int x, int y) {
            return AbsoluteBounds.Contains(x, y);
        }

        public bool AreTextBoxesFocused() {
            if (Children.Count == 0) return false;
            for (int i = 0; i < Children.Count; i++)
                if (Children[i] != this && Children[i] != Parent) // this should NEVER happen, but check anyway
                    if (Children[i].Visible && ((Children[i] is TextBox && (Children[i] as TextBox).Focused) || Children[i].AreTextBoxesFocused()))
                        return true;
            return false;
        }

        public virtual void Update(GameTime time) {
            if (Children.Count > 0)
                for (int i = 0; i < Children.Count; i++)
                    if (Children[i] != this && Children[i] != Parent) // this should NEVER happen, but check anyway
                        if (Children[i].Visible)
                            Children[i].Update(time);
        }
        public virtual void Draw(SpriteBatch batch) {
            if (Children.Count > 0)
                for (int i = 0; i < Children.Count; i++)
                if (Children[i] != this && Children[i] != Parent) // this should NEVER happen, but check anyway
                    if (Children[i].Visible)
                        Children[i].Draw(batch);
        }
    }
}
