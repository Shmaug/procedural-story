using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story {
    class Debug {
        static List<string> logs = new List<string>();
        static Dictionary<int, string> labels = new Dictionary<int, string>();

        static Dictionary<int, Tuple<BoundingBox, Color, float>> boxes = new Dictionary<int, Tuple<BoundingBox, Color, float>>();

        static int f1 = 0;

        public static void Log(object l) {
            logs.Add(l.ToString());
            if (logs.Count > 10)
                logs.Remove(logs[0]);
            f1 = 200;
        }

        public static void Track(object l, int slot) {
            labels[slot] = l != null ? l.ToString() : "null";
        }
        
        public static void DrawText(SpriteBatch batch, SpriteFont font) {
            string str = "";
            foreach (string l in logs)
                str += l + "\n";

            batch.Draw(UI.UIElement.BlankTexture, new Rectangle(6, 6, 300, 160), Color.Black * .75f * MathHelper.Clamp(f1 / 10f, 0, 1));
            f1--;
            if (f1 < 0) f1 = 0;
            batch.DrawString(font, str, Vector2.One * 10, Color.White);

            string str2 = "";
            foreach (KeyValuePair<int, string> l in labels)
                str2 += l.Value + "\n";
            batch.Draw(UI.UIElement.BlankTexture, new Rectangle(506, 6, (int)font.MeasureString(str2).X + 6, (int)font.MeasureString(str2).Y), Color.Black * .5f);
            batch.DrawString(font, str2, Vector2.One * 10 + Vector2.UnitX * 500, Color.White);

            labels = new Dictionary<int, string>();
        }
    }
}
