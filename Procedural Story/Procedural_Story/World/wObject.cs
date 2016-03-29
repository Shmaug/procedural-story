using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Jitter;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;

using Procedural_Story.Util;

namespace Procedural_Story.World {
    struct VertexPositionColorNormal : IVertexType {
        public Vector3 Position;
        public Color Color;
        public Vector3 Normal;

        public VertexPositionColorNormal(Vector3 p, Color c, Vector3 n) {
            Position = p;
            Color = c;
            Normal = n;
        }

        public VertexDeclaration VertexDeclaration {
            get {
                return new VertexDeclaration(
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                    new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
                    );
            }
        }
    }
    struct VertexInstanced : IVertexType {
        public Vector4 Position0;
        public Vector4 Position1;
        public Vector4 Position2;
        public Vector4 Position3;

        public VertexInstanced(Matrix m) {
            Position0 = new Vector4(m.M11, m.M12, m.M13, m.M14);
            Position1 = new Vector4(m.M21, m.M22, m.M23, m.M24);
            Position2 = new Vector4(m.M31, m.M32, m.M33, m.M34);
            Position3 = new Vector4(m.M41, m.M42, m.M43, m.M44);
        }

        public VertexDeclaration VertexDeclaration {
            get {
                return new VertexDeclaration(
                    new VertexElement(0 , VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
                    new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
                    new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
                    new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
                    );
            }
        }
        public static VertexDeclaration instanceVertexDeclaration {
            get {
                return new VertexDeclaration(
                    new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
                    new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
                    new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
                    new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
                    );
            }
        }
    }
    abstract class wObject {
        public Area area;

        public RigidBody RigidBody;
        public Vector3 Position {
            get {
                return new Vector3(RigidBody.Position.X, RigidBody.Position.Y, RigidBody.Position.Z);
            }
            set {
                RigidBody.Position = new Jitter.LinearMath.JVector(value.X, value.Y, value.Z);
            }
        }
        public Vector3 Velocity {
            get {
                return new Vector3(RigidBody.LinearVelocity.X, RigidBody.LinearVelocity.Y, RigidBody.LinearVelocity.Z);
            }
            set {
                RigidBody.LinearVelocity = new Jitter.LinearMath.JVector(value.X, value.Y, value.Z);
            }
        }
        public Matrix Orientation {
            get {
                return new Matrix(
                    RigidBody.Orientation.M11, RigidBody.Orientation.M12, RigidBody.Orientation.M13, 0,
                    RigidBody.Orientation.M21, RigidBody.Orientation.M22, RigidBody.Orientation.M23, 0,
                    RigidBody.Orientation.M31, RigidBody.Orientation.M32, RigidBody.Orientation.M33, 0,
                    0, 0, 0, 1 );
            }
            set {
                RigidBody.Orientation = new Jitter.LinearMath.JMatrix(
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33 );
            }
        }
        public float Scale;
        
        public wObject(Vector3 pos, Area a) {
            RigidBody = new RigidBody(new BoxShape(1, 1, 1));
            Position = pos;
            Scale = 1f;

            area = a;
        }

        public virtual void Update(GameTime gameTime) { }
        public virtual void PostUpdate() { }

        public abstract void Draw(GraphicsDevice device);
    }

    class ModelObject : wObject {
        public Model Model;
        public Texture2D Texture;
        public Vector3 DrawOffset;

        public Matrix[] transforms;

        public ModelObject(Vector3 pos, Model mod, Area area) : base(pos, area) {
            Model = mod;

            transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
        }

        public override void Draw(GraphicsDevice device) {
            Matrix W =
                Matrix.CreateScale(Scale) *
                Orientation *
                Matrix.CreateTranslation(Position + DrawOffset);

                Models.WorldEffect.Parameters["Textured"].SetValue(false);

            foreach (ModelMesh mm in Model.Meshes) {
                Models.WorldEffect.Parameters["World"].SetValue(transforms[mm.ParentBone.Index] * W);
                foreach (ModelMeshPart mmp in mm.MeshParts) {
                    mmp.Effect = Models.WorldEffect;
                    device.SetVertexBuffers(new VertexBufferBinding(mmp.VertexBuffer, mmp.VertexOffset, 0));
                    device.Indices = mmp.IndexBuffer;

                    Models.WorldEffect.Parameters["MaterialColor"].SetValue((Vector4)mmp.Tag);
                    Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["Model"];
                    foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                        p.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mmp.NumVertices, mmp.StartIndex, mmp.PrimitiveCount);
                    }
                }
            }
        }
    }

    class VertexBufferObject : wObject {
        public VertexBuffer VBuffer;
        public IndexBuffer IBuffer;
        public Texture Texture;

        public VertexBufferObject(Vector3 pos, Area a) : base(pos, a) {
            VBuffer = null;
            IBuffer = null;
        }

        public override void Draw(GraphicsDevice device) {
            Matrix W = 
                Matrix.CreateScale(Scale) *
                Orientation *
                Matrix.CreateTranslation(Position);
            
            Models.WorldEffect.Parameters["World"].SetValue(W);
            Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
            Models.WorldEffect.Parameters["Textured"].SetValue(Texture != null);
            Models.WorldEffect.Parameters["Tex"].SetValue(Texture);

            Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques[Texture != null ? "TexturedVBO" : "VBO"];
            if (VBuffer != null) {
                device.Indices = IBuffer;
                device.SetVertexBuffer(VBuffer);
                if (IBuffer != null)
                    foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                        p.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VBuffer.VertexCount, 0, IBuffer.IndexCount / 3);
                    }
                else
                    foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                        p.Apply();
                        device.DrawPrimitives(PrimitiveType.TriangleList, 0, VBuffer.VertexCount / 3);
                    }
            }
        }
    }

    class BillboardObject : wObject {
        public static VertexBuffer VBuffer;

        public Texture2D Texture;

        public BillboardObject(Vector3 pos, Texture2D tex, Area a) : base(pos, a) {
            Texture = tex;
        }

        public override void Draw(GraphicsDevice device) {
            if (VBuffer == null) {
                VBuffer = new VertexBuffer(device, typeof(VertexPositionNormalTexture), 6, BufferUsage.WriteOnly);
                VBuffer.SetData<VertexPositionNormalTexture>(new VertexPositionNormalTexture[] {
                    new VertexPositionNormalTexture(new Vector3(-.5f,  .5f, 0), Vector3.Up, Vector2.Zero),
                    new VertexPositionNormalTexture(new Vector3( .5f,  .5f, 0), Vector3.Up, Vector2.UnitX),
                    new VertexPositionNormalTexture(new Vector3(-.5f, -.5f, 0), Vector3.Up, Vector2.UnitY),

                    new VertexPositionNormalTexture(new Vector3( .5f,  .5f, 0), Vector3.Up, Vector2.UnitX),
                    new VertexPositionNormalTexture(new Vector3( .5f, -.5f, 0), Vector3.Up, Vector2.One),
                    new VertexPositionNormalTexture(new Vector3(-.5f, -.5f, 0), Vector3.Up, Vector2.UnitY)
                });
            }

            Matrix W =
                Matrix.CreateScale(Scale) *
                Matrix.CreateLookAt(Position, Camera.CurrentCamera.Position, Vector3.Up);
            
            Models.WorldEffect.Parameters["World"].SetValue(W);
            Models.WorldEffect.Parameters["MaterialColor"].SetValue(Vector4.One);
            Models.WorldEffect.Parameters["Tex"].SetValue(Texture);
            Models.WorldEffect.Parameters["Textured"].SetValue(true);

            Models.WorldEffect.CurrentTechnique = Models.WorldEffect.Techniques["TexturedVBO"];
            if (VBuffer != null) {
                device.SetVertexBuffer(VBuffer);
                foreach (EffectPass p in Models.WorldEffect.CurrentTechnique.Passes) {
                    p.Apply();
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, VBuffer.VertexCount / 3);
                }
            }
        }
    }
}
