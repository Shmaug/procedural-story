using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Procedural_Story.Util {
    static class Models {
        public static Effect WorldEffect;

        public static Model PlayerModel;
        public static Model[] TreeModels;
        public static Model[] GrassModels;
        public static Model BoxModel;

        public static void Load(ContentManager Content) {
            WorldEffect = Content.Load<Effect>("fx/world");

            PlayerModel = Content.Load<Model>("model/player");
            TreeModels = new Model[] {
                Content.Load<Model>("model/Tree_01"),
                Content.Load<Model>("model/Tree_02") };
            GrassModels = new Model[] {
                Content.Load<Model>("model/Grass_01") };

            BoxModel = Content.Load<Model>("model/Box");

            SetMaterialColor(BoxModel, WorldEffect);
            SetMaterialColor(PlayerModel, WorldEffect);
            for (int i = 0; i < TreeModels.Length; i++)
                SetMaterialColor(TreeModels[i], WorldEffect);
            for (int i = 0; i < GrassModels.Length; i++)
                SetMaterialColor(GrassModels[i], WorldEffect);
        }

        public static void SetMaterialColor(Model m, Effect e) {
            Matrix[] meshTransforms = new Matrix[m.Bones.Count];
            m.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mm in m.Meshes) {
                foreach (ModelMeshPart mmp in mm.MeshParts) {

                    if (mmp.Effect is BasicEffect) {
                        BasicEffect be = mmp.Effect as BasicEffect;
                        mmp.Tag = new Vector4(be.DiffuseColor, 1);
                    }
                }
            }
        }
    }
}
