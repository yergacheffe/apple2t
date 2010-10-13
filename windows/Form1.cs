using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using TweetSharp.Fluent;
using TweetSharp.Model;
using TweetSharp.Extensions;
using System.Net;

namespace TweetWall
{
    public partial class Form1 : Form
    {
        Timer idler = null;
        object locker = new object();
        List<WallItem> tweetWallItems = null;
        int tweetWallIndex = 0;
        List<WallItem> apple2Items = null;
        int apple2Index = 0;
        bool refreshTweets = false;
        Apple2TweetDisplay apple2 = null;

        public Form1()
        {
            InitializeComponent();

            apple2 = new Apple2TweetDisplay(apple2srcImage, apple2LoresImage, apple2HiresImage);

            // Display animation idler
            idler = new Timer();
            idler.Interval = 50;
            idler.Tick += new EventHandler(idler_Tick);
            idler.Enabled = true;
        }

        void idler_Tick(object sender, EventArgs e)
        {
            //            memeWrestle.Idle();
            //            Idle_TweetWall();
            Idle_Apple2t();
        }

        DateTime nextTweet = DateTime.UtcNow;
        void Idle_Apple2t()
        {
            if (apple2Items == null || apple2Index >= apple2Items.Count)
            {
//              apple2Items = TwitterProvider.GetDirectMessages();
                apple2Items = TwitterProvider.GetLatestItems();

                List<WallItem> culled = new List<WallItem>();

                // Limit number to most recent 5 or last 10 minutes
                for (int cullIndex=0; cullIndex<apple2Items.Count; ++cullIndex)
                {
                    if ((cullIndex < 5) || (DateTime.Now - apple2Items[cullIndex].TimeStamp) < TimeSpan.FromMinutes(10))
                    {
                        culled.Add(apple2Items[cullIndex]);
                    }
                }
                apple2Items = culled;

                tweetList.Items.Clear();
                foreach (WallItem item in apple2Items)
                {
                    ListViewItem uxItem = new ListViewItem(new string[] { item.From, item.Message });
                    tweetList.Items.Add(uxItem);
                }

                apple2Index = 0;
            }

            if (apple2Index < apple2Items.Count)
            {
                if (DateTime.UtcNow > nextTweet && apple2.ReadyToDisplay)
                {
                    var item = apple2Items[apple2Index];

                    try
                    {
                        apple2.DisplayTweet(item);
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Apple2 Exception: {0}", e.Message);
                    }
                    nextTweet = DateTime.UtcNow + TimeSpan.FromSeconds(20);
                    
                    ++apple2Index;
                }
            }
        }


    }
}
