// Copyright (c) 2010 Chris Yerga
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
    /// <summary>
    /// Data source provider for Twitter data. Some of the methods require you
    /// to authenticate with Twitter. For testing the unauthenticated methods
    /// will work fine, but you'll eventually want your own personal timeline.
    /// <para>
    /// To get authentication working, you'll need to do the following steps:
    /// 
    ///   1. Register a new application at http://dev.twitter.com/apps/new
    ///      For Application type, select "Client" which means you do not
    ///      need to provide a callback URL. I selected Read&Write access,
    ///      but Read may be sufficient.
    ///      
    ///   2. Go to http://dev.twitter.com/apps and select the newly created
    ///      application to get access to the authentication keys. On this page
    ///      under "OAuth 1.0a Settings" you will see a "Consumer key" and a
    ///      "Consumer secret" that identifies your application. Copy these
    ///      to the ConsumerKey and ConsumerSecret fields below.
    ///      
    ///   3. On the right side of the page is a link to view "My access token"
    ///      Click this link. This gives you the access token for your Twitter
    ///      account. Copy these values into the AccessToken and AccessTokenSecret
    ///      fields below
    /// 
    /// You should now be ready to use the authenticated queries.
    /// </para>
    /// </summary>
    public class TwitterProvider
    {
        // Twitter OAuth secrets in plaintext! This is extremely unsecure, so
        // make sure nobody has access to this or implement secure local storage
        // for these.
        private const string ConsumerKey = "<consumer key here>";
        private const string ConsumerSecret = "<consumer secret here>";
        private const string AccessToken = "<access token here>";
        private const string AccessTokenSecret = "<access token secret here>";

        /// <summary>
        /// Returns a TweetSharp request object without authentication.
        /// </summary>
        private static IFluentTwitter GetPublicRequest()
        {
            return FluentTwitter.CreateRequest();
        }

        /// <summary>
        /// Returns an authenticated TweetSharp request object
        /// </summary>
        private static IFluentTwitter GetAuthenticatedRequest()
        {
            return FluentTwitter.CreateRequest().AuthenticateWith(
                ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
        }

        public static List<WallItem> GetMentions()
        {
            int attempts = 3;
            int failSleepMs = 500;

            do
            {
                try
                {
                    // Get authenticated request
                    var req = GetAuthenticatedRequest().Statuses().Mentions().Take(10).AsXml();
                    var data = req.Request();

                    if (data.ResponseHttpStatusCode == 401)
                    {
                        throw new ApplicationException("Twitter Authentication Exception. Did you enter your token info in WallItem.cs?", data.Exception);
                    }

                    XDocument doc = XDocument.Parse(data.Response);

                    // <created_at>Sat May 22 02:31:05 +0000 2010</created_at>
                    var wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value,
                                   e.Element("text").Value,
                                   e.Element("user").Element("profile_image_url").Value,
                                   DateTime.ParseExact(e.Element("created_at").Value, "ddd MMM dd HH:mm:ss %zzzz yyyy", null));

                    return wall.ToList();
                }
                catch (ApplicationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    --attempts;
                    System.Threading.Thread.Sleep(failSleepMs);
                    failSleepMs = failSleepMs * 2;
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
                catch (Exception)
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
                    // FIXME: Need to upgrade this to use OAuth. Basic auth is no longer supported!
                    var req = GetPublicRequest().Statuses().OnUserTimeline().For(user).AsXml();
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
                catch (Exception)
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
                    // FIXME: Need to upgrade this to use OAuth. Basic auth is no longer supported!
                    var req = GetAuthenticatedRequest().Statuses().OnFriendsTimeline().AsXml();
                    var data = req.Request().Response;

                    if (req.Request().ResponseHttpStatusCode == 401)
                    {
                        throw new ApplicationException("Twitter Authentication Exception. Did you enter your token info in WallItem.cs?", req.Request().Exception);
                    }

                    XDocument doc = XDocument.Parse(data);

                    var wall = from e in doc.Descendants("status")
                               select new WallItem(e.Element("user").Element("screen_name").Value, e.Element("text").Value, e.Element("user").Element("profile_image_url").Value);

                    return wall.ToList();
                }
                catch (ApplicationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    --attempts;
                    System.Threading.Thread.Sleep(500);
                }
            } while (attempts > 0);

            return null;
        }
    }

    /// <summary>
    /// Encapsulates a single tweet pulled from Twitter to be displayed on the
    /// Apple II.
    /// </summary>
    public class WallItem
    {
        /// <summary>
        /// Constructs a new WallItem for display.
        /// </summary>
        /// <param name="from">Twitter account name the message is from. The initial
        /// "@" is omitted here.</param>
        /// <param name="message">The text of the message to display</param>
        /// <param name="imageUri">URL pointing to the user's avatar image</param>
        public WallItem(string from, string message, string imageUri)
        {
            From = from;
            Message = message;
            ImageUri = imageUri;
        }

        /// <summary>
        /// Constructs a new WallItem for display.
        /// </summary>
        /// <param name="from">Twitter account name the message is from. The initial
        /// "@" is omitted here.</param>
        /// <param name="message">The text of the message to display</param>
        /// <param name="imageUri">URL pointing to the user's avatar image</param>
        /// <param name="timestamp">Timestamp of when the tweet was posted</param>
        public WallItem(string from, string message, string imageUri, DateTime timestamp)
        {
            From = from;
            Message = message;
            ImageUri = imageUri;
            TimeStamp = timestamp;
        }

        /// <summary>
        /// Timestamp of when the tweet was posted
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// URL for the posting user's avatar
        /// </summary>
        public string ImageUri { get; set; }

        /// <summary>
        /// Twitter account name of the user the tweet was sent from. There
        /// is no "@" prepended to this, so add at display time if you wish.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// The text of the tweet message.
        /// </summary>
        public string Message { get; set; }
    }
}
