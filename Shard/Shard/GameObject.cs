﻿using System;
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
    abstract class GameObject
    {
        private int id;
        private double x;
        private double y;
        private double direction;
        private double velocity;
        private double rotationVelocity;
        private double health;

        #region Mutating and Returning Fields

        //public double GetX() { return this.x; }
        //public double GetY() { return this.y; }
        //public double GetDirection() { return this.direction; }
        //public double GetVelocity() { return this.velocity; }
        //public double GetRotationalVelocity() { return this.rotationVelocity; }
        //public double GetHealth() { return this.health; }

        //public void setX(double x) { this.x = x; }
        //public void setY(double y) { this.y = y; }
        //public void setDirection(double direction) { this.direction = direction; }
        //public void setVelocity(double velocity) { this.velocity = velocity; }
        //public void setRotationalVel(double rotationVelocity) { this.rotationVelocity = rotationVelocity; }

        public virtual int ID
        {
            get
            {
                return id;
            }
            set
            {
                if (value > 0)
                    id = value;
            }
        }

        public virtual double X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public virtual double Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public virtual double Direction
        {
            get
            {
                return direction;
            }
            set
            {
                direction = value;
            }
        }

        public virtual double Velocity
        {
            get
            {
                return velocity;
            }
            set
            {
                velocity = value;
            }
        }

        public virtual double RotationVelocity
        {
            get
            {
                return rotationVelocity;
            }
            set
            {
                rotationVelocity = value;
            }
        }

        public virtual double Health
        {
            get
            {
                return health;
            }
            set
            {
                health = value;
            }
        }

        #endregion

        //Should be Ovverriden
        public void Move()
        {
            this.X += Math.Cos(direction) * velocity;
            this.Y += Math.Sin(direction) * velocity;
        }

        public void Move(double distance)
        {
            this.X += Math.Cos(direction) * distance;
            this.Y += Math.Sin(direction) * distance;
        }

        //Should be Overriden 
        public void Update(List<GameObject> gameObjects, GameTime gameTime)
        {
            Move();
        }

        public abstract Rectangle GetBounds();

        public abstract bool Intersects(GameObject gameObject);

        public abstract void Draw(SpriteBatch spriteBatch, Texture2D sprite);

    }
}
