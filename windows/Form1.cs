using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
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
        List<WallItem> apple2Items = null;
        int apple2Index = 0;
        Apple2TweetDisplay apple2 = null;

        public Form1()
        {
            InitializeComponent();

            apple2 = new Apple2TweetDisplay(apple2srcImage, apple2LoresImage, apple2HiresImage);

            // If you are bootstrapping your Apple II, uncomment the following line
            // to transfer the Apple II client code. Doing so requires that you've
            // hand-entered the mini bootloader code at location $300 on the Apple II
            // so the serial transfer protocol is working.
//          apple2.LoadCode();

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
                // Pick a Twitter query to display:
#if false
                // This gets @mentions of your account. It requires authentication,
                // so you will need to modify WallItem.cs to contain your tokens
                apple2Items = TwitterProvider.GetMentions();
#endif

#if false
                // This gets the timeline view of the people you follow. It's
                // the standard twitter view we're all used to. It also requires
                // authentication, so follow the instructions in WallItem.cs to
                // make this work.
                apple2Items = TwitterProvider.GetLatestItems();
#endif

                // This gets the latest tweets from a given user. It does not
                // require authentication, so it is the easiest to test with.
                apple2Items = TwitterProvider.GetItemsFromUser("yergacheffe");

                // Limit number to most recent 5 or last 10 minutes. I did this
                // at Maker Faire to assure that folks tweeting in real-time would
                // be able to see their tweet on the Apple II in a fairly timely
                // manner. You may wish to turn this off for viewing your own
                // Twitter timeline at home.
                List<WallItem> culled = new List<WallItem>();
                for (int cullIndex = 0; cullIndex < apple2Items.Count; ++cullIndex)
                {
                    if ((cullIndex < 5) || (DateTime.Now - apple2Items[cullIndex].TimeStamp) < TimeSpan.FromMinutes(10))
                    {
                        culled.Add(apple2Items[cullIndex]);
                    }
                }
                apple2Items = culled;

                // This just updates the UI list in the Windows GUI
                tweetList.Items.Clear();
                foreach (WallItem item in apple2Items)
                {
                    ListViewItem uxItem = new ListViewItem(new string[] { item.From, item.Message });
                    tweetList.Items.Add(uxItem);
                }

                apple2Index = 0;
            }

            // Are there more tweets to display?
            if (apple2Index < apple2Items.Count)
            {
                // Yes -- is it time yet? And if so, is the Apple II even
                // ready for another one?
                if (DateTime.UtcNow > nextTweet && apple2.ReadyToDisplay)
                {
                    // We're ready for a new item -- grab the next one
                    var item = apple2Items[apple2Index];

                    try
                    {
                        // Convert the image to Apple II format, send via the
                        // serial protocol, etc. Lots can go wrong here :)
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
