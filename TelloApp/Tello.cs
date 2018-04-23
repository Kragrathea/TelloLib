using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TelloApp
{
    class Tello
    {
        private static UdpUser client;
        private static float[] joyAxis = new float[] { 0, 0, 0, 0, 0 };
        private static DateTime lastMessageTime;//for connection timeouts.

        public delegate void updateDeligate();
        public static event updateDeligate onUpdate;
        public delegate void connectionDeligate(ConnectionState newState);
        public static event connectionDeligate onConnection;
        public static FlyData state= new FlyData();

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }
        public static ConnectionState connectionState = ConnectionState.Disconnected;

        public static CancellationTokenSource cancelTokens = new CancellationTokenSource();//used to cancel listeners


        public static void takeOff()
        {
            var takeOffPacket = new byte[] { 0xcc, 0x58, 0x00, 0x7c, 0x68, 0x54, 0x00, 0xe4, 0x01, 0xc2, 0x16 };
            client.Send(takeOffPacket);
        }
        public static void land()
        {
            var landPacket = new byte[] { 0xcc, 0x60, 0x00, 0x27, 0x68, 0x55, 0x00, 0xe5, 0x01, 0x00, 0xba, 0xc7 };
            client.Send(landPacket);
        }
        public static void setAxis(float[] axis)
        {
            joyAxis = axis.Take(5).ToArray(); ;
            joyAxis[2] = axis[10];
            joyAxis[3] = axis[11];
        }

        public static void disconnect()
        {
            //kill listeners
            cancelTokens.Cancel();
            //client.Client.Close();
            connected = false;

            if (connectionState != ConnectionState.Disconnected)
            {
                //if changed to disconnect send event
                onConnection(connectionState);
            }

            connectionState = ConnectionState.Disconnected;
            
        }
        public static void connect()
        {
            Console.WriteLine("Connecting to tello.");
            client = UdpUser.ConnectTo("192.168.10.1", 8889);

            connectionState = ConnectionState.Connecting;
            //send event
            onConnection(connectionState);

            byte[] connectPacket = Encoding.UTF8.GetBytes("conn_req:\x00\x00");
            connectPacket[connectPacket.Length - 2] = 0x96;
            connectPacket[connectPacket.Length - 1] = 0x17;
            client.Send(connectPacket);
        }
        public static void startListeners()
        {
            cancelTokens = new CancellationTokenSource();
            CancellationToken token = cancelTokens.Token;

            //wait for reply messages from the tello and process. 
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested)//handle canceling thread.
                            break;
                        var received = await client.Receive();
                        lastMessageTime = DateTime.Now;//for timeouts

                        if(connectionState==ConnectionState.Connecting)
                        {
                            if(received.Message.StartsWith("conn_ack"))
                            {
                                connected = true;
                                connectionState = ConnectionState.Connected;
                                //send event
                                onConnection(connectionState);

                                startHeartbeat();
                                Console.WriteLine("Tello connected!");
                                continue;
                            }
                        }


                        int cmdId = ((int)received.bytes[5] | ((int)received.bytes[6] << 8));
                        if (cmdId == 86)//state command
                        {
                            //update
                            state.set(received.bytes.Skip(9).ToArray());

                            //fire update event.
                            onUpdate();
                        }

                        var cmdName = "unknown";
                        //if (cmdIdLookup.ContainsKey(cmdId))
                        //    cmdName = cmdIdLookup[cmdId];

                        var dataStr = BitConverter.ToString(received.bytes.Skip(9).Take(30).ToArray()).Replace("-", " ");

                        //Debug printing of select command messages.
                        if (cmdId != 26 && cmdId != 86 && cmdId != 53 && cmdId != 4176 && cmdId != 4177 && cmdId != 4178)
                            //    if (cmdId == 86)
                            Console.WriteLine("cmdId:" + cmdId + "(0x" + cmdId.ToString("X2") + ")" + cmdName + " " + dataStr);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Receive thread error:" + ex.Message);
                        disconnect();
                        break;
                    }
                }
            }, token);

        }
        public static void startHeartbeat()
        {
            CancellationToken token = cancelTokens.Token;

            //heartbeat.
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var rx = joyAxis[2];//Axis[0]((float)joyState.RotationX / 0x8000) - 1;
                    var ry = -joyAxis[3];//-(((float)joyState.RotationY / 0x8000) - 1);
                    var lx = joyAxis[0];// ((float)joyState.X / 0x8000) - 1;
                    var ly = -joyAxis[1];//-(((float)joyState.Y / 0x8000) - 1);
                    var deadBand = 0.15f;
                    rx = Math.Abs(rx) < deadBand ? 0.0f : rx;
                    ry = Math.Abs(ry) < deadBand ? 0.0f : ry;
                    lx = Math.Abs(lx) < deadBand ? 0.0f : lx;
                    ly = Math.Abs(ly) < deadBand ? 0.0f : ly;


                    var boost = 0.0f;
                    if (joyAxis[4] > 0.5)
                        boost = 1.0f;
                    var limit = 0.5f;//Slow down while testing.
                    rx = rx * limit;
                    ry = ry * limit;

                    var packet = createJoyPacket(rx, ry, lx, ly, boost);
                    //Console.WriteLine(rx + " " + ry + " " + lx + " " + ly);
                    try
                    {
                        if (token.IsCancellationRequested)
                            break;
                        client.Send(packet);
                        Thread.Sleep(50);//Often enough?
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Heatbeat error:" + ex.Message);
                        disconnect();
                        break;
                    }
                }
            }, token);

        }
        public static bool connected = false;
        public static void init()
        {
            //Thread to handle connecting.
            Task.Factory.StartNew(async () =>
            {
                var timeout = new TimeSpan(2000);//2 second connection timeout
                while (true)
                {
                    try
                    {
                        switch (connectionState)
                        {
                            case ConnectionState.Disconnected:
                                connect();
                                lastMessageTime = DateTime.Now;

                                startListeners();
                                
                                break;
                            case ConnectionState.Connecting:
                            case ConnectionState.Connected:
                                var elapsed = DateTime.Now - lastMessageTime;
                                if (elapsed.Seconds > 1)
                                {
                                    Console.WriteLine("Connection timeout :");
                                    disconnect();
                                }
                                break;
                        }
                        Thread.Sleep(500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Connection thread error:" + ex.Message);
                    }
                }
            });

        }


        Dictionary<int, string> cmdIdLookup = new Dictionary<int, string>
            {
                { 26, "Wifi" },//2 bytes. Strength, Disturb.
                { 53, "Light" },//1 byte?
                { 86, "FlyData" },
                { 4176, "Data" },//wtf?
            };

        //Create joystick packet from floating point axis.
        //Center = 0.0. 
        //Up/Right =1.0. 
        //Down/Left=-1.0. 
        public static byte[] createJoyPacket(float fRx, float fRy, float fLx, float fLy, float unk)
        {
            //template joy packet.
            var packet = new byte[] { 0xcc, 0xb0, 0x00, 0x7f, 0x60, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x16, 0x01, 0x0e, 0x00, 0x25, 0x54 };

            short axis1 = (short)(660.0F * fRx + 1024.0F);//RightX center=1024 left =364 right =-364
            short axis2 = (short)(660.0F * fRy + 1024.0F);//RightY down =364 up =-364
            short axis3 = (short)(660.0F * fLy + 1024.0F);//LeftY down =364 up =-364
            short axis4 = (short)(660.0F * fLx + 1024.0F);//LeftX left =364 right =-364
            short axis5 = (short)(660.0F * unk + 1024.0F);//Unknown. Possibly camera controls. 

            if (unk > 0.1f)
                axis5 = 0x7fff;

            long packedAxis = ((long)axis1 & 0x7FF) | (((long)axis2 & 0x7FF) << 11) | ((0x7FF & (long)axis3) << 22) | ((0x7FF & (long)axis4) << 33) | ((long)axis5 << 44);
            packet[9] = ((byte)(int)(0xFF & packedAxis));
            packet[10] = ((byte)(int)(packedAxis >> 8 & 0xFF));
            packet[11] = ((byte)(int)(packedAxis >> 16 & 0xFF));
            packet[12] = ((byte)(int)(packedAxis >> 24 & 0xFF));
            packet[13] = ((byte)(int)(packedAxis >> 32 & 0xFF));
            packet[14] = ((byte)(int)(packedAxis >> 40 & 0xFF));

            //Add time info.		
            var now = DateTime.Now;
            packet[15] = (byte)now.Hour;
            packet[16] = (byte)now.Minute;
            packet[17] = (byte)now.Second;
            packet[18] = (byte)(now.Millisecond & 0xff);
            packet[19] = (byte)(now.Millisecond >> 8);

            CRC.calcUCRC(packet, 4);//Not really needed.

            //calc crc for packet. 
            CRC.calcCrc(packet, packet.Length);

            return packet;
        }
        public class FlyData
        {
            public int batteryLow;
            public int batteryLower;
            public int batteryPercentage;
            public int batteryState;
            public int cameraState;
            public int downVisualState;
            public int droneBatteryLeft;
            public int droneFlyTimeLeft;
            public int droneHover;
            public int eMOpen;
            public int eMSky;
            public int eMground;
            public int eastSpeed;
            public int electricalMachineryState;
            public int factoryMode;
            public int flyMode;
            public int flySpeed;
            public int flyTime;
            public int frontIn;
            public int frontLSC;
            public int frontOut;
            public int gravityState;
            public int groundSpeed;
            public int height;
            public int imuCalibrationState;
            public int imuState;
            public int lightStrength;
            public int northSpeed;
            public int outageRecording;
            public int powerState;
            public int pressureState;
            public int smartVideoExitMode;
            public int temperatureHeight;
            public int throwFlyTimer;
            public int wifiDisturb;
            public int wifiStrength = 100;
            public int windState;

            public void set(byte[] data)
            {
                var index = 0;
                height = data[index] | (data[index + 1] << 8); index += 2;
                northSpeed = data[index] | (data[index + 1] << 8); index += 2;
                eastSpeed = data[index] | (data[index + 1] << 8); index += 2;
                flySpeed = ((int)Math.Sqrt(Math.Pow(northSpeed, 2.0D) + Math.Pow(eastSpeed, 2.0D)));
                groundSpeed = data[index] | (data[index + 1] << 8); index += 2;// ah.a(paramArrayOfByte[6], paramArrayOfByte[7]);
                flyTime = data[index] | (data[index + 1] << 8); index += 2;// ah.a(paramArrayOfByte[8], paramArrayOfByte[9]);

                imuState = (data[index] >> 0 & 0x1);
                pressureState = (data[index] >> 1 & 0x1);
                downVisualState = (data[index] >> 2 & 0x1);
                powerState = (data[index] >> 3 & 0x1);
                batteryState = (data[index] >> 4 & 0x1);
                gravityState = (data[index] >> 5 & 0x1);
                windState = (data[index] >> 7 & 0x1);
                index += 1;

                //if (paramArrayOfByte.length < 19) { }
                imuCalibrationState = data[index]; index += 1;
                batteryPercentage = data[index]; index += 1;
                droneFlyTimeLeft = data[index] | (data[index + 1] << 8); index += 2;
                droneBatteryLeft = data[index] | (data[index + 1] << 8); index += 2;

                //index 17
                eMSky = (data[index] >> 0 & 0x1);
                eMground = (data[index] >> 1 & 0x1);
                eMOpen = (data[index] >> 2 & 0x1);
                droneHover = (data[index] >> 3 & 0x1);
                outageRecording = (data[index] >> 4 & 0x1);
                batteryLow = (data[index] >> 5 & 0x1);
                batteryLower = (data[index] >> 6 & 0x1);
                factoryMode = (data[index] >> 7 & 0x1);
                index += 1;

                flyMode = data[index]; index += 1;
                throwFlyTimer = data[index]; index += 1;
                cameraState = data[index]; index += 1;

                //if (paramArrayOfByte.length >= 22)
                electricalMachineryState = data[index]; index += 1; //(paramArrayOfByte[21] & 0xFF);

                //if (paramArrayOfByte.length >= 23)
                frontIn = (data[index] >> 0 & 0x1);//22
                frontOut = (data[index] >> 1 & 0x1);
                frontLSC = (data[index] >> 2 & 0x1);
                index += 1;
                temperatureHeight = (data[index] >> 0 & 0x1);//23
            }
        }

    }
}