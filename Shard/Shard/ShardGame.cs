using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Shard
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ShardGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D spritesheet;
        GameImageSourceDirectory sourceDirectory;

        KeyboardState previousKeyboard;
        MouseState previousMouse;
        GamePadState previousGamePad;

        //Options
        private bool realisticSpaceMovement;
        private bool automaticDeceleration;
        private bool mouseDirectionalControl;

        //Visual Graphics
        protected bool debugVisible;
        protected SpriteFont debugFont;

        List<ShardObject> shardObjects;
        Ship player;

        #region Database Fields
        DatabaseConnection objConnect;
        string conString;

        DataSet ds;
        //DataRow dRow;

        int MaxRows;
        //int inc = 0;
        #endregion

        public ShardGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.Window.Title = "Shard";
            this.IsMouseVisible = true;

            previousGamePad = GamePad.GetState(PlayerIndex.One);
            previousKeyboard = Keyboard.GetState();
            previousMouse = Mouse.GetState();

            #region Database Connection

            //try
            //{
            //    objConnect = new DatabaseConnection();
            //    conString = Shard.Properties.Settings.Default.DatabaseConnectionString;
            //    objConnect.Connection_string = conString;
            //    objConnect.Sql = Shard.Properties.Settings.Default.SQL_LoginInformation;
            //    ds = objConnect.GetConnection;
            //    MaxRows = ds.Tables[0].Rows.Count;

            //    //NavigateRecords();
            //}
            //catch (Exception err)
            //{
            //    Console.Out.Write(err.Message);
            //}

            #endregion

            //Default Options
            debugVisible = true;
            realisticSpaceMovement = false;
            automaticDeceleration = true;
            mouseDirectionalControl = false;

            shardObjects = new List<ShardObject>();

            player = new Ship(100, 100);
            player.Width = 32;
            player.Height = 32;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            debugFont = Content.Load<SpriteFont>("debugWindowFont");

            //Spritesheet Loading
            spritesheet = Content.Load<Texture2D>("spritesheet_shard_i1");
            sourceDirectory = new GameImageSourceDirectory();
            sourceDirectory.LoadSourcesFromFile(@"Content/spritesheet_shard_i1.txt");
            player.ImageSource = sourceDirectory.GetSourceRectangle("playerShip1_colored");

            //Add a bunch of debris for testing purposes
            int numDebris = 30;
            Random random = new Random();
            for (int i = 0; i < numDebris; i++)
            {
                Debris debris = new Debris(random.Next(GraphicsDevice.Viewport.Width), random.Next(GraphicsDevice.Viewport.Height));
                debris.Direction = random.NextDouble() * Math.PI * 2;
                debris.ImageSource = sourceDirectory.GetSourceRectangle("asteroid_medium1_shaded");
                debris.Width = debris.ImageSource.Width;
                debris.Height = debris.ImageSource.Height;
                shardObjects.Add(debris);
            }

            //player.ImageSource = new Rectangle(64, 32, 32, 32);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            GamePadState currentGamePad = GamePad.GetState(PlayerIndex.One);
            KeyboardState currentKeyboard = Keyboard.GetState();
            MouseState currentMouse = Mouse.GetState();

            // Exit Game
            if (currentGamePad.Buttons.Back == ButtonState.Pressed || currentKeyboard.IsKeyDown(Keys.Escape))
                this.Exit();

            // TODO: Add your update logic here

            #region Player Rotation

            double maximumRotationalVelocity = Math.PI / 16.0;
            double rotationalVelocityIncrement = .001;
            double directionalChangeIncrement = .05;

            if (!mouseDirectionalControl)
            {
                if (realisticSpaceMovement)
                {
                    if (currentKeyboard.IsKeyDown(Keys.Left) || currentKeyboard.IsKeyDown(Keys.A))
                    {
                        if (player.RotationalVelocity > -maximumRotationalVelocity)
                            player.RotationalVelocity -= rotationalVelocityIncrement;
                    }
                    if (currentKeyboard.IsKeyDown(Keys.Right) || currentKeyboard.IsKeyDown(Keys.D))
                    {
                        if (player.RotationalVelocity < maximumRotationalVelocity)
                            player.RotationalVelocity += rotationalVelocityIncrement;
                    }
                }
                else
                {
                    if (currentKeyboard.IsKeyDown(Keys.Left) || currentKeyboard.IsKeyDown(Keys.A))
                        player.Direction -= directionalChangeIncrement;
                    if (currentKeyboard.IsKeyDown(Keys.Right) || currentKeyboard.IsKeyDown(Keys.D))
                        player.Direction += directionalChangeIncrement;

                    //player.Velocity = player.Velocity; //Looks weird, is necessary
                }
            }
            else
            {
                player.RotationalVelocity = 0;
                player.Direction = Math.Atan2(currentMouse.Y - player.Y, currentMouse.X - player.X);
            }

            #endregion

            #region Player Movement and Deceleration Implementation

            double maxVelocity = 3;
            double velocityIncrement = .1;


            //Changes must be made directly to horizontal/vertical velocity of ship to simulate movement within space
            if (realisticSpaceMovement)
            {
                if (currentKeyboard.IsKeyDown(Keys.Up) || currentKeyboard.IsKeyDown(Keys.W))
                {
                    player.HorizontalVelocity += Math.Cos(player.Direction) * velocityIncrement;
                    player.VerticalVelocity += Math.Sin(player.Direction) * velocityIncrement;
                }
                else if (currentKeyboard.IsKeyDown(Keys.Down) || currentKeyboard.IsKeyDown(Keys.S))
                {
                    player.HorizontalVelocity += Math.Cos(player.Direction + Math.PI) * velocityIncrement;
                    player.VerticalVelocity += Math.Sin(player.Direction + Math.PI) * velocityIncrement;
                }
            }
            else //Ship Movement without velocity/rotation preservation
            {
                if (currentKeyboard.IsKeyDown(Keys.Up) || currentKeyboard.IsKeyDown(Keys.W))
                {
                    player.HorizontalVelocity += Math.Cos(player.Direction) * velocityIncrement;
                    player.VerticalVelocity += Math.Sin(player.Direction) * velocityIncrement;
                }
                else if (currentKeyboard.IsKeyDown(Keys.Down) || currentKeyboard.IsKeyDown(Keys.S)) // <-- Potentially Fixed Problems
                {
                    player.HorizontalVelocity -= Math.Cos(player.Direction) * velocityIncrement;
                    player.VerticalVelocity -= Math.Sin(player.Direction) * velocityIncrement;
                }
            }

            //Automatic Deceleration of Player Ship Movement and Rotation
            if (automaticDeceleration)
            {
                //Player Rotation Deceleration
                if (!currentKeyboard.IsKeyDown(Keys.Left) && !currentKeyboard.IsKeyDown(Keys.Right) && !currentKeyboard.IsKeyDown(Keys.W) && !currentKeyboard.IsKeyDown(Keys.S))
                {
                    if (Math.Abs(player.RotationalVelocity) < rotationalVelocityIncrement)
                        player.RotationalVelocity = 0;
                    else
                    {
                        if (GetSign(player.RotationalVelocity) > 0)
                            player.RotationalVelocity -= rotationalVelocityIncrement / 2;
                        else if(GetSign(player.RotationalVelocity) < 0)
                            player.RotationalVelocity += rotationalVelocityIncrement / 2;
                    }
                }

                //Player Movement Deceleration
                if (!currentKeyboard.IsKeyDown(Keys.Down) && !currentKeyboard.IsKeyDown(Keys.Up) && !currentKeyboard.IsKeyDown(Keys.W) && !currentKeyboard.IsKeyDown(Keys.S))
                {
                    if (player.Velocity < velocityIncrement)
                        player.Velocity = 0;
                    else
                    {
                        if (realisticSpaceMovement)
                        {
                            //double directionOfVelocity = Math.Atan2(player.VerticalVelocity, player.HorizontalVelocity);
                            //player.HorizontalVelocity += Math.Cos(player.Direction) * velocityIncrement / 2.0;
                            //player.VerticalVelocity += Math.Sin(player.Direction) * velocityIncrement / 2.0;

                            //if (player.HorizontalVelocity > 0)
                            //    player.HorizontalVelocity -= Math.Cos(player.Direction) * velocityIncrement / 2.0;
                            //else if (player.HorizontalVelocity < 0)
                            //    player.HorizontalVelocity += Math.Cos(player.Direction) * velocityIncrement / 2.0;

                            //if (player.VerticalVelocity > 0)
                            //    player.VerticalVelocity -= Math.Sin(player.Direction) * velocityIncrement / 2.0;
                            //else if (player.VerticalVelocity < 0)
                            //    player.VerticalVelocity += Math.Sin(player.Direction) * velocityIncrement / 2.0;

                            if (player.HorizontalVelocity > 0)
                                player.HorizontalVelocity -= velocityIncrement / 2.0;
                            else if (player.HorizontalVelocity < 0)
                                player.HorizontalVelocity += velocityIncrement / 2.0;

                            if (player.VerticalVelocity > 0)
                                player.VerticalVelocity -= velocityIncrement / 2.0;
                            else if (player.VerticalVelocity < 0)
                                player.VerticalVelocity += velocityIncrement / 2.0;
                        }
                        else
                        {
                            if (player.HorizontalVelocity > 0)
                                player.HorizontalVelocity -= velocityIncrement / 2.0;
                            else if (player.HorizontalVelocity < 0)
                                player.HorizontalVelocity += velocityIncrement / 2.0;

                            if (player.VerticalVelocity > 0)
                                player.VerticalVelocity -= velocityIncrement / 2.0;
                            else if (player.VerticalVelocity < 0)
                                player.VerticalVelocity += velocityIncrement / 2.0;
                            
                            //if (player.Velocity >= velocityIncrement)
                                //player.Velocity -= velocityIncrement / 2.0;
                        }
                    }
                }
            }

            //Maximum Velocity Control

            if (Math.Abs(player.HorizontalVelocity) > maxVelocity)
            {
                player.HorizontalVelocity = maxVelocity * GetSign(player.HorizontalVelocity);
            }

            if (Math.Abs(player.VerticalVelocity) > maxVelocity)
            {
                player.VerticalVelocity = maxVelocity * GetSign(player.VerticalVelocity);
            }

            #endregion

            //Shooting
            if ((currentMouse.LeftButton.Equals(ButtonState.Pressed) && mouseDirectionalControl) || (currentKeyboard.IsKeyDown(Keys.Space) && !mouseDirectionalControl))
            {
                Projectile p = new Projectile((int)player.X + (int)player.Width / 2, (int)player.Y + (int)player.Height / 2);
                p.ImageSource = sourceDirectory.GetSourceRectangle("shipBullet");
                p.Width = p.ImageSource.Width;
                p.Height = p.ImageSource.Height;
                p.Direction = player.Direction; //Math.Atan2(currentMouse.Y - player.Y, currentMouse.X - player.X);
                p.RotationalVelocity = player.RotationalVelocity;
                p.Velocity = 8;
                shardObjects.Add(p);
            }

            player.Update(new List<GameObject>(), gameTime);

            foreach (ShardObject so in shardObjects)
            {
                so.Update(new List<GameObject>(),gameTime);
            }

            if (player.X > GraphicsDevice.Viewport.Width)
                player.X = 0;
            if (player.X + player.Width < 0)
                player.X = GraphicsDevice.Viewport.Width;
            if (player.Y > GraphicsDevice.Viewport.Height)
                player.Y = 0;
            if (player.Y + player.Height < 0)
                player.Y = GraphicsDevice.Viewport.Height;

            //Update Previous States
            previousGamePad = currentGamePad;
            previousKeyboard = currentKeyboard;

            base.Update(gameTime);
        }

        #region Helper Methods

        private int GetSign(double value)
        {
            if (value < 0)
                return -1;
            if (value == 0)
                return 0;
            else
                return 1;
        }

        #endregion

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin();
            foreach (ShardObject so in shardObjects)
            {
                so.Draw(spriteBatch, spritesheet);
            }
            player.Draw(spriteBatch, spritesheet);
            DrawDebugWindow(spriteBatch, Color.Red);
            //spriteBatch.Draw(spritesheet, new Rectangle(32,32,32,32), new Rectangle(0,0,32,32), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        //precondition: spriteBatch.Begin() has been called
        protected void DrawDebugWindow(SpriteBatch spriteBatch, Color textColor)
        {
            if (debugVisible)
            {
                List<string> debugLines = new List<string>();
                debugLines.Add("Player Ship Coordinates: (" + player.X + ", " + player.Y + ")");
                debugLines.Add("xVelocity: " + player.HorizontalVelocity + "   yVelocity: " + player.VerticalVelocity);
                debugLines.Add("Rotational Velocity: " + player.RotationalVelocity);
                debugLines.Add("ShardObject Size: " + shardObjects.Count);

                Vector2 offset = new Vector2(4, 2);
                foreach (string line in debugLines)
                {
                    spriteBatch.DrawString(debugFont, line, offset, textColor);
                    offset.Y += debugFont.MeasureString(line).Y - 1;
                }
            }
        }
    }
}
