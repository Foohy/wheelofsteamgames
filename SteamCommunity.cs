using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
namespace WheelOfSteamGames
{
    class SteamCommunity
    {
        public class Game
        {
            public int AppID;
            public float HoursLast2Weeks;
            public float HoursOnRecord;
            public string StatsLink;
            public string GlobalStatsLink;
            public string Name;
            public string Logo;
            public string storeLink;

            public bool IsSingleplayer;
            public bool IsMultiplayer;
            public bool HasVAC;
            public bool HasAchievements;
            public bool HasLevelEditor;
            public bool HasHDR;

            public static Game Default = new Game()
            {
                AppID = -1,
                Name = "NO GAME",
            };
        }

        /// <summary>
        /// Called when a singular game is loaded from the community
        /// </summary>
        public static event Action<Game> OnLoadGame;

        public const string SteamURL = "http://steamcommunity.com/id/{0}/games?tab=all&xml=1";
        public const string ProfileURL = "http://steamcommunity.com/profiles/{0}/games?tab=all&xml=1";
        public const string StoreURL = "http://store.steampowered.com/api/appdetails/?appids={0}";
        public const string SavesFolder = "Saves/";

        public static string CommunityID { get; private set; }
        public static string SteamName { get; private set; }
        public static List<Game> Games = new List<Game>();
        public static bool IsOnline { get; private set; }

        public static bool IsValidName(string communityName, out string reason, out bool outOfDate )
        {
            outOfDate = true;
            reason = "No reason specified";
            string url = string.Format(SteamURL, communityName);

            return getValidProfile(url, out reason, out outOfDate);
        }

        public static bool IsValidCommunityID(string communityID, out string reason, out bool outOfDate)
        {
            outOfDate = true;
            reason = "No reason specified";
            string url = string.Format(ProfileURL, communityID);

            return getValidProfile(url, out reason, out outOfDate);
        }

        private static bool getValidProfile(string url, out string reason, out bool outOfDate)
        {
            IsOnline = false;
            outOfDate = true;
            reason = "No reason specified";
            bool valid = true;

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.DtdProcessing = DtdProcessing.Ignore;
                using (XmlReader reader = XmlReader.Create(url, settings))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(reader);

                    XmlNode errornode = doc.SelectSingleNode("response/error");
                    XmlNode successnode = doc.SelectSingleNode("gamesList/steamID64");

                    if (errornode != null)
                    {
                        reason = errornode.InnerText;
                        valid = false;
                    }
                    else if (successnode != null)
                    {
                        CommunityID = successnode.InnerText;
                        IsOnline = true;
                        valid = true;

                        XmlNode gamesnode = doc.SelectSingleNode("gamesList/games");
                        outOfDate = gamesnode.ChildNodes.Count != GetLocalSaveGameCount(CommunityID);

                        SteamName = doc.SelectSingleNode("gamesList/steamID").InnerText;
                    }
                    else //some other mystery problem
                    {
                        valid = false;
                    }
                }
            }
            catch (Exception e) { valid = false; reason = e.Message; }

            return valid;
        }

        public static List<Game> GetGamesFromCommunity(string communityID)
        {
            Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
            Games.Clear();
            string url = string.Format(ProfileURL, communityID);
            using (XmlReader reader = XmlReader.Create(url))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        //Do some preliminary parsing before we start checking out some steam gaemz
                        switch (reader.Name)
                        {
                            case "steamID64":
                                reader.Read();
                                CommunityID = reader.Value;
                                break;

                            case "steamID":
                                reader.Read();
                                SteamName = reader.Value;
                                break;

                            case "games":
                                ParsegamesList(reader);
                                break;

                            default:
                                Console.WriteLine("Unknown element: '{0}' with value '{1}'", reader.Name, reader.Value);
                                break;
                        }
                    }
                }
            }

            foreach( Game g in Games )
            {
                GetAdditionalInfo(g);
            }

            if (IsOnline)
                SaveGames(Games);

            return Games;
        }

        public static bool GetLoadFromCache(string communityid)
        {
            string filename = GetSave(communityid);
            return !string.IsNullOrEmpty(filename) && File.Exists(filename);
        }

        public static List<Game> GetGames(string communityid, bool RefreshCache=false )
        {
            if (RefreshCache || !GetLoadFromCache(communityid))
            {
                Console.WriteLine("Loading data from internet!");
                return GetGamesFromCommunity(communityid);
            }
            else
            {
                string filename = GetSave(communityid);

                Console.WriteLine("Loading data from file!");
                string json = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<List<Game>>(json);
            }
        }

        public static int GetLocalSaveGameCount(string communityID)
        {
            string filename = GetSave(communityID);
            if (string.IsNullOrEmpty( filename)) return -1;

            Console.WriteLine("Retrieving cached game count...");
            string json = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<List<Game>>(json).Count;
        }

        public static bool GetSaveExists(string communityID)
        {
            return !(string.IsNullOrEmpty(GetSave(communityID)));
        }

        private static string GetSave(string communityID)
        {
            var localSaves = GetLocalSaves();

            foreach (var save in localSaves)
            {
                if (save.Key == communityID)
                {
                    return string.Format("{0}{1} {2}", SavesFolder, save.Key, save.Value);
                }
            }

            return null;
        }

        public static Dictionary<string, string> GetLocalSaves()
        {
            Dictionary<string, string> Saves = new Dictionary<string, string>();

            try
            {
                if (!Directory.Exists(SavesFolder)) Directory.CreateDirectory(SavesFolder);

                string[] files = Directory.GetFiles(SavesFolder, "* *");
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                    string communityid = filename.Substring(0, 17);
                    string communityname = filename.Substring(18);
                    Saves.Add(communityid, communityname);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get local saves! {0}", e.Message);
            }

            return Saves;   
        }

        private static void SaveGames( List<Game> Games)
        {
            string json = JsonConvert.SerializeObject(Games);

            if (!Directory.Exists(SavesFolder)) Directory.CreateDirectory(SavesFolder);
            File.WriteAllText( string.Format("{0}{1} {2}",SavesFolder, CommunityID, SteamName), json);
        }

        private static void ParsegamesList(XmlReader reader)
        {
            Game CurrentGame = new Game();
            bool First = true;
            while (reader.Read())
            {
                if (!reader.IsStartElement()) continue;


                switch (reader.Name)
                {
                    case "game":
                        if (!First)
                        {
                            Games.Add(CurrentGame);
                        }
                        else First = false;
                        CurrentGame = new Game();
                        break;

                    case "appID":
                        reader.Read();
                        CurrentGame.AppID = Convert.ToInt32(reader.Value);
                        break;

                    case "name":
                        reader.Read();
                        CurrentGame.Name = reader.Value;
                        break;

                    case "logo":
                        reader.Read();
                        CurrentGame.Logo = reader.Value;
                        break;

                    case "storeLink":
                        reader.Read();
                        CurrentGame.storeLink = reader.Value;
                        break;

                    case "hoursLast2Weeks":
                        reader.Read();
                        CurrentGame.HoursLast2Weeks = Convert.ToSingle(reader.Value);
                        break;

                    case "hoursOnRecord":
                        reader.Read();
                        CurrentGame.HoursOnRecord = Convert.ToSingle(reader.Value);
                        break;

                    case "statsLink":
                        reader.Read();
                        CurrentGame.StatsLink = reader.Value;
                        break;

                    case "globalStatsLink":
                        reader.Read();
                        CurrentGame.GlobalStatsLink = reader.Value;
                        break;  
                }
            }

            Games.Add(CurrentGame); //Add the last game
        }

        private static void GetAdditionalInfo( Game game )
        {
            if (OnLoadGame != null) OnLoadGame(game);
            string jsonString = "";
            Newtonsoft.Json.Linq.JObject infObj = null;
            try
            {
                WebClient wc = new WebClient();
                jsonString = wc.DownloadString(string.Format(StoreURL, game.AppID));
                infObj = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get game info for {0}: {1}", game.Name, e.Message);
                return;
            }

            

            bool successful = infObj[game.AppID.ToString()]["success"].ToString() == bool.TrueString;

            if (successful)
            {
                Newtonsoft.Json.Linq.JToken dataObj = infObj[game.AppID.ToString()]["data"];
                if (dataObj["categories"] != null)
                {
                    foreach (var item in dataObj["categories"])
                    {
                        switch (item["id"].ToString())
                        {
                            case "1":
                                game.IsMultiplayer = true;
                                break;

                            case "2":
                                game.IsSingleplayer = true;
                                break;

                            case "8":
                                game.HasVAC = true;
                                break;

                            case "12":
                                game.HasHDR = true;
                                break;

                            case "17":
                                game.HasLevelEditor = true;
                                break;

                            case "22":
                                game.HasAchievements = true;
                                break;
                        }
                    }
                }
                Console.WriteLine( dataObj["name"]);
            }
        }
    }
}
