using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Procedural_Story.World {
    class Camera {
        public static Camera CurrentCamera;

        public Vector2 Position;
        public float Scale;

        public Camera() {
            Position = Vector2.Zero;
            Scale = 1;
        }

        public Vector2 Project(Vector2 p) {
            return Vector2.Transform(p, getMatrix());
        }

        public Vector2 Unproject(Vector2 p) {
            return Vector2.Transform(p, getMatrixBackwards());
        }

        public Matrix getMatrix() {
            return
                Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                Matrix.CreateScale(Scale) *
                Matrix.CreateTranslation(UI.UIElement.ScreenWidth * .5f, UI.UIElement.ScreenHeight * .5f, 0);
        }

        public Matrix getMatrixBackwards() {
            return
                Matrix.CreateTranslation(-UI.UIElement.ScreenWidth * .5f, -UI.UIElement.ScreenHeight * .5f, 0) *
                Matrix.CreateScale(1f / Scale) *
                Matrix.CreateTranslation(new Vector3(Position, 0));
        }
    }
}
