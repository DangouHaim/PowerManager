using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;

namespace Gamma
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern ushort GetAsyncKeyState(int vKey);

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            new Thread(() =>
            {
                while(true)
                {
                    if(IsKeyPushedDown(Keys.LControlKey) &&
                    IsKeyPushedDown(Keys.LShiftKey) &&
                    IsKeyPushedDown(Keys.Tab) &&
                    IsKeyPushedDown(Keys.S))
                    {
                        PowerManager.EnergySafeSwitch();
                    }
                    Thread.Sleep(50);
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        public static bool IsKeyPushedDown(Keys vKey)
        {
            return 0 != (GetAsyncKeyState((int)vKey) & 0x8000);
        }
    }
}