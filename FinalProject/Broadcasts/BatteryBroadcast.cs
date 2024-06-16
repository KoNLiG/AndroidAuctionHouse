using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBatteryChanged })]
    class BatteryBroadcast : BroadcastReceiver
    {
        private TextView display_tv;

        public BatteryBroadcast()
        {
        }

        public BatteryBroadcast(TextView display_tv)
        {
            this.display_tv = display_tv;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            int battery = intent.GetIntExtra("level", 0);

            display_tv.Text = $"{battery}%";
        }
    }
}