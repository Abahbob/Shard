﻿using System;
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
    class Ship : ShardObject
    {
        //Maximum for all levels, scaling wise, is five (potential to be changed later)
        private int speedLevel;
        private int armorLevel;
        private int gunLevel;
        private int missileLevel;
        private int laserLevel;

        protected int reloadTime;
        protected int rearmTime;
        private bool isPlayer;
        private SoundPlayer sp;

        public Ship(int xPosition, int yPosition, bool isPlayer, ref SoundPlayer soundPlayer)
        {
            this.X = xPosition;
            this.Y = yPosition;
            this.Velocity = 0;
            this.Direction = 0;
            this.Health = 1;
            this.RotationalVelocity = 0;
            this.Energy = 0;
            this.Ore = 0;
            this.Oxygen = 0;
            this.Water = 0;
            this.gunLevel = 1;
            this.missileLevel = 1;
            this.laserLevel = 1;
            this.armorLevel = 1;
            this.speedLevel = 1;
            this.reloadTime = 0;
            this.rearmTime = 0;
            this.isPlayer = isPlayer;
            sp = soundPlayer;
        }

        public Ship(XElement node, ref SoundPlayer soundPlayer)
        {
            X = Convert.ToDouble(node.Element("x").Value);
            Y = Convert.ToDouble(node.Element("y").Value);
            HorizontalVelocity = Convert.ToDouble(node.Element("horizontalVelocity").Value);
            VerticalVelocity = Convert.ToDouble(node.Element("verticalVelocity").Value);
            Direction = Convert.ToDouble(node.Element("direction").Value);
            Health = Convert.ToDouble(node.Element("Health").Value);
            RotationalVelocity = Convert.ToDouble(node.Element("RotationalVelocity").Value);
            Energy = Convert.ToInt32(node.Element("Energy").Value);
            Ore = Convert.ToInt32(node.Element("Ore").Value);
            Oxygen = Convert.ToInt32(node.Element("Oxygen").Value);
            Water = Convert.ToInt32(node.Element("Water").Value);
            gunLevel = Convert.ToInt32(node.Element("gunLevel").Value);
            missileLevel = Convert.ToInt32(node.Element("missileLevel").Value);
            speedLevel = Convert.ToInt32(node.Element("speedLevel").Value);
            laserLevel = Convert.ToInt32(node.Element("laserLevel").Value);
            armorLevel = Convert.ToInt32(node.Element("armorLevel").Value);
            reloadTime = Convert.ToInt32(node.Element("reloadTime").Value);
            rearmTime = Convert.ToInt32(node.Element("rearmTime").Value);
            isPlayer = Convert.ToBoolean(node.Element("isPlayer").Value);
            MaximumHealth = Convert.ToDouble(node.Element("maximumHealth").Value);
            sp = soundPlayer;
        }

        #region Modifying and Returning Fields

        public bool IsPlayer
        {
            get
            {
                return isPlayer;
            }
        }


        public int GunLevel
        {
            get
            {
                return gunLevel;
            }
            set
            {
                if (value > 0)
                    this.gunLevel = value;
                else
                    gunLevel = 0;
            }
        }

        public int MissileLevel
        {
            get
            {
                return missileLevel;
            }
            set
            {
                if (value > 0)
                    this.missileLevel = value;
                else
                    missileLevel = 0;
            }
        }

        public int LaserLevel
        {
            get
            {
                return laserLevel;
            }
            set
            {
                if (value > 0)
                    this.laserLevel = value;
                else
                    laserLevel = 0;
            }
        }

        public int SpeedLevel
        {
            get
            {
                return speedLevel;
            }
            set
            {
                if (value > 0)
                    this.speedLevel = value;
                else
                    speedLevel = 0;
            }
        }

        public int ArmorLevel
        {
            get
            {
                return this.armorLevel;
            }
            set
            {
                if (value > 0)
                    this.armorLevel = value;
                else
                    armorLevel = 0;
            }
        }

        public int ReloadTime
        {
            get
            {
                return reloadTime;
            }
        }

        public int RearmTime
        {
            get
            {
                return rearmTime;
            }
        }
                

        public Vector2 ShipFront
        {
            get
            {
                return new Vector2((float)(Center.X + (Width / 2 * Math.Cos(Direction))), (float)(Center.Y + (Height / 2 * Math.Sin(Direction))));
            }
        }

        #endregion

        #region Projectile Creation

        #region Bullet Creation and Stat Determination

        //Should be Overriden for ships with different scaling
        protected Projectile GetGunBullet(GameImageSourceDirectory sourceDirectory)
        {
            if (GunLevel > 0)
            {
                Projectile p = new Projectile((int)(this.ShipFront.X), (int)(this.ShipFront.Y));
                p.Alignment = this.Alignment;
                p.Direction = this.Direction;
                p.RotationalVelocity = this.RotationalVelocity;
                p.ImageSource = GetBulletImageSource(sourceDirectory);
                p.Direction = SwayDirection(p.Direction, MathHelper.ToRadians(GetBulletSway()));
                p.Velocity = GetBulletSpeed();
                p.Damage = GetBulletDamage();
                p.Width = p.ImageSource.Width;
                p.Height = p.ImageSource.Height;
                p.X -= p.Width / 2;
                p.Y -= p.Height / 2;
                return p;
            }
            return null;
        }

        //Source directory name should be changed for different images
        protected virtual Rectangle GetBulletImageSource(GameImageSourceDirectory sourceDirectory)
        {
            switch (GunLevel)
            {
                default:
                    return sourceDirectory.GetSourceRectangle("shipBullet");
            }
        }

        //Switch values should be changed for different scaling
        protected virtual int GetBulletDamage()
        {
            switch (GunLevel)
            {
                case 1:
                    return 1;
                case 2:
                    return 2;
                case 3:
                    return 3;
                case 4:
                    return 5;
                case 5:
                    return 7;
                default:
                    return 1;
            }
        }

        //Switch values should be changed for different scaling
        protected virtual double GetBulletSpeed()
        {
            switch (GunLevel)
            {
                case 1:
                    return 6.0;
                case 2:
                    return 6.5;
                case 3:
                    return 7.0;
                case 4:
                    return 7.5;
                case 5:
                    return 8.0;
                default:
                    return 1;
            }
        }

        //Switch values should be changed for different scaling
        protected virtual float GetBulletSway()
        {
            switch (GunLevel)
            {
                case 1:
                    return 7.5f;
                case 2:
                    return 6.0f;
                case 3:
                    return 4.5f;
                case 4:
                    return 3.0f;
                case 5:
                    return 1.5f;
                default:
                    return 10.0f;
            }
        }

        //Switch values should be changed for different scaling
        protected virtual int GetReloadTime()
        {
            switch (GunLevel)
            {
                case 1:
                    return 20;
                case 2:
                    return 17;
                case 3:
                    return 14;
                case 4:
                    return 11;
                case 5:
                    return 8;
                default:
                    return 100;
            }
        }

        #endregion

        #region Missile Creation and Stat Determination

        //Should be Overriden for ships with different scaling
        public Missile GetMissile(GameImageSourceDirectory sourceDirectory)
        {
            if (MissileLevel > 0)
            {
                Missile missile = new Missile((int)(this.ShipFront.X), (int)(this.ShipFront.Y));
                missile.Alignment = this.Alignment;
                missile.Direction = this.Direction;
                missile.RotationalVelocity = 0;
                missile.ImageSource = GetMissileImageSource(sourceDirectory);
                missile.TravelSpeed = GetMissileTravelSpeed();
                missile.Damage = GetMissileDamage();
                
                missile.Width = missile.ImageSource.Width;
                missile.Height = missile.ImageSource.Height;
                missile.X -= missile.Width / 2;
                missile.Y -= missile.Height / 2;
                return missile;
            }
            return null;
        }

        //Source directory name should be changed for different images
        protected virtual Rectangle GetMissileImageSource(GameImageSourceDirectory sourceDirectory)
        {
            switch (MissileLevel)
            {
                case 1:
                    return sourceDirectory.GetSourceRectangle("shipMissile");;
                default:
                    return sourceDirectory.GetSourceRectangle("shipMissile");;
            }
        }

        //Switch values should be changed for different scaling
        protected virtual int GetRearmTime()
        {
            switch (MissileLevel)
            {
                case 1:
                    return 40;
                case 2:
                    return 35;
                case 3:
                    return 30;
                case 4:
                    return 25;
                case 5:
                    return 20;
                default:
                    return 100;
            }
        }

        //Switch values should be changed for different scaling
        protected virtual int GetMissileDamage()
        {
            switch (MissileLevel)
            {
                case 1:
                    return 3;
                case 2:
                    return 5;
                case 3:
                    return 8;
                case 4:
                    return 11;
                case 5:
                    return 15;
                default:
                    return 2;
            }
        }

        //Switch values should be changed for different scaling
        protected virtual double GetMissileTravelSpeed()
        {
            switch (MissileLevel)
            {
                case 1:
                    return 3.0;
                case 2:
                    return 3.5;
                case 3:
                    return 4.0;
                case 4:
                    return 4.75;
                case 5:
                    return 5.5;
                default:
                    return 3.0;
            }
        }

        #endregion

        #endregion

        #region Shooting

        public virtual void ShootAll(List<ShardObject> shardObjects, GameImageSourceDirectory sourceDirectory)
        {
            ShootBullet(shardObjects, sourceDirectory);
            ShootMissile(shardObjects, sourceDirectory);
        }

        public virtual void ShootBullet(List<ShardObject> shardObjects, GameImageSourceDirectory sourceDirectory)
        {
            if (reloadTime <= 0)
            {
                Projectile bullet = GetGunBullet(sourceDirectory);
                if (bullet != null)
                {
                    if (!isPlayer)
                    {
                        sp.getSound("enemyShoot").Play();
                    }
                    shardObjects.Add(bullet);
                    reloadTime = this.GetReloadTime();
                }
            }
        }

        public virtual void ShootMissile(List<ShardObject> shardObjects, GameImageSourceDirectory sourceDirectory)
        {
            //Missiles
            if (rearmTime <= 0)
            {
                Missile missile = GetMissile(sourceDirectory);
                if (missile != null)
                {
                    if (!isPlayer)
                    {
                        sp.getSound("enemyMissile").Play();
                    }
                    missile.SelectTarget(shardObjects);
                    missile.Direction = this.Direction;
                    missile.Velocity = missile.TravelSpeed / 2;
                    shardObjects.Add(missile);
                    rearmTime = this.GetRearmTime();
                }
            }
        }

        //Changes the given direction +- maximumSway
        protected virtual double SwayDirection(double direction, double maximumSway)
        {
            Random random = new Random();
            double swayAmount = random.NextDouble() * maximumSway * 2;
            return (direction - maximumSway) + swayAmount;
        }

        #endregion

        #region Statistics (Speed, Armor, and Health) - Can Be Overriden in Inheriting Classes

        //Should be Overriden for ships with different scaling
        public virtual double GetMaxSpeed()
        {
            switch (SpeedLevel)
            {
                case 0:
                case 1:
                    return 2.0;
                case 2:
                    return 2.25;
                case 3:
                    return 2.6;
                case 4:
                    return 3.0;
                case 5:
                    return 3.5;
                default:
                    return 2.0;
            }
        }

        public virtual int GetDamageReductionFromArmor()
        {
            switch (ArmorLevel)
            {
                case 0:
                case 1:
                    return 0;
                case 2:
                    return 1;
                case 3:
                    return 2;
                case 4:
                    return 4;
                case 5:
                    return 5;
                default:
                    return 0;
            }
        }

        #endregion

        #region Overriding Base Methods

        public override void ApplyDamage(int damage)
        {
            int reducedDamage = damage - GetDamageReductionFromArmor();
            if (reducedDamage <= 0)
                base.ApplyDamage(0);
            else
                base.ApplyDamage(reducedDamage);
        }

        #endregion

        public override void Update(List<ShardObject> shardObjects, GameTime gameTime)
        {
            base.Update(shardObjects, gameTime);
            reloadTime -= 1;
            rearmTime -= 1;
            //foreach (ShardObject shardObject in shardObjects)
            //{
            //    if (shardObject is Resource)
            //    {
            //        if (shardObject.IsValid() && this.GetBounds().Intersects(shardObject.GetBounds()))
            //        {
            //            this.Energy += shardObject.Energy;
            //            this.Ore += shardObject.Ore;
            //            this.Oxygen += shardObject.Oxygen;
            //            this.Water += shardObject.Water;
            //            shardObject.SetValid(false);
            //        }
            //    }
            //}
        }


        public override XElement toNode()
        {
            XElement node =
                new XElement("ship",
                    new XElement("x", this.X),
                    new XElement("y", this.Y),
                    new XElement("horizontalVelocity", this.HorizontalVelocity),
                    new XElement("verticalVelocity", this.VerticalVelocity),
                    new XElement("direction", this.Direction),
                    new XElement("speedLevel", this.speedLevel),
                    new XElement("armorLevel", this.armorLevel),
                    new XElement("gunLevel", this.gunLevel),
                    new XElement("missileLevel", this.missileLevel),
                    new XElement("laserLevel", this.laserLevel),
                    new XElement("Health", this.Health),
                    new XElement("RotationalVelocity", this.RotationalVelocity),
                    new XElement("Energy", this.Energy),
                    new XElement("Ore", this.Ore),
                    new XElement("Water", this.Water),
                    new XElement("Oxygen", this.Oxygen),
                    new XElement("reloadTime", this.reloadTime),
                    new XElement("rearmTime", this.rearmTime),
                    new XElement("maximumHealth", this.MaximumHealth),
                    new XElement("isPlayer", this.isPlayer)
                    );
            return node;
        }
    }
}
