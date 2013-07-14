using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OlegEngine;
using OpenTK;
using OlegEngine.GUI;

namespace WheelOfSteamGames
{
    class Splash
    {
        public static bool SplashOver = false;

        private static int splashMat = 0;
        private static float Fade = 0;
        private static bool FadingIn = true;
        private static double FadeOutTime = 0;
        private static double StartTime = 0;

        private static Vector2 Dimensions = new Vector2(1280, 720);

        public static void Initialize()
        {
            splashMat = Resource.GetTexture("pt_splash.png");
            StartTime = Utilities.Time + 1 ;

            GUIManager.PostDrawHUD += new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
        }

        static void GUIManager_PostDrawHUD(EventArgs e)
        {
            Draw();
        }

        private static float GetScalingFactor()
        {
            float wScale = Utilities.window.Width / Dimensions.X;
            float hScale = Utilities.window.Height / Dimensions.Y;

            return wScale > hScale ? wScale : hScale;
        }

        public static void Draw()
        {
            Surface.SetTexture(splashMat);
            Surface.SetDrawColorVector(Fade, Fade, Fade);

            float scale = GetScalingFactor();
            Surface.DrawRect(Utilities.window.Width / 2 - (Dimensions.X * scale) / 2, Utilities.window.Height / 2 - (Dimensions.Y * scale) / 2, Dimensions.X * scale, Dimensions.Y * scale);

            if (FadingIn)
            {
                if (Utilities.Time > StartTime)
                {

                    Fade = Utilities.Lerp(0, 1, (float)(Utilities.Time - StartTime) * 2.0f);

                    if (Fade >= 1)
                    {
                        FadingIn = false;
                        FadeOutTime = Utilities.Time + 2;
                    }
                }
            }
            else
            {
                if (Utilities.Time > FadeOutTime && Fade > 0)
                {
                    Fade = Utilities.Lerp(1, 0, (float)(Utilities.Time - (FadeOutTime)) * 2.0f);
                }
                else if (Fade < 0)
                {
                    MainRoom.Initialize();
                    SplashOver = true;
                    GUIManager.PostDrawHUD -= new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
                }
            }
        }
    }
}
