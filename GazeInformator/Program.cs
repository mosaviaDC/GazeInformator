using System;
using System.Management;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Tobii.Interaction;
using System.Configuration;
using System.Net;
using System.Net.WebSockets;
using System.Web;
using System.Diagnostics;

namespace GazeInformator
{
  

    class Program:IDisposable
    {
        private static double[] values = new double[7];

        private static double Width;
        private static double Height;
        private static Host host;
        private static FixationDataStream fixationDataStream;

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
        static void Main(string[] args)
        {



         host = new Host();
            
            ManagementObjectSearcher mydisplayResolution = new ManagementObjectSearcher("SELECT CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController");
       
            foreach (ManagementObject record in mydisplayResolution.Get())
            {

             Width = Convert.ToDouble(record["CurrentHorizontalResolution"]);
             Height = Convert.ToDouble(record["CurrentVerticalResolution"]);

            }

            fixationDataStream = host.Streams.CreateFixationDataStream(Tobii.Interaction.Framework.FixationDataMode.Slow);
            fixationDataStream.Next += FixationDataStream_Next;

            Thread UdpThread = new Thread(new ThreadStart(SendData));
            UdpThread.Start();

        }

        private static void FixationDataStream_Next(object sender, StreamData<FixationData> e)
        {
            values[0] = TransformToNormCoordinates(e.Data.X, Width);
            values[1] = TransformToNormCoordinates(e.Data.Y, Height);
            values[6] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        static  void SendData()
        {
       
  
            UdpClient udpClient = new UdpClient(ConfigurationManager.AppSettings["DestinationIP"], int.Parse(ConfigurationManager.AppSettings["DestinationPort"]));
      
          //  UdpClient udpClient = new UdpClient("127.0.0.1", 5444);
            byte[] bytes = new byte[values.Length * sizeof(double)];
         //   FreeConsole();
        
            while (true)
            {
              Debug.WriteLine($"Xpos {values[0]} Ypos {values[1]}");
      
                Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
                udpClient.Send(bytes, bytes.Length);
            }
        }


        static double TransformToNormCoordinates(double value, double koef)
        {

            return Convert.ToDouble((value * (2 / (koef))) - 1f);
        }

        public void Dispose()
        {
            host.Dispose();
        }

    }
}
