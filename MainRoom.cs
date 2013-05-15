using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

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
        private static GUI.PauseMenu Menu;
        public delegate bool ReturnCriteria(SteamCommunity.Game game);
        private static BackgroundWorker worker;
        private static List<SteamCommunity.Game> AllGames = new List<SteamCommunity.Game>(); //Keep a  list of ALL steam games handy. Do not edit this list

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

            //Set the sync context to point to this (main) thread
            System.Threading.SynchronizationContext.SetSynchronizationContext(System.Threading.SynchronizationContext.Current);
            System.Threading.Thread.CurrentThread.SetApartmentState(System.Threading.ApartmentState.STA);
            System.Threading.Thread.CurrentThread.IsBackground = true;

            //Create a background worker to load steam data
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(AsyncBeginLoad);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AsyncEndLoad);

            //GIMME THE DATA
            //BeginLoadData();

            //FUCKTHREADS
            AllGames = SteamCommunity.GetGamesFromCommunity("foohy");
            var FilteredGames = GetFilteredGamesList();
            Spinner.CreateElements(FilteredGames);
            StartupDelay = (float)Utilities.Time + 1.5f;


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
             * */
        }

        public static void BeginLoadData()
        {
            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        private static void AsyncBeginLoad(object sender, DoWorkEventArgs e)
        {
            var Games = SteamCommunity.GetGamesFromCommunity("foohy");
            // Steam.Initialize();
            e.Result = (object)Games;
        }

        private static void AsyncEndLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                EndLoad end = new EndLoad(AsyncEndLoad);
                end.Invoke((List<SteamCommunity.Game>)e.Result);
            }

        }

        private static void AsyncEndLoad(List<SteamCommunity.Game> Games)
        {
            //Store the list of games
            AllGames = Games;

            //Create the spinner elements
            var FilteredGames = GetFilteredGamesList();
            Spinner.CreateElements(FilteredGames);

            //If we're just starting up, tell the lights to turn on
            StartupDelay = (float)Utilities.Time + 1.5f;
        }



        public delegate void EndLoad(List<SteamCommunity.Game> Games);

        static void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            if (e.Key == OpenTK.Input.Key.Space)
            {
                Spinner.Spin(0.095f);
                HintManager.RemoveHintNice("spin_controls_hint");
            }

            if (e.Key == OpenTK.Input.Key.F1)
            {
                Steam.App[] apps = Steam.RefreshGames();
                if (apps != null && apps.Length > 0)
                {
                    foreach (Steam.App app in apps)
                    {
                        Console.WriteLine(app.Name);
                    }
                }
                else Console.WriteLine("Failed to get steam game list!");
            }

            if (e.Key == OpenTK.Input.Key.Escape)
            {
                if (Menu.IsShown)
                {
                    Menu.HideToLeft();
                    Input.LockMouse = true;
                }
                else
                {
                    Menu.ShowToLeft();
                    Input.LockMouse = false;
                }
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

            //Create some hint text
            HintManager.Initialize();

            //Create a panel holding filter options
            Menu = GUIManager.Create<GUI.PauseMenu>();
            Menu.SetWidth(220);
            Menu.SetHeight(400);
            Menu.SetTitle("Filters");
            Menu.AddCheckBox("Show Singleplayer Games", "game_single");
            Menu.AddCheckBox("Show Multiplayer Games", "game_multi");
            Menu.AddCheckBox("Show Games with VAC", "game_vac");
            Menu.AddCheckBox("Show Favorites only", "game_favorites");
            Menu.AddCheckBox("Show Games with HDR", "game_hdr");
            Menu.AddCheckBox("Show Recently played games", "game_2weeks");

            Menu.OnAcceptPress += new Action(Menu_OnAcceptPress);
        }

        static List<SteamCommunity.Game> GetFilteredGames(ReturnCriteria rc)
        {
            List<SteamCommunity.Game> FilteredGames = new List<SteamCommunity.Game>();
            foreach (SteamCommunity.Game game in AllGames)
            {
                //if (!Steam.IsAppSubscribed( (uint)game.AppID ))
                //    continue;

                if (!rc(game))
                    continue;

                FilteredGames.Add(game);
            }

            return FilteredGames;
        }

        public static List<SteamCommunity.Game> GetFilteredGamesList()
        {
            //Save settings?
            List<SteamCommunity.Game> FilteredGames = GetFilteredGames((game) =>
            {
                if (Menu.GetCheckboxChecked("game_single") && !game.IsSingleplayer)
                    return false;

                if (Menu.GetCheckboxChecked("game_multi") && !game.IsMultiplayer)
                    return false;

                if (Menu.GetCheckboxChecked("game_vac") && !game.HasVAC)
                    return false;

                if (Menu.GetCheckboxChecked("game_hdr") && !game.HasHDR)
                    return false;

                if (Menu.GetCheckboxChecked("game_2weeks") && game.HoursLast2Weeks <= 0)
                    return false;

                return true;

            });

            return FilteredGames;
        }

        static void Menu_OnAcceptPress()
        {
            Menu.HideToLeft();

            var FilteredGames = GetFilteredGamesList();
            Spinner.CreateElements(FilteredGames);
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

            if (Utilities.Time > StartupDelay && !Started && !worker.IsBusy)
            {
                HintManager.AddHint("Press space to spin!", 2.0f, 5.0f, "spin_controls_hint");
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
