using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TweetWall
{
    /// <summary>
    /// Class for handling communications with Apple II. 
    /// <para>
    /// The physical interface uses an FTDI USB cable running in bitbang mode 
    /// to drive 3 lines connected to TTL button inputs on the Apple II game 
    /// connector.
    /// </para>
    /// <para>
    /// The 3 lines are used to implement an SPI protocol. 2 of the lines are
    /// standard DATA and CLK signals and the 3rd (ATTN) is used for framing the
    /// start of a command sequence.
    /// </para>
    /// <para>
    /// The Apple II code is contained in the file TwitterII.m65
    /// </para>
    /// </summary>
    public class Apple2
    {
        /// <summary>
        /// FTDI API handle for talking to FT232R chip
        /// </summary>
        int Handle;

        /// <summary>
        /// Last error returned from FT_XXX API call
        /// </summary>
        int Err;

        /// <summary>
        /// PIN bitmasks for SPI protocol
        /// </summary>
        const int IO_ATTN  = 4;
        const int IO_CLOCK = 2;
        const int IO_DATA  = 1;

        // FTDI PINS
        // 1 - Orange (TX)
        // 2 - Yellow (RX)
        // 4 - Green (RTS)
        // 8 - Brown (CTS)

        /// <summary>
        /// Initializes the FT_XXX API driver and opens the first device.
        /// </summary>
        public Apple2()
        {
            int flags = 0;
            int id = 0;
            int locid = 0;
            int type = 0;
            IntPtr serial = Marshal.AllocHGlobal(1000);
            IntPtr desc = Marshal.AllocHGlobal(1000);
            int deviceCount;

            Err = FT_CreateDeviceInfoList(out deviceCount);
            Err = FT_GetDeviceInfoDetail(0, ref flags, ref type, ref id, ref locid, serial, desc, out Handle);
            string s = Marshal.PtrToStringAnsi(serial);
            System.Console.WriteLine("ID={0} Serial#={1}", 0, s);
            Err = FT_GetDeviceInfoDetail(1, ref flags, ref type, ref id, ref locid, serial, desc, out Handle);
            s = Marshal.PtrToStringAnsi(serial);
            System.Console.WriteLine("ID={0} Serial#={1}", 1, s);

            IntPtr str = Marshal.StringToHGlobalAnsi("FTELSFTV");
            Err = FT_OpenEx(str, 1, out Handle);

//            Err = FT_Open(0, out Handle);
            Err = FT_SetBitMode(Handle, 0x07, 1);
            Err = FT_SetBaudRate(Handle, 3600);
            BitBang(0);
        }

        /// <summary>
        /// Returns the last error code
        /// </summary>
        public int Error { get { return Err; } }

        /// <summary>
        /// Writes a byte of data containing pin states.
        /// </summary>
        /// <param name="data">Data to write</param>
        private void BitBang(int data)
        {
            byte[] buffer = new byte[1];
            int written = 0;

            buffer[0] = (byte)data;
            Err = FT_Write(Handle, buffer, 1, out written);
        }

        public void PrintString(string str)
        {
            foreach (char c in str)
            {
                SendByte((byte)(c | (byte)128));
            }
            SendByte(0x8d);
        }

        public void ClearHires()
        {
            BitBang(7);
            System.Threading.Thread.Sleep(50);
            BitBang(0);
            System.Threading.Thread.Sleep(400);
        }

        public void SendByte(int address, byte data)
        {
            BitBang(6);
            System.Threading.Thread.Sleep(50);
            BitBang(0);

            SendByte((byte)(address & 255));
            SendByte((byte)((address >> 8) & 255));
            SendByte(data);
        }

        public void TickleAddress(int address)
        {
            BitBang(6);
            System.Threading.Thread.Sleep(5);
            BitBang(0);

            SendByte((byte)(address & 255));
            SendByte((byte)((address >> 8) & 255));
            SendByte(0x00);
        }

        public void ScrollLores()
        {
            BitBang(5);
            System.Threading.Thread.Sleep(5);
            BitBang(0);

            // Really takes about a second to scroll, but just to be safe...
            System.Threading.Thread.Sleep(1000);
        }

        public void SendPage(byte page, byte[] data)
        {
            if (data.Length != 256)
            {
                throw new ArgumentException("data buffer not 256 bytes long");
            }

            // Signal to receive a page
            BitBang(IO_ATTN);
            System.Threading.Thread.Sleep(5);
            BitBang(0);

            // Send page
            SendByte(page);

            // Send data
            List<byte> bangers = new List<byte>();
            foreach (byte b in data)
            {
                // Calculate bitbang pins for the transmission of this byte
                for (int bit = 7; bit >= 0; --bit)
                {
                    int pins = (b & (1 << bit)) == 0 ? 0 : 1;

                    bangers.Add((byte)pins);
                    bangers.Add((byte)(pins | IO_CLOCK));
                    bangers.Add((byte)pins);
                }
            }
            byte[] bitstosend = bangers.ToArray();
            int written;
            FT_Write(Handle, bitstosend, bitstosend.Length, out written);
            // Delay a bit to let computer beep
            System.Threading.Thread.Sleep(10);
        }

        public int SendByte(byte data)
        {
            // Send data
            for (int bit = 7; bit >= 0; --bit)
            {
                int pins = (data & (1 << bit)) == 0 ? 0 : 1;

                BitBang(pins);
                BitBang(pins | IO_CLOCK);
                BitBang(pins);
            }

            return Err;
        }

        public void SendBuffer(int address, byte[] buffer, int length)
        {
            int pages = length / 256;
            int bytes = length % 256;
            int offset = 0;
            int count = 0;

            while (length >= 256)
            {
                byte[] pagedata = new byte[256];
                Buffer.BlockCopy(buffer, offset, pagedata, 0, 256);
                SendPage((byte)(address >> 8), pagedata);

                length -= 256;
                offset += 256;
                address += 256;

                System.Console.WriteLine("{0} / {1}", count, pages + bytes);
                ++count;
            }

            while (length > 0)
            {
                SendByte(address, buffer[offset]);

                --length;
                ++offset;
                address++;
                ++count;

                System.Console.WriteLine("{0} / {1}", count, pages + bytes);
            }
        }

        [DllImport("ftd2xx.dll")]
        extern static int FT_Open(int deviceNumber, out int handle);

        [DllImport("ftd2xx.dll")]
        extern static int FT_OpenEx(IntPtr comString, int flags, out int handle);

        [DllImport("ftd2xx.dll")]
        extern static int FT_SetBitMode(int handle, byte Mask, byte enable);

        [DllImport("ftd2xx.dll")]
        extern static int FT_Write(int handle, byte[] data, int byteCount, out int bytesWritten);

        [DllImport("ftd2xx.dll")]
        extern static int FT_SetBaudRate(int handle, int baud);

        [DllImport("ftd2xx.dll")]
        extern static int FT_GetDeviceInfoList(IntPtr buffer, ref int bufferSize);

        [DllImport("ftd2xx.dll")]
        extern static int FT_GetDeviceInfoDetail(int index, ref int flags, ref int type, ref int id, ref int locid,
            IntPtr serialNumber, IntPtr description, out int handle);

        [DllImport("ftd2xx.dll")]
        extern static int FT_CreateDeviceInfoList(out int deviceCount);

    }
}
