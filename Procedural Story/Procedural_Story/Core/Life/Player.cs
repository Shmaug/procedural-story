using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Procedural_Story.Util;

using Jitter.LinearMath;
using Jitter.Dynamics;

using Procedural_Story.UI;
using Procedural_Story.Core.Structures;
using Procedural_Story.Core.Crops;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.Core.Life {
    class Player : Character {
        public Frame InventoryFrame;

        public bool FreeCam = false;
        float CameraDistance = 10;

        public Player(Area area) : base(area) {
            Inventory = new Inventory(8, 8);

            InventoryFrame = new Frame(Main.Instance.MainFrame, "Inventory", new UDim2(0, 0, 100, 50), new UDim2(0, 0, 600, 650), Color.Black * .75f);
            InventoryFrame.Visible = false;
            InventoryFrame.Draggable = true;
            TextLabel l = new TextLabel(InventoryFrame, "label", new UDim2(0, 0, 85, 10), new UDim2(0, 0, 0, 14), "INVENTORY [Q]", "Avenir16", Color.White);
            Inventory.Parent = InventoryFrame;
            Inventory.Position = new UDim2(0, 0, 0, 50);
            Inventory.Size = new UDim2(1, 1, 0, -50);
            Inventory.owner = this;
        }
        
        public override void Update(GameTime gameTime) {
            bool k = !Input.KeysBlocked;

            if (Input.KeyPressed(Keys.Q) && k)
                InventoryFrame.Visible = !InventoryFrame.Visible;

            if (Input.ms.LeftButton == ButtonState.Pressed && Input.lastms.LeftButton == ButtonState.Released) {
                bool flag = false;
                // pick item from invnetory
                if (InventoryFrame.Visible) {
                    Item i;
                    bool c = Inventory.getSelected(out i);
                    if (c) {
                        Equipped = i;
                        flag = true;
                    }
                }

                // try to use an equipped item
                if (!flag) {
                    RigidBody hit;
                    Vector3 norm, pos;
                    Camera.CurrentCamera.MouseRaycast(area, out pos, out hit, out norm);

                    if (Equipped is UsableItem)
                        (Equipped as UsableItem).Use(this, pos, hit);
                }
            }

            #region camera input
            if (!Input.MouseBlocked && Input.ms.RightButton == ButtonState.Pressed) {
                Camera.CurrentCamera.Rotation.X -= (Input.ms.Y - Input.lastms.Y) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;
                Camera.CurrentCamera.Rotation.Y -= (Input.ms.X - Input.lastms.X) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;

                Camera.CurrentCamera.Rotation.X = MathHelper.Clamp(Camera.CurrentCamera.Rotation.X, -MathHelper.PiOver2, MathHelper.PiOver2);
                Camera.CurrentCamera.Rotation.Y = MathHelper.WrapAngle(Camera.CurrentCamera.Rotation.Y);
            }

            if (Input.KeyPressed(Keys.C) && k)
                FreeCam = !FreeCam;

            if (Input.ms.ScrollWheelValue > Input.lastms.ScrollWheelValue)
                CameraDistance = Math.Max(CameraDistance - 2, 4);
            else if (Input.ms.ScrollWheelValue < Input.lastms.ScrollWheelValue)
                CameraDistance = Math.Min(CameraDistance + 2, 10);
            #endregion

            #region move input
            Move = Vector3.Zero;
            if (Input.ks.IsKeyDown(Keys.W) && k)
                Move += Vector3.Forward;
            else if (Input.ks.IsKeyDown(Keys.S) && k)
                Move += Vector3.Backward;
            if (Input.ks.IsKeyDown(Keys.A) && k)
                Move += Vector3.Left;
            else if (Input.ks.IsKeyDown(Keys.D) && k)
                Move += Vector3.Right;
            #endregion

            #region attack input
            if (!Input.MouseBlocked && Input.ms.LeftButton == ButtonState.Pressed) {
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
                if (Input.ks.IsKeyDown(Keys.LeftControl) && k)
                    Move *= .25f;
                else if (Input.ks.IsKeyDown(Keys.LeftShift) && k)
                    Move *= 1.35f;
            }
            if (FreeCam) {
                if (Move != Vector3.Zero) {
                    if (Input.ks.IsKeyDown(Keys.LeftControl) && k)
                        Move *= .1f;
                    if (Input.ks.IsKeyDown(Keys.LeftShift) && k)
                        Move *= 5;
                    Move = Vector3.Transform(Move, Camera.CurrentCamera.RotationMatrix);
                    Camera.CurrentCamera.Position += Move / 10;
                    Move = Vector3.Zero;
                }
                if (Input.ks.IsKeyDown(Keys.Space) && k) {
                    Position = Camera.CurrentCamera.Position + Vector3.Transform(Vector3.Forward, Camera.CurrentCamera.RotationMatrix) * CameraDistance;
                    Velocity = Vector3.Zero;
                }
            } else {
                if (Move != Vector3.Zero)
                    Move = Vector3.Transform(Move, Matrix.CreateRotationY(Camera.CurrentCamera.Rotation.Y));
                
                // Jump if we're on the ground
                if (Input.ks.IsKeyDown(Keys.Space) && k)
                    Move.Y = 7;
            }
            #endregion

            Look = Camera.CurrentCamera.Rotation;
            base.Update(gameTime);
        }

        float[] lastf = new float[2];
        float curDist = 0;
        public override void PostUpdate() {
            base.PostUpdate();

            if (!FreeCam) {
                Vector3 pos = Position + Vector3.Up * (Height * .5f - .2f);

                Vector3 dir = Camera.CurrentCamera.RotationMatrix.Backward * CameraDistance;
                float dist = CameraDistance;

                float frac = 0;
                JVector norm;
                RigidBody hit;
                bool cast = area.Physics.CollisionSystem.Raycast(new JVector(pos.X, pos.Y, pos.Z), new JVector(dir.X, dir.Y, dir.Z),
                    (RigidBody bd, JVector n, float d) => {
                        return bd != RigidBody;
                    },
                    out hit, out norm, out frac);
                if (cast && frac < 1 && lastf[0] < 1 && lastf[1] < 1)
                    dist = frac * CameraDistance - .2f;
                lastf[1] = lastf[0];
                lastf[0] = frac;

                curDist = MathHelper.Lerp(curDist, dist, .2f);

                Camera.CurrentCamera.Position = pos + Camera.CurrentCamera.RotationMatrix.Backward * curDist;
            }
            Debug.Track(Camera.CurrentCamera.Position, "camera");
        }

        public void DrawHud(SpriteBatch batch) {
            if (Equipped != null)
                batch.Draw(Equipped.Icon, new Rectangle(100, UIElement.ScreenHeight - 164, 64, 64), Equipped.Src, Color.White);
        }
    }
}
