using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "StatsActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class StatsActivity : AppCompatActivity
    {
        private NavigationMenu navigation_menu;

        private ToggleButton switch_mode_button;
        private TextView stats_tv;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_stats);

            Title = "Statistics";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            switch_mode_button = FindViewById<ToggleButton>(Resource.Id.switchModeButton);
            switch_mode_button.Click += Switch_mode_button_Click;

            stats_tv = FindViewById<TextView>(Resource.Id.statsTextView);

            PopulateStats();
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        private void Switch_mode_button_Click(object sender, EventArgs e)
        {
            PopulateStats();
        }
        
        private void PopulateStats()
        {
            // "switch_mode_button.Checked"
            // If true meaning seller stats are displayed,
            // otherwise buyer stats are displayed.

            Client runtime_client = RuntimeClient.Get();

            // Seller stats.
            if (switch_mode_button.Checked)
            {
                stats_tv.Text = $"" +
                    $"• Auctions created: {runtime_client.Statistics.AuctionsCreated.ToString("N0")}\n" +
                    $"• Auctions completed with bids: {runtime_client.Statistics.AuctionsCompletedWithBids.ToString("N0")}\n" +
                    $"• Auctions completed without bids: {runtime_client.Statistics.AuctionsCompletedWithoutBids.ToString("N0")}\n\n" +
                    $"• Highest auction held: {runtime_client.Statistics.HighestAuctionHeld.ToString("N0")}\n" +
                    $"• Total coins earned: {runtime_client.Statistics.TotalCoinsEarned.ToString("N0")}\n" +
                    $"• Coins spent on fees: {runtime_client.Statistics.CoinsSpentOnFees.ToString("N0")}";
            }
            // Buyer stats.
            else
            {
                stats_tv.Text = $"" +
                    $"• Auctions won: {runtime_client.Statistics.AuctionsWon.ToString("N0")}\n" +
                    $"• Total bids: {runtime_client.Statistics.TotalBids.ToString("N0")}\n\n" +
                    $"• Highest bid: {runtime_client.Statistics.HighestBid.ToString("N0")}\n" +
                    $"• Coins spent: {runtime_client.Statistics.CoinsSpent.ToString("N0")}";
            }

            ColorStats();
        }

        private void ColorStats()
        {
            SpannableString spannableText = new SpannableString(stats_tv.Text);
            for (int i = 0; i < stats_tv.Text.Length; i++)
            {
                int start = stats_tv.Text.IndexOf(':', i);
                if (start == -1)
                {
                    break;
                }

                int end = stats_tv.Text.IndexOf("\n", start);
                if (end == -1)
                {
                    end = stats_tv.Text.Length;
                }

                // Skip the space, and properly set "i".
                start++;
                i = end;

                spannableText.SetSpan(new ForegroundColorSpan(new Color(0, 133, 159)), start, end, SpanTypes.Composing);
            }

            stats_tv.TextFormatted = spannableText;
        }
    }
}