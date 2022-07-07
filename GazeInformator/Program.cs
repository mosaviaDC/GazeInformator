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
        private  static Process process;

        public static UdpClient ReceiveUDPClient { get; private set; }

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
            Thread UdpReceiveThread = new Thread(new ThreadStart(UDPReceive));
            UdpReceiveThread.Start();
        }

        private static async void UDPReceive()
        {
           ReceiveUDPClient = new UdpClient(7000);
            while (true)
            {
              
                var result = await ReceiveUDPClient.ReceiveAsync();
                if (result.Buffer[0] == 1)
                {
                    //Запуск видео
                    if (process == null)
                    StartVideoRecording();
                }
                else if (result.Buffer[0] == 0)
                {
                    if (process != null)
                        process.Kill();
                    process = null;
                }
            }

        }



        private static void FixationDataStream_Next(object sender, StreamData<FixationData> e)
        {
            values[0] = TransformToNormCoordinates(e.Data.X, Width);
            values[1] = TransformToNormCoordinates(e.Data.Y, Height);
       //     Debug.WriteLine(e.Data.Timestamp);
            values[6] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private  static void  StartVideoRecording()
        {

            string argument = @"-f gdigrab  -r 20 -show_region 1 -video_size 1920x1080  -i desktop   -f rawvideo  -vcodec mjpeg -preset ultrafast  -crf:v 17 -tune zerolatency -threads 4   -b:v 1M  -filter:v fps=90  -filter:v " + " setpts=1*PTS " + $"udp://{ConfigurationManager.AppSettings["DestinationIP"]}:7777";
            process = new Process();
            {
                Debug.WriteLine("StartVideo");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.FileName = @"C:\Users\User\Source\Repos\GazeView\GazeViewer\FFmpeg\ffmpeg.exe";
                process.StartInfo.Arguments = argument;
                process.Start();
                //process.WaitForExit();

            }

        }




        static  void SendData()
        {
       
  
            UdpClient udpClient = new UdpClient(ConfigurationManager.AppSettings["DestinationIP"], int.Parse(ConfigurationManager.AppSettings["DestinationPort"]));
      
          //  UdpClient udpClient = new UdpClient("127.0.0.1", 5444);
            byte[] bytes = new byte[values.Length * sizeof(double)];
         //   FreeConsole();
        
            while (true)
            {
                //      Debug.WriteLine($"Xpos {values[0]} Ypos {values[1]}");)
                //Thread.Sleep(561);
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
            process.Close();
        }

    }
}
