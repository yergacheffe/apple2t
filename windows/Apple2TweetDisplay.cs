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
using System.Windows.Forms;
using System.Net;
using System.Drawing;
using System.IO;
using System.Threading;
using TweetSharp.Twitter.Fluent;
using System.Xml.Linq;

namespace TweetWall
{
    public class Apple2TweetDisplay
    {
        Apple2 apple = new Apple2();
        PictureBox loresImage = null;
        PictureBox srcImage = null;
        PictureBox hiresImage = null;
        int[] hiresScanlines = null;
        Random rnd = new Random((int)DateTime.Now.Ticks);
        bool flipflop = false;
        bool readyToDisplay = true;

        public Apple2TweetDisplay(PictureBox pb1, PictureBox pb2, PictureBox pb3)
        {
            // Save references to WinForms controls
            srcImage = pb1;
            loresImage = pb2;
            hiresImage = pb3;

            // Build table for HIRES screen offsets
            hiresScanlines = new int[192];
            for (int scanline = 0; scanline < 192; ++scanline)
            {
                int vtab = scanline / 8;
                int triad = vtab / 8;
                int leaf = vtab % 8;
                int row = scanline % 8;

                int offset = (triad * 40) + (leaf * 128) + (row * 1024);
                if (offset >= 8192)
                {
                    System.Console.WriteLine("poop");
                }

                hiresScanlines[scanline] = offset;
            }
        }

        public bool ReadyToDisplay
        {
            get { return readyToDisplay; }
            set { readyToDisplay = value; }
        }

        public void Dos33()
        {
            StreamReader reader = new StreamReader(@"..\..\dos33.dmp");
            string file = reader.ReadToEnd();
            string[] lines = file.Split('\r');
            int address = 0;
            int length = 0;
            byte[] buffer = new byte[65536];

            foreach (string line in lines)
            {
                string data;

                if (line.StartsWith("CALL"))
                {
                    continue;
                }

                if (line[0] != ':')
                {
                    if (address != 0)
                    {
                        apple.SendBuffer(address, buffer, length);
                    }

                    // Parse hex address
                    string addrString = line.Substring(0, line.IndexOf(':'));
                    address = Int32.Parse(addrString, System.Globalization.NumberStyles.HexNumber);
                    data = line.Substring(addrString.Length);
                }
                else
                {
                    data = line;
                }

                if (data[0] != ':')
                {
                    throw new ArgumentException("String format unrecognized");
                }
                data = data.Substring(1);

                while (data.Length > 0)
                {
                    buffer[length] = (byte)int.Parse(data.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
                    ++length;
                    data = data.Substring(2);
                    data = data.Trim();
                }
            }

            apple.SendBuffer(address, buffer, length);
        }

        public void LoadCode()
        {
            // Use bootloader to download latest code
            FileStream fs = new FileStream(@"..\..\TwitterII.bin", FileMode.Open, FileAccess.Read);
            int len = (int)(fs.Length + 255);
            byte[] code = new byte[len];

            fs.Read(code, 0, (int)fs.Length);
            int targetPage = 0x60;

            int srcOffset = 0;
            while (len > 255)
            {
                byte[] xfer = new byte[256];
                Buffer.BlockCopy(code, srcOffset, xfer, 0, 256);
                apple.SendPage((byte)targetPage, xfer);

                srcOffset += 256;
                len -= 256;
                ++targetPage;
            }
        }

        public void DisplayTweet(WallItem item)
        {
            // Buffer for lores screen
            byte[] lores = new byte[1024];
            byte[] hires = new byte[8192];

            RenderImage(item.From, item.ImageUri, lores, hires, false);
            RenderFrom(item.From, lores);
            RenderMessage(item.Message, lores);

            // Send LORES buffer to Apple II
            byte[] buffer = new byte[256];
            Buffer.BlockCopy(lores, 0, buffer, 0, 256);
            apple.SendPage(0x08, buffer);
            Buffer.BlockCopy(lores, 256, buffer, 0, 256);
            apple.SendPage(0x09, buffer);
            Buffer.BlockCopy(lores, 512, buffer, 0, 256);
            apple.SendPage(0x0A, buffer);
            Buffer.BlockCopy(lores, 768, buffer, 0, 256);
            apple.SendPage(0x0B, buffer);

            // Alternate between HIRES & LORES
            flipflop = !flipflop;

            if (flipflop)
            {
                // LORES
                apple.TickleAddress(0xC056);
                apple.TickleAddress(0xC053);

                // And scroll it on-screen
                apple.ScrollLores();
            }
            else
            {
                // HIRES
                Thread hiresCopy = new Thread(SendHiresThreadProc);
                ReadyToDisplay = false;
                hiresCopy.Start(hires);
            }
        }

        void SendHiresThreadProc(object param)
        {
            byte[] hires = (byte[])param;
            byte[] buffer = new byte[256];

            apple.ClearHires();

            // Send HIRES buffer to Apple II
            byte page = 0x20;
            for (int offset = 0; offset < 8192; offset += 256)
            {
                Buffer.BlockCopy(hires, offset, buffer, 0, 256);
                apple.SendPage(page, buffer);
                page++;
            }
            apple.TickleAddress(0xC057);
            apple.TickleAddress(0xC053);

            // And scroll it on-screen
            apple.ScrollLores();

            System.Threading.Thread.Sleep(15 * 1000);

            ReadyToDisplay = true;
        }

        void RenderFrom(string text, byte[] lores)
        {
            // Only 1 line of text, so limit input
            if (text.Length > 40)
            {
                text = text.Substring(0, 40);
            }
            text = text.ToUpperInvariant();

            // Base address inside buffer
            int baseOffset = 0x250;

            // erase top line
            for (int index = 0; index < 40; ++index)
            {
                lores[baseOffset + index] = 0xA0;
            }

            // draw from
            for (int index = 0; index < text.Length; ++index)
            {
                lores[baseOffset + index] = (byte)text[index];
            }
        }

        void RenderTextLine(string text, byte[] lores, int baseOffset)
        {
            // erase line
            for (int index = 0; index < 40; ++index)
            {
                lores[baseOffset + index] = 0xA0;
            }

            // draw text
            for (int index = 0; index < text.Length; ++index)
            {
                if (index > 39)
                {
                    break;
                }
                lores[baseOffset + index] = (byte)(text[index] | 0x80);
            }
        }

        void RenderMessage(string text, byte[] lores)
        {
            string message = text + "                                                                                                                             ";

            // Line 1, 2, 3
            RenderTextLine(message.Substring(0, 40), lores, 0x2D0);
            RenderTextLine(message.Substring(40, 40), lores, 0x350);
            RenderTextLine(message.Substring(80, 40), lores, 0x3D0);
        }

        // Apple II lores graphics
        //
        // $400: 0
        //       1
        // $480: 2
        //       3
        // $500: 4
        //       5
        // $600: 6
        //       7
        public void RenderImage(string fromuser, string uri, byte[] lores, byte[] hires, bool tweet)
        {
            // 1. Load source image
            System.Net.WebRequest request = HttpWebRequest.Create(uri);
            WebResponse resp = request.GetResponse();
            Stream respStream = resp.GetResponseStream();
            Bitmap result = new Bitmap(respStream);

            // 2. Reduce down to 40x40 and display that version
            Bitmap dest = new Bitmap(40, 40, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(dest);

            Rectangle destRect = new Rectangle(0, 0, 40, 40);
            int srcMinorAxis = result.Width > result.Height ? result.Height : result.Width;
            Rectangle srcRect = new Rectangle(0, 0, srcMinorAxis, srcMinorAxis);
            g.DrawImage(result, destRect, srcRect, GraphicsUnit.Pixel);
            srcImage.Image = dest;

            // 3. Map to 16 lores colors and create LORES buffer
            Bitmap loresBitmap = new Bitmap(40, 40, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            for (int y = 0; y < 40; ++y)
            {
                for (int x = 0; x < 40; ++x)
                {
                    Color c = dest.GetPixel(x, y);
                    //c = Dither(c, x, y);
                    Color lc;
                    int lci = FindLoresColor(c, out lc);
                    loresBitmap.SetPixel(x, y, lc);

                    Plot(lores, x, y, lci);
                }
            }
            loresImage.Image = loresBitmap;

            // 3. Convert to HIRES image format
            Bitmap hiresBitmap = new Bitmap(120, 160, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Graphics h = Graphics.FromImage(hiresBitmap);
            destRect = new Rectangle(0, 0, 120, 160);
            srcMinorAxis = result.Width > result.Height ? result.Height : result.Width;
            srcRect = new Rectangle(0, 0, srcMinorAxis, srcMinorAxis);
            h.DrawImage(result, destRect, srcRect, GraphicsUnit.Pixel);
            hiresImage.Image = hiresBitmap;

            // Scan bitmap -- 190 scanlines
            for (int y = 0; y < 160; ++y)
            {
                // Process scanlines in 7-pixel blocks. This allows us to lay down
                // 2 consecutive bytes of 14 half-pixels in the same palette.
                for (int xblock = 0; xblock < (120 / 7); ++xblock)
                {
                    double greenVioletError = 0.0;
                    double orangeBlueError = 0.0;
                    int greenVioletBits = 0;
                    int orangeBlueBits = 0;

                    for (int subpixel = 0; subpixel < 7; ++subpixel)
                    {
                        int x = (xblock * 7) + subpixel;
                        Color c = hiresBitmap.GetPixel(x, y);
                        c = Dither(c, x, y);
                        Color cx;
                        double err;
                        int ci;

                        // Match to green/violet clut
                        ci = FindClosestColor(c, hiresClutA, out cx, out err);
                        greenVioletBits <<= 2;
                        greenVioletBits |= ci;
                        greenVioletError += err;

                        // Match to orange/blue clut
                        ci = FindClosestColor(c, hiresClutB, out cx, out err);
                        orangeBlueBits <<= 2;
                        orangeBlueBits |= ci;
                        orangeBlueError += err;
                    }

                    List<Color> winningClut;
                    int clutbits;
                    int bits14 = 0;

                    if (greenVioletError <= orangeBlueError)
                    {
                        winningClut = hiresClutA;
                        clutbits = 0x00;
                        bits14 = greenVioletBits;
                    }
                    else
                    {
                        winningClut = hiresClutB;
                        clutbits = 0x80;
                        bits14 = orangeBlueBits;
                    }

                    // Recolor source bitmap with matched clut
                    for (int subpixel = 0; subpixel < 7; ++subpixel)
                    {
                        int x = (xblock * 7) + subpixel;
                        Color c = hiresBitmap.GetPixel(x, y);
                        c = Dither(c, x, y);
                        Color cx;
                        double err;

                        // Match to winning clut
                        FindClosestColor(c, winningClut, out cx, out err);
                        hiresBitmap.SetPixel(x, y, cx);
                    }

                    // Now calculate the damnable bits for the HIRES buffer
                    int byte1 = 0;
                    int byte2 = 0;

                    for (int index = 0; index < 7; ++index)
                    {
                        byte2 <<= 1;
                        byte2 |= bits14 & 1;
                        bits14 >>= 1;
                    }
                    for (int index = 0; index < 7; ++index)
                    {
                        byte1 <<= 1;
                        byte1 |= bits14 & 1;
                        bits14 >>= 1;
                    }

                    byte2 |= clutbits;
                    byte1 |= clutbits;

                    int offset = hiresScanlines[y] + xblock * 2;
                    hires[offset] = (byte)byte1;
                    hires[offset+1] = (byte)byte2;
                }
            }

            if (tweet)
            {
                // Disabling this code -- this was used to autoreply with a 
                // tweet referencing the AppleII-ified image
#if false
                string hiresPath = string.Format("image-{0}-hires.jpg", fromuser);
                string loresPath = string.Format("image-{0}-lores.jpg", fromuser);

                Bitmap lbm = new Bitmap(400, 400, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                Graphics lgr = Graphics.FromImage(lbm);
                lgr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                lgr.DrawImage(loresBitmap, Rectangle.FromLTRB(0, 0, 400, 400));

                Bitmap hbm = new Bitmap(400, 400, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                Graphics hgr = Graphics.FromImage(hbm);
//                lgr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                hgr.DrawImage(hiresBitmap, Rectangle.FromLTRB(0, 0, 400, 400));

                hbm.Save(hiresPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                lbm.Save(loresPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                var req = FluentTwitter.CreateRequest().AuthenticateAs("user", "password").Photos().PostPhoto(hiresPath).Statuses().Update(
                    string.Format("@{0} Thanks for coming by the @yergacheffe booth to see the apple2t at Maker Faire. [HIRES]. ", fromuser));
                var data = req.Request();


                req = FluentTwitter.CreateRequest().AuthenticateAs("user", "password").Photos().PostPhoto(loresPath).Statuses().Update(
                    string.Format("@{0} Thanks for coming by the @yergacheffe booth to see the apple2t at Maker Faire. [LORES]. ", fromuser));
                data = req.Request();
#endif
            }

            g.Dispose();
        }

        static int[] loresAddrMap = new int[22]
        {
            0x000,  // 0/1
            0x080,  // 2/3
            0x100,  // 4/5
            0x180,  // 6/7
            0x200,  // 8/9
            0x280,  // 10/11
            0x300,  // 12/13
            0x380,  // 14/15
            0x028,  // 16/17
            0x0A8,  // 18/19
            0x128,  // 20/21
            0x1A8,  // 22/23
            0x228,  // 24/25
            0x2A8,  // 26/27
            0x328,  // 28/29
            0x3A8,  // 30/31
            0x050,  // 32/33
            0x0D0,  // 34/35
            0x150,  // 36/37
            0x1D0,  // 38/39
            0x250,  // 40/41
            0x2D0,  // 42/43
        };
        void Plot(byte[] loresBuffer, int x, int y, int ci)
        {
            if (x < 0 || x > 39)
            {
                throw new ArgumentException("x out of range");
            }
            if (y < 0 || y > 39)
            {
                throw new ArgumentException("y out of range");
            }

            int index = loresAddrMap[y / 2] + x;
            byte existing = loresBuffer[index];

            if ( (y & 1) != 0)
            {
                // Odd scanline -- high nybble
                existing = (byte)((existing & 0x0F) | ((byte)ci << 4));
            }
            else
            {
                // Even scanline -- low nybble
                existing = (byte)((existing & 0xF0) | ((byte)ci));
            }

            loresBuffer[index] = existing;
        }

        static int[] ditherMatrix = new int[4]
                                    {
                                        -16, 16, 
                                        8,  -8
                                    };
        byte Clamp(byte val, int delta)
        {
            int value = (int)val + delta;
            if (value < 0)
            {
                value = 0;
            }
            if (value > 255)
            {
                value = 255;
            }

            return (byte)value;
        }

        Color Dither(Color c, int x, int y)
        {
            int ditherIndex = (y % 2) * 2 + (x % 2);

            return Color.FromArgb(Clamp(c.R, ditherMatrix[ditherIndex]),
                Clamp(c.G, ditherMatrix[ditherIndex]),
                Clamp(c.B, ditherMatrix[ditherIndex]));
        }

        List<Color> loresClut = new List<Color>() 
        { 
            Color.FromArgb(0,     0,   0),  // 0: Black
            Color.FromArgb(227,  30,  96),  // 1: Red
            Color.FromArgb( 96,  78, 189),  // 2: Dark Blue
            Color.FromArgb(255,  68, 253),  // 3: Purple
            Color.FromArgb(  0, 163,  96),  // 4: Dark Green
            Color.FromArgb(156, 156, 156),  // 5: Gray
            Color.FromArgb( 20, 207, 253),  // 6: Medium Blue
            Color.FromArgb(208, 195, 255),  // 7: Light Blue
            Color.FromArgb( 96, 114,   3),  // 8: Brown
            Color.FromArgb(255, 106,  60),  // 9: Orange
            Color.FromArgb(156, 156, 156),  //10: Gray (bonus!)
            Color.FromArgb(255, 160, 208),  //11: Pink
            Color.FromArgb( 20, 245,  60),  //12: Light Green
            Color.FromArgb(208, 221, 141),  //13: Yellow
            Color.FromArgb(114, 255, 208),  //14: Aqua
            Color.FromArgb(255, 255, 255)   //15: White
        };

        List<Color> hiresClutA = new List<Color>() 
        { 
            Color.FromArgb(0,     0,   0),  // 0: Black
            Color.FromArgb( 20, 245,  60),  // 1: Light Green
            Color.FromArgb(255,  68, 253),  // 2: Purple
            Color.FromArgb(255, 255, 255)   // 3: White
        };

        List<Color> hiresClutB = new List<Color>() 
        { 
            Color.FromArgb(0,     0,   0),  // 0: Black
            Color.FromArgb(255, 106,  60),  // 1: Orange
            Color.FromArgb( 20, 207, 253),  // 2: Medium Blue
            Color.FromArgb(255, 255, 255)   // 3: White
        };

        private double DeltaSquared(byte a, byte b)
        {
            double da = (double)a;
            double db = (double)b;

            double delta = Math.Abs(da - db);

            return delta * delta;
        }

        private int FindLoresColor(Color c, out Color lc)
        {
            double err;

            return FindClosestColor(c, loresClut, out lc, out err);
        }

        private int FindClosestColor(Color c, List<Color> clut, out Color lc, out double error)
        {
            int bestMatch = 0;
            double bestError = 999999.9;

            int index = 0;
            foreach (Color candidate in clut)
            {
                double err;

                err = Math.Sqrt(DeltaSquared(c.R, candidate.R) +
                                   DeltaSquared(c.G, candidate.G) +
                                   DeltaSquared(c.B, candidate.B));
                if (err < bestError)
                {
                    bestError = err;
                    bestMatch = index;
                }

                ++index;
            }

            lc = clut[bestMatch];
            error = bestError;
            return bestMatch;
        }
    }
}
