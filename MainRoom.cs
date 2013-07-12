using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        public static bool Loaded = false;
        public static bool IsLoadingData = false;
        public static bool IsCreatingCache = false;

        private static string CurrentGame = "No Game";
        private static float StartupDelay = 0.0f;
        private static GUI.PauseMenu Menu;
        public delegate bool ReturnCriteria(SteamCommunity.Game game);
        private static List<SteamCommunity.Game> AllGames = new List<SteamCommunity.Game>(); //Keep a  list of ALL steam games handy. Do not edit this list

        private static Material LoadingMat;
        private static Text LoadingText;
        private static Text LongLoadingText;
        private static Text CurrentGameText;
        private static act_announcer Actor;
        private static Window usernameWindow;

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
            GUIManager.PostDrawHUD += new GUIManager.OnDrawHUD(GUIManager_PostDrawHUD);
            SteamCommunity.OnLoadGame += new Action<SteamCommunity.Game>(SteamCommunity_OnLoadGame);

            //On startup, ask for steam community username
            CreateUserSelectDialogue();
        }

        public static Window CreateUserSelectDialogue()
        {
            if (usernameWindow) return usernameWindow;

            usernameWindow = GUIManager.Create<Window>();
            usernameWindow.SetTitle("Enter Username");
            usernameWindow.Resizable = false;
            usernameWindow.SetWidth(380);
            usernameWindow.SetHeight(115);
            usernameWindow.SetEnableCloseButton(false);
            usernameWindow.SetPos(Utilities.window.Width / 2 - usernameWindow.Width / 2, Utilities.window.Height / 2 - usernameWindow.Height / 2);
            usernameWindow.MinimumSize = new Vector2(289, 115);

            #region Connection panel (For accessing an account over the internet)
            //Create a panel that will hold all of the connect online related things
            Panel connectPanel = GUIManager.Create<Panel>(usernameWindow);
            connectPanel.DockPadding(2, 2, 2, 2);
            connectPanel.Dock(Panel.DockStyle.FILL);
            connectPanel.ShouldDraw = false;
            //connectPanel.ShouldDrawChildren = true;
            connectPanel.Name = "panel_connect";


            Label questionText = GUIManager.Create<Label>(connectPanel);
            questionText.Autosize = true;
            questionText.SetText("Please enter your community URL");
            questionText.SetPos(15, 10);
            /*
            Label exampleText = GUIManager.Create<Label>(connectPanel);
            exampleText.Autosize = true;
            exampleText.SetText("http://steamcommunity.com/id/");
            exampleText.SetPos(15, questionText.Position.Y + 15);
            exampleText.SetColor(110, 110, 110);

            Label exampleTextImportant = GUIManager.Create<Label>(connectPanel);
            exampleTextImportant.Autosize = true;
            exampleTextImportant.SetText("YOURNAMEHERE");
            exampleTextImportant.SetPos(exampleText.Width + exampleText.Position.X, exampleText.Position.Y);
            */
            TextInput input = GUIManager.Create<TextInput>(connectPanel);
            input.SetAnchorStyle(Panel.Anchors.Left | Panel.Anchors.Top | Panel.Anchors.Right);
            input.SetPos(new Vector2(20, questionText.Position.Y + 20));
            input.SetWidth(connectPanel.Width - 40);
            input.Name = "community_input";

            Button localSaveBtn = GUIManager.Create<Button>(connectPanel);
            localSaveBtn.SetText("Local Saves...");
            localSaveBtn.SetWidth(120);
            localSaveBtn.SetHeight(20);
            localSaveBtn.SetPos(new Vector2(0, connectPanel.Height - (localSaveBtn.Height + 20)));
            localSaveBtn.AlignRight(20);
            localSaveBtn.SetAnchorStyle(Panel.Anchors.Bottom | Panel.Anchors.Right);
            localSaveBtn.OnButtonPress += new Button.OnButtonPressDel(localSaveBtn_OnButtonPress);

            Button acceptBtn = GUIManager.Create<Button>(connectPanel);
            acceptBtn.SetText("Go");
            acceptBtn.SetWidth(localSaveBtn.Position.X - 40);
            acceptBtn.SetHeight(20);
            acceptBtn.SetPos(new Vector2(20, localSaveBtn.Position.Y));
            acceptBtn.AlignLeft(20);
            acceptBtn.SetAnchorStyle(Panel.Anchors.Bottom | Panel.Anchors.Right | Panel.Anchors.Left);
            acceptBtn.OnButtonPress += new Button.OnButtonPressDel(acceptBtn_OnButtonPress);

            Label progressText = GUIManager.Create<Label>(connectPanel);
            progressText.SetWidth(connectPanel.Width);
            progressText.Below(input, 4);
            progressText.SetAlignment(Label.TextAlign.TopCenter);
            progressText.SetAnchorStyle(Panel.Anchors.Left | Panel.Anchors.Top | Panel.Anchors.Right);
            progressText.SetColor(255, 0, 0);
            progressText.Name = "progress_text";
            #endregion

            #region Local Saves panel (For using pre-existing saves of games)

            //Create a panel that will hold a list of local saves to load from
            Panel savesPanel = GUIManager.Create<Panel>(usernameWindow);
            savesPanel.DockPadding(2, 2, 2, 2);
            savesPanel.Dock(Panel.DockStyle.FILL);
            savesPanel.ShouldDraw = false;
            savesPanel.ShouldDrawChildren = false;
            savesPanel.Enabled = false;
            savesPanel.Name = "panel_saves";


            Label infoText = GUIManager.Create<Label>(savesPanel);
            infoText.Autosize = true;
            infoText.SetText("Select a pre-existing save from your saves folder");
            infoText.SetPos(15, 10);

            ListView savesList = GUIManager.Create<ListView>(savesPanel);
            savesList.SetHeight(30);
            savesList.SetWidth(savesPanel.Width - 40);
            savesList.SetPos(20, 30);
            savesList.SetAnchorStyle(Panel.Anchors.Left | Panel.Anchors.Bottom | Panel.Anchors.Top | Panel.Anchors.Right);
            savesList.Name = "list_saves";

            var saves = SteamCommunity.GetLocalSaves();
            foreach (var Save in saves)
            {
                savesList.AddListItem(Save, Save.Value, Save.Key);
            }

            Button connectPanelBtn = GUIManager.Create<Button>(savesPanel);
            connectPanelBtn.SetText("Connect to steam...");
            connectPanelBtn.SetWidth(120);
            connectPanelBtn.SetHeight(20);
            connectPanelBtn.SetPos(new Vector2(0, savesPanel.Height - (localSaveBtn.Height + 20)));
            connectPanelBtn.AlignRight(20);
            connectPanelBtn.SetAnchorStyle(Panel.Anchors.Bottom | Panel.Anchors.Right);
            connectPanelBtn.OnButtonPress += new Button.OnButtonPressDel(connectPanelBtn_OnButtonPress);

            Button acceptSavesBtn = GUIManager.Create<Button>(savesPanel);
            acceptSavesBtn.SetText("Go");
            acceptSavesBtn.SetWidth(localSaveBtn.Position.X - 40);
            acceptSavesBtn.SetHeight(20);
            acceptSavesBtn.SetPos(new Vector2(20, localSaveBtn.Position.Y));
            acceptSavesBtn.AlignLeft(20);
            acceptSavesBtn.SetAnchorStyle(Panel.Anchors.Bottom | Panel.Anchors.Right | Panel.Anchors.Left);
            acceptSavesBtn.OnButtonPress += new Button.OnButtonPressDel(acceptSavesBtn_OnButtonPress);

            progressText = GUIManager.Create<Label>(savesPanel);
            progressText.SetWidth(savesPanel.Width);
            progressText.Below(input, 4);
            progressText.SetAlignment(Label.TextAlign.TopCenter);
            progressText.SetAnchorStyle(Panel.Anchors.Left | Panel.Anchors.Top | Panel.Anchors.Right);
            progressText.SetColor(255, 0, 0);
            progressText.Name = "progress_text";

            #endregion

            return usernameWindow;
        }

        static void acceptSavesBtn_OnButtonPress(Panel sender)
        {
            ListView listView = sender.Parent.GetChildByName("list_saves") as ListView;
            if (!listView) { Utilities.Print("Could not get list view panel!"); return; }
            ListViewItem item = listView.SelectedPanel;
            if (!item) { Utilities.Print("Could not get list view item!"); return; }

            var data = (KeyValuePair<string, string>)item.Userdata;

            string communityname = data.Value;
            string communityid = data.Key;

            if (usernameWindow)
            {
                usernameWindow.Remove();
                usernameWindow = null;
            }

            GenericLoadCommunityID(communityname, communityid);
            //BeginLoadData(communityname, communityid );
        }

        private const float LoadingSize = 30f;
        private const float LoadingOffset = 30f;
        static void GUIManager_PostDrawHUD(EventArgs e)
        {
            if (LoadingMat == null) { LoadingMat = Resource.GetMaterial("gui/loading"); }
            if (LoadingText == null) { LoadingText = new Text("game_large", "Loading data..."); LoadingText.SetScale(0.25f, 0.25f); }
            if (LongLoadingText == null) { LongLoadingText = new Text("defaultTitle", "Loading/Caching game data from the internet. Please be patient, this only happens once." ); }
            if (CurrentGameText == null) { CurrentGameText = new Text("title", ""); }

            if (IsLoadingData)
            {
                Surface.SetDrawColor(255, 255, 255);
                Surface.SetTexture(LoadingMat.GetCurrentTexture());
                Surface.DrawRect(Utilities.window.Width - (LoadingSize + LoadingOffset), Utilities.window.Height - (LoadingSize + LoadingOffset), LoadingSize, LoadingSize);
                LoadingText.SetPos(Utilities.window.Width - (LoadingText.GetTextLength() * LoadingText.ScaleW + LoadingSize + LoadingOffset + 10), Utilities.window.Height - (LoadingText.GetTextHeight() / 2 + LoadingSize / 2 + LoadingOffset));
                LoadingText.Draw();

                if (IsCreatingCache)
                {
                    LongLoadingText.SetPos(Utilities.window.Width/2 - (LongLoadingText.GetTextLength() / 2), Utilities.window.Height/2 - (LongLoadingText.GetTextHeight() / 2));
                    LongLoadingText.Draw();

                    CurrentGameText.SetPos(LongLoadingText.X, LongLoadingText.Y + LongLoadingText.GetTextHeight() + 2);
                    CurrentGameText.SetText(CurrentGame);
                    CurrentGameText.Draw();
                }
            }
        }

        //If we're loading some data from the ~internet~ inform the user while they wait
        static void SteamCommunity_OnLoadGame(SteamCommunity.Game obj)
        {
            CurrentGame = obj.Name;
        }

        delegate bool loadIsValidDel(string name, out string failReason, out bool outOfDate);
        static void acceptBtn_OnButtonPress(Panel sender)
        {
            string username = "";
            TextInput input = sender.Parent.GetChildByName("community_input") as TextInput;
            if (input)
            {
                username = input.TextLabel.Text;
            }
            IsLoadingData = true;
            loadIsValidDel loadFunc = new loadIsValidDel(SteamCommunity.IsValidName);
            string FailReason = "Username is blank!";
            bool OutOfDate = true;
            IAsyncResult item = loadFunc.BeginInvoke(username, out FailReason, out OutOfDate, null, sender);

            TaskManager.AddTask(item, (IAsyncResult res) =>
            {
                IsLoadingData = false;

                if (loadFunc.EndInvoke(out FailReason, out OutOfDate, res) && !string.IsNullOrEmpty(username))
                {
                    if (usernameWindow)
                    {
                        usernameWindow.Remove();
                        usernameWindow = null;
                    }

                    //If the local cache is out of date, pop open a dialogue asking if they'd like to update
                    if (OutOfDate && SteamCommunity.GetSaveExists(SteamCommunity.CommunityID))
                    {
                        ShowShouldUpdateCacheDialogue();
                    }
                    else BeginLoadData(SteamCommunity.CommunityID);
                }
                else
                {
                    Console.WriteLine("Failed to retrieve profile. {0}", FailReason);
                    Label progressText = sender.Parent.GetChildByName("progress_text") as Label;
                    if (progressText)
                    {
                        progressText.SetText(FailReason);
                    }
                }
            });
        }

        static void connectPanelBtn_OnButtonPress(Panel sender)
        {
            Utilities.Print("Setting to connect tab", Utilities.PrintCode.INFO);

            Panel savesPanel = sender.Parent;
            Panel connectPanel = sender.Parent.Parent.GetChildByName("panel_connect");
            if (!connectPanel) { Utilities.Print("Saves panel not found!", Utilities.PrintCode.WARNING); return; }

            savesPanel.Enabled = false;
            savesPanel.ShouldDrawChildren = false;

            connectPanel.Enabled = true;
            connectPanel.ShouldDrawChildren = true;
            connectPanel.Parent.SetHeight(115);
        }

        static void localSaveBtn_OnButtonPress(Panel sender)
        {
            Utilities.Print("Setting to saves tab", Utilities.PrintCode.INFO);

            Panel connectPanel = sender.Parent;
            Panel savesPanel = sender.Parent.Parent.GetChildByName("panel_saves");
            if (!savesPanel) { Utilities.Print("Saves panel not found!", Utilities.PrintCode.WARNING); return; }

            savesPanel.Parent.SetHeight(145);
            savesPanel.Enabled = true;
            savesPanel.ShouldDrawChildren = true;

            connectPanel.Enabled = false;
            connectPanel.ShouldDrawChildren = false;     
        }

        public static void GenericLoadCommunityID(string CommunityName, string CommunityID)
        {
            IsLoadingData = true;

            loadIsValidDel loadFunc = new loadIsValidDel(SteamCommunity.IsValidCommunityID);
            string FailReason = "Username is blank!";
            bool OutOfDate = true;
            IAsyncResult item = loadFunc.BeginInvoke(CommunityID, out FailReason, out OutOfDate, null, null);

            TaskManager.AddTask(item, (IAsyncResult res) =>
            {
                IsLoadingData = false;

                if (loadFunc.EndInvoke(out FailReason, out OutOfDate, res) && !string.IsNullOrEmpty(CommunityID))
                {
                    if (usernameWindow)
                    {
                        usernameWindow.Remove();
                        usernameWindow = null;
                    }

                    //If the local cache is out of date, pop open a dialogue asking if they'd like to update
                    if (OutOfDate && SteamCommunity.GetSaveExists(SteamCommunity.CommunityID))
                    {
                        ShowShouldUpdateCacheDialogue();
                    }
                    else BeginLoadData(SteamCommunity.CommunityID);
                }
                else
                {
                    Utilities.Print("Failed to load save information. {0}", Utilities.PrintCode.WARNING, FailReason);

                    //Try loading the data directly from a save anyway
                    BeginLoadData(CommunityID);
                }
            });
        }

        delegate List<SteamCommunity.Game> LoadSteamDataDel( string communityID, bool refresh=false );
        public static void BeginLoadData( string CommunityID, bool RefreshCache = false )
        {
            IsCreatingCache = RefreshCache ? RefreshCache : !SteamCommunity.GetLoadFromCache(CommunityID);

            Menu.HideToLeft();

            IsLoadingData = true;
            LoadSteamDataDel loadFunc = new LoadSteamDataDel(SteamCommunity.GetGames);
            IAsyncResult item = loadFunc.BeginInvoke(CommunityID, IsCreatingCache, null, null);

            TaskManager.AddTask(item, (IAsyncResult res) =>
                {
                    var Games = loadFunc.EndInvoke(res);
                    IsLoadingData = false;

                    EndSteamDataLoad(Games);
                } );
        }

        public static void ShowShouldUpdateCacheDialogue()
        {
            Window window = GUIManager.Create<Window>();
            window.SetTitle("Cache out of date");
            window.Resizable = false;
            window.SetWidth(380);
            window.SetHeight(115);
            window.SetEnableCloseButton(false);
            window.SetPos(Utilities.window.Width / 2 - window.Width / 2, Utilities.window.Height / 2 - window.Height / 2);
            window.MinimumSize = new Vector2(289, 115);

            Label questionText = GUIManager.Create<Label>(window);
            questionText.Autosize = true;
            questionText.SetText("Local cache is out of date. Would you like to update it?");
            questionText.SetPos(15, 10);

            Button noBtn = GUIManager.Create<Button>(window);
            noBtn.SetText("No");
            noBtn.SetWidth(120);
            noBtn.SetHeight(20);
            noBtn.SetPos(new Vector2(0, window.Height - (noBtn.Height + 20)));
            noBtn.AlignRight(20);
            noBtn.SetAnchorStyle(Panel.Anchors.Bottom | Panel.Anchors.Right);
            noBtn.OnButtonPress += (Panel sender) =>
            {
                if (IsLoadingData) return;

                BeginLoadData(SteamCommunity.CommunityID, false);
                window.Remove();
            };

            Button yesBtn = GUIManager.Create<Button>(window);
            yesBtn.SetText("Yes");
            yesBtn.SetWidth(noBtn.Position.X - 40);
            yesBtn.SetHeight(20);
            yesBtn.SetPos(new Vector2(20, noBtn.Position.Y));
            yesBtn.AlignLeft(20);
            yesBtn.SetAnchorStyle(Panel.Anchors.Bottom | Panel.Anchors.Right | Panel.Anchors.Left);
            yesBtn.OnButtonPress += (Panel sender) =>
            {
                if (IsLoadingData) return;

                BeginLoadData(SteamCommunity.CommunityID, true);
                window.Remove();
            };
        }

        public static void EndSteamDataLoad(List<SteamCommunity.Game> Games)
        {
            AllGames = Games;
            Console.WriteLine("GOT SOME GAMES FOR YA: {0}", AllGames != null ? AllGames.Count.ToString() : "NULL");
            //Create the spinner elements
            var FilteredGames = GetFilteredGamesList();
            Spinner.CreateElements(FilteredGames);

            //If we're just starting up, tell the lights to turn on
            StartupDelay = (float)Utilities.Time + 1.5f;

            Loaded = true;
        }

        static void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            if (Loaded && !IsLoadingData)
            {
                if (!Spinner.IsSpinning)
                {
                    //If we press space, let's spin da wheel
                    if (e.Key == OpenTK.Input.Key.Space)
                    {
                        if (Spinner.Games.Count > 0)
                        {
                            Spinner.Spin(0.095f);
                        }
                        else
                        {
                            Actor.SayLine("Woah there tiger, you don't have any games on the board!");
                        }
                        HintManager.RemoveHintNice("spin_controls_hint");
                    }

                    //If we press enter, start the currently selected game
                    if (e.Key == OpenTK.Input.Key.Enter || e.Key == OpenTK.Input.Key.KeypadEnter)
                    {
                        var Game = Spinner.GetCurrentGame();
                        if (Game == null || Game.AppID < 1) return;

                        System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo(string.Format("steam://run/{0}", Game.AppID ) );
                        System.Diagnostics.Process.Start(start);

                        //Have fun!
                        Utilities.window.Exit();
                    }

                    //Make the a-meow-nncer say the next/previous line of a stack of lines about a nerd game.
                    if (e.Key == OpenTK.Input.Key.Left || e.Key == OpenTK.Input.Key.A)
                        Actor.SayPreviousPage();
                    else if (e.Key == OpenTK.Input.Key.Right || e.Key == OpenTK.Input.Key.D)
                        Actor.SayNextPage();

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
        }

        private static void SetUpScene()
        {
            //Load the world
            WorldMesh = Resource.GetMesh("stage.obj");
            WorldMesh.mat = Resource.GetMaterial("models/stage");

            //Turn on the lights
            DirectionalLight light = new DirectionalLight();
            light.AmbientIntensity = 0.4f;
            light.DiffuseIntensity = 0.9f;
            light.Color = new Vector3(0.845f, 0.745f, 0.745f);  //new Vector3(0.745f, 0.820f, 0.847f); 
            light.Direction = new Vector3(0.0f, -1.0f, -1.0f);
            light.Direction.Normalize();
            LightingTechnique.SetEnvironmentLight(light);
            LightingTechnique.EnableEnvironmentLight(false);

            ent_pointlight pointlight = EntManager.Create<ent_pointlight>();
            pointlight.Spawn();
            pointlight.AmbientIntensity = 0.4f;
            pointlight.DiffuseIntensity = 20.0f; //0.85f
            pointlight.Color = new Vector3(1.0f, 0.5f, 0.00f);
            pointlight.Linear = 2.7f;
            pointlight.Enabled = false;

            spotlight = EntManager.Create<ent_spotlight>();
            spotlight.Spawn();

            spotlight.Color = new Vector3(1.0f, 1.0f, 1.0f);
            spotlight.Constant = 1.0f;
            spotlight.Cutoff = 20.0f;
            spotlight.SetAngle( new Angle(-42.2f, -110.9349f, 0) );
            spotlight.SetPos(new Vector3(7.56424f, 22.80356f, 33.25445f));
            spotlight.Enabled = false;

            Audio.Precache("Resources/Audio/light_on.wav");

            Spinner = EntManager.Create<ent_spinner>();
            Spinner.Spawn();
            Spinner.SetAngle(new Angle( 0, -90, 0 ));
            Spinner.SetPos(new Vector3(-2.8f, -(float)Spinner.Model.BBox.Negative.Y, 18));
            Spinner.OnSpinnerStop += new Action<SteamCommunity.Game>(Spinner_OnSpinnerStop);
            Spinner.OnSpin += new Action<bool>(Spinner_OnSpin);
            Spinner.OnWheelGrab += new Action(Spinner_OnWheelGrab);
            Spinner.Enabled = false;

            View.Player.SetPos(new Vector3(0.807714f, 10.01533f, 50.94458f));
            View.Player.SetAngle(new Angle(-3.26675f, -90.13329f, 0));
            //View.Player.SetPos(new Vector3(-13.50925f, 5.614059f, 2.610255f));
            //View.Player.SetAngle(new Angle(0, 0, 0));
            //Notable camera positions
            //Pos: -13.50925f, 5.614059f, 2.610255
            //Ang: 0.9982005f, -0.03713433f, 0.05996396f

            //Spawn the meow meow
            Actor = EntManager.Create<act_announcer>();
            Actor.Spawn();
            Actor.SetAnimation("announcer_idle_player");
            Actor.SetAngle(new Angle( 0, 0, 0));
            Actor.Scale = new Vector3(10, 10, 10);
            Actor.SetPos(new Vector3(Spinner.Position.X + 8, -0.6f, Spinner.Position.Z));
            Actor.ShouldDraw = false;

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
            //Menu.AddCheckBox("Show Favorites only", "game_favorites");
            Menu.AddCheckBox("Show Games with HDR", "game_hdr");
            Menu.AddCheckBox("Show Recently played games", "game_2weeks");
            Menu.AddCheckBox("Show Games never played", "game_never");
            Menu.AddCheckBox("Show Games with trading cards", "game_tradecards");
            Menu.AddCheckBox("Show Games with custom text", "game_meow_text");
            Menu.HideToLeft();

            Menu.OnAcceptPress += new Action(Menu_OnAcceptPress);
        }

        /// <summary>
        /// Event for when the spinner stops, so we can retrieve its information and DO STUFF
        /// </summary>
        /// <param name="obj">The chosen game</param>
        static void Spinner_OnSpinnerStop(SteamCommunity.Game game)
        {
            Actor.SetTransitionAnimation("announcer_wheel_to_player", "announcer_idle_player");
            Actor.SayLine(game.AppID);

            HintManager.AddHint("Press enter to start game!", 0, 4, "hint_startgame");

            Console.WriteLine(string.Format("Landed on \"{0}\" ({1})", game.Name, game.AppID));
        }

        static void Spinner_OnWheelGrab()
        {
            Actor.SetTransitionAnimation("announcer_player_to_wheel", "announcer_idle_wheel");
        }

        static void Spinner_OnSpin( bool WasMouseSpin )
        {
            if (!WasMouseSpin)
                Actor.SetTransitionAnimation("announcer_player_to_wheel", "announcer_idle_wheel");

            Actor.FadeLine();
            HintManager.RemoveHintNice("hint_startgame");
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

                if (Menu.GetCheckboxChecked("game_tradecards") && !game.HasTradingCards)
                    return false;

                if (Menu.GetCheckboxChecked("game_2weeks") && game.HoursLast2Weeks <= 0)
                    return false;

                if (Menu.GetCheckboxChecked("game_never") && game.HoursOnRecord > 0)
                    return false;

                if( Menu.GetCheckboxChecked("game_meow_text") && !Actor.HasDialogue( game.AppID ) )
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
            TaskManager.PollTasks();

            if (spotlight != null && Utilities.window.Keyboard[OpenTK.Input.Key.Q])
            {
                Console.WriteLine(View.Player.Position);
                Console.WriteLine(View.Angles);

                spotlight.SetAngle(View.Angles);
                spotlight.SetPos(View.Player.Position);
            }

            if (Utilities.Time > StartupDelay && !Started && Loaded)
            {
                HintManager.AddHint("Press space to spin!", 2.0f, 5.0f, "spin_controls_hint");
                Started = true;
                spotlight.Enabled = true;
                Spinner.Enabled = true;
                Audio.PlaySound("Resources/Audio/light_on.wav");
                Actor.ShouldDraw = true;
                LightingTechnique.EnableEnvironmentLight(true);
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
