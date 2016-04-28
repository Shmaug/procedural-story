using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.UI
{
    class TextBox : UIElement
    {
        public AlignmentType TextAlignment;
        public string DefaultText;
        public string Text;
        public string Font;
        public Color BgColor;
        public Color TextColor;
        public float TextScale;
        public bool Focused;
        public int CursorPos;

        public TextBox(UIElement parent, string name, UDim2 position, UDim2 size, string text, string font, Color color1) : base(parent, name, position, size) {
            DefaultText = "";
            Text = text;
            Font = font;
            TextScale = 1;
            TextColor = color1;
            TextAlignment = AlignmentType.Center;
            BgColor = Color.Black;
            CursorPos = 0;
        }

        public virtual void EnterPressed() {
            Focused = false;
        }
        
        public virtual void KeyPressed(Keys key) {

        }

        float t = 1;
        public override void Update(GameTime time) {
            if (Input.ms.LeftButton == ButtonState.Released && Input.lastms.LeftButton == ButtonState.Pressed)
                if (AbsoluteBounds.Contains(new Point(Input.ms.X, Input.ms.Y)))
                    Focused = true;
                else
                    Focused = false;

            // typing
            if (Focused) {
                Keys[] ck = Input.ks.GetPressedKeys();
                Keys[] lk = Input.lastks.GetPressedKeys();
                for (int i = 0; i < ck.Length; i++) {
                    if (!lk.Contains(ck[i])) {
                        Keys k = ck[i];
                        KeyPressed(k);
                        switch (k) {
                            case Keys.Left:
                                CursorPos = Math.Min(CursorPos + 1, Text.Length - 1);
                                break;
                            case Keys.Right:
                                CursorPos = Math.Max(CursorPos - 1, 0);
                                break;
                            case Keys.Back:
                                if (Text.Length > 0)
                                    Text = Text.Remove(Text.Length - CursorPos - 1);
                                break;
                            case Keys.Enter:
                                EnterPressed();
                                break;
                            case Keys.OemTilde:
                                // for the command window
                                break;
                            default:
                                char c;
                                if (Input.TryConvertKeyboardInput(k, out c))
                                    if (CursorPos == 0)
                                        Text += c;
                                    else
                                        Text = Text.Insert(Text.Length - CursorPos, c.ToString());
                            break;
                        }
                    }
                }
                t -= (float)time.ElapsedGameTime.TotalSeconds;
                if (t < 0) t += 1;
            } else {
                t = 1;
            }
            
            base.Update(time);
        }

        public override void Draw(SpriteBatch batch) {
            Vector2 textSize2 = Fonts[Font].MeasureString(Text) * .5f;
            
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
            
            batch.Draw(BlankTexture, AbsoluteBounds, BgColor);
            batch.DrawString(Fonts[Font], DefaultText + Text, new Vector2(AbsoluteBounds.Center.X, AbsoluteBounds.Center.Y) + offset, TextColor, 0f, textSize2, 1f, SpriteEffects.None, 0f);
            if (t < .5f) {
                batch.DrawString(Fonts[Font], "|",
                    new Vector2(
                        AbsoluteBounds.Center.X + Fonts[Font].MeasureString(Text.Substring(0, Text.Length - CursorPos) + " ").X + Fonts[Font].MeasureString("|").X * .3f,
                        AbsoluteBounds.Center.Y) + offset,
                    TextColor, 0f, textSize2, 1f, SpriteEffects.None, 0f);
            }

            base.Draw(batch);
        }
    }
}
