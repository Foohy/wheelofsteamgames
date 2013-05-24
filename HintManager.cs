using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using OlegEngine.GUI;
using OlegEngine;

namespace WheelOfSteamGames
{
    class HintManager
    {
        /// <summary>
        /// Speed at which the hints move in and out of the screen
        /// </summary>
        public static float MoveSpeed = 3;
        class Hint
        {
            public Text hintText;
            public float Duration;
            public float Delay;
            public float YReal;
            public float YSmooth;
            public string Name;

            /// <summary>
            /// Create a new hint object, to be used with the hint manager
            /// </summary>
            /// <param name="Text">The actual hint text</param>
            /// <param name="duration">How long the hint should be displayed</param>
            /// <param name="delay">Time to wait before the hint is actually displayed</param>
            public Hint(string Text, float duration, float delay, string UniqueName)
            {
                hintText = new Text("windowtitle", Text);
                Delay = (float)Utilities.Time + delay;
                Duration = Delay + duration;

                float HintScale = (float)Utilities.window.Height / 800f;
                hintText.SetScale(HintScale, HintScale);
                hintText.SetPos((Utilities.window.Width / 2) - (hintText.GetTextLength() / 2) * hintText.ScaleW, -hintText.GetTextHeight());

                Name = UniqueName;
            }

            public void Remove()
            {
                HintQueue.Remove( this );
            }
        }

        private static List<Hint> HintQueue = new List<Hint>();

        public static void Initialize()
        {
            GUIManager.PostDrawHUD += new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
        }

        static void GUIManager_PostDrawHUD(EventArgs e)
        {
            for (int i = 0; i < HintQueue.Count; i++)
            {
                Hint h = HintQueue[i];

                if (Utilities.Time > h.Delay )
                {
                    if (Utilities.Time > h.Duration)
                    {
                        h.YReal = -h.hintText.GetTextHeight() - 10;

                        if ( Math.Abs(h.YSmooth - h.YReal) > 0.5 )
                        {
                            h.YSmooth = Utilities.Lerp(h.YSmooth, h.YReal, (float)Utilities.Frametime * MoveSpeed);
                        }
                        else
                        {
                            HintQueue.RemoveAt(i);
                            i--;
                        }
                    }
                    else
                    {
                        h.YReal = h.hintText.GetTextHeight() + 10;
                        h.YSmooth = Utilities.Lerp(h.YSmooth, h.YReal, (float)Utilities.Frametime * MoveSpeed);
                    }

                    h.hintText.SetPos(h.hintText.X, h.YSmooth);
                    h.hintText.Draw(); 
                }
            }
        }

        public static void AddHint(string Text, float delay, float time, string UniqueName = "")
        {
            Hint h = new Hint(Text, time, delay, UniqueName);
            HintQueue.Add(h);
        }

        public static void RemoveHint(string name)
        {
            for (int i = 0; i < HintQueue.Count; i++)
            {
                if (HintQueue[i].Name == name)
                {
                    HintQueue.RemoveAt(i);
                    i--;
                }
            }
        }

        public static void RemoveHintNice(string name)
        {
            for (int i = 0; i < HintQueue.Count; i++)
            {
                if (HintQueue[i].Name == name)
                {
                    HintQueue[i].Duration = 0;
                    HintQueue[i].Delay = 0;
                }
            }
        }
    }
}
