using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelloLib;

namespace TelloConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //subscribe to Tello connection events
            Tello.onConnection += (Tello.ConnectionState newState) =>
            {
                if (newState != Tello.ConnectionState.Connected)
                {
                }
                if (newState == Tello.ConnectionState.Connected)
                {
                    Tello.queryAttAngle();
                    Tello.setMaxHeight(50);

                    clearConsole();
                }
                printAt(0,0,"Tello " + newState.ToString());
            };

            //Log file setup.
            var logPath = "logs/";
            System.IO.Directory.CreateDirectory(Path.Combine("../", logPath));
            var logStartTime = DateTime.Now;
            var logFilePath = Path.Combine("../", logPath + logStartTime.ToString("yyyy-dd-M--HH-mm-ss") + ".csv");

            //write header for cols in log.
            File.WriteAllText(logFilePath, "time,"+Tello.state.getLogHeader());

                //subscribe to Tello update events.
            Tello.onUpdate += (cmdId) =>
            {
                if (cmdId == 86)//ac update
                {
                    //write update to log.
                    var elapsed = DateTime.Now - logStartTime;
                    File.AppendAllText(logFilePath, elapsed.ToString(@"mm\:ss\:ff\,") + Tello.state.getLogLine());

                    //display state in console.
                    var outStr = Tello.state.ToString();//ToString() = Formated state
                    printAt(0, 2, outStr);
                }
            };

            //subscribe to Joystick update events. Called ~10x second.
            PCJoystick.onUpdate += (SharpDX.DirectInput.JoystickState joyState) =>
            {

                var rx = ((float)joyState.RotationX / 0x8000) - 1;
                var ry = (((float)joyState.RotationY / 0x8000) - 1);
                var lx = ((float)joyState.X / 0x8000) - 1;
                var ly = (((float)joyState.Y / 0x8000) - 1);
                //var boost = joyState.Z
                float[] axes = new float[] { lx, ly, rx, ry, 0 };
                var outStr = string.Format("JOY {0: 0.00;-0.00} {1: 0.00;-0.00} {2: 0.00;-0.00} {3: 0.00;-0.00} {4: 0.00;-0.00}", axes[0], axes[1], axes[2], axes[3], axes[4]);
                printAt(0, 22, outStr);
                Tello.controllerState.setAxis(lx, ly,rx, ry);
                Tello.sendControllerUpdate();
            };
            PCJoystick.init();

            //Connection to send raw video data to local udp port.
            //To play: ffplay -probesize 32 -sync ext udp://127.0.0.1:7038
            //To play with minimum latency:ffmpeg -i udp://127.0.0.1:7038 -f sdl "Tello"
            var videoClient = UdpUser.ConnectTo("127.0.0.1", 7038);

            //subscribe to Tello video data
            Tello.onVideoData += (byte[] data) =>
            {
                try
                {
                    videoClient.Send(data.Skip(2).ToArray());//Skip 2 byte header and send to ffplay. 
                    //Console.WriteLine("Video size:" + data.Length);
                }catch (Exception ex)
                {

                }
            };

            Tello.startConnecting();//Start trying to connect.

            clearConsole();

            var str = "";
            while(str!="exit")
            {
                str = Console.ReadLine().ToLower();
                if (str == "takeoff" && Tello.connected && !Tello.state.flying)
                    Tello.takeOff();
                if (str == "land" && Tello.connected && Tello.state.flying)
                    Tello.land();
                if (str == "cls")
                {
                    Tello.setMaxHeight(9);
                    Tello.queryMaxHeight();
                    clearConsole();
                }
            }
        }
        //Print at x,y in console. 
        static void printAt(int x, int y, string str)
        {
            var saveLeft = Console.CursorLeft;
            var saveTop = Console.CursorTop;
            Console.SetCursorPosition(x, y);
            Console.WriteLine(str + "     ");//Hack. extra space is to clear any previous chars.
            Console.SetCursorPosition(saveLeft, saveTop);

        }
        static void clearConsole()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 23);
            Console.WriteLine("Commands:takeoff,land,exit,cls");
        }
    }
}
