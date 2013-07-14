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
            public bool HasTradingCards;

            public static Game Default = new Game()
            {
                AppID = -1,
                Name = "NO GAME",
            };
        }

        public enum CommunityVisibilityState
        {
            Unknown = 0,
            Private,
            FriendsOnly,
            FriendsOfFriends,
            UsersOnly,
            Public
        }

        /// <summary>
        /// Called when a singular game is loaded from the community
        /// </summary>
        public static event Action<Game> OnLoadGame;

        public const string SteamAPIKey = "45A026565B2587A7EA3419B570E0D951"; //probably shouldn't be public but oh well
        public const string StoreURL = "http://store.steampowered.com/api/appdetails/?appids={0}";
        public const string ResolveVanityURL = "http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}&format=json";
        public const string ProfileGameInfoURL = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&include_appinfo=1&format=json";
        public const string PlayerSummaryURL = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0001/?key={0}&steamids={1}&format=json";
        public const string SavesFolder = "Saves/";

        public static string CommunityID { get; private set; }
        public static string SteamName { get; private set; }
        public static List<Game> Games = new List<Game>();
        public static bool IsOnline { get; private set; }

        private const string steamIDToken = "steamcommunity.com/id/";
        private const string steamProfileToken = "steamcommunity.com/profiles/";

        /// <summary>
        /// Given some vague information, determine the community id if it's valid
        /// The information can be the the community id, custom url name, steam profile, or steam profile id
        /// </summary>
        /// <param name="vagueNamingInfo">CommunityID, custom url name, steam profile, or steam profile id</param>
        /// <param name="reason">If it did not succeed, this is the reason why</param>
        /// <param name="outOfDate">If true, the local cache is out of date</param>
        /// <returns>If the operation was successful</returns>
        public static bool IsValidName(string vagueNamingInfo, out string reason, out bool outOfDate )
        {
            outOfDate = true;
            reason = "The specified profile could not be found.";

            //First, get their steam community id
            string id;
            if (!getCommunityID(vagueNamingInfo, out id))
            {
                return false;
            }
            CommunityID = id;
            IsOnline = true;

            //Once we have their id, get some more information about their profile
            return getProfileInformation(CommunityID, out reason, out outOfDate);
        }

        public static bool IsValidCommunityID(string communityID, out string reason, out bool outOfDate)
        {
            outOfDate = true;
            reason = "No reason specified";
            CommunityID = communityID;
            
            //This time we're already given an ID, now just to determine additional info
            return getProfileInformation(communityID, out reason, out outOfDate);
        }

        private static Regex digitsOnly = new Regex(@"[^\d]");
        private static Regex alphanumericOnly = new Regex(@"[^a-zA-Z0-9 -]");  
        private static bool getCommunityID(string vagueinformation, out string steamID)
        {
            steamID = "INVALID";

            //If we're given their custom url thing
            if (vagueinformation.Contains(steamIDToken))
            {
                vagueinformation = vagueinformation.Remove(0, vagueinformation.IndexOf(steamIDToken) + steamIDToken.Length);
                vagueinformation = alphanumericOnly.Replace(vagueinformation, "");
                getCommunityIDFromCustomURL(vagueinformation, out steamID);
            }
            //We're given their profile url
            else if (vagueinformation.Contains(steamProfileToken))
            {
                vagueinformation = vagueinformation.Remove(0, vagueinformation.IndexOf(steamProfileToken) + steamProfileToken.Length);
                vagueinformation = digitsOnly.Replace(vagueinformation, "");
                steamID = vagueinformation;
            }
            //the fuck did they give us
            else
            {
                //Try parsing it as a 64 bit number, if it fails it might be their community id
                Int64 lCommunityID = -1;

                if (Int64.TryParse(vagueinformation, out lCommunityID))
                {
                    steamID = lCommunityID.ToString(); //Yay!
                }
                else //That didn't work. Maybe it's their custom url profile name
                {
                    getCommunityIDFromCustomURL(vagueinformation, out steamID);
                }
            }

            //Do some checks to make sure it's valid
            long res;
            return (!string.IsNullOrEmpty(steamID) && Int64.TryParse(steamID, out res));
        }

        private static bool getProfileInformation(string communityID, out string reason, out bool outOfDate)
        {
            IsOnline = false;
            outOfDate = true;
            reason = "No reason specified";
            bool valid = true;

            //First, get their name and a summary of their profile
            Newtonsoft.Json.Linq.JObject jsonProfile = null;
            try
            {
                WebClient wc = new WebClient();
                string jsonString = wc.DownloadString(string.Format(PlayerSummaryURL, SteamAPIKey, communityID));
                jsonProfile = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                reason = e.Message;
                valid = false;

                return false;
            }

            string strState = ParseSafe(jsonProfile["response"]["players"]["player"][0]["communityvisibilitystate"]);
            CommunityVisibilityState visState = (CommunityVisibilityState)ParseIntSafe(ParseSafe(strState));

            if (visState == CommunityVisibilityState.Unknown || visState == CommunityVisibilityState.Private || visState == CommunityVisibilityState.FriendsOnly)
            {
                reason = "User profile is not public!";
                valid = false;
            }

            if (ParseIntSafe(ParseSafe(jsonProfile["response"]["players"]["player"][0]["profilestate"])) != 1)
            {
                reason = "User has not set up their profile!";
                valid = false;
            }

            SteamName = ParseSafe(jsonProfile["response"]["players"]["player"][0]["personaname"]);

            //Given the communityid, let's get some DIRT on them
            try
            {
                WebClient wc = new WebClient();
                string jsonString = wc.DownloadString(string.Format(ProfileGameInfoURL, SteamAPIKey, communityID));
                jsonProfile = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get additional information! {1}", e.Message);
                reason = e.Message;
                valid = false;
            }

            //We're definitely online if things have been valid so far
            IsOnline = valid;

            //Get their game count to see if our local cache is out of date
            int GameCount = ParseIntSafe(ParseSafe(jsonProfile["response"]["game_count"]));
            outOfDate = GameCount != GetLocalSaveGameCount(CommunityID);

            return valid;
        }

        /// <summary>
        /// Given someone's custom url, determine the community id
        /// </summary>
        /// <param name="customurl">The custom URL for someone's steam profile (eg. http://steamcommunity.com/id/CUSTOMURL/) </param>
        /// <param name="ID">The determined community id</param>
        /// <returns>Whether the operation was successful</returns>
        private static bool getCommunityIDFromCustomURL(string customurl, out string ID)
        {
            ID = null;

            Newtonsoft.Json.Linq.JObject jsonProfile = null;
            try
            {
                WebClient wc = new WebClient();
                string jsonString = wc.DownloadString(string.Format(ResolveVanityURL, SteamAPIKey, customurl));
                jsonProfile = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get community ID! {0}", e.Message);
                return false;
            }

            //If we're not valid by this point then turn back
            if (ParseIntSafe(ParseSafe(jsonProfile["response"]["success"])) != 1)
            {
                return false;
            }

            //Extract the id from the json we were given
            ID = ParseSafe(jsonProfile["response"]["steamid"]);

            //Yay!
            return true;
        }


        /*
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

        private static bool getProfileInfo(string customurl, out string reason, out bool outOfDate)
        {
            IsOnline = false;
            outOfDate = true;
            reason = "No reason specified";
            bool valid = true;

            string id;
            //Given the information we have, try to determine the steamid
            if (!getCommunityID(customurl, out id))
            {
                reason = "No matching community URL";
            }
            CommunityID = id;

            Newtonsoft.Json.Linq.JObject jsonProfile = null;
            try
            {
                WebClient wc = new WebClient();
                string jsonString = wc.DownloadString(string.Format(ResolveVanityURL, SteamAPIKey, customurl));
                jsonProfile = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get games list! {0}", e.Message);
                return false;
            }

            //Check if the request was successful and we got an actual name
            valid = ParseIntSafe(ParseSafe(jsonProfile["response"]["success"])) == 1;

            //If we're not valid by this point then turn back
            if (!valid)
            {
                reason = "No matching community URL";
                return valid;
            }
            //Store some information it gave us
            CommunityID = ParseSafe(jsonProfile["response"]["steamid"]);
            IsOnline = true;

            //Now do another request, getting the total number of games they have to see if our local cache is out of date
            try
            {
                WebClient wc = new WebClient();
                string jsonString = wc.DownloadString(string.Format(ProfileGameInfoURL, SteamAPIKey, CommunityID));
                jsonProfile = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get games count! {1}", e.Message);
                valid = false;
            }

            int GameCount = ParseIntSafe(ParseSafe(jsonProfile["response"]["games_count"]));
            outOfDate = GameCount != GetLocalSaveGameCount(CommunityID);

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
        */

        public static List<Game> GetGamesFromCommunity(string communityID)
        {
            string jsonString = "";
            Newtonsoft.Json.Linq.JObject jsonGames = null;
            try
            {
                WebClient wc = new WebClient();
                jsonString = wc.DownloadString(string.Format(ProfileGameInfoURL, SteamAPIKey, communityID));
                jsonGames = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get games list! {0}", e.Message);
                return null;
            }

            //Clear the games list
            Games.Clear();

            try
            {
                var GamesInfo = jsonGames["response"]["games"];
                foreach (var game in GamesInfo)
                {
                    Game g = new Game();
                    g.AppID = ParseIntSafe(ParseSafe(game["appid"]));
                    g.Name = ParseSafe(game["name"]);
                    g.HoursLast2Weeks = ParseIntSafe(ParseSafe(game["playtime_2weeks"]));
                    g.HoursOnRecord = ParseIntSafe(ParseSafe(game["playtime_forever"]));

                    Games.Add(g);
                }
            }
            catch (Exception) { Console.WriteLine("Failed to parse game data!"); }

            foreach (Game g in Games)
            {
                GetAdditionalInfo(g);
            }

            if (IsOnline)
                SaveGames(Games);

            return Games;
        }

        private static string ParseSafe(Newtonsoft.Json.Linq.JToken obj)
        {
            if (obj == null) return "";
            return obj.ToString();
        }

        private static int ParseIntSafe(string json)
        {
            int def = 0;
            int.TryParse(json, out def );
            return def;
        }

        private static bool ParseBoolSafe(string json)
        {
            bool def = false;
            bool.TryParse(json, out def);
            return def;
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
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename)) return -1;

            Console.Write("Retrieving cached game count...");
            string json = File.ReadAllText(filename);
            int count = JsonConvert.DeserializeObject<List<Game>>(json).Count;
            Console.WriteLine(count);
            return count;
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

        private static Regex matchSaves = new Regex(@"(\d{17} )");
        public static Dictionary<string, string> GetLocalSaves()
        {
            Dictionary<string, string> Saves = new Dictionary<string, string>();

            try
            {
                if (!Directory.Exists(SavesFolder)) Directory.CreateDirectory(SavesFolder);

                string[] files = Directory.GetFiles(SavesFolder, "* *");
                foreach (string file in files)
                {
                    if (matchSaves.IsMatch(file))
                    {
                        string filename = Path.GetFileName(file);
                        string communityid = filename.Substring(0, 17);
                        string communityname = filename.Substring(18);
                        Saves.Add(communityid, communityname);
                    }
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

            //If a save exists with the same communityid but different name, delete it
            var saves = GetLocalSaves();
            foreach (var save in saves)
            {
                if (save.Key == CommunityID && save.Value != SteamName)
                {
                    try
                    {
                        File.Delete(string.Format("{0}{1} {2}", SavesFolder, save.Key, save.Value));
                    }
                    catch (Exception) { }
                }
            }

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

                            case "29":
                                game.HasTradingCards = true;
                                break;
                        }
                    }
                }
                Console.WriteLine( dataObj["name"]);
            }
        }
    }
}
