using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Com.Tomergoldst.Tooltips;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "ManageBidsActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class ManageBidsActivity : AppCompatActivity, ToolTipsManager.ITipListener
    {
        private NavigationMenu navigation_menu;

        private ListView bids_list_view;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_managebids);

            Title = "Manage Bids";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            bids_list_view = FindViewById<ListView>(Resource.Id.bidsListView);
            bids_list_view.ItemClick += Bids_list_view_ItemClick;

            FetchBids();
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        protected override void OnResume()
        {
            base.OnResume();

            navigation_menu.CreateBatteryBroadcast();
        }

        protected override void OnPause()
        {
            base.OnPause();

            navigation_menu.DestroyBatteryBroadcast();
        }

        private void Bids_list_view_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ManageBidAdapter adapter = (ManageBidAdapter)bids_list_view.Adapter;

            Intent intent = new Intent(this, typeof(AuctionOverviewActivity));
            intent.PutExtra("auction_id", adapter[e.Position].AuctionId);
            StartActivity(intent);
        }

        // Loads all bids.
        private void FetchBids()
        {
            Client runetime_client = RuntimeClient.Get();

            MySqlConnection db = Helper.DB.ConnectDatabase();

            MySqlCommand cmd = new MySqlCommand($"SELECT ab.id, `auction_id`, `bidder_phone`, ab.value, `bid_time`, `legacy_bid`, `bidder_acknowledged` " +
                $"FROM `ah_bids` ab INNER JOIN `ah_auctions` ON ah_auctions.id = ab.auction_id WHERE `bidder_phone` = { runetime_client.PhoneNumber } " +
                $"AND `legacy_bid` = 0 ORDER BY ah_auctions.status ASC, `bidder_acknowledged` ASC", db);

            List<Bid> bids = new List<Bid>();
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                // Loop through the rows of the reader
                while (reader.Read())
                {
                    long bid_row_id = reader.GetInt32("id");
                    long auction_id = reader.GetInt32("auction_id");

                    int bidder_phone = 0;
                    if (!reader.IsDBNull(reader.GetOrdinal("bidder_phone")))
                    {
                        bidder_phone = reader.GetInt32("bidder_phone");
                    }
                    
                    int value = reader.GetInt32("value");
                    long bid_time = reader.GetInt64("bid_time");
                    bool legacy_bid = reader.GetBoolean("legacy_bid");
                    bool bidder_acknowledged = reader.GetBoolean("bidder_acknowledged");

                    bids.Add(new Bid(bid_row_id, auction_id, bidder_phone, value, bid_time, legacy_bid, bidder_acknowledged));
                }
            }

            db.Close();

            bids_list_view.Adapter = new ManageBidAdapter(this, bids);
        }

        public void OnTipDismissed(View p0, int p1, bool p2)
        {
        }
    }
}