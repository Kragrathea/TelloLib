# TelloLib 
A Work-In-Progress cross platform Ryze/DJI Tello control library. Currently supports Android and PC.

# aTello 
A bare bones Android Tello control app with minimal UI. 
NOTE:Requires a PS3 Joystick connected via OTG cable for a controller. Currently NO onscreen joysticks.   

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/aTelloScreen5.png" width="600" >

# TelloConsole
PC console app. Similar to hello tello but more functional. Does video out to ffplay. Only supports a few commands right now.

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/TelloConsole-Screen1.jpg" width="400">

# HelloTello
A very basic TelloLib example.
```
using TelloLib;

namespace HelloTello
{
    class Program
    {
        static void Main(string[] args)
        {
            //Subscribe to Tello connection events. Called when connected/disconnected.
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
            Tello.onUpdate += (Tello.FlyData newState) =>
            {
                Console.WriteLine("FlyMode:" + newState.flyMode +" Height:" + newState.height);
            };

            Tello.startConnecting();//Start trying to connect.

            //Parse commands from console and send to drone.
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
```


Written in C# using Xarmin.
https://docs.microsoft.com/en-us/xamarin/cross-platform/get-started/installation/windows

