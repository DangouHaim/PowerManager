using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Gamma
{
    class PowerManager
    {
        private static object enegySafeInProcess = new object();
        private static bool turnedMonitorOn = true;
        private static bool energySafeOn = false;
        private static ushort normalScreemGamma = 128;
        private static ushort currentScreemGamma = 128;

        private const int MOVE = 0x0001;
        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool BlockInput([In, MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        public static extern int SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(IntPtr dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Blue;
        }

        public static bool IsTurnedMonitorOn()
        {
            return turnedMonitorOn;
        }
        public static void SetMonitorGamma(ushort Value)
        {
            currentScreemGamma = Value;
            IntPtr DC = GetDC(GetDesktopWindow());

            if (DC != null)
            {

                RAMP _Rp = new RAMP();

                _Rp.Blue = new ushort[256];
                _Rp.Green = new ushort[256];
                _Rp.Red = new ushort[256];

                for (int i = 1; i < 256; i++)
                {
                    int value = i * (Value + 128);

                    if (value > 65535)
                        value = 65535;

                    _Rp.Red[i] = _Rp.Green[i] = _Rp.Blue[i] = Convert.ToUInt16(value);
                }

                SetDeviceGammaRamp(DC, ref _Rp);
            }
        }

        public static ushort GetMonitorGamma()
        {
            return currentScreemGamma;
        }

        public static void SetMonitorBrightness(ushort brightness)
        {
            using (var mclass = new ManagementClass("WmiMonitorBrightnessMethods"))
            {
                mclass.Scope = new ManagementScope(@"\\.\root\wmi");
                using (var instances = mclass.GetInstances())
                {
                    foreach (ManagementObject instance in instances)
                    {
                        object[] args = new object[] { 1, brightness };
                        instance.InvokeMethod("WmiSetBrightness", args);
                    }
                }
            }
        }

        public static ushort GetMonitorBrightness()
        {
            using (var mclass = new ManagementClass("WmiMonitorBrightness"))
            {
                mclass.Scope = new ManagementScope(@"\\.\root\wmi");
                using (var instances = mclass.GetInstances())
                {
                    foreach (ManagementObject instance in instances)
                    {
                        return (byte)instance.GetPropertyValue("CurrentBrightness");
                    }
                }
            }
            return 0;
        }

        public static void TurnMonitorOff()
        {
            BlockInput(true);
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
        }

        public static void TurnMonitorOn()
        {
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, -1);
            BlockInput(false);
        }

        public static void ShutDownDevice(string name)
        {
            foreach (var v in FindDevices(name))
            {
                //x32
                Process devManViewProc = new Process();
                devManViewProc.StartInfo.FileName = @"DevManView32.exe";
                devManViewProc.StartInfo.Arguments = "/disable \"" + v + "\"";
                devManViewProc.Start();
                devManViewProc.WaitForExit();
                //x64
                devManViewProc = new Process();
                devManViewProc.StartInfo.FileName = @"DevManView.exe";
                devManViewProc.StartInfo.Arguments = "/disable \"" + v + "\"";
                devManViewProc.Start();
                devManViewProc.WaitForExit();
            }
        }

        public static void ShutUpDevice(string name)
        {
            foreach (var v in FindDevices(name))
            {
                //x32
                Process devManViewProc = new Process();
                devManViewProc.StartInfo.FileName = @"DevManView32.exe";
                devManViewProc.StartInfo.Arguments = "/enable \"" + v + "\"";
                devManViewProc.Start();
                devManViewProc.WaitForExit();
                while (!devManViewProc.HasExited)
                {
                    Thread.Sleep(100);
                }
                //x64
                devManViewProc = new Process();
                devManViewProc.StartInfo.FileName = @"DevManView.exe";
                devManViewProc.StartInfo.Arguments = "/enable \"" + v + "\"";
                devManViewProc.Start();
                devManViewProc.WaitForExit();
                Thread.Sleep(300);
                while (!devManViewProc.HasExited)
                {
                    Thread.Sleep(100);
                }
            }
        }

        public static string[] FindDevices(string name)
        {
            //x32
            Process devManViewProc = new Process();
            devManViewProc.StartInfo.FileName = "DevManView32.exe";
            devManViewProc.StartInfo.Arguments = "/stab \"devices\"";
            devManViewProc.Start();
            devManViewProc.WaitForExit();
            //x64
            devManViewProc = new Process();
            devManViewProc.StartInfo.FileName = @"DevManView.exe";
            devManViewProc.StartInfo.Arguments = "/stab \"devices\"";
            devManViewProc.Start();
            devManViewProc.WaitForExit();

            List<string> devices = new List<string>();
            using (FileStream f = new FileStream("devices", FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(f))
                {
                    while(!sr.EndOfStream)
                    {
                        string s = sr.ReadLine().Split('	')[0];
                        if(s.ToLower().IndexOf(name.ToLower()) > -1)
                        {
                            if(devices.IndexOf(s) < 0)
                            {
                                devices.Add(s);
                            }
                        }
                    }
                }
            }

            return devices.ToArray();
        }

        public static void TurnUsbOff()
        {
            ShutDownDevice("USB Input Device");
        }

        public static void TurnUsbOn()
        {
            ShutUpDevice("USB Input Device");
        }

        public static void TurnSoundOff()
        {
            ShutDownDevice("High Definition Audio Controller");
        }

        public static void TurnSoundOn()
        {
            ShutUpDevice("High Definition Audio Controller");
        }

        public static void TurnBluetoothOff()
        {
            ShutDownDevice("Bluetooth 4.0");
        }

        public static void TurnBluetoothOn()
        {
            ShutUpDevice("Bluetooth 4.0");
        }

        public static void TurnWiFiOff()
        {
            ShutDownDevice("Network Adapter");
        }

        public static void TurnWiFiOn()
        {
            ShutUpDevice("Network Adapter");
        }

        public static void EnergySafeOn()
        {
            SetMonitorBrightness(0);
            SetMonitorGamma(0);
            TurnUsbOff();
            TurnSoundOff();
            TurnWiFiOff();
            TurnBluetoothOff();
            energySafeOn = true;
        }

        public static void EnergySafeOff()
        {
            SetMonitorGamma(normalScreemGamma);
            SetMonitorBrightness(100);
            TurnUsbOn();
            TurnSoundOn();
            TurnWiFiOn();
            TurnBluetoothOn();
            energySafeOn = false;
        }

        public static void EnergySafeSwitch()
        {
            if(!energySafeOn)
            {
                EnergySafeOn();
            }
            else
            {
                EnergySafeOff();
            }
        }
    }
}
