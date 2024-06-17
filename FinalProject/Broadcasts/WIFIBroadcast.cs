using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

namespace FinalProject
{
    [BroadcastReceiver]
    class WIFIBroadcast : BroadcastReceiver
    {
        private int last_ip_addr = -1;

        public WIFIBroadcast()
        {
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            if (!action.Equals("android.net.wifi.STATE_CHANGE"))
            {
                return;
            }
            
            WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);

            WifiInfo wifiInfo = wifiManager.ConnectionInfo;

            // We just lost/regained our wifi connection.
            // Refresh to the main activity.
            if (last_ip_addr != -1)
            {
                // Regained internet connection!
                if (wifiInfo.IpAddress != 0 && last_ip_addr == 0)
                {
                    // Wait 1s since the internet isn't able to instantly regain db connection.
                    Timer t = new Timer(TimerCallback, null, 1000, 0);
                }
                // Lost internet connection :(
                else if (wifiInfo.IpAddress == 0 && last_ip_addr != 0)
                {
                    // Evacuate to main activity.
                    Activity a = Platform.CurrentActivity;
                    a.StartActivity(new Intent(a, typeof(MainActivity)));
                }
            }

            last_ip_addr = wifiInfo.IpAddress;
        }

        private static void TimerCallback(Object o)
        {
            // Evacuate to main activity.
            Activity a = Platform.CurrentActivity;
            a.StartActivity(new Intent(a, typeof(MainActivity)));
        }
    }
}