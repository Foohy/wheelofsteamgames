using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using OlegEngine;
using OlegEngine.GUI;
using System.IO;

namespace WheelOfSteamGames.Entity
{
    class act_announcer : base_actor
    {
        public const string DialoguesDir = "Resources/Text/dialogues/";
        public const double TextRemainTime = 6.0f;
        public const double TextFadeTime = 1.0f;
        public Dictionary<int, string[]> Lines = new Dictionary<int, string[]>();

        private FBO SpeechFBO; //This is where we'll render out the speech bubble for one frame and display it just as a texture
        private string curString;
        private string lastString;
        private Matrix4 Line2D3DMatrix;
        private const int LineBubbleRes = 256;
        private Mesh TextDisplayMesh;
        private Material TextMat;

        private double TextEndTime = 0;
        private double TextEndFadeTime = 0;

        public override void Init()
        {
            base.Init();

            //Create our FBO to do some offscreen rendering
            SpeechFBO = new FBO(LineBubbleRes, LineBubbleRes, false);

            this.LoadAnimations("test");
            this.LoadDialogue("announcer_lines");

            //We need a custom view matrix to draw our speech bubble in a way that makes sense
            Line2D3DMatrix = Matrix4.CreateOrthographicOffCenter(0, LineBubbleRes, 0, LineBubbleRes, Utilities.NearClip, Utilities.FarClip);

            TextMat = new Material(SpeechFBO.RenderTexture, Resource.GetProgram("default"));
            TextMat.Properties.AlphaTest = true;
            TextMat.Properties.NoCull = true;
            
            //Mesh to display the text
            TextDisplayMesh = EngineResources.CreateNewQuadMesh();
            TextDisplayMesh.mat = TextMat;


            GUIManager.PostDrawHUD += new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
        }

        public override void Draw()
        {
            base.Draw();

            if (TextEndFadeTime > Utilities.Time)
            {
                TextDisplayMesh.Position = this.Position + new Vector3(0, 13, -2);
                TextDisplayMesh.Angles = new Angle(this.Angles.Pitch + 180, this.Angles.Yaw + 140, this.Angles.Roll);
                TextDisplayMesh.Scale = Vector3.One * 4;
                TextDisplayMesh.mat = TextMat;
                TextDisplayMesh.Draw();
            }
        }

        void GUIManager_PostDrawHUD(EventArgs e)
        {

            if (this.curString != this.lastString)
            {
                this.lastString = this.curString;

                //Set our custom matrix
                Matrix4 oldView = Utilities.ViewMatrix;
                Matrix4 oldProj = Utilities.ProjectionMatrix;
                Utilities.ViewMatrix = Line2D3DMatrix;
                Utilities.ProjectionMatrix = Matrix4.Identity;


                //Start rendering from our ~custom~ framebuffer
                SpeechFBO.BindForWriting();
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


                DrawTextBubble();
                SpeechFBO.ResetFramebuffer();
                // DrawTextBubble();

                //Set the viewmatrix back to what it was
                Utilities.ViewMatrix = oldView;
                Utilities.ProjectionMatrix = oldProj;
            }
        }


        public override void Think()
        {
            base.Think();
        }

        public void LoadDialogue( string dialogueSet )
        {
            try
            {
                string file = DialoguesDir + dialogueSet + ".txt";
                string[] lines = File.ReadAllLines(file);

                int curAppID = -1;
                List<string> CurLinesList = new List<string>();
                foreach (string line in lines)
                {
                    //Remove comments
                    int index = line.IndexOf("//");
                    string lineClean = index > -1 ? line.Remove(index) : line;

                    int appid = -1;
                    if (int.TryParse(lineClean, out appid) && appid != curAppID)
                    {
                        this.Lines.Add(curAppID, CurLinesList.ToArray());
                        CurLinesList.Clear();
                        curAppID = appid;
                    }
                    else if (!string.IsNullOrEmpty( lineClean ) && !string.IsNullOrWhiteSpace( lineClean ) )
                    {
                        CurLinesList.Add(lineClean);
                    }
                }

                //Add any game lines left
                if (CurLinesList.Count > 0)
                    this.Lines.Add(curAppID, CurLinesList.ToArray());
            }
            catch (Exception e)
            {
                Utilities.Print("Failed to load animations for '{0}'! ({1})", Utilities.PrintCode.ERROR, dialogueSet, e.Message);
            }
        }

        public void SayLine(int AppID)
        {
            int ID = Lines.ContainsKey(AppID) ? AppID : -1;

            string Line = Lines[ID][Utilities.Rand.Next( 0, Lines[ID].Length)];
            SayLine(Line);
        }

        private void SayLine(string str)
        {
            this.curString = str;

            TextEndTime = Utilities.Time + TextRemainTime;
            TextEndFadeTime = TextEndTime + TextFadeTime;
        }

        /// <summary>
        /// Draw the texture that'll be the text for narrator
        /// This is only called once when the line actually changes
        /// </summary>
        private void DrawTextBubble()
        {
            Surface.SetDrawColor(255, 255, 255);

            //Draw the little talk bubble
            Surface.SetTexture(Resource.GetTexture("linebubble.png"));
            Surface.DrawRect(0, 0, LineBubbleRes, LineBubbleRes);

            //Draw the text itself
            Surface.SetNoTexture();
            Surface.SetDrawColor(0, 0, 0);
            Surface.DrawWrappedText("windowtitle", this.curString, 25, 45, LineBubbleRes - 80);
        }
    }
}
