using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelloLib
{
    public class Tello
    {
        private static UdpUser client;
        private static float[] joyAxis = new float[] { 0, 0, 0, 0, 0 };
        private static DateTime lastMessageTime;//for connection timeouts.

        public static FlyData state = new FlyData();
        public static bool connected = false;

        public delegate void updateDeligate(FlyData state);
        public static event updateDeligate onUpdate;
        public delegate void connectionDeligate(ConnectionState newState);
        public static event connectionDeligate onConnection;
        public delegate void videoUpdateDeligate(byte[] data);
        public static event videoUpdateDeligate onVideoData;

        private static ushort sequence = 1;

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }
        public static ConnectionState connectionState = ConnectionState.Disconnected;

        private static CancellationTokenSource cancelTokens = new CancellationTokenSource();//used to cancel listeners

        public static void takeOff()
        {
            var packet = new byte[] { 0xcc, 0x58, 0x00, 0x7c, 0x68, 0x54, 0x00, 0xe4, 0x01, 0xc2, 0x16 };
            setPacketSequence(packet);
            setPacketCRCs(packet);
            client.Send(packet);
        }
        public static void land()
        {
            var packet = new byte[] { 0xcc, 0x60, 0x00, 0x27, 0x68, 0x55, 0x00, 0xe5, 0x01, 0x00, 0xba, 0xc7 };

            //payload
            packet[9] = 0x00;//todo. Find out what this is for.
            setPacketSequence(packet);
            setPacketCRCs(packet);
            client.Send(packet);
        }
        public static void requestIframe()
        {
            var iframePacket = new byte[] { 0xcc, 0x58, 0x00, 0x7c, 0x60, 0x25, 0x00, 0x00, 0x00, 0x6c, 0x95 };
            client.Send(iframePacket);
        }
        public static void setMaxHeight(int height)
        {
            //                                          crc    typ  cmdL  cmdH  seqL  seqH  heiL  heiH  crc   crc
            var packet = new byte[] { 0xcc, 0x68, 0x00, 0x27, 0x68, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5b, 0xc5 };

            //payload
            packet[9] = (byte)(height & 0xff);
            packet[10] = (byte)((height >> 8) & 0xff);

            setPacketSequence(packet);
            setPacketCRCs(packet);

            client.Send(packet);
        }
        public static void queryAttAngle()
        {
            var packet = new byte[] { 0xcc, 0x58, 0x00, 0x7c, 0x48, 0x59, 0x10, 0x06, 0x00, 0xe9, 0xb3 };
            setPacketSequence(packet);
            setPacketCRCs(packet);
            client.Send(packet);
        }
        public static void setAttAngle(float angle)
        {
            //                                          crc    typ  cmdL  cmdH  seqL  seqH  ang1  ang2 ang3  ang4  crc   crc
            var packet = new byte[] { 0xcc, 0x68, 0x00, 0x27, 0x68, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00,0x00, 0x00, 0x5b, 0xc5 };

            //payload
            byte[] bytes = BitConverter.GetBytes(angle);
            packet[9] = bytes[0];
            packet[10] = bytes[1];
            packet[11] = bytes[2];
            packet[12] = bytes[3];

            setPacketSequence(packet);
            setPacketCRCs(packet);

            client.Send(packet);

            Tello.queryAttAngle();//refresh
        }
        public static void setJpgQuality(int quality)
        {
            //                                          crc    typ  cmdL  cmdH  seqL  seqH  quaL  crc   crc
            var packet = new byte[] { 0xcc, 0x60, 0x00, 0x27, 0x68, 0x37, 0x00, 0x09, 0x00, 0x00, 0x5b, 0xc5 };

            //payload
            packet[9] = (byte)(quality & 0xff);

            setPacketSequence(packet);
            setPacketCRCs(packet);

            client.Send(packet);
        }

        /*TELLO_CMD_SWITCH_PICTURE_VIDEO
	    49 0x31
	    0x68
	    switching video stream mode
        data: u8 (1=video, 0=photo)
        */
        public static void setPicVidMode(int mode)
        {
            //                                          crc    typ  cmdL  cmdH  seqL  seqH  modL  crc   crc
            var packet = new byte[] { 0xcc, 0x60, 0x00, 0x27, 0x68, 0x31, 0x00, 0x00, 0x00, 0x00, 0x5b, 0xc5 };

            //payload
            packet[9] = (byte)(mode & 0xff);

            setPacketSequence(packet);
            setPacketCRCs(packet);

            client.Send(packet);
        }

        private static void setPacketSequence(byte[] packet)
        {
            packet[7] = (byte)(sequence & 0xff);
            packet[8] = (byte)((sequence >> 8) & 0xff);
            sequence++;
        }
        private static void setPacketCRCs(byte[] packet)
        {
            packet[7] = (byte)(sequence & 0xff);
            packet[8] = (byte)((sequence >> 8) & 0xff);
            sequence++;
        }
        public static void setEV(int ev)
        {
        }
        public static void setEIS(int eis)
        {
        }

        public static void setAxis(float[] axis)
        {
            joyAxis = axis.Take(5).ToArray(); ;
            //joyAxis[4] = axis[7];
            //joyAxis[3] = axis[11];
        }

        private static void disconnect()
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
        private static void connect()
        {
            //Console.WriteLine("Connecting to tello.");
            client = UdpUser.ConnectTo("192.168.10.1", 8889);

            connectionState = ConnectionState.Connecting;
            //send event
            onConnection(connectionState);

            byte[] connectPacket = Encoding.UTF8.GetBytes("conn_req:\x00\x00");
            connectPacket[connectPacket.Length - 2] = 0x96;
            connectPacket[connectPacket.Length - 1] = 0x17;
            client.Send(connectPacket);
        }

        private static void startListeners()
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
                                requestIframe();
                                //Console.WriteLine("Tello connected!");
                                continue;
                            }
                        }

                        int cmdId = ((int)received.bytes[5] | ((int)received.bytes[6] << 8));
                        if (cmdId == 86)//state command
                        {
                            //update
                            var newState = new FlyData();
                            newState.set(received.bytes.Skip(9).ToArray());
                            try
                            {
                                //fire update event.
                                onUpdate(newState);
                            }
                            catch (Exception ex)
                            {
                                //Fixed. Update errors do not cause disconnect.
                                Console.WriteLine("onUpdate error:" + ex.Message);
                                break;
                            }
                            //update current state.
                            state = newState;
                        }
                        if (cmdId == 4185)//att angle response
                        {
                            //var array = received.bytes.Skip(9).Take(4).Reverse().ToArray();
                            //float f = BitConverter.ToSingle(array, 0);
                            //Console.WriteLine(f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Receive thread error:" + ex.Message);
                        disconnect();
                        break;
                    }
                }
            }, token);
            //video server
            var videoServer = new UdpListener(6038);
            //var videoServer = new UdpListener(new IPEndPoint(IPAddress.Parse("192.168.10.2"), 6038));

            Task.Factory.StartNew(async () => {
                //Console.WriteLine("video:1");
                var started = false;

                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested)//handle canceling thread.
                            break;
                        var received = await videoServer.Receive();
                        if (received.bytes[2] == 0 && received.bytes[3] == 0 && received.bytes[4] == 0 && received.bytes[5] == 1)//Wait for first NAL
                        {
                            var nal = (received.bytes[6] & 0x1f);
                            //if (nal != 0x01 && nal!=0x07 && nal != 0x08 && nal != 0x05)
                            //    Console.WriteLine("NAL type:" +nal);
                            started = true;
                        }
                        if (started)
                        {
                            onVideoData(received.bytes);
                        }

                    }catch (Exception ex)
                    {
                        Console.WriteLine("Video receive thread error:" + ex.Message);
                        
                        //dont disconnect();
                        break;
                    }
                }
            }, token);

        }
        private static void startHeartbeat()
        {
            CancellationToken token = cancelTokens.Token;

            //heartbeat.
            Task.Factory.StartNew(async () =>
            {
                int tick = 0;
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
                    var limit = 1.0f;//Slow down while testing.
                    rx = rx * limit;
                    ry = ry * limit;

                    //Console.WriteLine(rx + " " + ry + " " + lx + " " + ly);
                    var packet = createJoyPacket(rx, ry, lx, ly, boost);
                    try
                    {
                        if (token.IsCancellationRequested)
                            break;
                        client.Send(packet);
                        tick++;
                        if (tick % 10 == 0)
                            requestIframe();

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
        public static void startConnecting()
        {
            //Thread to handle connecting.
            Task.Factory.StartNew(async () =>
            {
                var timeout = new TimeSpan(3000);//3 second connection timeout
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
        private static byte[] createJoyPacket(float fRx, float fRy, float fLx, float fLy, float speed)
        {
            //template joy packet.
            var packet = new byte[] { 0xcc, 0xb0, 0x00, 0x7f, 0x60, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x16, 0x01, 0x0e, 0x00, 0x25, 0x54 };

            short axis1 = (short)(660.0F * fRx + 1024.0F);//RightX center=1024 left =364 right =-364
            short axis2 = (short)(660.0F * fRy + 1024.0F);//RightY down =364 up =-364
            short axis3 = (short)(660.0F * fLy + 1024.0F);//LeftY down =364 up =-364
            short axis4 = (short)(660.0F * fLx + 1024.0F);//LeftX left =364 right =-364
            short axis5 = (short)(660.0F * speed + 1024.0F);//Speed. 

            if (speed > 0.1f)
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
            public int flyMode;
            public int height;
            public int verticalSpeed;
            public int flySpeed;
            public int eastSpeed;
            public int northSpeed;
            public int flyTime;

            public bool flying;//

            public bool downVisualState;
            public bool droneHover;
            public bool eMOpen;
            public bool onGround;
            public bool pressureState;

            public int batteryPercentage;//
            public bool batteryLow;
            public bool batteryLower;
            public bool batteryState;
            public bool powerState;
            public int droneBatteryLeft;
            public int droneFlyTimeLeft;


            public int cameraState;//
            public int electricalMachineryState;
            public bool factoryMode;
            public bool frontIn;
            public bool frontLSC;
            public bool frontOut;
            public bool gravityState;
            public int imuCalibrationState;
            public bool imuState;
            public int lightStrength;//
            public bool outageRecording;
            public int smartVideoExitMode;
            public int temperatureHeight;
            public int throwFlyTimer;
            public int wifiDisturb;//
            public int wifiStrength = 100;//
            public bool windState;//

            public void set(byte[] data)
            {
                var index = 0;
                height = data[index] | (data[index + 1] << 8); index += 2;
                northSpeed = (Int16)(data[index] | (data[index + 1] << 8)); index += 2;
                eastSpeed = (Int16)(data[index] | (data[index + 1] << 8)); index += 2;
                flySpeed = ((int)Math.Sqrt(Math.Pow(northSpeed, 2.0D) + Math.Pow(eastSpeed, 2.0D)));
                verticalSpeed = (Int16)(data[index] | (data[index + 1] << 8)); index += 2;// ah.a(paramArrayOfByte[6], paramArrayOfByte[7]);
                flyTime = data[index] | (data[index + 1] << 8); index += 2;// ah.a(paramArrayOfByte[8], paramArrayOfByte[9]);

                imuState = (data[index] >> 0 & 0x1) == 1 ? true : false;
                pressureState = (data[index] >> 1 & 0x1) == 1 ? true : false;
                downVisualState = (data[index] >> 2 & 0x1) == 1 ? true : false;
                powerState = (data[index] >> 3 & 0x1) == 1 ? true : false;
                batteryState = (data[index] >> 4 & 0x1) == 1 ? true : false;
                gravityState = (data[index] >> 5 & 0x1) == 1 ? true : false;
                windState = (data[index] >> 7 & 0x1) == 1 ? true : false;
                index += 1;

                //if (paramArrayOfByte.length < 19) { }
                imuCalibrationState = data[index]; index += 1;
                batteryPercentage = data[index]; index += 1;
                droneFlyTimeLeft = data[index] | (data[index + 1] << 8); index += 2;
                droneBatteryLeft = data[index] | (data[index + 1] << 8); index += 2;

                //index 17
                flying = (data[index] >> 0 & 0x1)==1?true:false;
                onGround = (data[index] >> 1 & 0x1) == 1 ? true : false;
                eMOpen = (data[index] >> 2 & 0x1) == 1 ? true : false;
                droneHover = (data[index] >> 3 & 0x1) == 1 ? true : false;
                outageRecording = (data[index] >> 4 & 0x1) == 1 ? true : false;
                batteryLow = (data[index] >> 5 & 0x1) == 1 ? true : false;
                batteryLower = (data[index] >> 6 & 0x1) == 1 ? true : false;
                factoryMode = (data[index] >> 7 & 0x1) == 1 ? true : false;
                index += 1;

                flyMode = data[index]; index += 1;
                throwFlyTimer = data[index]; index += 1;
                cameraState = data[index]; index += 1;

                //if (paramArrayOfByte.length >= 22)
                electricalMachineryState = data[index]; index += 1; //(paramArrayOfByte[21] & 0xFF);

                //if (paramArrayOfByte.length >= 23)
                frontIn = (data[index] >> 0 & 0x1) == 1 ? true : false;//22
                frontOut = (data[index] >> 1 & 0x1) == 1 ? true : false;
                frontLSC = (data[index] >> 2 & 0x1) == 1 ? true : false;
                index += 1;
                temperatureHeight = (data[index] >> 0 & 0x1);//23
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                var count = 0;
                foreach (System.Reflection.FieldInfo property in this.GetType().GetFields())
                {
                    sb.Append(property.Name);
                    sb.Append(": ");
                    sb.Append(property.GetValue(this));
                    if(count++%2==1)
                        sb.Append(System.Environment.NewLine);
                    else
                        sb.Append("      ");

                }

                return sb.ToString();
            }
        }

    }
}
 