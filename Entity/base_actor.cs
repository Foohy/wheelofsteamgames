using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using OlegEngine;
using OlegEngine.Entity;

namespace WheelOfSteamGames.Entity
{
    class base_actor : BaseEntity 
    {
        public const string AnimationDir = "/animations/";
        public double TimePerFrame = 1d / 45d;
        public int CurrentFrame = 0;
        public string CurrentAnimation = "idle";
        public Dictionary<string, int[]> Animations = new Dictionary<string, int[]>();

        private double nextFrameTime = 0;
        public override void Init()
        {
            this.SetModel(Resource.GetMesh("character_plane.obj", true));
            this.Mat = new Material(Utilities.ErrorTex, "default");
            this.Mat.Properties.AlphaTest = true;
            this.Mat.Properties.NoCull = true;
        }

        public override void Think()
        {
            base.Think();

            //Change our frame in accordance to time
            if (nextFrameTime < Utilities.Time && Animations.Count > 0 && Animations.ContainsKey(CurrentAnimation))
            {
                double delta = Utilities.Time - nextFrameTime;
                nextFrameTime = Utilities.Time + this.TimePerFrame;
                CurrentFrame += 1 + (int)Math.Floor(delta / this.TimePerFrame);
                CurrentFrame = CurrentFrame < this.Animations[this.CurrentAnimation].Length ? CurrentFrame : 0;
                this.Mat.Properties.BaseTexture = this.Animations[this.CurrentAnimation][CurrentFrame];
            }
        }

        public void LoadAnimations(string name)
        {
            string folder = AnimationDir + name + "/";
            string[] files = Directory.GetFiles(Resource.TextureDir + folder);

            foreach (string file in files)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                string AnimationName = filename.Remove(filename.Length - 4);

                if (!Animations.ContainsKey(AnimationName)) LoadSingleAnimation(AnimationName, folder);
            }
        }

        public void SetAnimation(string name)
        {
            this.CurrentAnimation = name;
            this.CurrentFrame = 0;
        }

        private void LoadSingleAnimation(string animation, string folder )
        {
            int CurrentFrame = 1;
            string file = string.Format( "{0}{1}{2:D4}.png", folder, animation, CurrentFrame );
            List<int> Frames = new List<int>();
            while (File.Exists(Resource.TextureDir + file))
            {
                Frames.Add(Utilities.LoadTexture(file));
                CurrentFrame++;
                file = string.Format("{0}{1}{2:D4}.png", folder, animation, CurrentFrame);
            }

            Animations.Add(animation, Frames.ToArray());
        }
    }
}
