using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;

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
        }

        public static string SteamURL = "http://steamcommunity.com/id/{0}/games?tab=all&xml=1";
        public static string StoreURL = "http://store.steampowered.com/api/appdetails/?appids={0}";
        public static string CommunityID { get; private set; }
        public static string SteamName { get; private set; }
        public static List<Game> Games = new List<Game>();
        public static void Initialize()
        {

        }

        public static List<Game> GetGamesFromCommunity(string communityname)
        {
            Games.Clear();
            string url = string.Format("http://steamcommunity.com/id/{0}/games?tab=all&xml=1", communityname );
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

            return Games;
        }

        private static void ParsegamesList(XmlReader reader)
        {
            Game CurrentGame = new Game();
            bool First = true;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("TODO: STORE THE HELL OUT OF THIS INFO BECAUSE IT TAKES FOREVER AND A HALF");
            Console.WriteLine("ALSO DO SOME GUI MAGIC FOR SELECTING USER BECAUSE THAT SHIT ISN'T AUTOMATIC YET");
            Console.ResetColor();
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

            WebClient wc = new WebClient();
            string jsonString = "";

            try
            {
                jsonString = wc.DownloadString(string.Format(StoreURL, game.AppID));
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get game info for {0}: {1}", game.Name, e.Message);
            }

            Newtonsoft.Json.Linq.JObject infObj = Newtonsoft.Json.Linq.JObject.Parse(jsonString);

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
                Console.WriteLine("{0}\n\tSingleplayer: {1}\n\tMultiplayer: {2}\n\tRequired age: {3}", dataObj["name"], game.IsMultiplayer, game.IsSingleplayer, dataObj["required_age"]);
            }
        }
    }
}
