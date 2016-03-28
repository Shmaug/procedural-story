using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;

using Procedural_Story.Util;

namespace Procedural_Story.World.Life {
    class Character : ModelObject {
        public readonly float Height = 2.2f;
        public readonly float Radius = .5f;

        public Area Area;
        
        public Dictionary<string, Animation> activeAnimations;

        public float BodyRotation;

        public Vector3 Move;
        public Vector3 Look;

        public bool Attacking = false;

        List<string> toRemove;

        public Character(Area area) : base(Vector3.Zero, Models.PlayerModel, area) {
            Area = area;

            RigidBody = new RigidBody(new CapsuleShape(Height - Radius * 2, Radius));
            RigidBody.AllowDeactivation = false;
            RigidBody.Material.Restitution = 0;
            RigidBody.Tag = this;

            DrawOffset = new Vector3(0, .1f, 0);

            toRemove = new List<string>();
            activeAnimations = new Dictionary<string, Animation>();
            activeAnimations.Add("Walk", Animations.CharacterWalk);
        }

        public void PlayAnimation(string name, Animation anim) {
            activeAnimations.Add(name, anim);
            anim.Play();
        }

        public void RemoveAnimation(string name) {
            toRemove.Add(name);
        }

        public override void Update(GameTime gameTime) {
            #region move
            if (Move.X != 0 || Move.Z != 0) {
                if (!activeAnimations["Walk"].Playing)
                    activeAnimations["Walk"].Play();

                activeAnimations["Walk"].TimeModifier = new Vector2(RigidBody.LinearVelocity.X, RigidBody.LinearVelocity.Z).Length();

                Vector3 forward = Matrix.CreateRotationY(BodyRotation).Forward;
                Vector2 d = new Vector2(Move.X, -Move.Z);
                d.Normalize();
                Vector2 a = Vector2.SmoothStep(new Vector2(forward.X, -forward.Z), d, (float)gameTime.ElapsedGameTime.TotalSeconds * 15f);
                BodyRotation = (float)Math.Atan2(a.Y, a.X) - MathHelper.PiOver2;
            } else
                activeAnimations["Walk"].Stop();
            
            Vector3 delta = Move - Velocity;
            delta.Y = 0;
            delta *= .2f;
            if (delta.LengthSquared() > 0)
                RigidBody.ApplyImpulse(new JVector(delta.X, 0, delta.Z) * RigidBody.Mass);

            if (Move.Y > 0) {
                JVector norm;
                float frac = 1;
                RigidBody hit;
                bool cast = Area.Physics.CollisionSystem.Raycast(RigidBody.Position, JVector.Down * (Height * .5f),
                    (RigidBody bd, JVector n, float d) => {
                        return bd != RigidBody;
                    },
                    out hit, out norm, out frac);
                if (cast)
                    RigidBody.ApplyImpulse(new JVector(0, Move.Y, 0) * RigidBody.Mass);
            }
            #endregion
            #region animate
            List<Pose> curPoses = new List<Pose>();
            foreach (KeyValuePair<string, Animation> kf in activeAnimations) {
                kf.Value.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                if (kf.Value.Playing && !toRemove.Contains(kf.Key))
                    curPoses.Add(kf.Value.getCurrentPose());
            }

            Matrix[] t = new Matrix[transforms.Length];
            for (int i = 0; i < t.Length; i++) t[i] = Matrix.Identity;
            int[] weights = new int[transforms.Length];
            foreach (Pose p in curPoses) {
                Matrix[] pt = p.getTransforms();
                // go thru all transforms in this pose, add the ones that have more weight
                for (int i = 0; i < t.Length; i++) {
                    if (p.Weight >= weights[i] && p.UsesBone(i)) {
                        t[i] = pt[i];
                        weights[i] = p.Weight;
                    }
                }
            }
            transforms = t;

            // rotate head
            transforms[2] = Matrix.CreateTranslation(0, -3f * .125f, 0) *
                Matrix.CreateRotationX(MathHelper.Clamp(MathHelper.WrapAngle(Look.X), -MathHelper.PiOver2 * .75f, MathHelper.PiOver2 * .75f)) *
                Matrix.CreateRotationY(MathHelper.Clamp(MathHelper.WrapAngle(Look.Y - BodyRotation), -MathHelper.PiOver2 * .8f, MathHelper.PiOver2 * .8f)) *
                Matrix.CreateTranslation(0, 3f * .125f, 0);

            foreach (string s in toRemove)
                activeAnimations.Remove(s);
            toRemove.Clear();

            #endregion
        }

        public override void PostUpdate() {
            Orientation = Matrix.CreateRotationY(BodyRotation);
            RigidBody.AngularVelocity = JVector.Zero;
        }
    }
}
