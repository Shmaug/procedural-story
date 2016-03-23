using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Procedural_Story.Util {
    static class Models {
        public static Effect SceneEffect;

        public static Model PlayerModel;
        public static Model[] TreeModels;
        public static Model[] GrassModels;
        public static Model BoxModel;

        public static void Load(ContentManager Content) {
            SceneEffect = Content.Load<Effect>("fx/model");

            PlayerModel = Content.Load<Model>("model/player");
            TreeModels = new Model[] {
                Content.Load<Model>("model/Tree_01"),
                Content.Load<Model>("model/Tree_02") };
            GrassModels = new Model[] {
                Content.Load<Model>("model/Grass_01") };

            BoxModel = Content.Load<Model>("model/Box");

            SetMaterialColor(BoxModel, SceneEffect);
            SetMaterialColor(PlayerModel, SceneEffect);
            for (int i = 0; i < TreeModels.Length; i++)
                SetMaterialColor(TreeModels[i], SceneEffect);
            for (int i = 0; i < GrassModels.Length; i++)
                SetMaterialColor(GrassModels[i], SceneEffect);
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
