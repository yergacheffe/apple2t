using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using TweetSharp.Twitter.Fluent;
using TweetSharp.Model;
using TweetSharp.Extensions;
using System.IO;

namespace TweetWall
{
    public class WallItem
    {
        public WallItem(string from, string message, string imageUri)
        {
            From = from;
            Message = message;
            ImageUri = imageUri;
        }

        public WallItem(string from, string message, string imageUri, DateTime timestamp)
        {
            From = from;
            Message = message;
            ImageUri = imageUri;
            TimeStamp = timestamp;
        }

        public DateTime TimeStamp { get; set; }
        public string ImageUri { get; set; }
        public string From { get; set; }
        public string Message { get; set; }
    }

    public class TwitterProvider
    {
        // 
        public static List<WallItem> GetDirectMessages()
        {
            int attempts = 3;

            do
            {
                try
                {
                    var req = FluentTwitter.CreateRequest().AuthenticateAs("user", "password").Statuses().Mentions().Take(60).AsXml();
                    var data = req.Request();
                    XDocument doc = XDocument.Parse(data.Response);

                    // <created_at>Sat May 22 02:31:05 +0000 2010</created_at>
                    var wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value,
                                   e.Element("text").Value,
                                   e.Element("user").Element("profile_image_url").Value,
                                   DateTime.ParseExact(e.Element("created_at").Value, "ddd MMM dd HH:mm:ss %zzzz yyyy", null));

                    return wall.ToList();
                }
                catch (Exception e)
                {
                    --attempts;
                    System.Threading.Thread.Sleep(500);
                }
            } while (attempts > 0);

            return null;

        }

        public static List<WallItem> GetCachedItems()
        {
            List<string> users = new List<string>() { "makershed", "interactmatter", "dianaeng", "mythbusters", "adafruit", "make", "hackaday", "emsl", "sockington", 
                "nycresistor", "badbanana", "makershed", "techshopinc", "greenspeak", "kin", "noisebridge", "pilz", "josephflaherty",
                "hackerdojo", "dudecraft", "ptorrone", "makerbot", "johnedgarpark", "techshopjim", "ericskiff", "donttrythis",
                "antrod", "1lenore", "becausewecan" };

            var tweetWallItems = new List<WallItem>();
            foreach (string user in users)
            {
                try
                {
                    // Try to pull from cache
                    StreamReader cache = new StreamReader(string.Format("{0}-lasttweet.txt", user));
                    string data = cache.ReadToEnd();
                    cache.Close();
                    cache.Dispose();

                    XDocument doc = XDocument.Parse(data);

                    var wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value, e.Element("text").Value, e.Element("user").Element("profile_image_url").Value);
                    var items = wall.ToList();
                    if (items.Count > 0)
                    {
                        tweetWallItems.Add(items[0]);
                    }
                }
                catch (Exception e)
                {
                }
            }

            return tweetWallItems;
        }

        public static List<WallItem> GetItemsFromUser(string user)
        {
            int attempts = 3;

            do
            {
                try
                {
                    var req = FluentTwitter.CreateRequest().AuthenticateAs("user", "password").Statuses().OnUserTimeline().For(user).AsXml();
                    var data = req.Request().Response;

                    XDocument doc = XDocument.Parse(data);

                    var wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value, e.Element("text").Value, e.Element("user").Element("profile_image_url").Value);
                    List<WallItem> items = wall.ToList();

                    if (items.Count > 0)
                    {
                        StreamWriter cache = new StreamWriter(string.Format("{0}-lasttweet.txt", user));
                        cache.Write(data);
                        cache.Close();
                        cache.Dispose();
                    }
                    else
                    {
                        // Try to pull from cache
                        StreamReader cache = new StreamReader(string.Format("{0}-lasttweet.txt", user));
                        data = cache.ReadToEnd();
                        cache.Close();
                        cache.Dispose();

                        doc = XDocument.Parse(data);

                        wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value, e.Element("text").Value, e.Element("user").Element("profile_image_url").Value);
                        items = wall.ToList();
                    }

                    return items;
                }
                catch (Exception e)
                {
                    --attempts;
                    System.Threading.Thread.Sleep(500);
                }
            } while (attempts > 0);

            return null;
        }

        public static List<WallItem> GetLatestItems()
        {
            int attempts = 3;

            do
            {
                try
                {
                    var req = FluentTwitter.CreateRequest().AuthenticateAs("user", "password").Statuses().OnFriendsTimeline().AsXml();
                    var data = req.Request().Response;

                    XDocument doc = XDocument.Parse(data);

                    var wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value, e.Element("text").Value, e.Element("user").Element("profile_image_url").Value);

                    return wall.ToList();
                }
                catch (Exception e)
                {
                    --attempts;
                    System.Threading.Thread.Sleep(500);
                }
            } while (attempts > 0);

            return null;
        }
    }
}
