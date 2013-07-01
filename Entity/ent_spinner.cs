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
        System.Drawing.Color[] PanelColors = new System.Drawing.Color[]
        {
            System.Drawing.Color.FromArgb(187, 53, 46),
            System.Drawing.Color.FromArgb(61, 137, 18),
            System.Drawing.Color.FromArgb(58, 79, 192),
            System.Drawing.Color.FromArgb(187, 137, 46),
        };

        public List<SteamCommunity.Game> Games;
        public float SpinFriction = 0.0083f;
        public float CurrentSpeed { get; private set; }
        public float CurrentAngle { get; private set; }
        public float SpinupForce { get; set; }
        public float SpeedupTime = 1.2f; //Time to apply the force to spin up
        public bool IsSpinning { get; private set; }

        public event Action<SteamCommunity.Game> OnSpinnerStop;

        private double SpeedTime = 0;
        private Vector3 PaddlePositionOffset = new Vector3(0, 5.2f, 0);
        private float LastSoundRegion = 0;
        private float ElementSizeRadians = 0; //Size of each element, in radians
        Mesh Wheel;
        Mesh Paddle;
        Text CurrentGameText;

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
            float Height = CurrentGameText.GetTextHeight();
            float Scale = Length / (Utilities.window.Width) > 1.0f ? (Utilities.window.Width) / Length : 1;

            CurrentGameText.SetScale(Scale, 1);

            CurrentGameText.SetPos((Utilities.window.Width / 2) - (Length*Scale / 2), 20);
            CurrentGameText.Draw();

            Vector2 ScreenPos = Utilities.Get3Dto2D( Wheel.Position );
            Surface.DrawSimpleText("debug", ScreenPos.ToString(), ScreenPos.X, ScreenPos.Y);
        }

        public void CreateElements( List<SteamCommunity.Game> games )
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

                float StartAngle = (i * ElementSizeRadians + WheelAngleOffset) * Utilities.F_RAD2DEG;
                float EndAngle = ((i + 1) * ElementSizeRadians + WheelAngleOffset) * Utilities.F_RAD2DEG;
                gp.AddArc(0, 0, Diameter, Diameter, StartAngle + 360, Math.Abs(StartAngle - EndAngle));
                gp.CloseAllFigures();

                System.Drawing.Region rgn = new System.Drawing.Region( gp );
                System.Drawing.Brush b = new System.Drawing.SolidBrush( PanelColors[i%PanelColors.Length] );
                //System.Drawing.Brush b = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(Utilities.Rand.Next( 0, 255 ), Utilities.Rand.Next( 0, 255 ),Utilities.Rand.Next( 0, 255 )));
                g.FillRegion(b, rgn);


                float TextAngle = ((i + 0.5f) * ElementSizeRadians + WheelAngleOffset);
                g.TranslateTransform(WheelCenter, WheelCenter);
                g.RotateTransform(TextAngle * Utilities.F_RAD2DEG);
                g.DrawString(Games[i].Name, font, System.Drawing.Brushes.Black, new System.Drawing.PointF( 130 * TextureScale, -font.Size/2));
                g.RotateTransform(-TextAngle * Utilities.F_RAD2DEG);
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

            if (underlay != null && games.Count > 0)
            {
                System.Drawing.Graphics gU = System.Drawing.Graphics.FromImage(underlay);
                gU.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                int x = (int)Center.X - (Diameter / 2);
                int y = (int)Center.Y - (Diameter / 2);
                gU.DrawImage( bm, new System.Drawing.Point(x, y));
                gU.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
            }

            int tex = Utilities.LoadTexture(underlay);
            underlay.Dispose();

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
            if (this.IsSpinning || this.Games == null || this.Games.Count <= 0) return;

            SpeedTime = Utilities.Time;
            SpinupForce = force;

            this.IsSpinning = true;
            this.SpeedupTime = (float)Utilities.Rand.NextDouble(0.7, 1.3);
        }

        public float GetPaddleTurn( float angle )
        {
            angle = angle * Utilities.F_RAD2DEG;
            return ((float)Math.Cos(angle / (this.Games.Count / 4)) * 10) / Utilities.F_RAD2DEG;
        }

        public SteamCommunity.Game GetCurrentGame( int CurRegion )
        {
            return CurRegion > -1 ? Games[CurRegion] : SteamCommunity.Game.Default;
        }

        public override void Think()
        {
            Wheel.Position = this.Position;
            Wheel.Angles = this.Angles + new Angle(this.CurrentAngle * Utilities.F_RAD2DEG, 0, 0);

            if (Games == null) return;

            Paddle.Position = this.Position + PaddlePositionOffset;
            Paddle.Angles = new Angle(this.Angles.Pitch + GetPaddleTurn(this.CurrentAngle), this.Angles.Yaw, this.Angles.Roll);
            int CurrentRegion = GetRegionFromAngle(CurrentAngle);
            SteamCommunity.Game CurrentGame = GetCurrentGame(CurrentRegion);
            //Spin if neccessary
            if (Utilities.Time < SpeedTime + SpeedupTime)
            {
                CurrentSpeed += (float)Utilities.ThinkTime * this.SpinupForce;
            }
            else if (CurrentSpeed > 0)
            {
                CurrentSpeed -= (float)Utilities.ThinkTime * this.SpinFriction;
            }
            else if (this.IsSpinning)
            {
                CurrentSpeed = 0;
                IsSpinning = false;

                OnStopSpinning(CurrentGame);
            }


            if (CurrentRegion != LastSoundRegion)
            {
                LastSoundRegion = CurrentRegion;
                Audio.PlaySound("Resources/Audio/spinner_click.wav", 0.35f, Utilities.Rand.Next(35280, 52920));
            }

            CurrentAngle += CurrentSpeed;
            CurrentGameText.SetText(CurrentGame.AppID != -1 ? CurrentGame.Name : "No game");
        }

        public double NormalizeAngle(double angle)
        {
            angle = angle % (Math.PI * 2);
            return angle >= 0 ? angle : angle + Math.PI * 2;
        }

        public int GetRegionFromAngle(float angle)
        {
            if (Games.Count <= 0) return -1;

            angle = (float)NormalizeAngle(angle + Math.PI*2 - (ElementSizeRadians /2f));
            float percent = angle / (float)(Math.PI * 2);
            int region = (int)Math.Ceiling((percent * Games.Count));

            return region >= Games.Count ? 0 : region; //Handle wrapping around
        }

        private void OnStopSpinning( SteamCommunity.Game game )
        {
            //Invoke the event that we've stopped spinning and have got our game
            if (OnSpinnerStop != null)
                OnSpinnerStop.Invoke(game);
        }

        public override void Draw()
        {
            base.Draw();
            Wheel.Draw();
            Paddle.Draw();
        }
    }
}
