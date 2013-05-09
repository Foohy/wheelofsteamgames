using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OlegEngine;
using OlegEngine.Entity;
using OlegEngine.GUI;

using OpenTK;

namespace WheelOfSteamGames.Entity
{
    class ent_spinner : BaseEntity
    {
        public struct Game
        {
            public string Name;
            public int AppID;
            public string Image;

            public Game( string name, int id, string image)
            {
                Name = name;
                AppID = id;
                Image = image;
            }

            public Game(string name )
            {
                Name = name;
                AppID = 0;
                Image = "none";
            }
        }

        public List<Game> Games;
        public float SpinFriction = 0.0083f;
        public float CurrentSpeed { get; private set; }
        public float CurrentAngle { get; private set; }
        public float SpinupForce { get; set; }
        public float SpeedupTime = 1.2f; //Time to apply the force to spin up
        public bool IsSpinning { get; private set; }

        private double SpeedTime = 0;
        private Vector3 PaddlePositionOffset = new Vector3(0, 5.2f, 0);
        private float LastSoundRegion = 0;
        private float ElementSizeRadians = 0; //Size of each element, in radians
        Mesh Wheel;
        Mesh Paddle;
        Text CurrentGameText;
        const float RAD2DEG = 180f / (float)Math.PI;

        public override void Init()
        {
            this.SetModel(Resource.GetMesh("spinner_base.obj"));
            this.Mat = Resource.GetMaterial("models/spinner_base");

            Wheel = Resource.GetMesh("spinner_wheel.obj");
            Wheel.mat = Resource.GetMaterial("models/spinner_wheel");

            Paddle = Resource.GetMesh("spinner_paddle.obj");
            Paddle.mat = Resource.GetMaterial("models/spinner_paddle");

            this.IsSpinning = false;

            Audio.Precache("Resources/Audio/spinner_click.wav");
            CurrentGameText = new Text("game_large", "NONE");
            GUIManager.PostDrawHUD += new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
        }

        void GUIManager_PostDrawHUD(EventArgs e)
        {
            if (!MainRoom.Started) return;

            float Length = CurrentGameText.GetTextLength();


            if (Length / (Utilities.window.Width / 2.5) > 1.0f)
            {
                CurrentGameText.SetScale((Utilities.window.Width / 2.5f) / Length, 1);
            }
            else CurrentGameText.SetScale(1, 1);

            CurrentGameText.SetPos(Utilities.window.Width - Length * CurrentGameText.ScaleW - 100, (Utilities.window.Height / 2) - (CurrentGameText.GetTextHeight() / 2));
            CurrentGameText.Draw();
        }

        public void CreateElements( List<Game> games )
        {
            this.Games = games;
        }

        public void Spin( float force )
        {
            if (this.IsSpinning || this.Games == null) return;

            CurrentAngle = Wheel.Angle.X;
            SpeedTime = Utilities.Time;
            SpinupForce = force;

            this.IsSpinning = true;
            this.ElementSizeRadians = (float)(Math.PI * 2) / Games.Count;
            this.SpeedupTime = (float)Utilities.Rand.NextDouble(0.7, 1.3);
        }

        public float GetPaddleTurn( float angle )
        {
            angle = angle * RAD2DEG;
            return ((float)Math.Cos(angle / (this.Games.Count / 4)) * 10) / RAD2DEG;
        }

        public override void Think()
        {
            Wheel.Position = this.Position;
            Wheel.Angle = this.Angle + new Vector3(this.CurrentAngle, 0, 0);

            Paddle.Position = this.Position + PaddlePositionOffset;
            Paddle.Angle = new Vector3(this.Angle.X + GetPaddleTurn(this.CurrentAngle), this.Angle.Y, this.Angle.Z);

            if (Games == null) return;
            //Spin if neccessary
            if (Utilities.Time < SpeedTime + SpeedupTime)
            {
                CurrentSpeed += (float)Utilities.Frametime * this.SpinupForce;

                //Console.WriteLine("Spinning up: {0}", CurrentSpeed );
            }
            else if (CurrentSpeed > 0)
            {
                CurrentSpeed -= (float)Utilities.Frametime * this.SpinFriction;
                //Console.WriteLine("Slowing down: {0}", CurrentSpeed );
            }
            else if (this.IsSpinning)
            {
                CurrentSpeed = 0;
                IsSpinning = false;
                Console.WriteLine("Done!");

                OnStopSpinning(this.CurrentAngle);
            }

            int CurrentRegion = GetRegionFromAngle(CurrentAngle);
            if (CurrentRegion != LastSoundRegion)
            {
                LastSoundRegion = CurrentRegion;
                Audio.PlaySound("Resources/Audio/spinner_click.wav", 1.0f, Utilities.Rand.Next(35280, 52920));
            }

            CurrentAngle += CurrentSpeed;
            CurrentGameText.SetText(Games[CurrentRegion].Name);
        }

        public double NormalizeAngle(double angle)
        {
            angle = angle % (Math.PI * 2);
            return angle >= 0 ? angle : angle + Math.PI * 2;
        }

        public int GetRegionFromAngle(float angle)
        {
            angle = (float)NormalizeAngle(angle + Math.PI*2 - (ElementSizeRadians /2f));
            float percent = angle / (float)(Math.PI * 2);
            int region = (int)Math.Ceiling((percent * Games.Count));

            return region >= Games.Count ? 0 : region; //Handle wrapping around
        }

        private void OnStopSpinning( float angle )
        {
            Console.WriteLine(GetRegionFromAngle(angle));
            //Convert the angle to between 0 and 2pi
            angle = (float)Math.Asin(Math.Sin(angle)) + (float)Math.PI;

            float elementangle = angle / Games.Count;

        }

        public override void Draw()
        {
            base.Draw();
            Wheel.Draw();
            Paddle.Draw();
        }
    }
}
