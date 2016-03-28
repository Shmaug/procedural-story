using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;

using Procedural_Story.Util;

namespace Procedural_Story.World.Life {
    class Player : Character {

        public bool FreeCam = false;
        float CameraDistance = 10;

        public Player(Area area) : base(area) {

        }

        public override void Update(GameTime gameTime) {
            #region input
            if (Input.ms.RightButton == ButtonState.Pressed) {
                Camera.CurrentCamera.Rotation.X -= (Input.ms.Y - Input.lastms.Y) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;
                Camera.CurrentCamera.Rotation.Y -= (Input.ms.X - Input.lastms.X) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;

                Camera.CurrentCamera.Rotation.X = MathHelper.Clamp(Camera.CurrentCamera.Rotation.X, -MathHelper.PiOver2, MathHelper.PiOver2);
                Camera.CurrentCamera.Rotation.Y = MathHelper.WrapAngle(Camera.CurrentCamera.Rotation.Y);
            }

            if (Input.ms.ScrollWheelValue > Input.lastms.ScrollWheelValue)
                CameraDistance = Math.Max(CameraDistance - 2, 4);
            else if (Input.ms.ScrollWheelValue < Input.lastms.ScrollWheelValue)
                CameraDistance = Math.Min(CameraDistance + 2, 10);

            Move = Vector3.Zero;
            if (Input.ks.IsKeyDown(Keys.W))
                Move += Vector3.Forward;
            else if (Input.ks.IsKeyDown(Keys.S))
                Move += Vector3.Backward;
            if (Input.ks.IsKeyDown(Keys.A))
                Move += Vector3.Left;
            else if (Input.ks.IsKeyDown(Keys.D))
                Move += Vector3.Right;

            if (Input.ks.IsKeyDown(Keys.C) && Input.lastks.IsKeyUp(Keys.C))
                FreeCam = !FreeCam;

            if (Input.ms.LeftButton == ButtonState.Pressed) {
                if (!Attacking) {
                    Animation a = Animations.CharacterSwing;
                    a.AnimationCompleted += () => {
                        RemoveAnimation("Swing");
                        Attacking = false;
                    };
                    PlayAnimation("Swing", a);
                    Attacking = true;
                }
            }
            #endregion

            #region move player (camera if freecam)
            if (Move != Vector3.Zero) {
                Move.Normalize();
                Move *= 7;
                if (Input.ks.IsKeyDown(Keys.LeftControl))
                    Move *= .25f;
                else if (Input.ks.IsKeyDown(Keys.LeftShift))
                    Move *= 1.35f;
            }
            if (FreeCam) {
                if (Move != Vector3.Zero) {
                    Move = Vector3.Transform(Move, Camera.CurrentCamera.RotationMatrix);
                    Camera.CurrentCamera.Position += Move / 10;
                    Move = Vector3.Zero;
                }
            } else {
                if (Move != Vector3.Zero)
                    Move = Vector3.Transform(Move, Matrix.CreateRotationY(Camera.CurrentCamera.Rotation.Y));
                
                // Jump if we're on the ground
                if (Input.ks.IsKeyDown(Keys.Space))
                    Move.Y = 7;
            }
            #endregion

            Look = Camera.CurrentCamera.Rotation;
            base.Update(gameTime);
        }

        public override void PostUpdate() {
            base.PostUpdate();

            if (!FreeCam) {
                Vector3 dir = Camera.CurrentCamera.RotationMatrix.Backward * CameraDistance;
                Vector3 pos = Position + new Vector3(0, Height * .5f - .2f, 0);
                float dist = CameraDistance;

                JVector norm;
                float frac = 1;
                RigidBody hit;
                bool cast = Area.Physics.CollisionSystem.Raycast(new JVector(pos.X, pos.Y, pos.Z), new JVector(dir.X, dir.Y, dir.Z),
                (RigidBody bd, JVector n, float d) => {
                    return bd != RigidBody;
                },
                out hit, out norm, out frac);
                if (cast && frac < 1)
                    dist = frac * CameraDistance - .2f;

                Camera.CurrentCamera.Position = pos + Camera.CurrentCamera.RotationMatrix.Backward * dist;
            }
        }
    }
}
