using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using OlegEngine;
using OlegEngine.Entity;
using OlegEngine.GUI;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WheelOfSteamGames.Entity
{
    class act_announcer : base_actor
    {
        public const string DialoguesDir = "Resources/Text/dialogues/";
        public const double TextRemainTime = 8.0f;
        public const double TextFadeTime = 2.0f;

        private List<App> AppLines = new List<App>();
        private Line CurLine;
        private int CurLineIndex= 0;
        private FBO SpeechFBO; //This is where we'll render out the speech bubble for one frame and display it just as a texture
        private string curString;
        private string lastString;
        private Matrix4 Line2D3DMatrix;
        private const int LineBubbleRes = 256;
        private const int MeshScale = 8;
        private Vector2 LeftArrowPos = new Vector2(25, LineBubbleRes - 130);
        private Vector2 RightArrowPos = new Vector2(LineBubbleRes - 50, LineBubbleRes - 130);
        private Mesh TextDisplayMesh;
        private Material TextMat;
        private Vector3 TextDisplayOffset = new Vector3(-5, 16, 0);

        private double TextEndTime = 0;
        private double TextEndFadeTime = 0;

        //Stuff for dialogue
        class Line
        {
            public string[] GameLines = null;
            public string ReactionAnim = "";
            public string AdditionalInfo = ""; //Comments, etc.
        }

        class App
        {
            public int AppID = 0;
            public Line[] Lines = null;
        }

        public override void Init()
        {
            base.Init();

            this.RenderMode = RenderModes.Translucent;

            //Create our FBO to do some offscreen rendering
            SpeechFBO = new FBO(LineBubbleRes, LineBubbleRes, false);

            this.LoadAnimations("Announcer");
            this.LoadDialogue("announcer_lines");

            //We need a custom view matrix to draw our speech bubble in a way that makes sense
            Line2D3DMatrix = Matrix4.CreateOrthographicOffCenter(0, LineBubbleRes, 0, LineBubbleRes, Utilities.NearClip, Utilities.FarClip);

            TextMat = new Material(SpeechFBO.RenderTexture, Resource.GetProgram("default"));
            //TextMat.Properties.AlphaTest = true;
            //TextMat.Properties.NoCull = true;
            
            //Mesh to display the text
            TextDisplayMesh = EngineResources.CreateNewQuadMesh();
            TextDisplayMesh.mat = TextMat;

            EntManager.OnPostDrawTranslucentEntities += new Action(EntManager_OnPostDrawTranslucentEntities);
            GUIManager.PostDrawHUD += new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
        }

        /// <summary>
        /// Since the announcer text will fade, we draw it after all the opaque renderables are drawn
        /// </summary>
        void EntManager_OnPostDrawTranslucentEntities()
        {
            if (TextEndFadeTime > Utilities.Time)
            {
                //Fade out the alpha if the time is between the end time and the fade end time, else set the alpha to 1.0
                TextDisplayMesh.Alpha = TextEndTime < Utilities.Time ? 1 - (float)((Utilities.Time - TextEndTime) / (TextEndFadeTime - TextEndTime)) : 1.0f;


                TextDisplayMesh.Position = this.Position + TextDisplayOffset;
                TextDisplayMesh.Angles = new Angle(this.Angles.Pitch + 180, this.Angles.Yaw, this.Angles.Roll);
                TextDisplayMesh.Scale = Vector3.One * MeshScale;
                TextDisplayMesh.mat = TextMat;
                TextDisplayMesh.Draw();
            }


            //Handle pressing the directional buttons with the mouse
            /*
            Vector3 Up;
            Vector3 Forward = this.Angles.Forward();
            Vector3 Right;
            this.Angles.AngleVectors(out Forward, out Up, out Right);

            //Get the world position of the buttons
            Vector3 worldPosLeft = new Vector3(Forward.X * (LeftArrowPos.X / LineBubbleRes) * MeshScale, -Up.Y * (LeftArrowPos.Y / LineBubbleRes) * MeshScale, -Forward.Z * (LeftArrowPos.X / LineBubbleRes) * MeshScale) + this.Position + TextDisplayOffset;
            Vector3 worldPosRight = new Vector3(Forward.X * (RightArrowPos.X / LineBubbleRes) * MeshScale, -Up.Y * (RightArrowPos.Y / LineBubbleRes) * MeshScale, -Forward.Z * (RightArrowPos.X / LineBubbleRes) * MeshScale) + this.Position + TextDisplayOffset;

            Graphics.DrawSphere(worldPosLeft, 0.5f);
            Graphics.DrawSphere(worldPosRight, 0.5f);
             * */
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
                FBO.ResetFramebuffer();

                //Set the viewmatrix back to what it was
                Utilities.ViewMatrix = oldView;
                Utilities.ProjectionMatrix = oldProj;
            }
        }

        public bool HasDialogue(int AppID)
        {
            App app = FindAppByID(AppID);
            return app != null && app.AppID == AppID;
        }

        public void LoadDialogue( string dialogueSet )
        {
            try
            {
                string file = DialoguesDir + dialogueSet + ".txt";
                string json = File.ReadAllText(file);
                AppLines = JsonConvert.DeserializeObject<List<App>>(json);
            }
            catch (Exception e)
            {
                Utilities.Print("Failed to load animations for '{0}'! ({1})", Utilities.PrintCode.ERROR, dialogueSet, e.Message);
            }
        }

        public void SayLine(int AppID)
        {
            App app = FindAppByID(AppID);
            if (app == null) return;

            Line Line = app.Lines[Utilities.Rand.Next(0, app.Lines.Length)];
            SayLine(Line.GameLines[0]);
            this.CurLine = Line;
            this.CurLineIndex = 0;
        }

        public void SayNextPage()
        {
            SetTextPage(this.CurLineIndex + 1);
        }

        public void SayPreviousPage()
        {
            SetTextPage(this.CurLineIndex - 1);
        }

        public bool HasNextPage()
        {
            return this.CurLine != null && this.CurLineIndex < this.CurLine.GameLines.Length-1;
        }

        public bool HasPreviousPage()
        {
            return this.CurLine != null && this.CurLineIndex > 0;
        }

        public void SetTextPage(int pageIndex)
        {
            if (this.CurLine == null) return;

            int newIndex = Utilities.Clamp(pageIndex, this.CurLine.GameLines.Length - 1, 0);
            if (newIndex != this.CurLineIndex)
                this.CurLineIndex = newIndex;
            else return;

            this.SayLine(this.CurLine.GameLines[this.CurLineIndex]);
        }

        private App FindAppByID(int id, bool def=false)
        {
            foreach (App app in AppLines)
            {
                if (app.AppID == id) return app;
            }
            if (!def)
                return FindAppByID(0, true);
            else return null;
        }

        public void SayLine(string str)
        {
            this.curString = str;

            TextEndTime = Utilities.Time + TextRemainTime;
            TextEndFadeTime = TextEndTime + TextFadeTime;
        }

        public void FadeLine()
        {
            if (TextEndTime > Utilities.Time)
            {
                TextEndTime = Utilities.Time;
                TextEndFadeTime = TextEndTime + TextFadeTime;
            }
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
            Surface.DrawWrappedText("defaultTitle", this.curString, 25, 45, LineBubbleRes - 80);

            //Draw right directional arrow, if neccessary
            if (this.HasNextPage())
            {
                Surface.SetTexture(Resource.GetTexture("arrow_right.png"));
                Surface.SetDrawColor(255, 255, 255);
                Surface.DrawRect(RightArrowPos, 25, 25);
            }

            //Draw left directional arrow, if neccessary
            if (this.HasPreviousPage())
            {
                Surface.SetTexture(Resource.GetTexture("arrow_left.png"));
                Surface.SetDrawColor(255, 255, 255);
                Surface.DrawRect(LeftArrowPos, 25, 25);
            }
        }

        
    }
}
