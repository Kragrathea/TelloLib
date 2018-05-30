using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelloLib;

namespace HelloTello
{
    class Program
    {
        static void Main(string[] args)
        {
            //subscribe to Tello connection events
            Tello.onConnection += (Tello.ConnectionState newState) =>
            {
                if (newState == Tello.ConnectionState.Connected)
                {
                    //When connected update maxHeight to 5 meters
                    Tello.setMaxHeight(5);
                }
                //Show connection messages.
                Console.WriteLine("Tello " + newState.ToString());
            };

            //subscribe to Tello update events. Called when update data arrives from drone.
            Tello.onUpdate += (int cmdId) =>
            {
                if(cmdId==86)//ac update
                    Console.WriteLine("FlyMode:" + Tello.state.flyMode +" Height:" + Tello.state.height);
            };

            Tello.startConnecting();//Start trying to connect.

            var str = "";
            while (str != "exit")
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