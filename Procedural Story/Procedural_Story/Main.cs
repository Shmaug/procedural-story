using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Procedural_Story.UI;
using Procedural_Story.World;

namespace Procedural_Story {
    class Input {
        public static KeyboardState ks, lastks;
        public static MouseState ms, lastms;
    }
    public enum GameState {
        MainMenu,
        Loading,
        InGame,
        Paused
    }
    public class Main : Game {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        System.Windows.Forms.Form form;

        Frame MainFrame;
        
        GameState GameState = GameState.MainMenu;

        Area Area;
        Player Player;

        #region content
        Texture2D CursorTexture;
        Effect WorldEffect;
        
        RenderTarget2D SceneTarget;
        #endregion

        int fps;
        int fc; // frame count
        float ft; // frame time

        public Main() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferMultiSampling = true;
            graphics.ApplyChanges();
        }
        
        protected override void Initialize() {
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += (object sender, EventArgs e) => {
                graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                graphics.ApplyChanges();

                SceneTarget = new RenderTarget2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            };
            
            form = (System.Windows.Forms.Form) System.Windows.Forms.Form.FromChildHandle(Window.Handle);
            form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            
            base.Initialize();
        }

        protected override void LoadContent() {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            UIElement.BlankTexture = new Texture2D(GraphicsDevice, 1, 1); ;
            UIElement.BlankTexture.SetData<Color>(new Color[] { Color.White });

            CursorTexture = Content.Load<Texture2D>("sprite/ui/cursor");
            Player.HudTexture = Content.Load<Texture2D>("sprite/ui/hud");
            Player.Texture = Content.Load<Texture2D>("sprite/char/player");
            Tile.TileSets = new Texture2D[] { Content.Load<Texture2D>("sprite/env/desert") };

            WorldEffect = Content.Load<Effect>("fx/world");
            
            SceneTarget = new RenderTarget2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            #region font loading
            UIElement.Fonts = new Dictionary<string, SpriteFont>();
            string[] fonts = System.IO.Directory.GetFiles(Content.RootDirectory + "/font");
            for (int i = 0; i < fonts.Length; i++) {
                string[] names = fonts[i].Split('\\');
                string name = names[names.Length - 1].Split('.')[0];
                UIElement.Fonts.Add(name, Content.Load<SpriteFont>("font/" + name));
            }
            #endregion

            #region generate UI
            MainFrame = new Frame(null, "MainFrame", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0), Color.Black);

            Frame menuFrame = new Frame(MainFrame, "Main Menu", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0), Color.Black);
            Button start = new Button(menuFrame, "startbtn", new UDim2(0, .75f, 100, -30), new UDim2(0, 0, 350, 60), "START GAME", "Avenir36", Color.White, Color.Black, loadGame);
            start.TextAlignment = AlignmentType.CenterLeft;
            Button exit = new Button(menuFrame, "exitbtn", new UDim2(0, .75f, 100, 30), new UDim2(0, 0, 350, 60), "EXIT GAME", "Avenir36", Color.White, Color.Black, exitGame);
            exit.TextAlignment = AlignmentType.CenterLeft;

            Frame loadFrame = new Frame(MainFrame, "Load", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0), Color.Black);
            loadFrame.Visible = false;
            new ImageLabel(
                new ImageLabel(loadFrame, "bar", new UDim2(.5f, .5f, -100, -10), new UDim2(0, 0, 200, 20), UIElement.BlankTexture, Color.DarkSlateGray) // oh god what have i done
                , "bar", new UDim2(0, 0, 1, 1), new UDim2(0, 1, -2, -2), UIElement.BlankTexture, Color.White);
            #endregion
        }

        protected override void UnloadContent() {

        }

        protected override void Update(GameTime gameTime) {
            UIElement.ScreenWidth = Window.ClientBounds.Width;
            UIElement.ScreenHeight = Window.ClientBounds.Height;

            Input.ks = Keyboard.GetState();
            Input.ms = Mouse.GetState();

            #region toggle fullscreen
            if (Input.ks.IsKeyDown(Keys.LeftAlt) && Input.ks.IsKeyDown(Keys.Enter) && Input.lastks.IsKeyUp(Keys.Enter))
                if (form.FormBorderStyle != System.Windows.Forms.FormBorderStyle.None) {
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                } else {
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                    form.WindowState = System.Windows.Forms.FormWindowState.Normal;
                }
            if (form.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
                form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            #endregion

            MainFrame.Update(gameTime);

            switch (GameState) {
                case GameState.Loading:
                    if (Area.LoadProgress >= 1) {
                        GameState = GameState.InGame;
                        MainFrame.Children["Load"].Visible = false;
                    }
                    MainFrame.Children["Load"].Children["bar"].Children["bar"].Size.Scale.X = Area.LoadProgress;
                    break;
                case GameState.InGame:
                    Player.Update(gameTime);
                    Camera.CurrentCamera.Position = Player.Position + new Vector2(Player.HitBox.Width * .5f, Player.HitBox.Height * .5f);

                    if (Input.ms.ScrollWheelValue > Input.lastms.ScrollWheelValue)
                        Camera.CurrentCamera.Scale *= 1.25f;
                    else if (Input.ms.ScrollWheelValue < Input.lastms.ScrollWheelValue)
                        Camera.CurrentCamera.Scale /= 1.25f;
                    Camera.CurrentCamera.Scale = MathHelper.Clamp(Camera.CurrentCamera.Scale, .5f, 3f);
                    break;
            }

            Input.lastks = Input.ks;
            Input.lastms = Input.ms;
            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime) {
            fc++; ft += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (ft >= 1f) { fps = fc; fc = 1; ft = 0; }

            GraphicsDevice.Clear(Color.Black);

            // Draw game
            switch (GameState) {
                case GameState.InGame:
                    WorldEffect.Parameters["PixelSize"].SetValue(new Vector2(1f / GraphicsDevice.PresentationParameters.BackBufferWidth, 1f / GraphicsDevice.PresentationParameters.BackBufferHeight));
                    WorldEffect.Parameters["CameraPosition"].SetValue(new Vector2(Camera.CurrentCamera.Position.X, Camera.CurrentCamera.Position.Y));
                    WorldEffect.Parameters["Proj"].SetValue(Matrix.CreateOrthographicOffCenter(0, Window.ClientBounds.Width, Window.ClientBounds.Height, 0, 0, 1) * Matrix.CreateTranslation(-1f / Window.ClientBounds.Width, 1f / Window.ClientBounds.Height, 0));
                    
                    GraphicsDevice.SetRenderTarget(SceneTarget);
                    GraphicsDevice.Clear(Color.Black);

                    spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.CurrentCamera.getMatrix());
                    if (Input.ks.IsKeyDown(Keys.F))
                        Area.DrawVoronoi(spriteBatch);
                    else {
                        Area.DrawBackground(spriteBatch);
                        Area.DrawHulls(spriteBatch);
                    }
                    Player.Draw(spriteBatch);
                    spriteBatch.End();
                    
                    GraphicsDevice.SetRenderTarget(null);
                    GraphicsDevice.Clear(Color.Black);
                    
                    Vector3[] lc = new Vector3[16];
                    Vector3[] lp = new Vector3[16];
                    lc[0] = new Vector3(1, 1, 1);
                    lp[0] = new Vector3(Player.Center + new Vector2(Player.Direction == 2 ? -30 : (Player.Direction == 1 ? 30 : 0), Player.Direction == 0 ? 30 : (Player.Direction == 3 ? -30 : 0)), 150);
                    int ln = 1;
                    if (Area.visibleLights != null)
                        for (int i = 0; i < Area.visibleLights.Length; i++)
                            if (Area.visibleLights[i] != null) {
                                lc[ln] = Area.visibleLights[i].Color.ToVector3();
                                lp[ln] = new Vector3(Area.visibleLights[i].Position, Area.visibleLights[i].Radius);
                                ln++;
                            }
                    WorldEffect.Parameters["LightCount"].SetValue(ln);
                    WorldEffect.Parameters["LightColors"].SetValue(lc);
                    WorldEffect.Parameters["LightPositions"].SetValue(lp);
                    
                    WorldEffect.CurrentTechnique = WorldEffect.Techniques["Shadow"];
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, WorldEffect);
                    spriteBatch.Draw(SceneTarget, Vector2.Zero, Color.White);
                    spriteBatch.End();
                    break;
            }

            // UI overlays
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            MainFrame.Draw(spriteBatch);
            spriteBatch.Draw(CursorTexture, new Vector2(Input.ms.X, Input.ms.Y), Color.Red);

            Debug.DrawText(spriteBatch, UIElement.Fonts["Consolas9"]);
            spriteBatch.DrawString(UIElement.Fonts["Consolas9"], fps.ToString(), new Vector2(5, Window.ClientBounds.Height - 20), fps < 20 ? Color.Red : Color.Gray);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void loadGame() {
            Area = new Area(Biome.Desert, 0);
            Area.Generate();
            Player = new Player(Area);
            Player.Position = new Vector2(Area.Width / 2 * Tile.TILE_SIZE, Area.Height / 2 * Tile.TILE_SIZE);
            Camera.CurrentCamera = new Camera();

            MainFrame.Children["Main Menu"].Visible = false;
            MainFrame.Children["Load"].Visible = true;
            GameState = GameState.Loading;
        }

        void exitGame() {
            Exit();
        }
    }
}
