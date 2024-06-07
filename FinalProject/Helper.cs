 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content.Res;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Tomergoldst.Tooltips;
using MySqlConnector;

namespace FinalProject
{
    static class Helper
    {
        public class DB
        {
            public static string CLIENTS_TBL_NAME = "ah_clients";
            public static string AUCTIONS_TBL_NAME = "ah_auctions";
            public static string BIDS_TBL_NAME = "ah_bids";
            public static string IMAGES_TBL_NAME = "ah_images";
            public static string STATS_TBL_NAME = "ah_stats";

            /*public static class Connection
            {
                public static string Host { get; } = "geekgrove.co.il";
                public static string Database { get; } = "konlig";
                public static string User { get; } = "konlig";
                public static string Password { get; } = "xBA3XhxQbx52";
                public static uint Port { get; } = 3306;
            }*/

            public static class Connection
            {
                public static string Host { get; } = "191.96.229.55";
                public static string Database { get; } = "s89_ts3server";
                public static string User { get; } = "u89_lHJmI3zLqH";
                public static string Password { get; } = "!OY@e.x!9SY08aiHFLvU3BvM";
                public static uint Port { get; } = 3306;
            }
            
            private static string BuildConnectionString()
            {
                var builder = new MySqlConnectionStringBuilder
                {
                    Database = Connection.Database,
                    UserID = Connection.User,
                    Password = Connection.Password,
                    Server = Connection.Host,
                    Port = 3306,
                };

                return builder.ConnectionString;
            }

            public static MySqlConnection ConnectDatabase()
            {
                MySqlConnection conn = new MySqlConnection(BuildConnectionString());

                conn.Open();

                return conn;
            }
        }

        public class SP
        {
            private static ISharedPreferences sp;

            private static void Validate(Context context)
            {
                if (sp == null)
                {
                    sp = context.GetSharedPreferences("remember_me", FileCreationMode.Private);
                }
            }

            public static ISharedPreferences Get(Context context)
            {
                Validate(context);

                return sp;
            }
        }
        
        public static void SetButtonState(Context context, Button button, bool enabled)
        {
            button.Enabled = enabled;
            button.Background = ResourcesCompat.GetDrawable(context.Resources, enabled ? Resource.Drawable.active_button : Resource.Drawable.passive_button, null);
            button.SetTextColor(enabled ? new Color(255, 255, 255) : new Color(111, 125, 134));
        }

        public static void FireInputError(Context context, ViewGroup root, View view, string text)
        {
            TooltipEx tooltip = new TooltipEx(context, view, root, text, ToolTip.PositionAbove, 5);

            tooltip.builder.SetBackgroundColor(new Color(63, 113, 132));
            tooltip.builder.SetTextSize(12); // 12 as in "sp".

            tooltip.Display();
        }

        public static string FormatMinutes(int minutes)
        {
            if (minutes < 0)
            {
                return "";
            }

            string format;
            format = "";

            int totalMinutes = minutes % 60;
            int totalHours = (minutes / 60) % 24;
            int totalDays = (minutes / (60 * 24)) % 7;
            int totalWeeks = minutes / (60 * 24 * 7);
            
            if (totalWeeks > 0)
            {
                format = $"{totalWeeks}w"; /*, totalWeeks != 1 ? "s" : ""*/
            }
            
            if (totalDays > 0)
            {
                format += $"{(!string.IsNullOrEmpty(format) ? ", " : "")}{totalDays}d";
            }

            if (totalHours > 0)
            {
                format += $"{(!string.IsNullOrEmpty(format) ? ", " : "")}{totalHours}h";
            }

            if (totalMinutes > 0)
            {
                format += $"{(!string.IsNullOrEmpty(format) ? ", " : "")}{totalMinutes}m";
            }

            if (string.IsNullOrEmpty(format))
            {
                format = "Couple of seconds";
            }

            return format;
        }

        public static string BitmapToBase64(Bitmap bitmap)
        {
            string str = "";
            using (var stream = new MemoryStream())
            {
                bitmap.Compress(Bitmap.CompressFormat.Png, 0, stream);
                var bytes = stream.ToArray();
                str = Convert.ToBase64String(bytes);
            }
            return str;
        }

        public static Bitmap Base64ToBitmap(String base64String)
        {
            byte[] imageAsBytes = Base64.Decode(base64String, Base64Flags.Default);
            return BitmapFactory.DecodeByteArray(imageAsBytes, 0, imageAsBytes.Length);
        }

        public static long GetUnixTimeStamp()
        {
            // Current unix timestamp
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        }
    }
}