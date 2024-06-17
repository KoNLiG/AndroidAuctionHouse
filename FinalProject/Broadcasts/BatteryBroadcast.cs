using Android.App;
using Android.Content;
using Android.Graphics;
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

            // Modify the display TextView color
            // according to the current battery percentage.
            int[] color = GetColorFromPercent(battery);
            display_tv.SetTextColor(new Color(color[0], color[1], color[2]));
        }

        private int[] GetColorFromPercent(int percent)
        {
            int red = (int)(percent < 50 ? 255 : Math.Round(256 - (percent - 50) * 5.12));
            int green = (int)(percent > 50 ? 255 : Math.Round((percent) * 5.12));

            int[] color = new int[3];
            color[0] = red;
            color[1] = green;
            color[2] = 0;

            return color;
        }
    }
}