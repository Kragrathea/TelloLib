using System;
using System.Collections.Generic;
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
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Tello " + newState.ToString());
                if (newState != Tello.ConnectionState.Connected)
                {
                }
                if (newState == Tello.ConnectionState.Connected)
                {
                }
            };
            //subscribe to Tello update events
            Tello.onUpdate += (Tello.FlyData newState) =>
            {
                var outStr = newState.ToString();//ToString() = Formated state
                Console.SetCursorPosition(0, 5);
                Console.WriteLine(outStr);
            };


            //subscribe to Tello video data
            Tello.onVideoData += (byte[] data) =>
            {
            };

            Tello.startConnecting();//Start trying to connect.

            Console.WriteLine("Commands:takeoff,land,exit");
            var str = "";
            while(str!="exit")
            {
                str = Console.ReadLine().ToLower();
                if (str == "takeoff" && Tello.connected && !Tello.state.flying)
                    Tello.takeOff();
                if (str == "land" && Tello.connected && Tello.state.flying)
                    Tello.land();
            }
        }
    }
}
