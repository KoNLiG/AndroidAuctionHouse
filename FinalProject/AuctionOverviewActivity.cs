using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using CN.Iwgang.Countdownview;
using Google.Android.Material.Snackbar;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "AuctionOverviewActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class AuctionOverviewActivity : AppCompatActivity
    {
        private NavigationMenu navigation_menu;

        private RelativeLayout main_layout;

        private ListView bids_list_view;

        // Function button has 2 functions:
        // 1. Place a bid for aunction type sales only.
        // 2. Purchase a BIN item.
        private Button function_button, cancel_button;

        // Auction identifier we're overviewing.
        private long auction_id;

        // True if the user got redirected to this page from listing
        // a new auction, false otherwise.
        private bool is_from_listing;

        // Auction object filled with the data of 'auction_id'.
        //private Auction auction;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_auctionoverview);

            Title = "Auction House";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            main_layout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);

            if ((auction_id = Intent.GetLongExtra("auction_id", 0)) == 0)
            {
                // No auction id was sent, abort.
                OnBackPressed();
                return;
            }

            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // An auction id was sent, but it was invalid.
                OnBackPressed();
                return;
            }

            is_from_listing = Intent.GetBooleanExtra("from_listing", false);

            PopulatePageData(auction);

            function_button = FindViewById<Button>(Resource.Id.functionButton);

            // "textAllCaps" is enabled.
            function_button.Text = auction.Type == AuctionType.BIN ? "purchase" : "place a bid";
            function_button.Click += Function_button_Click;

            cancel_button = FindViewById<Button>(Resource.Id.cancelButton);
            cancel_button.Click += Cancel_button_Click;

            // Disable the cancel button in 2 conditions:
            // 1. Owners aren't matching.
            // 2. The item type is an "auction" and it has ATLEAST 1 bid.
            Client runtime_client = RuntimeClient.Get();

            if(runtime_client != auction.OwnerPhone || auction.Bids != null)
            {
                Helper.SetButtonState(this, cancel_button, false);
            }
        }

        // 1. Called once the user has triggered the "back" operation by left swiping, etc..
        //    If the navigation menu is open, override the action and close it first.
        // 
        // 2. If we didn't close any drawer, and 'is_from_listing' is true, 
        //    reditect the user to 'MainActivity' page. 
        public override void OnBackPressed()
        {
            if (!navigation_menu.OnBackPressed(base.OnBackPressed) && is_from_listing)
            {
                StartActivity(new Intent(this, typeof(MainActivity)));
            }
        }

        private void PopulatePageData(Auction auction)
        {
            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;
            // -------------------------

            // Contains both the item name and the item price/starting bid/top bid.
            TextView title_tv = FindViewById<TextView>(Resource.Id.titleText);

            Bid top_bid = auction.FindTopBid();
            string coins_prefix = auction.Type == AuctionType.BIN ? "" : top_bid == null ? "Starting bid is " : "Top bid is ";
            int coins = auction.Type == AuctionType.BIN ? auction.Value : top_bid == null ? auction.Value : top_bid.Value;

            title_tv.Text = $"╭ {auction.ItemName}\n╰┄ {coins_prefix}{coins.ToString("N0")} coins";

            Client owner = (auction.OwnerPhone != 0 ? new Client(auction.OwnerPhone) : null);
            string owner_name = $"◾ Seller: " + (owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unavailable");

            string description = string.IsNullOrEmpty(auction.ItemDescription) ? "No description available" : auction.ItemDescription;

            TextView item_desc_tv = FindViewById<TextView>(Resource.Id.itemDescText);
            item_desc_tv.Text = $"{owner_name}\n\n{description}";

            Gallery images_gallery = FindViewById<Gallery>(Resource.Id.imagesGallerys);
            images_gallery.Adapter = new StaticImageAdapter(this, auction.Images);
            images_gallery.SetSpacing(75);

            // Hook gallery click only if there are available images,
            // to avoid null exception later... 
            if (auction.Images != null)
            {
                images_gallery.ItemClick += Images_gallery_ItemClick;
            }

            bids_list_view = FindViewById<ListView>(Resource.Id.bidsListView);

            // Hide any bid features if the auction type is bin. (non bids related sale)
            if (auction.Type == AuctionType.BIN)
            {
                bids_list_view.Visibility = ViewStates.Invisible;
                FindViewById<TextView>(Resource.Id.bidsTitle).Visibility = ViewStates.Invisible;
            }
            else
            {
                FetchBids();
            }

            CountdownView time_left_countdown = FindViewById<CountdownView>(Resource.Id.timeLeftCountdown);

            // 'CountdownView:Start' function expects an "ms" input,
            // therefore we have to multiply the remaining time by 1000.
            time_left_countdown.Start(auction.RemainingTime * 1000);
        }

        private void Function_button_Click(object sender, EventArgs e)
        {
            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
                return;
            }

            if (auction.Type == AuctionType.BIN)
            {
                // stuff...
                return;
            }


        }

        private void Cancel_button_Click(object sender, EventArgs e)
        {
            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
                return;
            }

            // Someone placed a bid, abort and disable the cancel button.
            if (auction.Bids != null)
            {
                Helper.SetButtonState(this, cancel_button, false);
                FetchBids(); // Refresh the bids view.
                return;
            }

            // The auction has probably expired or was bought,
            // abort and disable the cancel button.
            if (auction.Status != AuctionStatus.Running)
            {
                // Evacuate the client.
                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();

                // Notify the client.
                Toast.MakeText(this, $"Auction {auction.ItemName} is no longer running", ToastLength.Long).Show();
                return;
            }

            // Cancel the auction.
            auction.Status = AuctionStatus.Canceled;
            auction.EndTime = Helper.GetUnixTimeStamp();

            // Evacuate the client.
            StartActivity(new Intent(this, typeof(MainActivity)));
            FinishAffinity();

            // Notify the client.
            Toast.MakeText(this, $"Auction {auction.ItemName} has been successfully canceled", ToastLength.Long).Show();
        }

        private void Images_gallery_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
                return;
            }

            MainActivity.DisplayImagesDialog(this, auction);
        }

        // Loads all bids.
        private void FetchBids()
        {
            MySqlConnection db = Helper.DB.ConnectDatabase();

            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{Helper.DB.BIDS_TBL_NAME}` WHERE `auction_id` = {auction_id} ORDER BY `bid_time` DESC", db);

            List<Bid> bids = new List<Bid>();
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                // Loop through the rows of the reader
                while (reader.Read())
                {
                    long bid_row_id = reader.GetInt32("id");

                    int bidder_phone = 0;
                    if (!reader.IsDBNull(reader.GetOrdinal("bidder_phone")))
                    {
                        bidder_phone = reader.GetInt32("bidder_phone");
                    }

                    int value = reader.GetInt32("value");
                    long bid_time = reader.GetInt64("bid_time");
                    bool legacy_bid = reader.GetBoolean("legacy_bid");


                    bids.Add(new Bid(bid_row_id, bidder_phone, value, bid_time, legacy_bid));
                }
            }

            db.Close();

            bids_list_view.Adapter = new BidAdapter(this, bids);
        }
    }
}