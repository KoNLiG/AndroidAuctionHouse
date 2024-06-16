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
using CN.Iwgang.Countdownview;
using Com.Spark.Submitbutton;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextField;
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

        // Properties related to the bid setup.
        private Dialog dialog_bid;
        private TextInputLayout bid_layout;
        
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

            Client runtime_client = RuntimeClient.Get();

            function_button = FindViewById<Button>(Resource.Id.functionButton);

            // "textAllCaps" is enabled.
            function_button.Text = auction.Type == AuctionType.BIN ? "purchase" : "place a bid";
            function_button.Click += Function_button_Click;
            Helper.SetButtonState(this, function_button, CanAffordAuction(auction, runtime_client));

            cancel_button = FindViewById<Button>(Resource.Id.cancelButton);
            cancel_button.Click += Cancel_button_Click;

            // Disable the cancel button in 2 conditions:
            // 1. Owners aren't matching.
            // 2. The item type is an "auction" and it has ATLEAST 1 bid.

            if (runtime_client != auction.OwnerPhone || auction.Bids != null)
            {
                Helper.SetButtonState(this, cancel_button, false);
            }

            PopulatePageData(auction, runtime_client);
        }

        protected override void OnResume()
        {
            base.OnResume();

            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // An auction id was sent, but it was invalid.
                OnBackPressed();
                return;
            }

            PopulatePageData(auction, RuntimeClient.Get());
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        private void PopulatePageData(Auction auction, Client runtime_client)
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

            string owner_name = "";
            if (auction.OwnerPhone == runtime_client.PhoneNumber)
            {
                owner_name = "This is your own auction!";
            }
            else
            {
                owner_name = $"◾ Seller: " + (owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unavailable");
            }

            string description = string.IsNullOrEmpty(auction.ItemDescription) ? "No description available" : auction.ItemDescription;

            SpannableString spannableText = new SpannableString($"{owner_name}\n\n{description}");

            // Color in green if this is our own auction!
            if (auction.OwnerPhone == runtime_client.PhoneNumber)
            {
                spannableText.SetSpan(new ForegroundColorSpan(new Color(144, 238, 144)), 0, owner_name.Length, 0);
            }

            TextView item_desc_tv = FindViewById<TextView>(Resource.Id.itemDescText);
            item_desc_tv.TextFormatted = spannableText;

            Gallery images_gallery = FindViewById<Gallery>(Resource.Id.imagesGallerys);
            images_gallery.Adapter = new StaticImageAdapter(this, auction.Images);
            images_gallery.SetSpacing(75);

            // Hook gallery click only if there are available images,
            // to avoid null exception later... 
            if (auction.Images != null)
            {
                images_gallery.ItemClick += Images_gallery_ItemClick;
            }

            TextView bids_title_tv = FindViewById<TextView>(Resource.Id.bidsTitle);
            bids_list_view = FindViewById<ListView>(Resource.Id.bidsListView);

            // Hide any bid features if the auction type is bin. (non bids related sale)
            if (auction.Type == AuctionType.BIN)
            {
                bids_list_view.Visibility = ViewStates.Invisible;
                bids_title_tv.Visibility = ViewStates.Invisible;
            }
            else
            {
                FetchBids(auction.Status != AuctionStatus.Running);
            }

            CountdownView time_left_countdown = FindViewById<CountdownView>(Resource.Id.timeLeftCountdown);
            if (time_left_countdown != null)
            {
                time_left_countdown.CountdownEnd += Time_left_countdown_CountdownEnd;

                // 'CountdownView:Start' function expects an "ms" input,
                // therefore we have to multiply the remaining time by 1000.
                time_left_countdown.Start(auction.RemainingTime * 1000);
            }

            // Handle "Manage Auctions" referals.
            if (auction.Status != AuctionStatus.Running)
            {
                // Remove the cancel button.
                if (cancel_button != null)
                {
                    ((ViewGroup)cancel_button.Parent).RemoveView(cancel_button);
                    cancel_button = null;
                }

                // Remove the countdown timer.
                if (time_left_countdown != null)
                {
                    ((ViewGroup)time_left_countdown.Parent).RemoveView(time_left_countdown);
                }

                // Fix "below" rule.
                LinearLayout buttons_layout = FindViewById<LinearLayout>(Resource.Id.buttonsLayout);

                RelativeLayout.LayoutParams layout_params = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                layout_params.AddRule(LayoutRules.Below, Resource.Id.countdownTitle);

                buttons_layout.LayoutParameters = layout_params;

                TextView countdown_title_tv = FindViewById<TextView>(Resource.Id.countdownTitle);

                DateTimeOffset dt_offset = DateTimeOffset.FromUnixTimeSeconds(auction.EndTime);
                countdown_title_tv.Text = $"Ended at: {dt_offset.ToString("dd/MM/yyy - HH:mm:ss")}";

                // Display the buyer contact info. IF BIN
                if (auction.Type == AuctionType.BIN)
                {
                    if (auction.BuyerPhone == 0)
                    {
                        countdown_title_tv.Text += "\n\nSold to: No one :(";
                    }
                    else
                    {
                        Client buyer = new Client(auction.BuyerPhone);

                        countdown_title_tv.Text += $"\n\nSold to: {buyer.FirstName} {buyer.LastName} [0{buyer.PhoneNumber}]";
                    }
                }

                bids_title_tv.Text = "bid history";

                // True if this is the owner and the owner haven't collected the auction.
                if (auction.OwnerPhone == runtime_client.PhoneNumber && !auction.OwnerAcknowledged)
                {
                    // Handle the "collect" button.
                    function_button.Text = auction.BuyerPhone == 0 ? "Dismiss Auction" : "Collect Auction";
                    function_button.Click -= Function_button_Click;
                    function_button.Click += Collect_button_Click;

                    Helper.SetButtonState(this, function_button, true);
                }
                // True if this is the buyer and the buyer haven't acknowledged the auction.
                else if (auction.BuyerPhone == runtime_client.PhoneNumber && !auction.BuyerAcknowledged)
                {
                    // Handle the "collect" button.
                    function_button.Text = "Dismiss Auction";
                    function_button.Click -= Function_button_Click;
                    function_button.Click += Collect_button_Click;

                    Helper.SetButtonState(this, function_button, true);

                    item_desc_tv.Text += $"\nContact info: 0{owner.PhoneNumber}";
                }
                else
                {
                    // Remove the function button.
                    if (function_button != null)
                    {
                        ((ViewGroup)function_button.Parent).RemoveView(function_button);
                        function_button = null;
                    }
                }

                // If we're the buyer, only display the relevant data TO US.
                if (auction.BuyerPhone == runtime_client.PhoneNumber)
                {
                    List<Bid> bids = new List<Bid>();
                    bids.Add(auction.Bids[0]);
                    bids_list_view.Adapter = new BidAdapter(this, bids);
                }
            }
        }

        private void Time_left_countdown_CountdownEnd(object sender, CountdownView.CountdownEndEventArgs e)
        {
            StartActivity(new Intent(this, typeof(MainActivity)));
        }

        private void Function_button_Click(object sender, EventArgs e)
        {
            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
                return;
            }

            // Retrieve and validate the runtime client.
            Client runtime_client = RuntimeClient.Get();
            if (runtime_client == null)
            {
                // Evacuate the client.
                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();

                // Notify the client.
                Toast.MakeText(this, "Failed to authorize your account.", ToastLength.Long).Show();
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
                Toast.MakeText(this, $"Auction {auction.ItemName} is no longer running.", ToastLength.Long).Show();
                return;
            }

            if (!CanAffordAuction(auction, runtime_client))
            {
                Helper.SetButtonState(this, function_button, false);

                // Notify the client.
                Snackbar snackbar = Snackbar.Make(main_layout, $"You cannot {(auction.Type == AuctionType.BIN ? "afford" : "bid in")} this auction!", Snackbar.LengthLong);
                snackbar.SetTextColor(new Color(218, 55, 60));
                snackbar.Show();

                return;
            }

            // "BIN"
            if (auction.Type == AuctionType.BIN)
            {
                // Buy the item!
                auction.Status = AuctionStatus.Bought;
                auction.EndTime = Helper.GetUnixTimeStamp();
                auction.BuyerPhone = runtime_client.PhoneNumber;

                // Subtract coins from the buyer.
                runtime_client.Coins -= auction.Value;

                // Update stats.
                runtime_client.Statistics.AuctionsWon++;
                runtime_client.Statistics.CoinsSpent += auction.Value;

                // Evacuate the client.
                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();

                // Notify the client.
                Toast.MakeText(this, $"Auction {auction.ItemName} has been successfully purchased.", ToastLength.Long).Show();
            }
            // "Auction"
            else
            {
                DisplayBidDialog(auction);
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
                Toast.MakeText(this, $"Auction {auction.ItemName} is no longer running.", ToastLength.Long).Show();
                return;
            }

            // Cancel the auction.
            auction.Status = AuctionStatus.Canceled;
            auction.EndTime = Helper.GetUnixTimeStamp();

            // Evacuate the client.
            StartActivity(new Intent(this, typeof(MainActivity)));
            FinishAffinity();

            // Notify the client.
            Toast.MakeText(this, $"Auction {auction.ItemName} has been successfully canceled.", ToastLength.Long).Show();
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
        private void FetchBids(bool ended = false)
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
                    long auction_id = reader.GetInt32("auction_id");

                    int bidder_phone = 0;
                    if (!reader.IsDBNull(reader.GetOrdinal("bidder_phone")))
                    {
                        bidder_phone = reader.GetInt32("bidder_phone");
                    }

                    int value = reader.GetInt32("value");
                    long bid_time = reader.GetInt64("bid_time");
                    bool legacy_bid = reader.GetBoolean("legacy_bid");

                    bids.Add(new Bid(bid_row_id, auction_id, bidder_phone, value, bid_time, legacy_bid));
                }
            }

            db.Close();

            bids_list_view.Adapter = new BidAdapter(this, bids, ended);
        }

        private bool CanAffordAuction(Auction auction, Client runtime_client)
        {
            // Clients cannot buy their own items.
            if (runtime_client == auction.OwnerPhone)
            {
                return false;
            }

            if (auction.Type == AuctionType.BIN)
            {
                return (auction.Value <= runtime_client.Coins);
            }

            Bid top_bid = auction.FindTopBid();
            return (top_bid == null || top_bid.Value <= runtime_client.Coins);
        }

        public void DisplayBidDialog(Auction auction)
        {
            dialog_bid = new Dialog(this);

            dialog_bid.SetContentView(Resource.Layout.dialog_bid);
            dialog_bid.SetTitle("Place a Bid");
            dialog_bid.SetCancelable(true);

            // Setup the title.
            TextView title_tv = dialog_bid.FindViewById<TextView>(Resource.Id.textViewTitle);
            title_tv.Text = $"• Please enter your desired bid amount for {auction.ItemName}.\n\n• Note that when bidding close to the auction end time, its duration will be extended.";

            // Setup the bid edittext.
            bid_layout = dialog_bid.FindViewById<TextInputLayout>(Resource.Id.bidLayout);
            bid_layout.EditText.Touch += Bid_layout_Touch;

            Bid top_bid = auction.FindTopBid();
            if (top_bid != null)
            {
                bid_layout.HelperText = $"* Minimum value for outbidding is {top_bid.OutBid.ToString("N0")} coins";
            }
            else
            {
                bid_layout.HelperText = $"* Starting bid is {auction.Value.ToString("N0")} coins";
            }

            // Setup the place button.
            SubmitButton place_button = dialog_bid.FindViewById<SubmitButton>(Resource.Id.placeButton);
            place_button.Touch += Place_button_Touch;
            place_button.Click += Place_button_Click;

            dialog_bid.Show();
        }

        // Required since blocking touch event is the only way to
        // prevent the submit button animation from happening.
        // (in-case of input error)
        private void Place_button_Touch(object sender, View.TouchEventArgs e)
        {
            // Reset old/unrelevant errors.
            bid_layout.Error = "";

            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
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
                Toast.MakeText(this, $"Auction {auction.ItemName} is no longer running.", ToastLength.Long).Show();
                return;
            }

            if (!int.TryParse(bid_layout.EditText.Text, out int input_value))
            {
                bid_layout.Error = "Please enter your desired bid";
                return;
            }

            // Firstly validate the input.
            Bid top_bid = auction.FindTopBid();
            if (top_bid != null)
            {
                if (top_bid.OutBid > input_value)
                {
                    bid_layout.Error = $"Your bid must be greater than or equal to the top bid + {Auction.OUTBID_PERCENT}%";
                    return;
                }
            }
            else if (auction.Value > input_value)
            {
                bid_layout.Error = "Your bid must be greater than or equal to the starting bid";
                return;
            }

            // Retrieve and validate the runtime client.
            Client runtime_client = RuntimeClient.Get();
            if (runtime_client == null)
            {
                // Evacuate the client.
                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();

                // Notify the client.
                Toast.MakeText(this, "Failed to authorize your account.", ToastLength.Long).Show();
                return;
            }

            // Secondly validate the client coins against their input.
            Bid client_top_bid = auction.FindTopBid(runtime_client);
            if (client_top_bid != null)
            {
                int difference = input_value - client_top_bid.Value;
                if (runtime_client.Coins < difference)
                {
                    bid_layout.Error = $"You are missing coins to increase your bid (missing {(difference - runtime_client.Coins).ToString("N0")} coins)";
                    return;
                }
            }
            else if (runtime_client.Coins < input_value)
            {
                bid_layout.Error = $"You are missing coins to place your bid (missing {(input_value - runtime_client.Coins).ToString("N0")} coins)";
                return;
            }

            // All checks passed.
            e.Handled = false;
        }

        // We can assume all checks passed.
        // Place a new bid!
        private void Place_button_Click(object sender, EventArgs e)
        {
            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
                return;
            }

            // Retrieve and validate the runtime client.
            Client runtime_client = RuntimeClient.Get();
            if (runtime_client == null)
            {
                // Evacuate the client.
                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();

                // Notify the client.
                Toast.MakeText(this, "Failed to authorize your account.", ToastLength.Long).Show();
                return;
            }

            // Charge the client.
            int value = int.Parse(bid_layout.EditText.Text);
            int take_amount = value;

            Bid client_top_bid = auction.FindTopBid(runtime_client);
            if (client_top_bid != null)
            {
                take_amount = value - client_top_bid.Value;
            }

            runtime_client.Coins -= take_amount;

            // Place the new bid!
            Bid.Insert(auction_id, runtime_client.PhoneNumber, value);

            // If there's less the defined remaining for the auction.
            if ((auction.RemainingTime / 60) <= 0)
            {
                auction.EndTime += Auction.EXTEND_SECONDS;
            }

            // Update stats.
            runtime_client.Statistics.TotalBids++;

            // The client just broke his highest bid record,
            // update it!
            if (value > runtime_client.Statistics.HighestBid)
            {
                runtime_client.Statistics.HighestBid = value;
            }

            runtime_client.Statistics.CoinsSpent += take_amount;

            // Still updating stats!
            Client auction_owner = new Client(auction.OwnerPhone);
            if (auction_owner != null && auction_owner.Statistics.HighestAuctionHeld < value)
            {
                auction_owner.Statistics.HighestAuctionHeld = value;
            }

            SubmitButton button = (SubmitButton)sender;

            button.Touch -= Place_button_Touch;
            button.Click -= Place_button_Click;

            button.PostDelayed(() =>
            {
                // Refresh the page.
                OnResume();

                dialog_bid.Cancel();

            }, 2300); // 2300ms for the animation duration. (see the layout)
        }

        private void Bid_layout_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up)
            {
                bid_layout.Error = "";
            }

            e.Handled = false;
        }

        private void Collect_button_Click(object sender, EventArgs e)
        {
            Auction auction = new Auction(auction_id);
            if (auction.RowId == 0)
            {
                // Shouldn't really happen.
                return;
            }

            // Retrieve and validate the runtime client.
            Client runtime_client = RuntimeClient.Get();
            if (runtime_client == null)
            {
                // Evacuate the client.
                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();

                // Notify the client.
                Toast.MakeText(this, "Failed to authorize your account.", ToastLength.Long).Show();
                return;
            }

            // Give the owner money!
            if (auction.OwnerPhone == runtime_client.PhoneNumber && !auction.OwnerAcknowledged)
            {
                // Only give money if the auction was bought!
                if (auction.BuyerPhone != 0)
                {
                    int coins;

                    if (auction.Type == AuctionType.BIN)
                    {
                        coins = auction.Value;
                    }
                    else // Regular auction.
                    {
                        Bid top_bid = auction.FindTopBid();

                        coins = top_bid.Value;
                    }

                    runtime_client.Coins += coins;
                    // Update stats.
                    runtime_client.Statistics.TotalCoinsEarned += coins;
                }

                auction.OwnerAcknowledged = true;
            }
            // Do practically nothing?
            else if (auction.BuyerPhone == runtime_client.PhoneNumber && !auction.BuyerAcknowledged)
            {
                auction.BuyerAcknowledged = true;
            }

            // Evacuate the client to the manage auctions page.
            StartActivity(new Intent(this, typeof(ManageAuctionsActivity)));
        }
    }
}