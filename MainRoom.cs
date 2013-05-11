using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Graphical stuff
using OpenTK;

//Engine calls
using OlegEngine;
using OlegEngine.Entity;
using OlegEngine.GUI;

using WheelOfSteamGames.Entity;

namespace WheelOfSteamGames
{
    class MainRoom
    {
        public static Mesh WorldMesh;
        public static ent_spotlight spotlight;
        public static ent_spinner Spinner;
        public static bool Started = false;

        private static float StartupDelay = 0.0f;

        public static void Initialize()
        {
            //Create the player
            ent_camera cam = EntManager.Create<ent_camera>();
            cam.Spawn();
            cam.SetPos(new OpenTK.Vector2(0, 4));

            //Set the player as the object that'll be controlling the view
            View.SetLocalPlayer(cam);

            SetUpScene();

            Utilities.window.Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);

            //TODO: make this load actual game data
            /*
            List<ent_spinner.Game> Games = new List<ent_spinner.Game>()
            {
                new ent_spinner.Game( "Absolutely nothing"),
                new ent_spinner.Game( "Mac's used tissues"),
                new ent_spinner.Game( "1000 GMC"),
                new ent_spinner.Game( "Visit Mac in Redmond Washington"),
                new ent_spinner.Game( "Free Donor"),
                new ent_spinner.Game( "1,000,000 GMC"),
                new ent_spinner.Game( "Become friends with admin"),
                new ent_spinner.Game( "Easter leftovers"),
                new ent_spinner.Game( "Custom model"),
                new ent_spinner.Game( "$0.70 USD"),
                new ent_spinner.Game( "100,000 GMC"),
                new ent_spinner.Game( "Free admin"),
                new ent_spinner.Game( "100,000 catsacks"),
                new ent_spinner.Game( "10,000 GMC"),
                new ent_spinner.Game( "A date with Foohy"),
                new ent_spinner.Game( "Free T-shirt"),
            };
            */

            Steam.Initialize();
            List<ent_spinner.Game> Games = new List<ent_spinner.Game>()
            {
                new ent_spinner.Game( "Nothing at all"),
                new ent_spinner.Game( "Buy more games"),
                new ent_spinner.Game( "Honestly"),
                new ent_spinner.Game( "Take the hint"),
            };

            for (int i = 0; i < 25; i++ )
            {
                Games.Add(new ent_spinner.Game(Utilities.Rand.Next(0, 1000).ToString()));
            }

            Spinner.CreateElements(Games);
        }

        static void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            if (e.Key == OpenTK.Input.Key.Space)
            {
                Spinner.Spin(0.095f);
                HintManager.RemoveHintNice("spin_controls_hint");
            }
        }

        private static void SetUpScene()
        {
            //Load the world
            WorldMesh = Resource.GetMesh("floor.obj");
            WorldMesh.mat = Resource.GetMaterial("engine/white");

            ent_pointlight pointlight = EntManager.Create<ent_pointlight>();
            pointlight.Spawn();
            pointlight.AmbientIntensity = 0.4f;
            pointlight.DiffuseIntensity = 20.0f; //0.85f
            pointlight.Color = new Vector3(1.0f, 0.5f, 0.00f);
            //pointlight.SetPos(new Vector3(-2.00f, 2.421f, -14.90f));
            pointlight.Linear = 2.7f;
            pointlight.Enabled = false;

            spotlight = EntManager.Create<ent_spotlight>();
            spotlight.Spawn();

            spotlight.Color = new Vector3(1.0f, 1.0f, 1.0f);
            spotlight.Constant = 1.0f;
            spotlight.Cutoff = 20.0f;
            //spotlight.SetAngle(new Vector3(0.9387657f, -0.8191915f, -0.3445562f));
            //spotlight.SetPos(new Vector3(-12.34661f, 15.45926f, 4.4074f));
            spotlight.SetAngle(new Vector3(0.5989776f, -0.841471f, -0.8007658f));
            spotlight.SetPos(new Vector3(-10.22733f, 17.82458f, 12.54623f));
            spotlight.Enabled = false;

            
            Audio.Precache("Resources/Audio/light_on.wav");

            ShadowTechnique.Enable();

            Spinner = EntManager.Create<ent_spinner>();
            Spinner.Spawn();
            Spinner.SetAngle(new Vector3(0, 180, 0));
            Spinner.SetPos(new Vector3(0, -(float)Spinner.Model.BBox.Negative.Y, 0));

            View.Player.SetPos(new Vector3(-13.50925f, 5.614059f, 2.610255f));
            View.Player.SetAngle(new Vector3(0.9982005f, -0.03713433f, 0.05996396f));
            //Notable camera positions
            //Pos: -13.50925f, 5.614059f, 2.610255
            //Ang: 0.9982005f, -0.03713433f, 0.05996396f


            //Delay before we turn on the lights for effect
            StartupDelay = (float)Utilities.Time + 1.5f;

            //Create some hint text
            HintManager.Initialize();
            HintManager.AddHint("Press space to spin!", 2.0f, 5.0f, "spin_controls_hint");
        }

        public static void Think()
        {
            if (spotlight != null && Utilities.window.Keyboard[OpenTK.Input.Key.Q])
            {
                Console.WriteLine(View.Player.Position);
                Console.WriteLine(View.ViewNormal);

                spotlight.SetAngle(View.ViewNormal);
                spotlight.SetPos(View.Player.Position);
            }

            if (Utilities.Time > StartupDelay && !Started)
            {
                Started = true;
                spotlight.Enabled = true;
                Audio.PlaySound("Resources/Audio/light_on.wav");
                ShadowTechnique.Enabled = true;
            }
        }

        public static void Draw()
        {
            if (WorldMesh != null)
            {
                WorldMesh.Draw();
            }
        }
    }
}
