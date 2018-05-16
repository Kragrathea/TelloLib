using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace aTello
{
    class GameController
    {
        //A class to represent the different Axes values
        public class AxesMapping
        {
            public static Axis AXIS_X = MotionEvent.AxisFromString("AXIS_X");
            public static Axis AXIS_Y = MotionEvent.AxisFromString("AXIS_Y");
            public static Axis AXIS_Z = MotionEvent.AxisFromString("AXIS_Z");
            public static Axis AXIS_RZ = MotionEvent.AxisFromString("AXIS_RZ");
            public static Axis AXIS_HAT_X = MotionEvent.AxisFromString("AXIS_HAT_X");
            public static Axis AXIS_HAT_Y = MotionEvent.AxisFromString("AXIS_HAT_Y");
            public static Axis AXIS_LTRIGGER = MotionEvent.AxisFromString("AXIS_LTRIGGER");
            public static Axis AXIS_RTRIGGER = MotionEvent.AxisFromString("AXIS_RTRIGGER");
            public static Axis AXIS_BRAKE = MotionEvent.AxisFromString("AXIS_BRAKE");
            public static Axis AXIS_GAS = MotionEvent.AxisFromString("AXIS_GAS");

            //Right Axis for xbox gamepads
            public static Axis AXIS_RX = MotionEvent.AxisFromString("AXIS_RX");
            public static Axis AXIS_RY = MotionEvent.AxisFromString("AXIS_RY");

            public static int size = 12;
            private static int motion_event;

            public static int OrdinalValue(Axis axis)
            {
                if (axis == AXIS_X)
                {
                    return 0;
                }
                else if (axis == AXIS_Y)
                {
                    return 1;
                }
                else if (axis == AXIS_Z)
                {
                    return 2;
                }
                else if (axis == AXIS_RZ)
                {
                    return 3;
                }
                else if (axis == AXIS_HAT_X)
                {
                    return 4;
                }
                else if (axis == AXIS_HAT_Y)
                {
                    return 5;
                }
                else if (axis == AXIS_LTRIGGER)
                {
                    return 6;
                }
                else if (axis == AXIS_RTRIGGER)
                {
                    return 7;
                }
                else if (axis == AXIS_BRAKE)
                {
                    return 8;
                }
                else if (axis == AXIS_GAS)
                {
                    return 9;
                }
                else if (axis == AXIS_RX)
                {
                    return 10;
                }
                else if (axis == AXIS_RY)
                {
                    return 11;
                }
                else
                {
                    return -1;
                }


            }
            public static Axis OrdinalValueAxis(int val)
            {
                switch (val)
                {
                    case 0:
                        return AXIS_X;
                    case 1:
                        return AXIS_Y;
                    case 2:
                        return AXIS_Z;
                    case 3:
                        return AXIS_RZ;
                    case 4:
                        return AXIS_HAT_X;
                    case 5:
                        return AXIS_HAT_Y;
                    case 6:
                        return AXIS_LTRIGGER;
                    case 7:
                        return AXIS_RTRIGGER;
                    case 8:
                        return AXIS_BRAKE;
                    case 9:
                        return AXIS_GAS;
                    case 10:
                        return AXIS_RX;
                    case 11:
                        return AXIS_RY;

                }
                return new Axis();
            }
            public AxesMapping(int motionevent)
            {
                motion_event = motionevent;
            }
            public static int getMotionEvent()
            {
                return AxesMapping.motion_event;
            }

        }
        //A class used to hold the button keycode mapping of the controller
        public class ButtonMapping
        {
            public static Keycode BUTTON_A = Keycode.ButtonA;
            public static Keycode BUTTON_B = Keycode.ButtonB;
            public static Keycode BUTTON_X = Keycode.ButtonX;
            public static Keycode BUTTON_Y = Keycode.ButtonY;
            public static Keycode BUTTON_Z = Keycode.ButtonZ;
            public static Keycode BUTTON_L1 = Keycode.ButtonL1;
            public static Keycode BUTTON_R1 = Keycode.ButtonR1;
            public static Keycode BUTTON_L2 = Keycode.ButtonL2;
            public static Keycode BUTTON_R2 = Keycode.ButtonR2;
            public static Keycode BUTTON_SELECT = Keycode.ButtonSelect;
            public static Keycode BUTTON_START = Keycode.ButtonStart;
            public static Keycode BUTTON_THUMBL = Keycode.ButtonThumbl;
            public static Keycode BUTTON_THUMBR = Keycode.ButtonThumbr;
            public static Keycode BACK = Keycode.Back;
            public static Keycode POWER = Keycode.ButtonMode;
            public static int size = 14;
            private static int key_code;

            public ButtonMapping(int keyCode)
            {
                key_code = keyCode;
            }

            public static int getKeyCode()
            {
                return key_code;
            }

            public static int OrdinalValue(Keycode key)
            {
                if (key == BUTTON_A)
                {
                    return 0;
                }
                else if (key == BUTTON_B)
                {
                    return 1;
                }
                else if (key == BUTTON_X)
                {
                    return 2;
                }
                else if (key == BUTTON_Y)
                {
                    return 3;
                }
                else if (key == BUTTON_L1)
                {
                    return 4;
                }
                else if (key == BUTTON_R1)
                {
                    return 5;
                }
                else if (key == BUTTON_L2)
                {
                    return 6;
                }
                else if (key == BUTTON_R2)
                {
                    return 7;
                }
                else if (key == BUTTON_Z)
                {
                    return 8;
                }
                else if (key == BUTTON_SELECT)
                {
                    return 9;
                }
                else if (key == BUTTON_START)
                {
                    return 10;
                }
                else if (key == BUTTON_THUMBL)
                {
                    return 11;
                }
                else if (key == BUTTON_THUMBR)
                {
                    return 12;
                }
                else if (key == BACK)
                {
                    return 13;
                }
                else if (key == POWER)
                {
                    return 14;
                }
                else
                {
                    return -1;
                }

            }

            public static Keycode OrdinalValueButton(int val)
            {
                switch (val)
                {
                    case 0:
                        return BUTTON_A;
                    case 1:
                        return BUTTON_B;
                    case 2:
                        return BUTTON_X;
                    case 3:
                        return BUTTON_Y;
                    case 4:
                        return BUTTON_L1;
                    case 5:
                        return BUTTON_R1;
                    case 6:
                        return BUTTON_L2;
                    case 7:
                        return BUTTON_R2;
                    case 8:
                        return BUTTON_SELECT;
                    case 9:
                        return BUTTON_START;
                    case 10:
                        return BUTTON_THUMBL;
                    case 11:
                        return BUTTON_THUMBR;
                    case 12:
                        return BACK;
                    case 13:
                        return POWER;
                }
                return new Keycode();
            }
        }
    }
}