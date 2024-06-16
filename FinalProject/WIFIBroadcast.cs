using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FinalProject
{
    [BroadcastReceiver]
    class WIFIBroadcast : BroadcastReceiver
    {
        Activity activity;

        public WIFIBroadcast(Activity activity)
        {
            this.activity = activity;
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

            // We just lost our wifi connection.
            // Evacuate to the main activity.
            if (wifiInfo.IpAddress == 0)
            {
                activity.StartActivity(new Intent(activity, typeof(MainActivity)));
            }
        }
    }
}