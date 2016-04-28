using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Procedural_Story.UI;

namespace Procedural_Story {
    static class Debug {
        public static bool DrawWireFrame;
        public static bool DrawSettlements = true;
        public static bool DrawTerrain = true;
        public static bool DrawPaths;

        static List<string> logs = new List<string>();
        static Dictionary<string, string> labels = new Dictionary<string, string>();
        
        public static void Log(object l) {
            logs.Add(l.ToString());
            if (logs.Count > 10)
                logs.RemoveAt(0);
        }

        public static void Track(object l, string name) {
            labels[name] = l?.ToString() ?? "null";
        }

        public static void Update() {
            if (Input.KeyPressed(Keys.D1) && !Input.KeysBlocked)
                DrawTerrain = !DrawTerrain;
            if (Input.KeyPressed(Keys.D2) && !Input.KeysBlocked)
                DrawSettlements = !DrawSettlements;
            if (Input.KeyPressed(Keys.D3) && !Input.KeysBlocked)
                DrawPaths = !DrawPaths;
            if (Input.KeyPressed(Keys.D0) && !Input.KeysBlocked)
                DrawWireFrame = !DrawWireFrame;
        }
        
        public static void Draw(SpriteBatch batch, SpriteFont font) {
            int h = (int)font.MeasureString("|").Y;
            int y = 10;
            for (int i = 0; i < logs.Count; i++) {
                string s = logs[i].ToString();
                batch.Draw(UIElement.BlankTexture, new Rectangle(7, y - 3, (int)font.MeasureString(s).X + 6, h + 3), Color.Black * .75f);
                batch.DrawString(font, s, new Vector2(10, y), Color.White);

                y += h + 5;
            }

            y = 10;
            foreach (KeyValuePair<string, string> l in labels) {
                batch.Draw(UIElement.BlankTexture, new Rectangle(UIElement.ScreenWidth / 2 - 3, y - 3, (int)font.MeasureString(l.Value).X + 6, h + 3), Color.Black * .75f);
                batch.DrawString(font, l.Value, new Vector2(UIElement.ScreenWidth * .5f, y), Color.White);

                y += h + 5;
            }
        }
    }
}
