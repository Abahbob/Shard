using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Shard
{
    public class ShardGame : Microsoft.Xna.Framework.Game
    {
        //debug option
        private bool skipLoadingFromDatabase = false;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D spritesheet;
        Texture2D menusheet;
        Texture2D background;
        GameImageSourceDirectory gameSourceDirectory;
        GameImageSourceDirectory menuSourceDirectory;
        String username;
        public DateTime SaveTime;

        Song songy;

        KeyboardState previousKeyboard;
        MouseState previousMouse;
        GamePadState previousGamePad;
        Rectangle previousScreen;
        Vector2 previousPosition;

        Quadtree collisionQuadtree;

        //Options
        private bool gamePaused;
        private bool gameMuted;
        private bool realisticSpaceMovement;
        private bool automaticDeceleration;
        private bool mouseDirectionalControl;

        //Visual Graphics Options
        protected bool debugVisible;
        protected SpriteFont debugFont;
        protected SpriteFont statusIndicatorFont;
        protected SpriteFont menuFont;

        protected bool staticBackground;

        List<ShardObject> shardObjects;
        List<GameMenu> shardMenus;
        Ship player;
        int maximumPlayerHealth;

        public SoundPlayer soundPlayer;

        protected Camera camera;
        protected float zoomZ;
        protected int zoomIO;
        protected bool isZooming;
        int count;

        //Database
        private XMLDatabase database;
        //private float bgZoomZ;
        protected Camera bgCam;
        protected double bgHM, bgVM;

        protected Rectangle[] backgrounds;
        protected Rectangle currentRect;
        protected Rectangle centerRect;

        private TitleScreen ts;

        private bool fixSkipping = true;

        public ShardGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            username = "debugMode";
        }

        public ShardGame(String username, TitleScreen ts)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.username = username;
            this.ts = ts;
        }

        protected override void Initialize()
        {
            this.Window.Title = "Shard";
            this.IsMouseVisible = true;
            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferredBackBufferWidth = 720;
            graphics.ApplyChanges();

            previousGamePad = GamePad.GetState(PlayerIndex.One);
            previousKeyboard = Keyboard.GetState();
            previousMouse = Mouse.GetState();

            soundPlayer = new SoundPlayer();

            songy = Content.Load<Song>("Sounds/Musicmusic");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(songy);

            //Database
            System.IO.Directory.CreateDirectory("SaveData");
            database = new XMLDatabase("SaveData/" + username + ".xml", "objects", true);

            //Default Options
            //Visual
            debugVisible = false;
            staticBackground = false;

            //In-Game
            gamePaused = false;
            if (ts == null)
            {
                gameMuted = false;
                realisticSpaceMovement = false;
                automaticDeceleration = true;
                mouseDirectionalControl = false;
            }
            else
            {
                gameMuted = ts.mute;
                realisticSpaceMovement = ts.rsm;
                automaticDeceleration = ts.decel;
                mouseDirectionalControl = false;
            }


            shardObjects = new List<ShardObject>();
            shardMenus = new List<GameMenu>();

            player = new Ship(0, 0, true, ref soundPlayer);

            camera = new Camera(player.Width / 2, player.Height / 2);
            camera.ScreenWidth = GraphicsDevice.Viewport.Width;
            camera.ScreenHeight = GraphicsDevice.Viewport.Height;
            zoomZ = 1;
            zoomIO = 2;
            isZooming = false;

            bgCam = new Camera(0, 0);
            //bgZoomZ = 1.3f;
            bgHM = 0;
            bgVM = 0;

            collisionQuadtree = new Quadtree(0, new Rectangle(0, 0, (int)camera.ScreenWidth, (int)camera.ScreenHeight));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            debugFont = Content.Load<SpriteFont>("Fonts//debugWindowFont");
            statusIndicatorFont = Content.Load<SpriteFont>("Fonts//statusIndicatorFont");
            menuFont = Content.Load<SpriteFont>("Fonts//menuFont");

            //Spritesheet Loading
            spritesheet = Content.Load<Texture2D>("Spritesheets//spritesheet_shard_final");
            gameSourceDirectory = new GameImageSourceDirectory();
            gameSourceDirectory.LoadSourcesFromFile(@"Content/Spritesheets//spritesheet_shard_final.txt");

            menusheet = Content.Load<Texture2D>("Spritesheets//menusheet_shard_final");
            menuSourceDirectory = new GameImageSourceDirectory();
            menuSourceDirectory.LoadSourcesFromFile(@"Content/Spritesheets//menusheet_shard_final.txt");

            #region Menu Creation

            Rectangle menuBackgroundSource = menuSourceDirectory.GetSourceRectangle("grayMenuPanel");
            MenuImage menuBackground = new MenuImage(new Vector2(GraphicsDevice.Viewport.Width / 2 - menuBackgroundSource.Width / 2, GraphicsDevice.Viewport.Height / 2 - menuBackgroundSource.Height / 2), menuBackgroundSource, .8f);
            menuBackground.Depth = 0f;

            #region Introduction Menu

            GameMenu introMenu = new GameMenu(this);
            introMenu.Name = "Intro";
            introMenu.SetGamePauseEffect(true);
            introMenu.Active = false;
            introMenu.AddMenuImage(menuBackground);

            MenuImage closeButtonImage3 = new MenuImage(new Vector2(0, (int)menuBackground.Y), menuSourceDirectory.GetSourceRectangle("menuExitButton"), .5f);
            closeButtonImage3.X = menuBackground.X + menuBackground.Width - closeButtonImage3.Width;
            Button close3 = new CloseButton(this, closeButtonImage3);
            introMenu.AddButton(close3);

            MenuText introText = new MenuText(new Vector2((int)menuBackground.X + 12, (int)menuBackground.Y + 12), "You wake up, the image of Earth's\ncataclysm still burning in your mind.\nAs you look out your starboard window\nyou see the shattered remains of your home\ndrift by. In the distance, you can see the\nblue shells of the vessels that appeared\nseconds before Earth's destruction began.\nThere is no doubt that these alien\nconstructions were the cause of Earth's\ndemise. In memory of your fallen people,\nyou swear vengeance on the mysterious\naggresors, you will let none leave alive.", menuFont);
            introMenu.AddMenuText(introText);

            shardMenus.Add(introMenu);

            #endregion

            #region Pause Menu

            GameMenu pauseMenu = new GameMenu(this);
            pauseMenu.Name = "Pause";
            pauseMenu.SetGamePauseEffect(true);
            pauseMenu.Active = false;
            pauseMenu.AddMenuImage(menuBackground);

            MenuImage closeButtonImage = new MenuImage(new Vector2(0, (int)menuBackground.Y), menuSourceDirectory.GetSourceRectangle("menuExitButton"), .5f);
            closeButtonImage.X = menuBackground.X + menuBackground.Width - closeButtonImage.Width;
            Button close = new CloseButton(this, closeButtonImage);
            pauseMenu.AddButton(close);

            MenuText optionsText = new MenuText(new Vector2((int)menuBackground.X + 16, (int)menuBackground.Y + 36), "Options: ", menuFont);
            pauseMenu.AddMenuText(optionsText);
            MenuImage optionsButtonImage = new MenuImage(new Vector2((int)menuBackground.X + 80, (int)menuBackground.Y + 36), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button options = new OptionsButton(this, optionsButtonImage);
            pauseMenu.AddButton(options);

            MenuText upgradesText = new MenuText(new Vector2((int)menuBackground.X + 16, (int)optionsButtonImage.Y + optionsButtonImage.Height + 8), "Upgrades: ", menuFont);
            pauseMenu.AddMenuText(upgradesText);
            MenuImage upgradesButtonImage = new MenuImage(new Vector2((int)optionsButtonImage.X, (int)upgradesText.Y), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button upgrades = new UpgradesButton(this, upgradesButtonImage);
            pauseMenu.AddButton(upgrades);

            MenuText saveMenuText = new MenuText(new Vector2((int)menuBackground.X + 16, (int)upgradesButtonImage.Y + upgradesButtonImage.Height + 8), "Save/Load:", menuFont);
            pauseMenu.AddMenuText(saveMenuText);
            MenuImage saveMenuButtonImage = new MenuImage(new Vector2((int)optionsButtonImage.X, (int)upgradesButtonImage.Y + upgradesButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button saveMenuButton = new SaveMenuButton(this, saveMenuButtonImage);
            pauseMenu.AddButton(saveMenuButton);

            shardMenus.Add(pauseMenu);

            #endregion

            #region Win Menu

            GameMenu winMenu = new GameMenu(this);
            winMenu.Name = "Win";
            winMenu.SetGamePauseEffect(true);
            winMenu.Active = false;
            winMenu.AddMenuImage(menuBackground);

            string winningText = "You Win!";
            MenuText winText = new MenuText(new Vector2((int)menuBackground.X + menuBackground.Width / 2 - menuFont.MeasureString(winningText).X / 2 + 1, (int)menuBackground.Y + menuBackground.Height / 2 - menuFont.MeasureString(winningText).Y / 2), winningText, menuFont);
            winMenu.AddMenuText(winText);

            winMenu.AddButton(close);

            shardMenus.Add(winMenu);
            
            #endregion

            #region Options Menu

            GameMenu optionsMenu = new GameMenu(this);
            optionsMenu.Name = "Options";
            optionsMenu.SetGamePauseEffect(true);
            optionsMenu.Active = false;
            optionsMenu.AddMenuImage(menuBackground);
            string headText = "Options";
            MenuText headerText = new MenuText(new Vector2((int)menuBackground.X + menuBackground.Width / 2 - menuFont.MeasureString(headText).X / 2 + 1, (int)menuBackground.Y + 12), headText, menuFont);
            optionsMenu.AddMenuText(headerText);

            optionsMenu.AddButton(close);

            MenuImage muteButtonImage = new MenuImage(new Vector2((int)menuBackground.X + 16, (int)menuBackground.Y + 36), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button mute = new MuteButton(this, muteButtonImage);
            optionsMenu.AddButton(mute);
            MenuText muteText = new MenuText(new Vector2((int)muteButtonImage.X + muteButtonImage.Width + 12, (int)muteButtonImage.Y), "Mute: ", menuFont);
            optionsMenu.AddMenuText(muteText);

            MenuImage rsmButtonImage = new MenuImage(new Vector2((int)muteButtonImage.X, (int)muteButtonImage.Y + muteButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button rsm = new RealisticSpaceMovementToggleButton(this, rsmButtonImage);
            optionsMenu.AddButton(rsm);
            MenuText rsmText = new MenuText(new Vector2((int)rsmButtonImage.X + rsmButtonImage.Width + 12, (int)rsmButtonImage.Y), "Realistic Movement: ", menuFont);
            optionsMenu.AddMenuText(rsmText);

            MenuImage adButtonImage = new MenuImage(new Vector2((int)rsmButtonImage.X, (int)rsmButtonImage.Y + rsmButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button ad = new AutomaticDecelerationToggleButton(this, adButtonImage);
            optionsMenu.AddButton(ad);
            MenuText adText = new MenuText(new Vector2((int)adButtonImage.X + adButtonImage.Width + 12, (int)adButtonImage.Y), "Auto-Stop: " + "Off", menuFont);
            optionsMenu.AddMenuText(adText);

            shardMenus.Add(optionsMenu);

            #endregion

            #region Game Over Menu

            GameMenu gameOverMenu = new GameMenu(this);
            gameOverMenu.Name = "GameOver";
            gameOverMenu.SetGamePauseEffect(true);
            gameOverMenu.Active = false;
            gameOverMenu.AddMenuImage(menuBackground);
            headText = "Game Over";
            headerText = new MenuText(new Vector2((int)menuBackground.X + menuBackground.Width / 2 - menuFont.MeasureString(headText).X / 2, (int)menuBackground.Y + 12), headText, menuFont);
            gameOverMenu.AddMenuText(headerText);

            MenuImage restartButtonImage = new MenuImage(new Vector2((int)menuBackground.X + 16, (int)menuBackground.Y + 36), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button restart = new RestartButton(this, restartButtonImage);
            gameOverMenu.AddButton(restart);
            MenuText restartText = new MenuText(new Vector2((int)muteButtonImage.X + muteButtonImage.Width + 12, (int)muteButtonImage.Y), "Continue from last Save? ", menuFont);
            gameOverMenu.AddMenuText(restartText);

            shardMenus.Add(gameOverMenu);

            #endregion

            #region Upgrade Menu

            GameMenu upgradeMenu = new GameMenu(this);
            upgradeMenu.Name = "Upgrades";
            upgradeMenu.SetGamePauseEffect(true);
            upgradeMenu.Active = false;
            upgradeMenu.AddMenuImage(menuBackground);
            headText = "Upgrade";
            headerText = new MenuText(new Vector2((int)menuBackground.X + menuBackground.Width / 2 - menuFont.MeasureString(headText).X / 2, (int)menuBackground.Y + 12), headText, menuFont);
            upgradeMenu.AddMenuText(headerText);

            Button closeUpgrade = new CloseButton(this, closeButtonImage);
            upgradeMenu.AddButton(closeUpgrade);

            MenuImage repairButtonImage = new MenuImage(new Vector2((int)menuBackground.X + 16, (int)menuBackground.Y + 36), menuSourceDirectory.GetSourceRectangle("emptyButton"));
            repairButtonImage.Depth = .5f;
            Button repair = new RepairButton(this, repairButtonImage);
            upgradeMenu.AddButton(repair);
            MenuText repairText = new MenuText(new Vector2((int)repairButtonImage.X + repairButtonImage.Width + 12, (int)repairButtonImage.Y), "Repair Cost: " + "0" + " Ore", menuFont);
            upgradeMenu.AddMenuText(repairText);

            MenuImage upgradeGunButtonImage = new MenuImage(new Vector2((int)repairButtonImage.X, (int)repairButtonImage.Y + repairButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"));
            upgradeGunButtonImage.Depth = .5f;
            Button upgradeGun = new UpgradeGunButton(this, upgradeGunButtonImage);
            upgradeMenu.AddButton(upgradeGun);
            MenuText gunText = new MenuText(new Vector2((int)upgradeGunButtonImage.X + upgradeGunButtonImage.Width + 12, (int)upgradeGunButtonImage.Y), "Gun Upgrade Cost:", menuFont);
            upgradeMenu.AddMenuText(gunText);

            MenuImage upgradeMissileButtonImage = new MenuImage(new Vector2((int)upgradeGunButtonImage.X, (int)upgradeGunButtonImage.Y + upgradeGunButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"));
            upgradeMissileButtonImage.Depth = .5f;
            Button upgradeMissile = new UpgradeMissileButton(this, upgradeMissileButtonImage);
            upgradeMenu.AddButton(upgradeMissile);
            MenuText missileText = new MenuText(new Vector2((int)upgradeMissileButtonImage.X + upgradeMissileButtonImage.Width + 12, (int)upgradeMissileButtonImage.Y), "Missile Upgrade Cost:", menuFont);
            upgradeMenu.AddMenuText(missileText);

            MenuImage upgradeSpeedButtonImage = new MenuImage(new Vector2((int)upgradeMissileButtonImage.X, (int)upgradeMissileButtonImage.Y + upgradeMissileButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"));
            upgradeSpeedButtonImage.Depth = .5f;
            Button upgradeSpeed = new UpgradeSpeedButton(this, upgradeSpeedButtonImage);
            upgradeMenu.AddButton(upgradeSpeed);
            MenuText speedText = new MenuText(new Vector2((int)upgradeSpeedButtonImage.X + upgradeSpeedButtonImage.Width + 12, (int)upgradeSpeedButtonImage.Y), "Speed Upgrade Cost:", menuFont);
            upgradeMenu.AddMenuText(speedText);

            MenuImage upgradeArmorButtonImage = new MenuImage(new Vector2((int)upgradeSpeedButtonImage.X, (int)upgradeSpeedButtonImage.Y + upgradeSpeedButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"));
            upgradeArmorButtonImage.Depth = .5f;
            Button upgradeArmor = new UpgradeArmorButton(this, upgradeArmorButtonImage);
            upgradeMenu.AddButton(upgradeArmor);
            MenuText armorText = new MenuText(new Vector2((int)upgradeArmorButtonImage.X + upgradeArmorButtonImage.Width + 12, (int)upgradeArmorButtonImage.Y), "Armor Upgrade Cost:", menuFont);
            upgradeMenu.AddMenuText(armorText);

            shardMenus.Add(upgradeMenu);

            #endregion

            #region Save Menu

            GameMenu saveMenu = new GameMenu(this);
            saveMenu.Name = "Save";
            saveMenu.SetGamePauseEffect(true);
            saveMenu.Active = false;
            saveMenu.AddMenuImage(menuBackground);

            saveMenu.AddButton(close);

            MenuImage saveButtonImage = new MenuImage(new Vector2((int)menuBackground.X + 16, (int)menuBackground.Y + 36), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button save = new SaveButton(this, saveButtonImage);
            saveMenu.AddButton(save);
            MenuText saveText = new MenuText(new Vector2((int)saveButtonImage.X + saveButtonImage.Width + 12, (int)saveButtonImage.Y), "Save: \nLast Save: " + DateTime.Now, menuFont);
            saveMenu.AddMenuText(saveText);

            MenuImage loadButtonImage = new MenuImage(new Vector2((int)saveButtonImage.X, (int)saveButtonImage.Y + saveButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button load = new LoadButton(this, loadButtonImage);
            saveMenu.AddButton(load);
            MenuText loadText = new MenuText(new Vector2((int)loadButtonImage.X + loadButtonImage.Width + 12, (int)loadButtonImage.Y), "Load: ", menuFont);
            saveMenu.AddMenuText(loadText);

            MenuImage quitGameButtonImage = new MenuImage(new Vector2((int)loadButtonImage.X, (int)loadButtonImage.Y + loadButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button quitGame = new QuitGameButton(this, quitGameButtonImage);
            saveMenu.AddButton(quitGame);
            MenuText quitGameText = new MenuText(new Vector2((int)quitGameButtonImage.X + quitGameButtonImage.Width + 12, (int)quitGameButtonImage.Y), "Quit Game: ", menuFont);
            saveMenu.AddMenuText(quitGameText);

            MenuImage quitAppButtonImage = new MenuImage(new Vector2((int)quitGameButtonImage.X, (int)quitGameButtonImage.Y + quitGameButtonImage.Height + 8), menuSourceDirectory.GetSourceRectangle("emptyButton"), .5f);
            Button quitApp = new QuitAppButton(this, quitAppButtonImage);
            saveMenu.AddButton(quitApp);
            MenuText quitAppText = new MenuText(new Vector2((int)quitAppButtonImage.X + quitAppButtonImage.Width + 12, (int)quitAppButtonImage.Y), "Quit Application: ", menuFont);
            saveMenu.AddMenuText(quitAppText);

            shardMenus.Add(saveMenu);

            #endregion

            gamePaused = false;

            #endregion

            //Background Loading
            background = Content.Load<Texture2D>("Backgrounds//seamlessNebulaBackground");

            backgrounds = new Rectangle[9];
            backgrounds[4] = background.Bounds;
            currentRect = background.Bounds;
            centerRect = background.Bounds;
            int cc = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (!(i == 1 && j == 1))
                    {
                        backgrounds[cc] = new Rectangle(j * (background.Bounds.Width) - 800, i * (background.Bounds.Height) - 420, background.Bounds.Width, background.Bounds.Height);
                        cc++;
                    }
                }
            }

            soundPlayer.LoadSounds(Content);

            if (database.isEmpty() || skipLoadingFromDatabase)
            {
                //Player Creation
                player.ImageSource = gameSourceDirectory.GetSourceRectangle("playerShip1_colored");
                player.Alignment = Alignment.GOOD;
                player.Width = player.ImageSource.Width;
                player.Height = player.ImageSource.Height;
                player.Health = 100;
                player.GunLevel = 1;
                player.MissileLevel = 1;
                player.ArmorLevel = 1;
                player.SpeedLevel = 1;
                player.Energy = 100;
                player.Ore = 100;
                player.Oxygen = 20;
                player.Water = 15;
                player.MaximumHealth = player.Health;
                shardObjects.Add(player);

                //Add boss for testing
                BossShip boss = new BossShip(200, 200, ref soundPlayer);
                boss.MaximumHealth = 1000;
                boss.Health = boss.MaximumHealth;
                boss.FollowingDistance = 200;
                boss.ActivationRange = 750;
                boss.Velocity = 0;
                boss.GetImageSource(gameSourceDirectory);

                //shardObjects.Add(boss);

                GenerateRandomWorld(-200, -200, 4000, 4000);

                ToggleMenu("Intro");

                //Add a bunch of debris for testing purposes
                //int numDebris = 0;
                //Random random = new Random();
                //for (int i = 0; i < numDebris; i++)
                //{
                //    //Debris debris = new Debris(random.Next(-GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Width), random.Next(-GraphicsDevice.Viewport.Height,GraphicsDevice.Viewport.Height));
                //    Debris debris = new Debris(random.Next(GraphicsDevice.Viewport.Width), random.Next(GraphicsDevice.Viewport.Height));
                //    debris.Alignment = Shard.Alignment.NEUTRAL;
                //    debris.Health = 10;
                //    debris.Energy = 10;
                //    debris.Ore = 10;
                //    debris.Oxygen = 10;
                //    debris.Water = 10;
                //    debris.Direction = random.NextDouble() * Math.PI * 2;
                //    debris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_medium1_shaded");
                //    debris.Width = debris.ImageSource.Width;
                //    debris.Height = debris.ImageSource.Height;
                //    shardObjects.Add(debris);
                //}

                ////Add evil ships
                //int numEnemies = 0;
                //for (int i = 0; i < numEnemies; i++)
                //{
                //    EnemyShip evil = new Bruiser((int)(random.NextDouble() * 1000), (int)(random.NextDouble() * 1000));
                //    evil.MaximumHealth = 20;
                //    evil.Health = evil.MaximumHealth;
                //    evil.GunLevel = 2;
                //    evil.MissileLevel = 1;
                //    evil.ArmorLevel = 1;
                //    evil.Velocity = 0;
                //    evil.GetImageSource(gameSourceDirectory);
                //    evil.GiveListReference(shardObjects);
                //    shardObjects.Add(evil);
                //}
                SaveGame();
            }
            else
            {
                LoadGame(database);
            }

        }

        #region Returning Important Fields

        internal List<ShardObject> GetShardObjectList()
        {
            return this.shardObjects;
        }

        internal GameImageSourceDirectory GetGameSourceDirectory()
        {
            return this.gameSourceDirectory;
        }

        internal GameImageSourceDirectory GetMenuSourceDirectory()
        {
            return this.menuSourceDirectory;
        }

        internal Ship Player
        {
            get
            {
                return this.player;
            }
        }

        public bool Paused
        {
            get
            {
                return gamePaused;
            }
            set
            {
                gamePaused = value;
            }
        }

        public bool Muted
        {
            get
            {
                return gameMuted;
            }
            set
            {
                gameMuted = value;
            }
        }

        public bool AutomaticDeceleration
        {
            get
            {
                return automaticDeceleration;
            }
            set
            {
                this.automaticDeceleration = value;
            }
        }

        public bool MouseDirectionalControl
        {
            get
            {
                return mouseDirectionalControl;
            }
            set
            {
                this.mouseDirectionalControl = value;
            }
        }

        public bool RealisticSpaceMovement
        {
            get
            {
                return realisticSpaceMovement;
            }
            set
            {
                this.realisticSpaceMovement = value;
            }
        }

        #endregion

        protected override void UnloadContent(){}

        protected virtual void GenerateRandomWorld(int startingX, int startingY, int width, int height)
        {
            #region Debris Creation

            int numberSmallDebris = 100;
            int numberMediumDebris = 100;
            int numberLargeDebris = 50;

            #region Small Debris

            for (int i = 0; i < numberSmallDebris; i++)
            {
                Debris debris = new Debris(EuclideanMath.RandomInteger(startingX, startingX + width), EuclideanMath.RandomInteger(startingY, startingY + height));
                debris.Alignment = Shard.Alignment.NEUTRAL;
                debris.Health = 15;

                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    debris.Energy = EuclideanMath.RandomInteger(1, 3);
                debris.Ore = EuclideanMath.RandomInteger(2, 5);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    debris.Oxygen = EuclideanMath.RandomInteger(1, 3);
                debris.Water = EuclideanMath.RandomInteger(1, 4);

                debris.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                int image = EuclideanMath.RandomInteger(0, 2);
                switch(image)
                {
                    case 0:
                        debris.ImageSource = gameSourceDirectory.GetSourceRectangle("smallAsteroid1_shaded");
                        break;
                    case 1:
                        debris.ImageSource = gameSourceDirectory.GetSourceRectangle("smallAsteroid2");
                        break;
                    default:
                        debris.ImageSource = gameSourceDirectory.GetSourceRectangle("smallAsteroid3");
                        break;
                }
                debris.Width = debris.ImageSource.Width;
                debris.Height = debris.ImageSource.Height;
                shardObjects.Add(debris);
            }

            #endregion

            #region Medium Debris

            for (int i = 0; i < numberMediumDebris; i++)
            {
                Debris debris = new Debris(EuclideanMath.RandomInteger(startingX, startingX + width), EuclideanMath.RandomInteger(startingY, startingY + height));
                debris.Alignment = Shard.Alignment.NEUTRAL;
                debris.Health = EuclideanMath.RandomInteger(35,60);

                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    debris.Energy = EuclideanMath.RandomInteger(2, 4);
                debris.Ore = EuclideanMath.RandomInteger(4, 9);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    debris.Oxygen = EuclideanMath.RandomInteger(2, 4);
                debris.Water = EuclideanMath.RandomInteger(3, 7);

                debris.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                int image = EuclideanMath.RandomInteger(0, 1);
                switch (image)
                {
                    case 0:
                        debris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_medium1_shaded");
                        break;
                    default:
                        debris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_medium3_shaded");
                        break;
                }
                debris.Width = debris.ImageSource.Width;
                debris.Height = debris.ImageSource.Height;
                shardObjects.Add(debris);
            }

            #endregion

            #region Large Debris

            for (int i = 0; i < numberLargeDebris; i++)
            {
                Debris debris = new Debris(EuclideanMath.RandomInteger(startingX, startingX + width), EuclideanMath.RandomInteger(startingY, startingY + height));
                debris.Alignment = Shard.Alignment.NEUTRAL;
                debris.Health = EuclideanMath.RandomInteger(35, 60);

                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    debris.Energy = EuclideanMath.RandomInteger(3, 8);
                debris.Ore = EuclideanMath.RandomInteger(7, 15);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    debris.Oxygen = EuclideanMath.RandomInteger(3, 8);
                debris.Water = EuclideanMath.RandomInteger(5, 12);

                debris.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                debris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_large1_shaded");
                debris.Width = debris.ImageSource.Width;
                debris.Height = debris.ImageSource.Height;
                shardObjects.Add(debris);
            }

            #endregion

            #endregion

            #region Enemy Creation

            int numberWeakEnemies = 25;
            int numberMediumEnemies = 20;
            int numberStrongEnemies = 15;
            int numberSuperEnemies = 7;

            #region Weaker Enemies, Top Left

            for (int i = 0; i < numberWeakEnemies; i++)
            {
                EnemyShip enemy;
                int shipType = EuclideanMath.RandomInteger(0, 1);
                switch (shipType)
                {
                    case 0:
                        enemy = new Seeker(0, 0, ref soundPlayer);
                        ((Seeker)enemy).FollowingDistance = EuclideanMath.RandomInteger(150, 250);
                        break;
                    default:
                        enemy = new Thug(0, 0, ref soundPlayer);
                        ((Thug)enemy).FollowingDistance = EuclideanMath.RandomInteger(100, 200);
                        break;
                }
                enemy.Alignment = Alignment.EVIL;
                enemy.GetImageSource(gameSourceDirectory);
                enemy.Width = enemy.ImageSource.Width;
                enemy.Height = enemy.ImageSource.Height;
                enemy.X = EuclideanMath.RandomInteger(startingX, startingX + width / 2);
                enemy.Y = EuclideanMath.RandomInteger(startingY, startingY + height / 2);

                enemy.Energy = EuclideanMath.RandomInteger(2, 7);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Ore = EuclideanMath.RandomInteger(1, 5);
                enemy.Oxygen = EuclideanMath.RandomInteger(2, 7);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Water = EuclideanMath.RandomInteger(1, 5);

                enemy.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                enemy.Velocity = 0;
                enemy.GiveListReference(shardObjects);

                enemy.ActivationRange = 250;

                enemy.MaximumHealth = EuclideanMath.RandomInteger(15,35);
                enemy.Health = enemy.MaximumHealth;
                enemy.GunLevel = EuclideanMath.RandomInteger(1,2);
                enemy.MissileLevel = EuclideanMath.RandomInteger(1, 2);
                enemy.SpeedLevel = EuclideanMath.RandomInteger(1, 2);
                enemy.ArmorLevel = 1;

                shardObjects.Add(enemy);
            }

            #endregion

            #region Medium Enemies, Top Right

            for (int i = 0; i < numberMediumEnemies; i++)
            {
                EnemyShip enemy;
                int shipType = EuclideanMath.RandomInteger(0, 1);
                switch (shipType)
                {
                    case 0:
                        enemy = new Seeker(0, 0, ref soundPlayer);
                        ((Seeker)enemy).FollowingDistance = EuclideanMath.RandomInteger(150, 250);
                        break;
                    case 1:
                        enemy = new Thug(0, 0, ref soundPlayer);
                        ((Thug)enemy).FollowingDistance = EuclideanMath.RandomInteger(100, 200);
                        break;
                    default:
                        enemy = new Bruiser(0, 0, ref soundPlayer);
                        ((Bruiser)enemy).FollowingDistance = EuclideanMath.RandomInteger(175, 300);
                        break;
                }
                enemy.Alignment = Alignment.EVIL;
                enemy.GetImageSource(gameSourceDirectory);
                enemy.Width = enemy.ImageSource.Width;
                enemy.Height = enemy.ImageSource.Height;
                enemy.X = EuclideanMath.RandomInteger(startingX + width / 2, startingX + width);
                enemy.Y = EuclideanMath.RandomInteger(startingY, startingY + height / 2);

                enemy.Energy = EuclideanMath.RandomInteger(3, 9);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Ore = EuclideanMath.RandomInteger(2, 6);
                enemy.Oxygen = EuclideanMath.RandomInteger(3, 9);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Water = EuclideanMath.RandomInteger(2, 6);

                enemy.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                enemy.Velocity = 0;
                enemy.GiveListReference(shardObjects);

                enemy.ActivationRange = 250;

                enemy.MaximumHealth = EuclideanMath.RandomInteger(30, 55);
                enemy.Health = enemy.MaximumHealth;
                enemy.GunLevel = EuclideanMath.RandomInteger(2, 4);
                enemy.MissileLevel = EuclideanMath.RandomInteger(1, 3);
                enemy.SpeedLevel = EuclideanMath.RandomInteger(1, 3);
                enemy.ArmorLevel = EuclideanMath.RandomInteger(1, 2);

                shardObjects.Add(enemy);
            }

            #endregion

            #region Strong Enemies, Bottom Right

            for (int i = 0; i < numberStrongEnemies; i++)
            {
                EnemyShip enemy;
                int shipType = EuclideanMath.RandomInteger(0, 2);
                switch (shipType)
                {
                    case 0:
                        enemy = new Seeker(0, 0, ref soundPlayer);
                        ((Seeker)enemy).FollowingDistance = EuclideanMath.RandomInteger(150, 250);
                        break;
                    case 1:
                        enemy = new Thug(0, 0, ref soundPlayer);
                        ((Thug)enemy).FollowingDistance = EuclideanMath.RandomInteger(100, 200);
                        break;
                    default:
                        enemy = new Bruiser(0, 0, ref soundPlayer);
                        ((Bruiser)enemy).FollowingDistance = EuclideanMath.RandomInteger(175, 300);
                        break;
                }
                enemy.Alignment = Alignment.EVIL;
                enemy.GetImageSource(gameSourceDirectory);
                enemy.Width = enemy.ImageSource.Width;
                enemy.Height = enemy.ImageSource.Height;
                enemy.X = EuclideanMath.RandomInteger(startingX + width / 2, startingX + width);
                enemy.Y = EuclideanMath.RandomInteger(startingY + height / 2, startingY + height);

                enemy.Energy = EuclideanMath.RandomInteger(5, 12);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Ore = EuclideanMath.RandomInteger(3, 8);
                enemy.Oxygen = EuclideanMath.RandomInteger(5, 12);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Water = EuclideanMath.RandomInteger(3, 8);

                enemy.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                enemy.Velocity = 0;
                enemy.GiveListReference(shardObjects);

                enemy.ActivationRange = 250;

                enemy.MaximumHealth = EuclideanMath.RandomInteger(45, 70);
                enemy.Health = enemy.MaximumHealth;
                enemy.GunLevel = EuclideanMath.RandomInteger(2, 5);
                enemy.MissileLevel = EuclideanMath.RandomInteger(2, 4);
                enemy.SpeedLevel = EuclideanMath.RandomInteger(2, 4);
                enemy.ArmorLevel = EuclideanMath.RandomInteger(2, 3);

                shardObjects.Add(enemy);
            }

            #endregion

            #region Super Enemies, Bottom Left

            for (int i = 0; i < numberSuperEnemies; i++)
            {
                EnemyShip enemy = new Bruiser(0, 0, ref soundPlayer);
                ((Bruiser)enemy).FollowingDistance = EuclideanMath.RandomInteger(175, 300);
                enemy.Alignment = Alignment.EVIL;
                enemy.GetImageSource(gameSourceDirectory);
                enemy.Width = enemy.ImageSource.Width;
                enemy.Height = enemy.ImageSource.Height;
                enemy.X = EuclideanMath.RandomInteger(startingX, startingX + width / 2);
                enemy.Y = EuclideanMath.RandomInteger(startingY + height / 2, startingY + height);

                enemy.Energy = EuclideanMath.RandomInteger(10, 20);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Ore = EuclideanMath.RandomInteger(6, 14);
                enemy.Oxygen = EuclideanMath.RandomInteger(10, 20);
                if (EuclideanMath.RandomInteger(0, 1) == 1)
                    enemy.Water = EuclideanMath.RandomInteger(6, 14);

                enemy.Direction = EuclideanMath.RandomDouble() * Math.PI * 2;
                enemy.Velocity = 0;
                enemy.GiveListReference(shardObjects);

                enemy.ActivationRange = 250;

                enemy.MaximumHealth = EuclideanMath.RandomInteger(75, 100);
                enemy.Health = enemy.MaximumHealth;
                enemy.GunLevel = EuclideanMath.RandomInteger(3, 5);
                enemy.MissileLevel = EuclideanMath.RandomInteger(3, 5);
                enemy.SpeedLevel = EuclideanMath.RandomInteger(3, 5);
                enemy.ArmorLevel = EuclideanMath.RandomInteger(3, 5);

                shardObjects.Add(enemy);
            }

            #endregion

            #region Boss Spawning

            BossShip boss = new BossShip(startingX + 200, startingY + height - 200, ref soundPlayer);
            boss.MaximumHealth = 1000;
            boss.Health = boss.MaximumHealth;
            boss.FollowingDistance = 200;
            boss.ActivationRange = 750;
            boss.Velocity = 0;
            boss.GetImageSource(gameSourceDirectory);

            shardObjects.Add(boss);

            #endregion

            #endregion

        }

        protected override void Update(GameTime gameTime)
        {
            if (fixSkipping) Paused = false;
            GamePadState currentGamePad = GamePad.GetState(PlayerIndex.One);
            KeyboardState currentKeyboard = Keyboard.GetState();
            MouseState currentMouse = Mouse.GetState();
            bool pauseStateChanged = false;

            if (Muted)
            {
                MediaPlayer.IsMuted = true;
            }
            if (!Muted && MediaPlayer.IsMuted == true)
            {
                MediaPlayer.IsMuted = false;
            }

            if (!player.IsValid())
            {
                for (int i = 0; i < shardMenus.Count; i++)
                {
                    if (shardMenus[i].Name.Equals("GameOver") && !shardMenus[i].Active)
                        ToggleMenu("GameOver");
                }
            }

            // Exit Game
            //if (currentGamePad.Buttons.Back == ButtonState.Pressed || currentKeyboard.IsKeyDown(Keys.Escape))
                //this.Exit();

            //Unpause Game
            if (EdgeDetect(currentKeyboard, Keys.P) && gamePaused)
            {
                gamePaused = false;
                pauseStateChanged = true;
            }

            //Toggle Debug Information
            if (EdgeDetect(currentKeyboard, Keys.I))
            {
                debugVisible = !debugVisible;
            }

            //Open Options Menu
            if (EdgeDetect(currentKeyboard, Keys.O))
            {
                ToggleMenu("Options");
            }

            //Open or Close Upgrades Menu
            if (EdgeDetect(currentKeyboard, Keys.U))
            {
                ToggleMenu("Upgrades");
            }

            //Main Pause Menu
            if (EdgeDetect(currentKeyboard, Keys.Escape))
            {
                ToggleMenu("Pause");
            }
            
            if (!gamePaused)
            {

                #region Player Rotation

                double maximumRotationalVelocity = Math.PI / 16.0;
                double rotationalVelocityIncrement = player.GetMaxSpeed() / 3000.0;
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
                    }
                }
                else
                {
                    float unitx = 0;
                    float unity = 0;
                    TraceScreenCoord((int)currentMouse.X, (int)currentMouse.Y, out unitx, out unity);
                    player.Direction = (float)Math.Atan2(unity - (player.Y + player.Height / 2), unitx - (player.X + player.Width / 2));
                }

                #endregion

                #region Player Movement and Deceleration Implementation

                double maxVelocity = player.GetMaxSpeed();
                double velocityIncrement = maxVelocity / 40.0;


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

                    //Strafing Movement, only works when Mouse Directional Control is enabled
                    if (mouseDirectionalControl)
                    {
                        double directionOfStrafe = player.Direction;
                        if ((currentKeyboard.IsKeyDown(Keys.Left) && !currentKeyboard.IsKeyDown(Keys.Right)) || (currentKeyboard.IsKeyDown(Keys.A) && !currentKeyboard.IsKeyDown(Keys.D)))
                            directionOfStrafe = player.Direction - MathHelper.PiOver2;
                        else if ((currentKeyboard.IsKeyDown(Keys.Right) && !currentKeyboard.IsKeyDown(Keys.Left)) || (currentKeyboard.IsKeyDown(Keys.D) && !currentKeyboard.IsKeyDown(Keys.A)))
                            directionOfStrafe = player.Direction + MathHelper.PiOver2;

                        if (directionOfStrafe != player.Direction)
                        {
                            player.HorizontalVelocity += Math.Cos(directionOfStrafe) * velocityIncrement;
                            player.VerticalVelocity += Math.Sin(directionOfStrafe) * velocityIncrement;
                        }
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
                            else if (GetSign(player.RotationalVelocity) < 0)
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
                            double reductionFactor = 2.0;

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
                                    player.HorizontalVelocity -= velocityIncrement / reductionFactor;
                                else if (player.HorizontalVelocity < 0)
                                    player.HorizontalVelocity += velocityIncrement / reductionFactor;

                                if (player.VerticalVelocity > 0)
                                    player.VerticalVelocity -= velocityIncrement / reductionFactor;
                                else if (player.VerticalVelocity < 0)
                                    player.VerticalVelocity += velocityIncrement / reductionFactor;
                            }
                            else
                            {
                                if (player.HorizontalVelocity > 0)
                                    player.HorizontalVelocity -= velocityIncrement / reductionFactor;
                                else if (player.HorizontalVelocity < 0)
                                    player.HorizontalVelocity += velocityIncrement / reductionFactor;

                                if (player.VerticalVelocity > 0)
                                    player.VerticalVelocity -= velocityIncrement / reductionFactor;
                                else if (player.VerticalVelocity < 0)
                                    player.VerticalVelocity += velocityIncrement / reductionFactor;

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

                #region Player Shooting

                //Shooting
                if ((currentMouse.LeftButton.Equals(ButtonState.Pressed)))
                {
                    if (player.ReloadTime <= 0 && player.IsValid() && !Muted) soundPlayer.getSound("playerShoot").Play();
                    double temp = player.Direction;
                    float unitx = 0;
                    float unity = 0;
                    TraceScreenCoord((int)currentMouse.X, (int)currentMouse.Y, out unitx, out unity);
                    player.Direction = (float)Math.Atan2(unity - (player.Y + player.Height / 2), unitx - (player.X + player.Width / 2));
                    player.ShootBullet(shardObjects, gameSourceDirectory);
                    player.Direction = temp;
                }

                if ((currentMouse.RightButton.Equals(ButtonState.Pressed)))
                {
                    if (player.RearmTime <= 0 && player.IsValid() && !Muted) soundPlayer.getSound("playerMissile").Play();
                    double temp = player.Direction;
                    float unitx = 0;
                    float unity = 0;
                    TraceScreenCoord((int)currentMouse.X, (int)currentMouse.Y, out unitx, out unity);
                    player.Direction = (float)Math.Atan2(unity - (player.Y + player.Height / 2), unitx - (player.X + player.Width / 2));
                    player.ShootMissile(shardObjects, gameSourceDirectory);
                    player.Direction = temp;
                }

                #endregion

                //Vacuum Button
                if (currentKeyboard.IsKeyDown(Keys.LeftShift))
                {
                    int vacuumWidth = (int)player.Width * 5;
                    int vacuumHeight = (int)player.Height * 5;
                    Rectangle vacuumBounds = new Rectangle((int)player.Center.X - vacuumWidth / 2, (int)player.Center.Y - vacuumHeight / 2, vacuumWidth, vacuumHeight);
                    foreach (ShardObject so in shardObjects)
                    {
                        if (so is Resource)
                        {
                            if (vacuumBounds.Intersects(so.GetBounds()))
                            {
                                so.PointTowards(player.Center);
                                so.Velocity = player.GetMaxSpeed() * Math.Sqrt(2) + .25;
                            }
                        }
                    }
                }

                //Pause Button
                if (EdgeDetect(currentKeyboard, Keys.P) && !gamePaused && !pauseStateChanged)
                    gamePaused = true;

                if (currentMouse.ScrollWheelValue > previousMouse.ScrollWheelValue) //Mousewheel UP
                {
                    isZooming = true;
                    zoomIO = 1; //Sets I/O to zooming IN;
                }
                if (currentMouse.ScrollWheelValue < previousMouse.ScrollWheelValue) //Mousewheel DOWN
                {
                    isZooming = true;
                    zoomIO = 0; //Sets I/O to zooming OUT;
                }

                if (isZooming)
                {
                    if (zoomIO == 1 && !(zoomZ >= 2))
                    {
                        zoomZ += .1f;
                        camera.Zoom = zoomZ;
                    }
                    if (zoomIO == 0 && !(zoomZ <= 1))
                    {
                        zoomZ -= .1f;
                        camera.Zoom = zoomZ;
                    }
                    if (zoomZ >= 2 && zoomIO == 1)
                    {
                        isZooming = false;
                    }
                    if (zoomZ <= 1 && zoomIO == 0)
                    {
                        isZooming = false;
                    }
                }

                bgHM += player.HorizontalVelocity;
                bgVM += player.VerticalVelocity;

                foreach (Rectangle r in backgrounds)
                {
                    if (r.Contains(player.GetBounds()))
                        currentRect = r;
                }

                if (currentRect != centerRect)
                {
                    centerRect = currentRect;
                    backgrounds[4] = currentRect;
                    //^ Sets center Rectangle to your player's current background Rectangle

                    backgrounds[0] = new Rectangle(centerRect.X - currentRect.Width, centerRect.Y - currentRect.Height, background.Bounds.Width, background.Bounds.Height);
                    backgrounds[1] = new Rectangle(centerRect.X, centerRect.Y - currentRect.Height, background.Bounds.Width, background.Bounds.Height);
                    backgrounds[2] = new Rectangle(centerRect.X + currentRect.Width, centerRect.Y - currentRect.Height, background.Bounds.Width, background.Bounds.Height);

                    backgrounds[3] = new Rectangle(centerRect.X - currentRect.Width, centerRect.Y, background.Bounds.Width, background.Bounds.Height);
                    //backgrounds[4] = already set to currentRect
                    backgrounds[5] = new Rectangle(centerRect.X + currentRect.Width, centerRect.Y, background.Bounds.Width, background.Bounds.Height);

                    backgrounds[6] = new Rectangle(centerRect.X - currentRect.Width, centerRect.Y + currentRect.Height, background.Bounds.Width, background.Bounds.Height);
                    backgrounds[7] = new Rectangle(centerRect.X, centerRect.Y + currentRect.Height, background.Bounds.Width, background.Bounds.Height);
                    backgrounds[8] = new Rectangle(centerRect.X + currentRect.Width, centerRect.Y + currentRect.Height, background.Bounds.Width, background.Bounds.Height);
                }


                //player.Update(new List<GameObject>(), gameTime);
                //Throw all ShardObjects into a Quadtree for collision optimization purposes
                collisionQuadtree.Clear();
                collisionQuadtree.MaximumBounds = camera.Screen;
                foreach (ShardObject so in shardObjects)
                {
                    if (camera.ScreenContains(so.GetBounds()))
                        collisionQuadtree.Insert(so);
                    //else if (so is EnemyShip)
                    //so.Velocity = 0;
                }

                //Update all ShardObjects using potentially colliding objects
                List<ShardObject> potentialCollisions = new List<ShardObject>();
                for (int i = 0; i < shardObjects.Count; i++)
                {
                    ShardObject so = shardObjects[i];
                    if (!so.HasListReference())
                        so.GiveListReference(shardObjects);
                    potentialCollisions.Clear();
                    collisionQuadtree.Retrieve(potentialCollisions, so);
                    if (so is EnemyShip)
                    {
                        if (so is BossShip)
                        {
                            if (((BossShip)so).GameReference == null)
                                ((BossShip)so).GameReference = this;
                        }
                        EnemyShip e = (EnemyShip)so;
                        if (!e.HasPlayerReference())
                            e.SetPlayerReference(player);
                        e.ShootAll(shardObjects, gameSourceDirectory);
                    }
                    so.Update(potentialCollisions, gameTime);
                }

                if (count > 25)
                {
                    if (camera.Screen != previousScreen)
                    {
                        Rectangle bounds = new Rectangle();
                        int x = (int)(player.Position.X - previousPosition.X);
                        int y = (int)(player.Position.Y - previousPosition.Y);
                        if ((Math.Abs(x) > 50) || (Math.Abs(y) > 50))
                        {
                            if (GetSign(x) == -1)
                                bounds = new Rectangle(camera.Screen.X - 70, camera.Screen.Y, Math.Abs(x), camera.Screen.Height);
                            else
                                bounds = new Rectangle(camera.Screen.Right, camera.Screen.Y, x, camera.Screen.Height);

                            Random random = new Random();
                            Debris debris = new Debris(random.Next(bounds.X, bounds.Right), random.Next(bounds.Y, bounds.Bottom));
                            debris.Alignment = Shard.Alignment.NEUTRAL;
                            debris.Health = 50;
                            debris.Energy = 10;
                            debris.Ore = 10;
                            debris.Oxygen = 10;
                            debris.Water = 10;
                            debris.Direction = random.NextDouble() * Math.PI * 2;
                            debris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_medium1_shaded");
                            debris.Width = debris.ImageSource.Width;
                            debris.Height = debris.ImageSource.Height;
                        }
                        if ((Math.Abs(x) > 50) || (Math.Abs(y) > 50))
                        {
                            if (GetSign(y) == -1)
                                bounds = new Rectangle(camera.Screen.X, camera.Screen.Y - 70, camera.Screen.Width, Math.Abs(y));
                            else
                                bounds = new Rectangle(camera.Screen.X, camera.Screen.Bottom, camera.Screen.Width, y);

                            Random random = new Random();
                            Debris debris = new Debris(random.Next(bounds.X, bounds.Right), random.Next(bounds.Y, bounds.Bottom));
                            debris.Alignment = Shard.Alignment.NEUTRAL;
                            debris.Health = 50;
                            debris.Energy = 10;
                            debris.Ore = 10;
                            debris.Oxygen = 10;
                            debris.Water = 10;
                            debris.Direction = random.NextDouble() * Math.PI * 2;
                            debris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_medium1_shaded");
                            debris.Width = debris.ImageSource.Width;
                            debris.Height = debris.ImageSource.Height;
                        }
                    }
                }


                //Remove ShardObjects declared invalid after the last update cycle
                for (int i = 0; i < shardObjects.Count; i++)
                {
                    if (!shardObjects[i].IsValid())
                    {
                        shardObjects[i].Destroy(shardObjects, gameSourceDirectory);
                        shardObjects.RemoveAt(i);
                    }
                }

                camera.SetPosition((float)player.X, (float)player.Y, 0);
                if (fixSkipping)
                {
                    Paused = true;
                    fixSkipping = false;
                }
                Paused = false;
            }

            foreach (GameMenu menu in shardMenus)
            {
                if (menu.Active)
                    menu.HandleMouseState(previousMouse, currentMouse);
            }

            //Update Previous States

            previousGamePad = currentGamePad;
            previousKeyboard = currentKeyboard;
            previousMouse = currentMouse;
            if (count > 25)
            {
                previousScreen = camera.Screen;
                previousPosition = player.Position;
                count = 0;
            }
            else
                count++;

            base.Update(gameTime);
        }

        public void SaveGame()
        {
            SaveTime = DateTime.Now;
            database.clear();
            foreach (ShardObject so in shardObjects)
            {
                database.addNode(so.toNode());
            }
            database.addNode(
                new XElement("background",
                    new XElement("bg1",
                        new XElement("x", backgrounds[0].X),
                        new XElement("y", backgrounds[0].Y)),
                    new XElement("bg2",
                        new XElement("x", backgrounds[1].X),
                        new XElement("y", backgrounds[1].Y)),
                    new XElement("bg3",
                        new XElement("x", backgrounds[2].X),
                        new XElement("y", backgrounds[2].Y)),
                    new XElement("bg4",
                        new XElement("x", backgrounds[3].X),
                        new XElement("y", backgrounds[3].Y)),
                    new XElement("bg5",
                        new XElement("x", backgrounds[4].X),
                        new XElement("y", backgrounds[4].Y)),
                    new XElement("bg6",
                        new XElement("x", backgrounds[5].X),
                        new XElement("y", backgrounds[5].Y)),
                    new XElement("bg7",
                        new XElement("x", backgrounds[6].X),
                        new XElement("y", backgrounds[6].Y)),
                    new XElement("bg8",
                        new XElement("x", backgrounds[7].X),
                        new XElement("y", backgrounds[7].Y)),
                    new XElement("bg9",
                        new XElement("x", backgrounds[8].X),
                        new XElement("y", backgrounds[8].Y)),
                    new XElement("m",
                        new XElement("hm", bgHM),
                        new XElement("vm", bgVM))
                        ));
            database.save();
        }

        public void RestartGame()
        {
            LoadGame(database);
            gamePaused = false;
        }

        protected void LoadGame(XMLDatabase db)
        {
            shardObjects.Clear();
            foreach (XElement xe in db.getDocument().Root.Elements())
            {
                switch (xe.Name.ToString())
                {
                    case "ship":
                        Ship tempShip = new Ship(xe, ref soundPlayer);
                        shardObjects.Add(tempShip);
                        if (tempShip.IsPlayer)
                        {
                            player = tempShip;
                            player.ImageSource = gameSourceDirectory.GetSourceRectangle("playerShip1_colored");
                            player.Width = player.ImageSource.Width;
                            player.Height = player.ImageSource.Height;
                            maximumPlayerHealth = (int)player.Health;
                        }
                        break;
                    case "debris":
                        Debris tempDebris = new Debris(xe);
                        tempDebris.ImageSource = gameSourceDirectory.GetSourceRectangle("asteroid_medium1_shaded");
                        tempDebris.Width = tempDebris.ImageSource.Width;
                        tempDebris.Height = tempDebris.ImageSource.Height;
                        tempDebris.Alignment = Shard.Alignment.NEUTRAL;
                        shardObjects.Add(tempDebris);
                        break;
                    case "enemyShip":
                        EnemyShip tempEnemy = new EnemyShip(xe, ref soundPlayer);
                        tempEnemy.ImageSource = gameSourceDirectory.GetSourceRectangle("pirateShip1_colored");
                        tempEnemy.Width = tempEnemy.ImageSource.Width;
                        tempEnemy.Height = tempEnemy.ImageSource.Height;
                        shardObjects.Add(tempEnemy);
                        break;
                    case "bruiser":
                        EnemyShip tempBruiser = new Bruiser(xe, ref soundPlayer);
                        tempBruiser.GetImageSource(gameSourceDirectory);
                        tempBruiser.Width = tempBruiser.ImageSource.Width;
                        tempBruiser.Height = tempBruiser.ImageSource.Height;
                        shardObjects.Add(tempBruiser);
                        break;
                    case "bossship":
                        EnemyShip tempBoss = new BossShip(xe, ref soundPlayer);
                        tempBoss.GetImageSource(gameSourceDirectory);
                        tempBoss.Width = tempBoss.ImageSource.Width;
                        tempBoss.Height = tempBoss.ImageSource.Height;
                        shardObjects.Add(tempBoss);
                        break;
                    case "thug":
                        EnemyShip tempThug = new Thug(xe, ref soundPlayer);
                        tempThug.ImageSource = gameSourceDirectory.GetSourceRectangle("pirateShip1_colored");
                        tempThug.Width = tempThug.ImageSource.Width;
                        tempThug.Height = tempThug.ImageSource.Height;
                        shardObjects.Add(tempThug);
                        break;
                    case "background":
                        int bgcount = 0;
                        bgVM = 0;
                        bgHM = 0;
                        foreach (XElement x in xe.Elements())
                        {
                            if (x.Name != "m")
                            {
                                backgrounds[bgcount] = new Rectangle();
                                backgrounds[bgcount].Height = background.Bounds.Height;
                                backgrounds[bgcount].Width = background.Bounds.Width;
                                backgrounds[bgcount].X = Convert.ToInt32(x.Element("x").Value);
                                backgrounds[bgcount].Y = Convert.ToInt32(x.Element("y").Value);
                                bgcount++;
                            }
                            else if (x.Name == "m")
                            {
                                bgHM = Convert.ToDouble(x.Element("hm").Value);
                                bgVM = Convert.ToDouble(x.Element("vm").Value);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            fixSkipping = true;
            gamePaused = false;
        }

        //Helper Methods
        private bool EdgeDetect(KeyboardState current, Keys key)
        {
            return EdgeDetect(current, previousKeyboard, key);
        }

        private bool EdgeDetect(KeyboardState current, KeyboardState previous, Keys key)
        {
            return (current.IsKeyDown(key) && previous.IsKeyUp(key));
        }

        public void ToggleMenu(string menuName)
        {
            GameMenu menuToToggle = null;
            for (int i = 0; i < shardMenus.Count; i++)
            {
                if (shardMenus[i].Name.Equals(menuName))
                    menuToToggle = shardMenus[i];
                else
                    shardMenus[i].Active = false; //Deactivates all other menus
            }
            if (menuToToggle != null)
                menuToToggle.Active = !menuToToggle.Active;
        }

        private void TraceScreenCoord(int x, int y, out float unitx, out float unity)
        {
            //Convert the pixel coordinate to units
            Matrix inv = Matrix.Invert(camera.GetViewMatrix());
            Vector2 pixel = new Vector2(x, y);
            Vector2 unit = Vector2.Transform(pixel, inv);
            unitx = unit.X + 0.5f;
            unity = unit.Y + 0.5f;
        }

        protected Vector2 ScreenToWorld(Vector2 coordinate)
        {
            Vector2 correctedCoords = new Vector2(0, 0);
            return correctedCoords;
        }

        protected Vector2 WorldToScreen(Vector2 coordinate)
        {
            Vector2 correctedCoords = new Vector2(0, 0);
            return correctedCoords;
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

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            //Draw the Background
            if (staticBackground)
                spriteBatch.Begin();
            else
                spriteBatch.Begin(SpriteSortMode.BackToFront, null, null, null, null, null, bgCam.GetViewMatrix());
            foreach (Rectangle r in backgrounds)
                spriteBatch.Draw(background, new Rectangle(r.X - (int)bgHM, r.Y - (int)bgVM, r.Width, r.Height), Color.White);
            spriteBatch.End();

            //Draw all ShardObjects relative to camera (ShardGraphics not included)
            spriteBatch.Begin(SpriteSortMode.BackToFront, null, null, null, null, null, camera.GetViewMatrix());

            List<ShardGraphic> shardGraphics = new List<ShardGraphic>();
            foreach (ShardObject so in shardObjects)
            {
                if (!(so is ShardGraphic))
                {
                    //This fixes zooming for some reason!
                    so.Depth = .1f;
                    if (so is EnemyShip)
                        DrawHealthBar(spriteBatch, spritesheet, (Ship)so);
                    so.Draw(spriteBatch, spritesheet);
                }
                else
                {
                    so.Depth = 0;
                    shardGraphics.Add((ShardGraphic)so);
                }
            }

            //Draw all ShardGraphics above all other shardObjects
            //spriteBatch.Begin(SpriteSortMode.FrontToBack, null, null, null, null, null, camera.GetViewMatrix());
            foreach (ShardGraphic sg in shardGraphics)
            {
                if (!sg.HasValidFont())
                    sg.Font = statusIndicatorFont;
                sg.Depth = 0;
                sg.Draw(spriteBatch, spritesheet);
            }
            spriteBatch.End();

            //Draw the Debug Window
            spriteBatch.Begin();
            DrawDebugWindow(spriteBatch, Color.Red);
            //spriteBatch.Draw(spritesheet, new Rectangle(32,32,32,32), new Rectangle(0,0,32,32), Color.White);
            spriteBatch.End();

            //Draw the Player
            spriteBatch.Begin(SpriteSortMode.BackToFront, null, null, null, null, null, camera.GetViewMatrix());
            //player.Draw(spriteBatch, spritesheet);
            spriteBatch.End();

            //Draw the Player Healthbar
            spriteBatch.Begin(SpriteSortMode.BackToFront, null);
            Rectangle healthBarSource = gameSourceDirectory.GetSourceRectangle("healthBarOutline");
            Rectangle healthBarGradient = gameSourceDirectory.GetSourceRectangle("healthBar");
            spriteBatch.Draw(spritesheet, new Rectangle(0, 0, healthBarSource.Width, healthBarSource.Height), healthBarSource, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            spriteBatch.Draw(spritesheet, new Rectangle(4, 5, (int)(healthBarGradient.Width * (player.Health / (double)player.MaximumHealth)), healthBarGradient.Height), healthBarGradient, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1.0f);
            spriteBatch.End();

            //Draw HUD
            spriteBatch.Begin(SpriteSortMode.BackToFront, null);
            string playerInfo = "(" + (int)player.X + ", " + (int)player.Y + ")";
            spriteBatch.DrawString(debugFont, playerInfo, new Vector2(127, 73), Color.LightBlue);
            if (player.X < -200 || player.Y < -200 || player.X > 3800 || player.Y > 3800)
            {
                string danger = "Danger!\nEntering Dead Space!\nTurn Back!";
                spriteBatch.DrawString(debugFont, danger, new Vector2(127, 73 + debugFont.MeasureString(playerInfo).Y + 2), Color.Red);
            }
            spriteBatch.End();


            //Draw Resource Display
            spriteBatch.Begin(SpriteSortMode.BackToFront, null);
            Vector2 offset = new Vector2(24, 75);
            string fuelDisplay = "" + player.Energy;
            spriteBatch.DrawString(debugFont, fuelDisplay, offset, Color.White);
            offset.Y += debugFont.MeasureString(fuelDisplay).Y + 7;
            string oreDisplay = "" + player.Ore;
            spriteBatch.DrawString(debugFont, oreDisplay, offset, Color.White);
            offset.Y += debugFont.MeasureString(oreDisplay).Y + 7;
            string oxygenDisplay = "" + player.Oxygen;
            spriteBatch.DrawString(debugFont, oxygenDisplay, offset, Color.White);
            offset.Y += debugFont.MeasureString(oxygenDisplay).Y + 8;
            string waterDisplay = "" + player.Water;
            spriteBatch.DrawString(debugFont, waterDisplay, offset, Color.White);
            spriteBatch.End();

            //Draw any active menus
            spriteBatch.Begin(SpriteSortMode.FrontToBack, null);
            foreach (GameMenu menu in shardMenus)
            {
                menu.Draw(spriteBatch, menusheet);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        internal void DrawHealthBar(SpriteBatch spriteBatch, Texture2D spritesheet, Ship ship)
        {
            Rectangle healthBarSource = gameSourceDirectory.GetSourceRectangle("enemyHealthBarOutline");
            Rectangle healthBarGradient = gameSourceDirectory.GetSourceRectangle("enemyHealthBar");
            spriteBatch.Draw(spritesheet, new Rectangle((int)ship.Center.X - healthBarSource.Width / 2 + 1, (int)ship.Y - 10, healthBarSource.Width, healthBarSource.Height), healthBarSource, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1.0f);
            spriteBatch.Draw(spritesheet, new Rectangle((int)ship.Center.X - healthBarSource.Width / 2 + 1, (int)ship.Y - 9, (int)(healthBarGradient.Width * (ship.Health / (double)ship.MaximumHealth)), healthBarGradient.Height), healthBarGradient, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        //precondition: spriteBatch.Begin() has been called
        protected void DrawDebugWindow(SpriteBatch spriteBatch, Color textColor)
        {
            if (debugVisible)
            {
                List<string> debugLines = new List<string>();
                debugLines.Add("Player Ship Coordinates: (" + player.X + ", " + player.Y + ")");
                debugLines.Add("Resources: " + player.Energy + " " + player.Ore + " " + player.Oxygen + " " + player.Water);
                //debugLines.Add("xVelocity: " + player.HorizontalVelocity + "   yVelocity: " + player.VerticalVelocity);
                //debugLines.Add("Rotational Velocity: " + player.RotationalVelocity);
                debugLines.Add("ShardObject Size: " + shardObjects.Count);
                debugLines.Add("G: " + player.GunLevel + " M: " + player.MissileLevel + " S: " + player.SpeedLevel + " A: " + player.ArmorLevel);
                debugLines.Add("Center: " + centerRect.X + "," + centerRect.Y);
                debugLines.Add("Current: " + currentRect.X + "," + currentRect.Y);

                Vector2 offset = new Vector2(204, 4);
                foreach (string line in debugLines)
                {
                    spriteBatch.DrawString(debugFont, line, offset, textColor);
                    offset.Y += debugFont.MeasureString(line).Y - 1;
                }
            }
        }
    }
}
