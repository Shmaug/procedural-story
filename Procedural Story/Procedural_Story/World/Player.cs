using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Procedural_Story.World {
    class Player {
        public static Texture2D Texture;
        public static Texture2D HudTexture;

        static int FrameWidth = 32;
        static int FrameHeight = 48;

        public Rectangle HitBox {
            get {
                return new Rectangle((int)Position.X, (int)Position.Y, FrameWidth, FrameHeight);
            }
        }
        public Vector2 Center {
            get {
                return Position + new Vector2(FrameWidth, FrameHeight) * .5f;
            }
        }
        public Vector2 Position;
        public Vector2 Velocity;
        public byte Direction; // 0:down 1:right 2: left 3:up
        int WalkFrame;
        float WalkFrameDist;

        Area Area;

        public Player(Area area) {
            Direction = 0;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Area = area;
        }

        public void Update(GameTime gameTime) {
            Velocity = Vector2.Zero;
            if (Input.ks.IsKeyDown(Keys.W)) {
                Direction = 3;
                Velocity += new Vector2(0, -1);
            }
            if (Input.ks.IsKeyDown(Keys.S)) {
                Direction = 0;
                Velocity += new Vector2(0, 1);
            }
            if (Input.ks.IsKeyDown(Keys.A)) {
                Direction = 2;
                Velocity += new Vector2(-1, 0);
            }
            if (Input.ks.IsKeyDown(Keys.D)) {
                Direction = 1;
                Velocity += new Vector2(1, 0);
            }
            if (Input.ms.LeftButton == ButtonState.Pressed) {
                Vector2 dir = Camera.CurrentCamera.Unproject(new Vector2(Input.ms.X, Input.ms.Y)) - (Position + new Vector2(HitBox.Width * .5f, HitBox.Height * .5f));
                dir.Normalize();
                Velocity = dir;

                if (Math.Abs(dir.X) > Math.Abs(dir.Y)) {
                    if (dir.X > 0)
                        Direction = 1;
                    else
                        Direction = 2;
                } else {
                    if (dir.Y > 0)
                        Direction = 0;
                    else
                        Direction = 3;
                }
            }
            if (Velocity != Vector2.Zero) {
                Velocity.Normalize();
                Velocity *= 300;
                Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

                WalkFrameDist += Velocity.Length() * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (WalkFrameDist > 11) {
                    WalkFrameDist = 0;
                    WalkFrame++;
                    if (WalkFrame > 3)
                        WalkFrame = 0;
                }
            } else {
                WalkFrame = 0;
            }
        }

        public void Draw(SpriteBatch batch) {
            batch.Draw(Texture, new Rectangle((int)Position.X, (int)Position.Y, FrameWidth, FrameHeight), new Rectangle(FrameWidth * WalkFrame, FrameHeight * Direction, FrameWidth, FrameHeight), Color.White, 0, Vector2.Zero, SpriteEffects.None, (1 - Area.Height / (float)HitBox.Bottom) / Camera.CurrentCamera.Scale);
        }
    }
}
