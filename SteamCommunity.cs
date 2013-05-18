﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;

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

        public const string SteamURL = "http://steamcommunity.com/id/{0}/games?tab=all&xml=1";
        public const string StoreURL = "http://store.steampowered.com/api/appdetails/?appids={0}";
        public const string SavesFolder = "Saves/";

        public static string CommunityID { get; private set; }
        public static string SteamName { get; private set; }
        public static List<Game> Games = new List<Game>();

        public static bool IsValidName(string communityName, out string reason )
        {
            bool valid = true;
            reason = "No reason specified";
            try
            {
                string url = string.Format(SteamURL, communityName);
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.DtdProcessing = DtdProcessing.Ignore;
                using (XmlReader reader = XmlReader.Create(url, settings))
                {
                    
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            //Do some preliminary parsing before we start checking out some steam gaemz
                            switch (reader.Name)
                            {
                                case "error":
                                    reader.Read();
                                    reason = reader.Value;
                                    valid = false;
                                    break;

                                case "steamID64":
                                    reader.Read();
                                    CommunityID = reader.Value;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { valid = false; reason = e.Message; }

            return valid;
        }

        public static List<Game> GetGamesFromCommunity(string communityname)
        {
            Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
            Games.Clear();
            string url = string.Format(SteamURL, communityname);
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

            SaveGames(Games);

            return Games;
        }

        public static List<Game> GetGames(string communityname, string communityid)
        {
            if (File.Exists(SavesFolder + communityid))
            {
                Console.WriteLine("Loading data from file!");
                string json = File.ReadAllText(SavesFolder + communityid);
                return JsonConvert.DeserializeObject<List<Game>>(json);
            }
            else
            {
                Console.WriteLine("Loading data from internet!");
                return GetGamesFromCommunity(communityname);
            }
        }

        private static void SaveGames( List<Game> Games)
        {
            string json = JsonConvert.SerializeObject(Games);

            if (!Directory.Exists(SavesFolder)) Directory.CreateDirectory(SavesFolder);
            File.WriteAllText(SavesFolder + CommunityID, json);
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
