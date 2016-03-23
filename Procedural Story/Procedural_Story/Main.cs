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
using Procedural_Story.Util;

using Jitter;
using Jitter.LinearMath;

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

    class JitterDrawer : IDebugDrawer {
        GraphicsDevice device;
        BasicEffect effect;

        public JitterDrawer(GraphicsDevice dev) {
            device = dev;
            effect = new BasicEffect(device);
            effect.LightingEnabled = false;
        }

        public void DrawLine(JVector start, JVector end) {
            VertexPositionColor[] line = new VertexPositionColor[2] {
                new VertexPositionColor(new Vector3(start.X, start.Y, start.Z), Color.White),
                new VertexPositionColor(new Vector3(end.X, end.Y, end.Z), Color.White),
            };

            effect.World = Matrix.Identity;
            effect.View = Camera.CurrentCamera.View;
            effect.Projection = Camera.CurrentCamera.Projection;

            foreach (EffectPass p in effect.CurrentTechnique.Passes) {
                p.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, line, 0, 1);
            }
        }

        public void DrawPoint(JVector pos) {

        }

        public void DrawTriangle(JVector pos1, JVector pos2, JVector pos3) {
            VertexPositionColor[] tri = new VertexPositionColor[3] {
                new VertexPositionColor(new Vector3(pos1.X, pos1.Y, pos1.Z), Color.Green),
                new VertexPositionColor(new Vector3(pos2.X, pos2.Y, pos2.Z), Color.Green),
                new VertexPositionColor(new Vector3(pos3.X, pos3.Y, pos3.Z), Color.Green),
            };

            effect.World = Matrix.Identity;
            effect.View = Camera.CurrentCamera.View;
            effect.Projection = Camera.CurrentCamera.Projection;

            foreach (EffectPass p in effect.CurrentTechnique.Passes) {
                p.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleList, tri, 0, 1);
            }
        }
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

        RenderTarget2D DepthTarget;
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
                graphics.PreferMultiSampling = true;
                graphics.ApplyChanges();

                SceneTarget = new RenderTarget2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
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

            Models.Load(Content);

            SceneTarget = new RenderTarget2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

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

            DepthTarget = new RenderTarget2D(GraphicsDevice, 4096, 4096, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8);
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
                        Player.Position = new Vector3(0, Area.CellAt(Player.Position).Elevation + Player.Height * .5f, 0);
                    }
                    MainFrame.Children["Load"].Children["bar"].Children["bar"].Size.Scale.X = Area.LoadProgress;
                    break;
                case GameState.InGame:
                    if (Input.ms.RightButton == ButtonState.Pressed)
                        Camera.CurrentCamera.Rotation.Y -= (Input.ms.X - Input.lastms.X) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;
                    
                    Area.Update(gameTime);

                    if (Input.ms.RightButton == ButtonState.Pressed) {
                        //Camera.CurrentCamera.Rotation.X -= (Input.ms.Y - Input.lastms.Y) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;
                        Camera.CurrentCamera.Rotation.Y -= (Input.ms.X - Input.lastms.X) * (float)gameTime.ElapsedGameTime.TotalSeconds * .3f;
                    }
                    Camera.CurrentCamera.Rotation.X = MathHelper.ToRadians(-25);
                    Camera.CurrentCamera.Position = Player.Position + Vector3.Up * 2 + Camera.CurrentCamera.RotationMatrix.Backward * 10;

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
                    Camera.CurrentCamera.AspectRatio = (float)UIElement.ScreenWidth / UIElement.ScreenHeight;
                    
                    if (Input.ks.IsKeyDown(Keys.F))
                        GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.CullCounterClockwiseFace, FillMode = FillMode.WireFrame };
                    GraphicsDevice.BlendState = BlendState.Opaque;
                    GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

                    // draw depth
                    GraphicsDevice.SetRenderTarget(DepthTarget);
                    GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                    GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferFunction = CompareFunction.LessEqual };
                    Models.SceneEffect.Parameters["DepthDraw"].SetValue(true);
                    Area.Draw(GraphicsDevice, true);
                    
                    // draw scene
                    GraphicsDevice.SetRenderTarget(SceneTarget);
                    GraphicsDevice.Clear(Color.SkyBlue);
                    GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    Models.SceneEffect.Parameters["DepthDraw"].SetValue(false);
                    Models.SceneEffect.Parameters["DepthTexture"].SetValue(DepthTarget);
                    Models.SceneEffect.Parameters["DepthPixelSize"].SetValue(new Vector2(1f / DepthTarget.Width, 1f / DepthTarget.Height));
                    Area.Draw(GraphicsDevice, false);

                    GraphicsDevice.SetRenderTarget(null);
                    GraphicsDevice.Clear(Color.Black);
                    
                    spriteBatch.Begin();
                    spriteBatch.Draw(SceneTarget, Vector2.Zero, Color.White);
                    spriteBatch.End();
                    break;
            }

            // UI overlays
            spriteBatch.Begin();
            MainFrame.Draw(spriteBatch);
            spriteBatch.Draw(CursorTexture, new Vector2(Input.ms.X, Input.ms.Y), Color.Red);

            Debug.DrawText(spriteBatch, UIElement.Fonts["Consolas9"]);
            spriteBatch.DrawString(UIElement.Fonts["Consolas9"], fps.ToString(), new Vector2(5, Window.ClientBounds.Height - 20), fps < 20 ? Color.Red : Color.DarkGray);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void loadGame() {

            Area = new Area(Biome.Forest, 0);

            Player = new Player(Area);

            Area.Generate(GraphicsDevice);
            Area.Physics.AddBody(Player.RigidBody);
            Area.WorldObjects.Add(Player);

            MainFrame.Children["Main Menu"].Visible = false;
            MainFrame.Children["Load"].Visible = true;
            GameState = GameState.Loading;

            Camera.CurrentCamera = new Camera();
        }

        void exitGame() {
            Exit();
        }
    }
}
