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
                if (newState != Tello.ConnectionState.Connected)
                {
                }
                if (newState == Tello.ConnectionState.Connected)
                {
                    clearConsole();
                }
                printAt(0,0,"Tello " + newState.ToString());
            };
            //subscribe to Tello update events
            Tello.onUpdate += (Tello.FlyData newState) =>
            {
                var outStr = newState.ToString();//ToString() = Formated state
                printAt(0, 2, outStr);
            };


            //subscribe to Tello video data
            Tello.onVideoData += (byte[] data) =>
            {
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
                    clearConsole();
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
            Console.SetCursorPosition(0, 22);
            Console.WriteLine("Commands:takeoff,land,exit,cls");
        }
    }
}
