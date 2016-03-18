using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.World {
    enum TileType {
        DesertGrass
    }
    class Tile {
        public static Texture2D[] TileSets;
        public static int TILE_SIZE = 32;

        public Texture2D Texture;

        public Area Area;

        public TileType Type;
        public Point Position;
        
        public Rectangle BackgroundSource;

        public bool HasHull;
        public Rectangle HullSource;
        public Vector2 HullOffset;
        public float HullZOffset;

        public bool Collidable;
        public bool Breakable;
        public Color MiniMapColor;

        public Tile(TileType type, Point pos, Area area) {
            Position = pos;
            Type = type;
            Area = area;
            setDefaults();
        }

        public void DrawBackground(SpriteBatch batch, Camera cam) {
            batch.Draw(Texture, new Rectangle(Position.X, Position.Y, TILE_SIZE, TILE_SIZE), BackgroundSource, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
       }

        public void DrawHull(SpriteBatch batch, Camera cam) {
            if (HasHull)
                batch.Draw(Texture, new Vector2(Position.X, Position.Y) + HullOffset, HullSource, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, (1 - Area.Height / (Position.Y + HullSource.Height + HullOffset.Y + HullZOffset)) / cam.Scale);
        }

        /// <summary>
        /// VERY messy
        /// </summary>
        void setDefaults() {
            switch (Type) {
                case TileType.DesertGrass:
                    Texture = TileSets[0];
                    Collidable = false;
                    Breakable = false;
                    MiniMapColor = new Color(212, 202, 120);
                    BackgroundSource = new Rectangle(0, 0, TILE_SIZE, TILE_SIZE);
                    break;
            }
        }
    }
}
