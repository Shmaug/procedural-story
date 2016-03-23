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

namespace Procedural_Story.World {
    class Player : ModelObject {
        public readonly float Height = 2.2f;
        public readonly float Radius = .5f;

        Area Area;

        float armRot;
        float armDelta = 1;
        Matrix[] origTransforms;

        float BodyRotation;

        public Player(Area area) : base(Vector3.Zero, Models.PlayerModel, area) {
            Area = area;

            RigidBody = new RigidBody(new CapsuleShape(Height - Radius * 2, Radius));
            RigidBody.AllowDeactivation = false;
            RigidBody.Material.Restitution = 0;

            DrawOffset = new Vector3(0, .1f, 0);

            origTransforms = new Matrix[transforms.Length];
            Array.Copy(transforms, origTransforms, transforms.Length);
        }

        public override void Update(GameTime gameTime) {
            // rotate head
            Vector3 a = Matrix.CreateRotationY(Camera.CurrentCamera.Rotation.Y).Forward;
            Vector3 b = Matrix.CreateRotationY(BodyRotation).Forward;
            float ang = (float)Math.Acos(Vector3.Dot(a, b)) * Math.Sign(Vector3.Cross(b, a).Y);
            transforms[2] = origTransforms[2] * Matrix.CreateRotationY(
                MathHelper.Clamp(ang, -MathHelper.Pi * .3f, MathHelper.Pi * .3f));

            // gather input
            Vector3 Move = Vector3.Zero;
            if (Input.ks.IsKeyDown(Keys.W))
                Move += Vector3.Forward;
            else if (Input.ks.IsKeyDown(Keys.S))
                Move += Vector3.Backward;
            if (Input.ks.IsKeyDown(Keys.A))
                Move += Vector3.Left;
            else if (Input.ks.IsKeyDown(Keys.D))
                Move += Vector3.Right;
            
            if (Move != Vector3.Zero) {
                Move.Normalize();
                Move = Vector3.Transform(Move, Matrix.CreateRotationY(Camera.CurrentCamera.Rotation.Y));
                
                a = Move;
                ang = (float)Math.Acos(Vector3.Dot(a, b)) * Math.Sign(Vector3.Cross(b, a).Y);
                BodyRotation = MathHelper.Lerp(BodyRotation, BodyRotation + ang, (float)gameTime.ElapsedGameTime.TotalSeconds * 15f);
                Orientation = Matrix.CreateRotationY(BodyRotation);

                Move *= 10;
                if (Input.ks.IsKeyDown(Keys.LeftControl))
                    Move *= .5f;
                else if (Input.ks.IsKeyDown(Keys.LeftShift))
                    Move *= 2f;
            }

            Vector3 delta = Move - Velocity;
            delta.Y = 0;

            delta *= .05f;

            if (Velocity.Y >= Move.Y)
                Move.Y = 0;

            if (delta.LengthSquared() > 0)
                RigidBody.ApplyImpulse(new JVector(delta.X, 0, delta.Z) * RigidBody.Mass);

            if (Input.ks.IsKeyDown(Keys.Space)) {
                // Jump if we're on the ground
                JVector norm;
                float frac = 1;
                RigidBody hit;
                bool cast = Area.Physics.CollisionSystem.Raycast(RigidBody.Position, JVector.Down * (Height * .5f),
                    (RigidBody bd, JVector n, float d) => {
                        return bd != RigidBody;
                    },
                    out hit, out norm, out frac);
                if (cast)
                    RigidBody.ApplyImpulse(new JVector(0, 7, 0) * RigidBody.Mass);
            }

            // animate arms n shit
            float v = new Vector2(Velocity.X, Velocity.Z).Length();
            if (v > .01f) {
                armRot += armDelta * v * (float)gameTime.ElapsedGameTime.TotalSeconds * .7f;
                if (armRot > MathHelper.PiOver4 && armDelta > 0)
                    armDelta = -1;
                if (armRot < -MathHelper.PiOver4 && armDelta < 0)
                    armDelta = 1;
            } else {
                armRot = armRot * .5f;
            }
        }

        public override void PostUpdate() {
            Orientation = Matrix.CreateRotationY(BodyRotation);
            RigidBody.AngularVelocity = JVector.Zero;
        }
    }
}
