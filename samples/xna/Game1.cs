using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Twitch.Broadcast;

namespace TwitchTvTestXna
{
    public class Game1 : Game
    {
        private const string ClientId = "";
        private const string ClientSecret = "";
        private const string User = "";
        private const string Password = "";

        private readonly GraphicsDeviceManager m_Graphics = null;
        private Texture2D m_ImageTexture = null;
        private SpriteBatch m_SpriteBatch = null;
        private RenderTarget2D m_RenderTarget = null;
        private readonly Stopwatch m_StopWatch = new Stopwatch();
        private readonly XnaBroadcastController m_BroadcastController = new XnaBroadcastController();
        private VideoParams m_VideoParams = null;
        private bool m_StartBroadcasting = true;

        private DateTime m_LastBackBufferResize = DateTime.Now;
        private int m_NextResizeDelta = 128;
        private DateTime m_LastCaptureTime = DateTime.MinValue;

        public Game1()
        {
            m_Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            m_Graphics.PreferredBackBufferWidth = 1280;
            m_Graphics.PreferredBackBufferHeight = 720;

            m_BroadcastController.BroadcastStateChanged += MBroadcastControllerOnBroadcastStateChanged;

            m_BroadcastController.ClientId = ClientId;
            m_BroadcastController.Initialize();
        }

        private void MBroadcastControllerOnBroadcastStateChanged(BroadcastController.BroadcastState state)
        {
            switch (state)
            {
                case BroadcastController.BroadcastState.Initialized:
                {
                    m_BroadcastController.ClientSecret = ClientSecret;
                    m_BroadcastController.RequestAuthToken(User, Password);
                    break;
                }
                case BroadcastController.BroadcastState.ReadyToBroadcast:
                {
                    if (m_StartBroadcasting)
                    {
                        m_VideoParams = m_BroadcastController.GetRecommendedVideoParams(1280, 720, 30);
                        m_VideoParams.PixelFormat = PixelFormat.TTV_PF_BGRA;
                        m_BroadcastController.SetGraphicsDevice(this.GraphicsDevice);
                        m_BroadcastController.StartBroadcasting(m_VideoParams);
                        m_StartBroadcasting = false;
                    }
                    break;
                }
            }
        }

        protected override void LoadContent()
        {
            var singlePixel = new[] {Color.White};
            m_ImageTexture = new Texture2D(GraphicsDevice, 1, 1);
            m_ImageTexture.SetData(singlePixel);
            m_SpriteBatch = new SpriteBatch(GraphicsDevice);
            m_RenderTarget = new RenderTarget2D(GraphicsDevice, 1280, 720);
        }

        protected override void Update(GameTime gameTime)
        {
            m_BroadcastController.Update();
        }

        protected override void Draw(GameTime gameTime)
        {
            //Draw to RT
            GraphicsDevice.SetRenderTarget(m_RenderTarget);
            GraphicsDevice.Clear(Color.Aqua);
            m_SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            for (int x = 0; x < 1280; x += 20)
            {
                m_SpriteBatch.Draw(m_ImageTexture, new Rectangle((int)(620 + 620 * Math.Sin(gameTime.TotalGameTime.TotalSeconds + x / 200.0)), (int)(360 + 360 * Math.Cos(gameTime.TotalGameTime.TotalSeconds + x / 200.0)), 200, 200), new Color(0, 0, ((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds + x)/1.5f)/2f + .5f) * .3f);
                m_SpriteBatch.Draw(m_ImageTexture, new Rectangle(x, (int)(360 + 360 * Math.Sin(gameTime.TotalGameTime.TotalSeconds + x / 500.0)), 20, 20), new Color((float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + x + 50) /2)/2f + .5f, 0, 0));
                m_SpriteBatch.Draw(m_ImageTexture, new Rectangle(x, (int)(360 + 360 * Math.Cos(gameTime.TotalGameTime.TotalSeconds + x / 200.0)), 30, 30), new Color(0, (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + x) * 2)/2f + .5f, 0));
            }
            m_SpriteBatch.End();

            //Draw to the screen
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            m_SpriteBatch.Begin();
            //sprite.Draw(rt, new Vector2(640, 360), null, Color.White, (float)gameTime.TotalGameTime.TotalSeconds, new Vector2(640, 360), 1, SpriteEffects.None, 0);
            m_SpriteBatch.Draw(m_RenderTarget, Vector2.Zero,Color.White);
            m_SpriteBatch.End();

            //Rt usage is over so we can grab the frame

            if (m_BroadcastController.IsBroadcasting)
            {
                // pace the submissions to the FPS of the broadcast or you'll be doing a lot of work for nothing
                DateTime now = DateTime.Now;
                TimeSpan delta = now - m_LastCaptureTime;
                if (delta.TotalMilliseconds >= (1000 / m_VideoParams.TargetFps))
                {
                    m_LastCaptureTime = now;

                    m_StopWatch.Reset();
                    m_StopWatch.Start();
                    m_BroadcastController.SubmitFrame(m_RenderTarget);
                    m_StopWatch.Stop();

                    // Don't print this or it'll slow down the entire game as it flushes to the console
                    //Debug.WriteLine(_s.ElapsedMilliseconds);
                }
            }
            
            KeyboardState ks = Keyboard.GetState();
            if (ks.IsKeyDown(Keys.Space))
            {
                TimeSpan delta = DateTime.Now - m_LastBackBufferResize;
                if (delta.TotalMilliseconds >= 500)
                {
                    m_LastBackBufferResize = DateTime.Now;

                    m_Graphics.PreferredBackBufferWidth = m_Graphics.PreferredBackBufferWidth + m_NextResizeDelta;
                    m_Graphics.PreferredBackBufferHeight = m_Graphics.PreferredBackBufferHeight + m_NextResizeDelta;
                    m_Graphics.ApplyChanges();

                    m_NextResizeDelta = -m_NextResizeDelta;
                }
            }

            base.Draw(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            if (m_BroadcastController.IsInitialized)
            {
                m_BroadcastController.SetGraphicsDevice(null);
                m_BroadcastController.Shutdown();
            }
            base.OnExiting(sender, args);
        }
    }
}
