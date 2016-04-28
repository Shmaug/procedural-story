using Microsoft.Xna.Framework;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Microsoft.Xna.Framework.Graphics;
using Procedural_Story.UI;

namespace Procedural_Story.Core {
    class Camera {
        public static Camera CurrentCamera;

        public Vector3 Position;
        public Vector3 Rotation;

        public float AspectRatio;

        public BoundingFrustum Frustum {
            get {
                return new BoundingFrustum(View * Projection);
            }
        }
        public Matrix RotationMatrix {
            get {
                return Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
            }
        }
        public Matrix View {
            get {
                Matrix rot = RotationMatrix;
                return Matrix.CreateLookAt(Position, Position + rot.Forward, rot.Up);
            }
        }
        public Matrix Projection {
            get {
                return Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(70), AspectRatio, .1f, 1000);
            }
        }

        public Camera() {
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
        }

        public bool MouseRaycast(Area area, out Vector3 position, out RigidBody body, out Vector3 normal) {
            normal = Vector3.Zero;
            position = Vector3.Zero;

            Vector3 near = Main.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(Input.ms.X, Input.ms.Y, 0), Projection, View, Matrix.Identity);
            Vector3 far = Main.Instance.GraphicsDevice.Viewport.Unproject(new Vector3(Input.ms.X, Input.ms.Y, 1), Projection, View, Matrix.Identity);
            Vector3 d = far - near;
            d.Normalize();

            float frac;
            JVector norm;
            bool h = area.CollisionSystem.Raycast(new JVector(Position.X, Position.Y, Position.Z), new JVector(d.X, d.Y, d.Z), null, out body, out norm, out frac);
            if (!h) return false;

            normal = new Vector3(norm.X, norm.Y, norm.Z);
            position = Position + (d * frac);

            // do more raycasts, closer to make it more accurate
            bool h2 = true;
            float t = 0;
            while (h2 && t < 1) {
                JVector p2 = new JVector(Position.X, Position.Y, Position.Z) + (new JVector(d.X, d.Y, d.Z) * frac * t);
                h2 = area.CollisionSystem.Raycast(
                    p2,
                    new JVector(d.X, d.Y, d.Z), null, out body, out norm, out frac);
                if (!h2) return true;

                normal = new Vector3(norm.X, norm.Y, norm.Z);
                position = new Vector3(p2.X, p2.Y, p2.Z) + (d * frac);
                t += .1f;
            }
            
            return true;
        }
    }
}
