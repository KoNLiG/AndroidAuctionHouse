using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.Android.Material.TextField;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "ManageAuctionsActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class ManageAuctionsActivity : AppCompatActivity
    {
        private NavigationMenu navigation_menu;

        private TextInputEditText search_edit_text;

        private ListView auctions_list_view;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_manageauctions);

            Title = "Manage Auctions";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            search_edit_text = FindViewById<TextInputEditText>(Resource.Id.searchField);
            search_edit_text.AfterTextChanged += Search_edit_text_AfterTextChanged;

            auctions_list_view = FindViewById<ListView>(Resource.Id.auctionsListView);
            auctions_list_view.ItemClick += Auctions_list_view_ItemClick;

            FetchAuctions();
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

        private void Search_edit_text_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            FetchAuctions();
        }

        private void Auctions_list_view_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Transfer the user to the item overview.
            Intent intent = new Intent(this, typeof(AuctionOverviewActivity));
            intent.PutExtra("auction_id", e.Id);
            StartActivity(intent);
        }

        // Loads and updates automatically all auctions.
        // (according to the given filters by the user)
        private void FetchAuctions()
        {
            Client runtime_client = RuntimeClient.Get();
            if (runtime_client == null)
            {
                return;
            }

            int phone = runtime_client.PhoneNumber;

            MySqlConnection db = Helper.DB.ConnectDatabase();

            // Order by algorithm:
            //  1. Firstly active auctions have priority.
            //  2. Secondly unactive-unacknowledged auctions.
            //  3. Then finally the rest.
            MySqlCommand cmd = new MySqlCommand($"SELECT au.id, owner_phone, buyer_phone, au.item_name, type, status, owner_acknowledged, buyer_acknowledged, img.image_data as image FROM `ah_auctions` au " +
                $"LEFT JOIN {Helper.DB.IMAGES_TBL_NAME} img ON img.auction_id = au.id AND img.id = (SELECT MIN(img2.id) FROM {Helper.DB.IMAGES_TBL_NAME} img2 " +
                $"WHERE img2.auction_id = au.id) WHERE (`owner_phone` = {phone} OR `buyer_phone` = {phone}) " +
                $"AND au.item_name LIKE '%{search_edit_text.Text}%'" +
                $"ORDER BY `status` ASC, CASE `owner_phone` WHEN {phone} THEN `owner_acknowledged` ELSE `buyer_acknowledged` END ASC", db);

            List<Auction> auctions = new List<Auction>();
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                // Loop through the rows of the reader
                while (reader.Read())
                {
                    int id = reader.GetInt32("id");

                    int owner_phone = 0, buyer_phone = 0;
                    if (!reader.IsDBNull(reader.GetOrdinal("owner_phone")))
                    {
                        owner_phone = reader.GetInt32("owner_phone");
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("buyer_phone")))
                    {
                        buyer_phone = reader.GetInt32("buyer_phone");
                    }

                    string item_name = reader.GetString("item_name");
                    AuctionType type = (AuctionType)reader.GetInt32("type");
                    AuctionStatus status = (AuctionStatus)reader.GetInt32("status");
                    bool owner_acknowledged = reader.GetBoolean("owner_acknowledged");
                    bool buyer_acknowledged = reader.GetBoolean("buyer_acknowledged");

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

                    Auction auction = new Auction(id, owner_phone, buyer_phone, item_name, type, status, owner_acknowledged, buyer_acknowledged, base64_image);

                    // Enable the "manage" flag.
                    auction.IsManage = true;

                    auctions.Add(auction);
                }
            }

            db.Close();

            auctions_list_view.Adapter = new AuctionAdapter(this, auctions, phone);
        }
    }
}