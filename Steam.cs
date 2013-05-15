using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Steam4NET;

using OlegEngine;

namespace WheelOfSteamGames
{
    class Steam
    {
        public static ISteam006 steam { get; private set; }

        public static ISteamClient009 SteamClient { get; private set; }
        public static IClientEngine ClientEngine { get; private set; }

        public static int HSteamPipe { get; private set; }
        public static int HSteamUser { get; private set; }

        public static IClientUser ClientUser { get; private set; }
        public static IClientApps ClientApps { get; private set; }

        public delegate bool ReturnCriteria(TSteamApp steamApp);

        public struct App
        {
            public string Name { get; set; }
            public uint AppID { get; set; }

            public string Icon { get; set; }
        }

        public static void Initialize()
        {
            try
            {
                InitSteam2();
                InitSteam3();
                PostInitSteam2();
            }
            catch (Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            CallbackDispatcher.SpawnDispatchThread(HSteamPipe);
        }

        public static string GetAppData(uint appId, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            ClientApps.GetAppData(appId, key, sb);

            return sb.ToString();
        }

        public static bool GetAppInternal(uint appId, out TSteamApp app)
        {
            TSteamError error = new TSteamError();
            app = new TSteamApp()
            {
                uMaxNameChars = 255,
                uMaxCurrentVersionLabelChars = 255,
                uMaxLatestVersionLabelChars = 255,
                uMaxInstallDirNameChars = 255,

                szName = new string('\0', 255),
                szCurrentVersionLabel = new string('\0', 255),
                szLatestVersionLabel = new string('\0', 255),
                szInstallDirName = new string('\0', 255),
            };

            if (steam.EnumerateApp(appId, ref app, ref error) == 0)
            {
                Console.WriteLine("ISteam006::EnumerateApp( {0} ) Failed: {1}", appId, error.szDesc);
                return false;
            }

            return true;
        }

        public static bool GetAppIds(out uint[] appIds)
        {
            TSteamError error = new TSteamError();
            TSteamAppStats stats = new TSteamAppStats();

            appIds = null;

            if (steam.GetAppStats(ref stats, ref error) == 0)
            {
                Console.WriteLine("ISteam006::GetAppStats() Failed: {0}", error.szDesc);
                return false;
            }

            appIds = new uint[stats.uNumApps];

            if (steam.GetAppIds(ref appIds, (uint)appIds.Length, ref error) == 0)
            {
                Console.WriteLine("ISteam006::GetAppIds( {0} ) Failed: {1}", appIds.Length, error.szDesc);
                return false;
            }

            return true;
        }

        public static bool IsAppSubscribed(uint appId)
        {
            TSteamError error = new TSteamError();

            int bSubscribed = 0;
            int reserved = 0;

            if (steam.IsAppSubscribed(appId, ref bSubscribed, ref reserved, ref error) == 0)
            {
                Console.WriteLine("ISteam006::IsAppSubscribed( {0} ) Failed: {1} ", appId, error.szDesc);
                return false;
            }

            return bSubscribed == 1;
        }

        public static App[] GetSubscribedApps(ReturnCriteria rc)
        {
            List<App> appList = new List<App>();

            uint[] appIDs;

            if (!GetAppIds(out appIDs))
                return null;

            foreach (uint appID in appIDs)
            {

                if (!IsAppSubscribed(appID))
                    continue;

                TSteamApp steamApp;

                if (!GetAppInternal(appID, out steamApp))
                    continue;

                if (rc != null)
                {
                    if (!rc(steamApp))
                        continue;
                }

                string icon = GetAppData(appID, "logo");

                App stmApp = new App()
                {
                    Name = steamApp.szName,
                    AppID = appID,

                    Icon = icon,
                };

                appList.Add(stmApp);
            }

            return appList.ToArray();
        }

        public static App[] RefreshGames()
        {

            App[] apps = Steam.GetSubscribedApps((app) =>
                {

                    // hide anything that isn't an installable game
                    if (Steam.GetAppData(app.uId, "gamedir") == "")
                        return false;

                    string state = Steam.GetAppData(app.uId, "state");

                    // hide sdks
                    if (state == "eStateTool")
                        return false;

                    // hide unavailable
                    if (state == "eStateUnAvailable")
                        return false;

                    // hide demos
                    if (Steam.GetAppData(app.uId, "DemoOfAppID") != "")
                        return false;

                    // hide dlc
                    if (Steam.GetAppData(app.uId, "DLCForAppID") != "")
                        return false;

                    // hide movies
                    if (Steam.GetAppData(app.uId, "IsMediaFile") == "1")
                        return false;

                    return true;

                });

            return apps;
        }

        static void InitSteam2()
        {
            Console.WriteLine("Initializing Steam2...");

            if (!Steamworks.LoadSteam())
                throw new InvalidOperationException("Unable to load steam.dll");

            steam = Steamworks.CreateSteamInterface<ISteam006>();

            if (steam == null)
                throw new InvalidOperationException("Unable to get ISteam006.");

            TSteamError error = new TSteamError();

            if (steam.Startup(0, ref error) == 0)
                throw new InvalidOperationException("Unable to startup steam interface: " + error.szDesc);

            Console.WriteLine("Steam2 startup success." + Environment.NewLine);
        }
        static void InitSteam3()
        {
            Console.WriteLine("Initializing Steam3...");

            if (!Steamworks.LoadSteamClient())
                throw new InvalidOperationException("Unable to load steamclient.dll");

            SteamClient = Steamworks.CreateInterface<ISteamClient009>();
            ClientEngine = Steamworks.CreateInterface<IClientEngine>();

            if (SteamClient == null || ClientEngine == null)
                throw new InvalidOperationException("Unable to get required steamclient interfaces.");

            HSteamPipe = SteamClient.CreateSteamPipe();
            HSteamUser = SteamClient.ConnectToGlobalUser(HSteamPipe);

            if (HSteamUser == 0 || HSteamPipe == 0)
                throw new InvalidOperationException("Unable to connect to global user.");

            ClientApps = ClientEngine.GetIClientApps<IClientApps>(HSteamUser, HSteamPipe);
            ClientUser = ClientEngine.GetIClientUser<IClientUser>(HSteamUser, HSteamPipe);

            if (ClientApps == null || ClientUser == null)
                throw new InvalidOperationException("Unable to get required interfaces.");

            Console.WriteLine("Steam3 startup success." + Environment.NewLine);
        }
        static void PostInitSteam2()
        {
            Console.WriteLine("Getting account name...");
            StringBuilder accName = new StringBuilder(255);

            if (!ClientUser.GetAccountName(accName))
                throw new InvalidOperationException("Unable to startup steam interface.");

            Console.WriteLine("Account = \"{0}\"", accName.ToString());

            TSteamError error = new TSteamError();

            Console.WriteLine("ISteam006::SetUser( \"{0}\" )...", accName.ToString());
            int bUserSet = 0;
            uint setUserHandle = steam.SetUser(accName.ToString(), ref bUserSet, ref error);
            Console.WriteLine("SetUserHandle = {0}", setUserHandle);

            if (setUserHandle == 0)
                throw new InvalidOperationException("Unable to get SetUser call handle.");

            Console.WriteLine("ISteam006::BlockingCall( {0} )...", setUserHandle);
            if (steam.BlockingCall(setUserHandle, 100, ref error) == 0)
                throw new InvalidOperationException("Unable to process SetUser call: " + error.szDesc);

            Console.WriteLine("User set!" + Environment.NewLine);
        }
    }
}
