using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Runtime;
using Android.Widget;
using Android.Content;
using MySqlConnector;
using Android.Content.PM;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using System;
using Android.Support.V4.Content.Res;
using System.IO;
using Android.Content.Res;
using System.Collections.Generic;
using Google.Android.Material.TextField;
using Google.Android.Material.Snackbar;

namespace FinalProject
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        // Auction "sort" available functions.
        enum SortFunctions
        {
            HIGHEST_VALUE = 1, // Starts from '1' since the first spinner position is always hint.
            LOWEST_VALUE,
            ENDING_SOON,
            RANDOM,
            // MAX
        }

        readonly string[] sort_function_names = 
        {
            "Highest Value",
            "Lowest Value",
            "Ending Soon",
            "Random"
        };

        // Auction "filter" available functions.
        enum FilterFunctions
        { 
            BIN = 1, // Starts from '1' since the first spinner position is always hint.
            Auction,
            // MAX
        }
        
        readonly string[] filter_function_names =
        {
            "BIN",
            "Auction"
        };

        readonly string[] required_permissions =
        {
            Android.Manifest.Permission.ReadExternalStorage,
            Android.Manifest.Permission.WriteExternalStorage,
            Android.Manifest.Permission.Camera
        };

        private NavigationMenu navigation_menu;

        private RelativeLayout main_layout;

        private Spinner sort_spinner, filter_spinner;
        private TextInputEditText search_edit_text;
        private ListView auctions_list_view;

        private WIFIBroadcast wifi_broadcast;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // Test the database connection once on application start-up,
            // if it failed enter a safe mode where any app features that uses
            // database features are disabled.
            try
            {
                Helper.DB.ConnectDatabase();
            }
            catch (MySqlException ex)
            {

#if DEBUG
                Console.WriteLine(ex);
#endif
                SetContentView(Resource.Layout.activity_main_nodb);
                return;
            }

            // Setup the wifi broadcast.
            wifi_broadcast = new WIFIBroadcast();
            RegisterReceiver(wifi_broadcast, new IntentFilter("android.net.wifi.STATE_CHANGE"));

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            HandlePermissions();

            // Setup database (create tables, events, functions, and override globals).
            SetupDatabase();

            RuntimeClient.Load(this);

            Title = "Auction House";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            main_layout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            // Setup the sort spinner.
            sort_spinner = FindViewById<Spinner>(Resource.Id.sortSpinner);
            ArrayAdapter<String> sort_adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleSpinnerItem, sort_function_names);
            sort_adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sort_spinner.Adapter = sort_adapter;
            sort_spinner.ItemSelected += Spinner_ItemSelected;
            
            // Setup the filter spinner.
            filter_spinner = FindViewById<Spinner>(Resource.Id.filterSpinner);
            ArrayAdapter<String> filter_adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleSpinnerItem, filter_function_names);
            filter_adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            filter_spinner.Adapter = filter_adapter;
            filter_spinner.ItemSelected += Spinner_ItemSelected;

            search_edit_text = FindViewById<TextInputEditText>(Resource.Id.searchField);
            search_edit_text.AfterTextChanged += Search_edit_text_AfterTextChanged;

            auctions_list_view = FindViewById<ListView>(Resource.Id.auctionsListView);
            auctions_list_view.ItemClick += Auctions_list_view_ItemClick;
            auctions_list_view.ItemLongClick += Auctions_list_view_ItemLongClick;

            FetchAuctions();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == 1)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    // Permission granted, continue with SMS sending code
                }
                else
                {
                    // Permission denied, handle the error or notify the user
                    Toast.MakeText(this, "SMS permission is required to use this application", ToastLength.Long).Show();
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            FetchAuctions();

            navigation_menu.CreateBatteryBroadcast();
        }

        protected override void OnPause()
        {
            base.OnPause();

            navigation_menu.DestroyBatteryBroadcast();
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            FetchAuctions();
        }

        private void Search_edit_text_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            FetchAuctions();
        }

        private void Auctions_list_view_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Limit access to guests.
            if (!RuntimeClient.IsLoggedIn())
            {
                Snackbar snackbar = Snackbar.Make(main_layout, $"Auction overview feature is not available for guests", Snackbar.LengthLong);
                snackbar.SetTextColor(new Color(218, 55, 60));
                snackbar.Show();
                return;
            }

            // Transfer the client to the item page.
            Intent intent = new Intent(this, typeof(AuctionOverviewActivity));
            intent.PutExtra("auction_id", e.Id);
            StartActivity(intent);
        }

        private void Auctions_list_view_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            // Load the auction image array.
            Auction auction = new Auction(e.Id);

            // Auction lookup has failed, abort.
            if (auction.RowId == 0)
            {
                return;
            }

            // Auction has no images, abort.
            if (auction.Images == null)
            {
                Snackbar snackbar = Snackbar.Make(main_layout, $"This auction has no images!", Snackbar.LengthLong);
                snackbar.SetTextColor(new Color(218, 55, 60));
                snackbar.Show();
                return;
            }

            // Display the auction images!
            DisplayImagesDialog(this, auction);
        }

        private void SetupDatabase()
        {
            MySqlConnection db = Helper.DB.ConnectDatabase();

            // Setup an input stream to read the database script asset.
            Stream stream = Assets.Open("db.sql");
            StreamReader reader = new StreamReader(stream);

            // Execute the script. (an import basically)
            MySqlCommand cmd = new MySqlCommand(reader.ReadToEnd(), db);
            cmd.Parameters.AddWithValue("?DEFAULT_COINS", Client.DEFAULT_COINS);
            cmd.ExecuteNonQuery();

            db.Close();
        }

        private void HandlePermissions()
        {
            // Request code 1.
            // Request for SMS permissions.
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.SendSms) != Permission.Granted)
            {
                RequestPermissions(new string[] { Android.Manifest.Permission.SendSms }, 1);
            }

            // Request code 0.
            // Loop through camera permissions and request each permission.
            for (int current_permission = 0; current_permission < required_permissions.Length; current_permission++)
            {
                if (ContextCompat.CheckSelfPermission(this, required_permissions[current_permission]) != Permission.Granted)
                {
                    RequestPermissions(new string[] { required_permissions[current_permission] }, 0);
                }
            }
        }

        // Loads and updates automatically all auctions.
        // (according to the given filters by the user)
        private void FetchAuctions()
        {
            MySqlConnection db;
            try
            {
                db = Helper.DB.ConnectDatabase();
            }
            catch
            {
                return;
            }

            MySqlCommand cmd = new MySqlCommand(BuildAuctionsFetchQuery(), db);

            List<Auction> auctions = new List<Auction>();
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                // Loop through the rows of the reader
                while (reader.Read())
                {
                    int id = reader.GetInt32("id");
                    string item_name = reader.GetString("item_name");
                    int value = reader.GetInt32("value");
                    AuctionType type = (AuctionType)reader.GetInt32("type");

                    // Set the display image, if there is any.
                    string base64_image;
                    if (!reader.IsDBNull(reader.GetOrdinal("image")))
                    {
                        base64_image = reader.GetString("image");
                    }
                    else
                    {
                        base64_image = "";
                    }

                    Auction auction = new Auction(id, item_name, value, type, base64_image);

                    // Fetch the top bid if available.
                    if (!reader.IsDBNull(reader.GetOrdinal("top_bid")))
                    {
                        List<Bid> top_bid_list = new List<Bid>();
                        top_bid_list.Add(new Bid(0, id, 0, reader.GetInt32("top_bid"), 0, true));

                        auction.Bids = top_bid_list;
                    }

                    auctions.Add(auction);
                }
            }

            db.Close();

            auctions_list_view.Adapter = new AuctionAdapter(this, auctions);
        }

        // Builds and retrieves a mysql query that fetches
        // all auctions accordingly with the user filters.
        // (sort by, filter by, or search bar)
        private string BuildAuctionsFetchQuery()
        {
            // auctions table - 'au'
            // images table - 'img'
            string query = $"SELECT au.id, au.item_name, value, type, img.image_data as image, (SELECT MAX(bids.value) FROM {Helper.DB.BIDS_TBL_NAME} bids WHERE bids.auction_id = au.id) as top_bid FROM {Helper.DB.AUCTIONS_TBL_NAME} au LEFT JOIN {Helper.DB.IMAGES_TBL_NAME} img ON img.auction_id = au.id AND img.id = (SELECT MIN(img2.id) FROM {Helper.DB.IMAGES_TBL_NAME} img2 WHERE img2.auction_id = au.id) WHERE `status` = {(int)AuctionStatus.Running}";

            // Handle filtering ('WHERE' clause always comes before 'ORDER BY'):
            int filter_spinner_position = filter_spinner.SelectedItemPosition;
            if (filter_spinner_position != 0)
            {
                query += $" AND au.type = {filter_spinner_position - 1}";
            }

            // Handle search bar (associated with the 'WHERE' clause):
            string search_text = search_edit_text.Text;
            if (!string.IsNullOrEmpty(search_text))
            {
                query += $" AND au.item_name LIKE '%{search_text}%'";
            }

            // Handle sorting (comes last):
            int sort_spinner_position = sort_spinner.SelectedItemPosition;
            switch (sort_spinner_position)
            {
                // Always hint text.
                case 0:
                {
                    // do nothing.
                    break;
                }
                case (int)SortFunctions.HIGHEST_VALUE:
                {
                    query += " ORDER BY `value` DESC";
                    break;
                }
                case (int)SortFunctions.LOWEST_VALUE:
                {
                    query += " ORDER BY `value` ASC";
                    break;
                }
                case (int)SortFunctions.ENDING_SOON:
                {
                    query += " ORDER BY (au.end_time - au.start_time) ASC";
                    break;
                }
                case (int)SortFunctions.RANDOM:
                {
                    // Utilize mysql 'RAND()' function.
                    query += " ORDER BY RAND()";
                    break;
                }
            }

            return query;
        }

        public static void DisplayImagesDialog(Context context, Auction auction)
        {
            Dialog dialog_images = new Dialog(context);

            dialog_images.SetContentView(Resource.Layout.dialog_images);
            dialog_images.SetTitle("View external images");
            dialog_images.SetCancelable(true);

            TextView title_tv = dialog_images.FindViewById<TextView>(Resource.Id.textViewTitle);
            title_tv.Text = $"Auction {auction.ItemName} has {auction.Images.Count} image(s)";

            Gallery gallery = dialog_images.FindViewById<Gallery>(Resource.Id.imagesGallery);
            gallery.SetSpacing(5);
            gallery.Adapter = new StaticImageAdapter(context, auction.Images);

            dialog_images.Show();
        }
    }
}