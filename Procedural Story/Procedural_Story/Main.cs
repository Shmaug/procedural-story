using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Procedural_Story.UI;
using Procedural_Story.Core;
using Procedural_Story.Core.Life;
using Procedural_Story.Util;

using Jitter;
using Jitter.LinearMath;
using Procedural_Story.Core.Crops;

namespace Procedural_Story {
    class Input {
        public static KeyboardState ks, lastks;

        public static bool KeyPressed(Keys key) {
            return ks.IsKeyDown(key) && lastks.IsKeyUp(key);
        }
        public static bool KeyReleased(Keys key) {
            return ks.IsKeyUp(key) && lastks.IsKeyDown(key);
        }

        public static MouseState ms, lastms;

        public static bool MouseBlocked;
        public static bool KeysBlocked;

        public static bool calcKeysBlocked() {
            return Main.Instance.MainFrame.AreTextBoxesFocused();
        }
        public static bool calcMouseBlocked() {
            return Main.Instance.MainFrame.IntersectsChildren(ms.X, ms.Y);
        }

        /// <summary>
        /// Tries to convert keyboard input to characters and prevents repeatedly returning the 
        /// same character if a key was pressed last frame, but not yet unpressed this frame.
        /// </summary>
        /// <param name="keyboard">The current KeyboardState</param>
        /// <param name="oldKeyboard">The KeyboardState of the previous frame</param>
        /// <param name="key">When this method returns, contains the correct character if conversion succeeded.
        /// Else contains the null, (000), character.</param>
        /// <returns>True if conversion was successful</returns>
        public static bool TryConvertKeyboardInput(Keys k, out char key) {
            bool shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);
            
            switch (k) {
                //Alphabet keys
                case Keys.A: if (shift) { key = 'A'; } else { key = 'a'; } return true;
                case Keys.B: if (shift) { key = 'B'; } else { key = 'b'; } return true;
                case Keys.C: if (shift) { key = 'C'; } else { key = 'c'; } return true;
                case Keys.D: if (shift) { key = 'D'; } else { key = 'd'; } return true;
                case Keys.E: if (shift) { key = 'E'; } else { key = 'e'; } return true;
                case Keys.F: if (shift) { key = 'F'; } else { key = 'f'; } return true;
                case Keys.G: if (shift) { key = 'G'; } else { key = 'g'; } return true;
                case Keys.H: if (shift) { key = 'H'; } else { key = 'h'; } return true;
                case Keys.I: if (shift) { key = 'I'; } else { key = 'i'; } return true;
                case Keys.J: if (shift) { key = 'J'; } else { key = 'j'; } return true;
                case Keys.K: if (shift) { key = 'K'; } else { key = 'k'; } return true;
                case Keys.L: if (shift) { key = 'L'; } else { key = 'l'; } return true;
                case Keys.M: if (shift) { key = 'M'; } else { key = 'm'; } return true;
                case Keys.N: if (shift) { key = 'N'; } else { key = 'n'; } return true;
                case Keys.O: if (shift) { key = 'O'; } else { key = 'o'; } return true;
                case Keys.P: if (shift) { key = 'P'; } else { key = 'p'; } return true;
                case Keys.Q: if (shift) { key = 'Q'; } else { key = 'q'; } return true;
                case Keys.R: if (shift) { key = 'R'; } else { key = 'r'; } return true;
                case Keys.S: if (shift) { key = 'S'; } else { key = 's'; } return true;
                case Keys.T: if (shift) { key = 'T'; } else { key = 't'; } return true;
                case Keys.U: if (shift) { key = 'U'; } else { key = 'u'; } return true;
                case Keys.V: if (shift) { key = 'V'; } else { key = 'v'; } return true;
                case Keys.W: if (shift) { key = 'W'; } else { key = 'w'; } return true;
                case Keys.X: if (shift) { key = 'X'; } else { key = 'x'; } return true;
                case Keys.Y: if (shift) { key = 'Y'; } else { key = 'y'; } return true;
                case Keys.Z: if (shift) { key = 'Z'; } else { key = 'z'; } return true;

                //Decimal keys
                case Keys.D0: if (shift) { key = ')'; } else { key = '0'; } return true;
                case Keys.D1: if (shift) { key = '!'; } else { key = '1'; } return true;
                case Keys.D2: if (shift) { key = '@'; } else { key = '2'; } return true;
                case Keys.D3: if (shift) { key = '#'; } else { key = '3'; } return true;
                case Keys.D4: if (shift) { key = '$'; } else { key = '4'; } return true;
                case Keys.D5: if (shift) { key = '%'; } else { key = '5'; } return true;
                case Keys.D6: if (shift) { key = '^'; } else { key = '6'; } return true;
                case Keys.D7: if (shift) { key = '&'; } else { key = '7'; } return true;
                case Keys.D8: if (shift) { key = '*'; } else { key = '8'; } return true;
                case Keys.D9: if (shift) { key = '('; } else { key = '9'; } return true;

                //Decimal numpad keys
                case Keys.NumPad0: key = '0'; return true;
                case Keys.NumPad1: key = '1'; return true;
                case Keys.NumPad2: key = '2'; return true;
                case Keys.NumPad3: key = '3'; return true;
                case Keys.NumPad4: key = '4'; return true;
                case Keys.NumPad5: key = '5'; return true;
                case Keys.NumPad6: key = '6'; return true;
                case Keys.NumPad7: key = '7'; return true;
                case Keys.NumPad8: key = '8'; return true;
                case Keys.NumPad9: key = '9'; return true;

                //Special keys
                case Keys.OemTilde: if (shift) { key = '~'; } else { key = '`'; } return true;
                case Keys.OemSemicolon: if (shift) { key = ':'; } else { key = ';'; } return true;
                case Keys.OemQuotes: if (shift) { key = '"'; } else { key = '\''; } return true;
                case Keys.OemQuestion: if (shift) { key = '?'; } else { key = '/'; } return true;
                case Keys.OemPlus: if (shift) { key = '+'; } else { key = '='; } return true;
                case Keys.OemPipe: if (shift) { key = '|'; } else { key = '\\'; } return true;
                case Keys.OemPeriod: if (shift) { key = '>'; } else { key = '.'; } return true;
                case Keys.OemOpenBrackets: if (shift) { key = '{'; } else { key = '['; } return true;
                case Keys.OemCloseBrackets: if (shift) { key = '}'; } else { key = ']'; } return true;
                case Keys.OemMinus: if (shift) { key = '_'; } else { key = '-'; } return true;
                case Keys.OemComma: if (shift) { key = '<'; } else { key = ','; } return true;
                case Keys.Space: key = ' '; return true;
            }

            key = (char)0;
            return false;
        }
    }
    public enum GameState {
        MainMenu,
        Loading,
        InGame,
        Paused
    }
    class JitterDrawer : IDebugDrawer {
        GraphicsDevice device;
        Effect effect;

        public JitterDrawer(GraphicsDevice dev, Effect e) {
            device = dev;
            effect = e;
        }

        public void DrawLine(JVector start, JVector end) {
            VertexPositionColor[] line = new VertexPositionColor[2] {
                new VertexPositionColor(new Vector3(start.X, start.Y, start.Z), Color.White),
                new VertexPositionColor(new Vector3(end.X, end.Y, end.Z), Color.White),
            };

            effect.Parameters["World"].SetValue(Matrix.Identity);
            effect.Parameters["ViewProj"].SetValue(Camera.CurrentCamera.View * Camera.CurrentCamera.Projection);
            effect.CurrentTechnique = effect.Techniques["VBO"];

            foreach (EffectPass p in effect.CurrentTechnique.Passes) {
                p.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, line, 0, 1);
            }
        }

        public void DrawPoint(JVector pos) {

        }

        public void DrawTriangle(JVector pos1, JVector pos2, JVector pos3) {
            JVector n = JVector.Cross(pos2 - pos1, pos3 - pos1);
            n.Normalize();
            Vector3 xn = new Vector3(n.X, n.Y, n.Z);
            VertexPositionColorNormal[] tri = new VertexPositionColorNormal[3] {
                new VertexPositionColorNormal(new Vector3(pos1.X, pos1.Y, pos1.Z), Color.Green, xn),
                new VertexPositionColorNormal(new Vector3(pos3.X, pos3.Y, pos3.Z), Color.Green, xn),
                new VertexPositionColorNormal(new Vector3(pos2.X, pos2.Y, pos2.Z), Color.Green, xn),
            };

            effect.Parameters["World"].SetValue(Matrix.Identity);
            effect.Parameters["ViewProj"].SetValue(Camera.CurrentCamera.View * Camera.CurrentCamera.Projection);
            effect.CurrentTechnique = effect.Techniques["VBO"];

            foreach (EffectPass p in effect.CurrentTechnique.Passes) {
                p.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleList, tri, 0, 1);
            }
        }
    }

    class Main : Game {
        public static Main Instance;
        
        System.Windows.Forms.Form form;

        GameState GameState = GameState.MainMenu;

        public Frame MainFrame;
        Frame CommandWindow;
        
        public Area Area;
        public Player Player;
        
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

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

            Instance = this;
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
            
            form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromChildHandle(Window.Handle);
            form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            
            base.Initialize();
        }

        protected override void LoadContent() {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            UIElement.BlankTexture = new Texture2D(GraphicsDevice, 1, 1); ;
            UIElement.BlankTexture.SetData(new Color[] { Color.White });

            UIElement.ClickSound = Content.Load<SoundEffect>("audio/ui/button");
            
            CursorTexture = Content.Load<Texture2D>("sprite/icon/cursor");

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

            #region item icon loading
            Item.ItemIcons = new Dictionary<string, Texture2D>();
            string[] items = System.IO.Directory.GetFiles(Content.RootDirectory + "/sprite/icon/items");
            for (int i = 0; i < items.Length; i++) {
                string[] names = items[i].Split('\\');
                string name = names[names.Length - 1].Split('.')[0];
                Item.ItemIcons.Add(name, Content.Load<Texture2D>("sprite/icon/items/" + name));
            }
            #endregion

            #region generate UI
            MainFrame = new Frame(null, "MainFrame", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0), Color.Transparent);

            Frame menuFrame = new Frame(MainFrame, "Main Menu", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0), Color.Black);
            TextButton start = new TextButton(menuFrame, "startbtn", new UDim2(0, .75f, 100, -30), new UDim2(0, 0, 350, 60), "START GAME", "Avenir36", Color.White, Color.Black, loadGame);
            start.TextAlignment = AlignmentType.CenterLeft;
            TextButton exit = new TextButton(menuFrame, "exitbtn", new UDim2(0, .75f, 100, 30), new UDim2(0, 0, 350, 60), "EXIT GAME", "Avenir36", Color.White, Color.Black, exitGame);
            exit.TextAlignment = AlignmentType.CenterLeft;

            Frame loadFrame = new Frame(MainFrame, "Load", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, 0), Color.Black);
            loadFrame.Visible = false;
            new TextLabel(loadFrame, "text", new UDim2(.5f, .5f, -100, -100), new UDim2(0, 0, 200, 20), "Loading", "Avenir36", Color.White); // oh god what have i done
            new ImageLabel(
                new ImageLabel(loadFrame, "bar", new UDim2(.5f, .5f, -100, -10), new UDim2(0, 0, 200, 20), UIElement.BlankTexture, Color.DarkSlateGray) // oh god what have i done
                , "bar", new UDim2(0, 0, 1, 1), new UDim2(0, 1, -2, -2), UIElement.BlankTexture, Color.White);

            CommandWindow = new Frame(MainFrame, "cmdWindow", new UDim2(.1f, 0, 0, 0), new UDim2(.8f, 0, 0, 300), Color.Black);
            new Frame(CommandWindow, "line", new UDim2(0, 1, 0, -36), new UDim2(1, 0, 0, 1), Color.White);
            CommandLine cmd = new CommandLine(CommandWindow, "cmd", new UDim2(0, 1, 0, -30), new UDim2(1, 0, 0, 30), "", "Consolas14", new Color(0f, 1f, 0f));
            TextLabel box = new TextLabel(CommandWindow, "prev", new UDim2(0, 0, 0, 0), new UDim2(1, 1, 0, -30), "", "Consolas14", Color.White);
            cmd.Tag = box;
            box.TextAlignment = AlignmentType.BottomLeft;
            CommandWindow.Visible = false;
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
            Input.MouseBlocked = Input.calcMouseBlocked();
            Input.KeysBlocked = Input.calcKeysBlocked();

            #region toggle fullscreen
            if (Input.ks.IsKeyDown(Keys.LeftAlt) && Input.KeyPressed(Keys.Enter) && !Input.KeysBlocked)
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

            if (Input.KeyPressed(Keys.OemTilde)) {
                CommandWindow.Visible = !CommandWindow.Visible;
                if (CommandWindow.Visible)
                    (CommandWindow["cmd"] as TextBox).Focused = true;
            }
            Debug.Update();
            
            switch (GameState) {
                case GameState.Loading:
                    if (Area.LoadProgress >= 2) {
                        GameState = GameState.InGame;
                        MainFrame["Load"].Visible = false;
                        Player.Position = new Vector3(Area.RealWidth * .5f, Area.HeightAt(Area.RealWidth * .5f, Area.RealLength * .5f), Area.RealLength * .5f);
                    }
                    try {
                        (MainFrame["Load"]["text"] as TextLabel).Text = Area.LoadMessage;
                         MainFrame["Load"]["bar"]["bar"].Size.Scale.X = Area.LoadProgress;
                    } catch { }
                    break;
                case GameState.InGame:
                    Area.Update(gameTime);
                    break;
            }
            
            MainFrame.Update(gameTime);

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
                    DrawGame();
                    break;
            }

            // UI overlays
            spriteBatch.Begin();
            MainFrame.Draw(spriteBatch);
            spriteBatch.Draw(CursorTexture, new Vector2(Input.ms.X, Input.ms.Y), Color.Red);

            if (!CommandWindow.Visible)
                Debug.Draw(spriteBatch, UIElement.Fonts["Consolas9"]);

            spriteBatch.DrawString(UIElement.Fonts["Consolas9"], fps.ToString(), new Vector2(5, Window.ClientBounds.Height - 20), fps < 20 ? Color.Red : Color.DarkGray);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void DrawGame() {
            Camera.CurrentCamera.AspectRatio = (float)UIElement.ScreenWidth / UIElement.ScreenHeight;

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            // draw depth
            GraphicsDevice.SetRenderTarget(DepthTarget);
            GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferFunction = CompareFunction.LessEqual };
            Models.WorldEffect.Parameters["DepthDraw"].SetValue(true);
            Area.Draw(GraphicsDevice, true);

            // draw scene
            GraphicsDevice.SetRenderTarget(SceneTarget);
            GraphicsDevice.Clear(Color.LightSkyBlue);
            GraphicsDevice.RasterizerState = new RasterizerState() {
                FillMode = Debug.DrawWireFrame ? FillMode.WireFrame : FillMode.Solid,
                CullMode = CullMode.CullCounterClockwiseFace
            };
            Models.WorldEffect.Parameters["DepthDraw"].SetValue(false);
            Models.WorldEffect.Parameters["DepthTexture"].SetValue(DepthTarget);
            Models.WorldEffect.Parameters["DepthPixelSize"].SetValue(new Vector2(1f / DepthTarget.Width, 1f / DepthTarget.Height));
            Area.Draw(GraphicsDevice, false);

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            Color c = Color.White;

            spriteBatch.Begin();
            spriteBatch.Draw(SceneTarget, Vector2.Zero, c);
            Player.DrawHud(spriteBatch);
            spriteBatch.End();
        }

        void loadGame() {

            Area = new Area(Biome.Forest, 0);

            Player = new Player(Area);
            Area.AddCharacter(Player);

            Area.Generate(GraphicsDevice);

            MainFrame["Main Menu"].Visible = false;
            MainFrame["Load"].Visible = true;
            GameState = GameState.Loading;

            Camera.CurrentCamera = new Camera();
        }

        void exitGame() {
            Exit();
        }
    }
}
