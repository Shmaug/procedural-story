using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Procedural_Story.World {
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
    }
}
