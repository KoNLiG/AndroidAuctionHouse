using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Com.Spark.Submitbutton;
using Com.Tomergoldst.Tooltips;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextField;
using MySqlConnector;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "ListAuctionActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class ListAuctionActivity : AppCompatActivity, ToolTipsManager.ITipListener, View.IOnClickListener
    {
        private static readonly int MAX_IMAGES = 3;

        private NavigationMenu navigation_menu;

        private RelativeLayout main_layout;
        
        private TextInputLayout name_layout, desc_layout, value_layout, duration_layout;
        private SubmitButton create_button;
        private AutoCompleteTextView duration_tv;

        // Camera dialog related properties.
        private Dialog dialog_camera;
        private Button take_button, select_button;

        // Created an auction? (used to skip this page)
        private bool created_auction;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_listauction);

            Title = "List an Auction";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            main_layout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            name_layout = FindViewById<TextInputLayout>(Resource.Id.itemNameLayout);
            name_layout.EditText.Touch += Name_layout_Touch;

            desc_layout = FindViewById<TextInputLayout>(Resource.Id.itemDescLayout);
            desc_layout.SetStartIconOnClickListener(this);
            name_layout.EditText.Touch += Desc_layout_Touch;

            value_layout = FindViewById<TextInputLayout>(Resource.Id.itemValueLayout);
            value_layout.SetEndIconOnClickListener(this);
            value_layout.EditText.AfterTextChanged += EditText_AfterTextChanged;
            value_layout.EditText.Touch += Value_layout_Touch;

            duration_layout = FindViewById<TextInputLayout>(Resource.Id.itemDurationLayout);
            duration_tv = FindViewById<AutoCompleteTextView>(Resource.Id.itemDurationField);

            // Populate the adapter.
            var adapter = new ArrayAdapter<string>(this, Resource.Layout.duration_layout);
            for (int current_duration = 0; current_duration < Auction.DURATIONS.Length; current_duration++)
            {
                adapter.Add(Helper.FormatMinutes(Auction.DURATIONS[current_duration]));
            }

            duration_tv.Adapter = adapter;

            create_button = FindViewById<SubmitButton>(Resource.Id.createButton);
            create_button.Touch += Create_button_Touch;
            create_button.Click += Create_button_Click;
        }

        // Skip this activity if an auction has been already 
        // created here.
        protected override void OnResume()
        {
            base.OnResume();

            if (created_auction)
            {
                this.OnBackPressed();
            }
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        // Required since blocking touch event is the only way to
        // prevent the submit button animation from happening.
        // (in-case of input error)
        private void Create_button_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action != MotionEventActions.Up)
            {
                e.Handled = false;
                return;
            }

            // Reset old/unrelevant errors.
            name_layout.Error = "";
            desc_layout.Error = "";
            value_layout.Error = "";
            duration_layout.Error = "";

            bool failed = false;
            string error = "";

            // Check if the client has exceeded their maximum allowed auctions at once.
            Client runtime_client = RuntimeClient.Get();
            if (runtime_client.ActiveAuctions >= Auction.MAX_ACTIVE_AUCTIONS)
            {
                Snackbar snackbar = Snackbar.Make(main_layout, $"You have exceeded the number of allowed active auctions ({Auction.MAX_ACTIVE_AUCTIONS})", Snackbar.LengthLong);
                snackbar.SetTextColor(new Color(218, 55, 60));
                snackbar.Show();
                failed = true;
            }

            // Handle item name verification.
            // empty string if no error occured.
            error = ParamValidation.ItemName(name_layout.EditText.Text);
            if (!string.IsNullOrEmpty(error))
            {
                name_layout.Error = error;
                failed = true;
            }

            // Handle item description verification.
            error = ParamValidation.ItemDesc(desc_layout.EditText.Text);
            if (!string.IsNullOrEmpty(error))
            {
                desc_layout.Error = error;
                failed = true;
            }

            // Handle item value verification.
            error = ParamValidation.ItemValue(value_layout.EditText.Text, value_layout.HelperText.Contains("BIN") ? "price" : "starting bid");
            if (!string.IsNullOrEmpty(error))
            {
                value_layout.Error = error;
                failed = true;
            }

            error = ParamValidation.ItemDuration(duration_tv.Text);
            if (!string.IsNullOrEmpty(error))
            {
                duration_layout.Error = error;
                failed = true;
            }

            if (!failed)
            {
                // All checks passed.
                e.Handled = false;
            }
        }

        // We can assume all checks passed.
        // Create and list a new auction!
        private void Create_button_Click(object sender, EventArgs e)
        {
            Client runtime_client = RuntimeClient.Get();
            int value = int.Parse(value_layout.EditText.Text);

            // Create the new auction!
            Auction new_auction = new Auction(
                runtime_client.PhoneNumber,
                name_layout.EditText.Text,
                desc_layout.EditText.Text,
                Auction.DURATIONS[Auction.FindDuration(duration_tv.Text)],
                value,
                value_layout.HelperText.Contains("BIN") ? AuctionType.BIN : AuctionType.Auction
                );

            // Only deduct coins if the list has been succeed.
            if (new_auction.List())
            {
                // Charge the client with the appropriate tax.
                int extra_fee = (value * Auction.FEE_PERCENT) / 100;
                runtime_client.Coins -= extra_fee;

                // Update stats.
                runtime_client.Statistics.CoinsSpent += extra_fee;
                runtime_client.Statistics.CoinsSpentOnFees += extra_fee;
                runtime_client.Statistics.AuctionsCreated++;
            }

            // Clear unrelevant images.
            AddImageAdapter.images.Clear();

            SubmitButton button = (SubmitButton)sender;

            button.Touch -= Create_button_Touch;
            button.Click -= Create_button_Click;

            MediaPlayer mp = MediaPlayer.Create(this, Resource.Raw.firework);
            mp.Start();

            button.PostDelayed(() => {
                // Transfer the client to the item page.
                Intent intent = new Intent(this, typeof(AuctionOverviewActivity));
                intent.PutExtra("auction_id", new_auction.RowId);

                created_auction = true;

                StartActivity(intent);
            }, 2300); // 2300ms for the animation duration. (see the layout)
        }

        private void Name_layout_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up)
            {
                name_layout.Error = "";
            }

            e.Handled = false;
        }

        private void Desc_layout_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up)
            {
                desc_layout.Error = "";
            }

            e.Handled = false;
        }

        private void Value_layout_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up)
            {
                value_layout.Error = "";
            }

            e.Handled = false;
        }

        // Modify the value suffix accordingly.
        private void EditText_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            string input = value_layout.EditText.Text;
            if (string.IsNullOrEmpty(input))
            {
                value_layout.SuffixText = "";
                return;
            }

            int value = int.Parse(value_layout.EditText.Text);
            
            value_layout.SuffixText = $"[Extra fee: +{((value * Auction.FEE_PERCENT) / 100).ToString("N0")} ({Auction.FEE_PERCENT}%)]";
            value_layout.SuffixTextView.TextSize = 12;
        }

        public void OnClick(View v)
        {
            ImageButton imageButton = (ImageButton)v;

            if (imageButton.Drawable == desc_layout.StartIconDrawable)
            {
                DisplayCameraDialog();
            }
            else if (imageButton.Drawable == value_layout.EndIconDrawable)
            {
                string next_type = GetOppositeTypeToCurrent();

                value_layout.HelperText = $"Type: {next_type}";
                value_layout.Hint = next_type == "BIN" ? "Price" : "Starting Bid";
            }
            else
            {
                // shouldn't be happening.
            }
        }

        private string GetOppositeTypeToCurrent()
        {
            string text = value_layout.HelperText;

            if (text.Contains("BIN"))
            {
                return "Auction";
            }
            else if (text.Contains("Auction"))
            {
                return "BIN";
            }

            return "";
        }

        [Obsolete]
        public void DisplayCameraDialog()
        {
            dialog_camera = new Dialog(this);

            dialog_camera.SetContentView(Resource.Layout.dialog_camera);
            dialog_camera.SetTitle("Manage external images");
            dialog_camera.SetCancelable(true);

            take_button = dialog_camera.FindViewById<Button>(Resource.Id.takeButton);
            take_button.Click += Take_button_Click;

            select_button = dialog_camera.FindViewById<Button>(Resource.Id.selectButton);
            select_button.Click += Select_button_Click;

            Gallery gallery = dialog_camera.FindViewById<Gallery>(Resource.Id.imagesGallery);
            gallery.Adapter = new AddImageAdapter(this);
            gallery.ItemClick += delegate (object sender, Android.Widget.AdapterView.ItemClickEventArgs args) 
            {
                RemovePhoto(args.Position);
            };

            ValidateInputLimit();

            dialog_camera.Show();
        }

        private void Take_button_Click(object sender, EventArgs e)
        {
            TakePhoto();
        }

        private void Select_button_Click(object sender, EventArgs e)
        {
            SelectPhoto();
        }

        async void TakePhoto()
        {
            await CrossMedia.Current.Initialize();

            var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
            {
                PhotoSize = Plugin.Media.Abstractions.PhotoSize.Small,
                CompressionQuality = 40,
                Name = "myimage.jpg",
                Directory = "sample"
            });

            if (file != null)
            {
                AddPhoto(file);
            }
        }
        
        async void SelectPhoto()
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                Toast.MakeText(this, "Photo selection isn't not supported on this device", ToastLength.Short).Show();
                return;
            }

            var file = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
            {
                PhotoSize = Plugin.Media.Abstractions.PhotoSize.Full,
                CompressionQuality = 40
            });

            if (file != null)
            {
                AddPhoto(file);
            }
        }

        // Convert file to byre array, to bitmap and set it to our ImageView
        private void AddPhoto(MediaFile file)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(file.Path);
            Bitmap bitmap = BitmapFactory.DecodeByteArray(imageArray, 0, imageArray.Length);
            AddImageAdapter.images.Add(bitmap);
            
            // Refresh the gallary adapter view.
            Gallery gallery = dialog_camera.FindViewById<Gallery>(Resource.Id.imagesGallery);
            ((AddImageAdapter)gallery.Adapter).NotifyDataSetChanged();

            ValidateInputLimit();
        }

        private void RemovePhoto(int position)
        {
            AddImageAdapter.images.RemoveAt(position);

            // Refresh the gallary adapter view.
            Gallery gallery = dialog_camera.FindViewById<Gallery>(Resource.Id.imagesGallery);
            ((AddImageAdapter)gallery.Adapter).NotifyDataSetChanged();

            ValidateInputLimit();
        }

        private void ValidateInputLimit()
        {
            // Block the user from adding anymore images
            // since we have reached the maximum limit.
            if (AddImageAdapter.images.Count >= MAX_IMAGES)
            {
                Helper.SetButtonState(this, take_button, false);
                Helper.SetButtonState(this, select_button, false);
            }
            // A new slot has been just freed.
            // Allow the user to add an additional image.
            else if (AddImageAdapter.images.Count == MAX_IMAGES - 1)
            {
                Helper.SetButtonState(this, take_button, true);
                Helper.SetButtonState(this, select_button, true);
            }
        }

        public void OnTipDismissed(View p0, int p1, bool p2)
        {
        }
    }
}