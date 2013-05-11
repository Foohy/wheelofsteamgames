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

        //Info for the basetexture so we can overlay new info on it
        private int Diameter = 815; //848;
        private Vector2 Center = new Vector2(518, 597);
        const int TextureScale = 2;

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

            Diameter *= TextureScale;
            Center *= TextureScale;
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
            this.ElementSizeRadians = (float)(Math.PI * 2) / Games.Count;

            //Create the texture
            System.Drawing.Bitmap bm = new System.Drawing.Bitmap(Diameter, Diameter);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bm);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.FillEllipse(System.Drawing.Brushes.Black, 0, 0, Diameter, Diameter);

            System.Drawing.Font font = new System.Drawing.Font("Arial", 25);
            int WheelCenter = Diameter / 2;
            float WheelAngleOffset = ElementSizeRadians / 2 - (float)Math.PI / 2 - ElementSizeRadians;

            for (int i = 0; i < Games.Count; i++)
            {
                int x = (int)(Math.Cos(i * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;
                int y = (int)(Math.Sin(i * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;

                int xNext = (int)(Math.Cos((i + 1) * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;
                int yNext = (int)(Math.Sin((i + 1) * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;

                int xMid = (int)(Math.Cos((i + 0.5) * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;
                int yMid = (int)(Math.Sin((i + 0.5) * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;

                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath( System.Drawing.Drawing2D.FillMode.Winding);
                System.Drawing.Point[] Points = new System.Drawing.Point[]
                {
                    new System.Drawing.Point(WheelCenter, WheelCenter),
                    new System.Drawing.Point(x, y),
                    new System.Drawing.Point(xMid, yMid),
                    new System.Drawing.Point(xNext, yNext),
                };

                System.Drawing.Point[] CurvePoints = new System.Drawing.Point[]
                {
                    new System.Drawing.Point(x, y),
                    new System.Drawing.Point(xMid, yMid),
                    new System.Drawing.Point(xNext, yNext),
                };

                gp.AddPolygon(Points);

                float StartAngle = (i * ElementSizeRadians + WheelAngleOffset) * RAD2DEG;
                float EndAngle = ((i + 1) * ElementSizeRadians + WheelAngleOffset) * RAD2DEG;
                gp.AddArc(0, 0, Diameter, Diameter, StartAngle + 360, Math.Abs(StartAngle - EndAngle));
                gp.CloseAllFigures();

                System.Drawing.Region rgn = new System.Drawing.Region( gp );
                System.Drawing.Brush b = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(Utilities.Rand.Next( 0, 255 ), Utilities.Rand.Next( 0, 255 ),Utilities.Rand.Next( 0, 255 )));
                g.FillRegion(b, rgn);


                float TextAngle = ((i + 0.5f) * ElementSizeRadians + WheelAngleOffset);
                g.TranslateTransform(WheelCenter, WheelCenter);
                g.RotateTransform(TextAngle * RAD2DEG);
                g.DrawString(Games[i].Name, font, System.Drawing.Brushes.Black, new System.Drawing.PointF( 130 * TextureScale, -font.Size/2));
                g.RotateTransform(-TextAngle * RAD2DEG);
                g.TranslateTransform(-WheelCenter, -WheelCenter);
            }

            for (int i = 0; i < Games.Count; i++)
            {
                int x = (int)(Math.Cos(i * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;
                int y = (int)(Math.Sin(i * ElementSizeRadians + WheelAngleOffset) * WheelCenter) + WheelCenter;
                g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.FromArgb(32, 32, 32), 4), WheelCenter, WheelCenter, x, y);
            }

            g.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);

            System.Drawing.Bitmap underlay = new System.Drawing.Bitmap("Resources/Materials/models/spinner_wheel.png");
            underlay = ResizeImage(underlay, underlay.Width * TextureScale, underlay.Height * TextureScale);

            if (underlay != null)
            {
                System.Drawing.Graphics gU = System.Drawing.Graphics.FromImage(underlay);
                gU.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                int x = (int)Center.X - (Diameter / 2);
                int y = (int)Center.Y - (Diameter / 2);
                gU.DrawImage( bm, new System.Drawing.Point(x, y));
                gU.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
            }

            int tex = Utilities.LoadTexture(underlay);
            this.Wheel.mat.Properties.BaseTexture = tex;
        }
        public static System.Drawing.Bitmap ResizeImage(System.Drawing.Bitmap imgToResize, int width, int height)
        {
            try
            {
                System.Drawing.Bitmap b = new System.Drawing.Bitmap(width, width);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    g.DrawImage(imgToResize, 0, 0, width, width);
                }

                return b;
            }
            catch
            {
                throw;
            }
        }

        public void Spin(float force)
        {
            if (this.IsSpinning || this.Games == null) return;

            CurrentAngle = Wheel.Angle.X;
            SpeedTime = Utilities.Time;
            SpinupForce = force;

            this.IsSpinning = true;
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
