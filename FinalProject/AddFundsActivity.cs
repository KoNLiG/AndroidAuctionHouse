using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Com.Spark.Submitbutton;
using Com.Tomergoldst.Tooltips;
using Google.Android.Material.TextField;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "AddFundsActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class AddFundsActivity : AppCompatActivity, ToolTipsManager.ITipListener
    {
        // Different add funds options.
        public static readonly int[] AMOUNT_OPTIONS =
        {
            200,
            400,
            1000,
            2000,
            4000
		};

        private NavigationMenu navigation_menu;

        private RelativeLayout main_layout;

        private TextInputLayout amount_layout, card_number_layout, expire_layout, cvc_layout;
        private AutoCompleteTextView amount_tv;
        private SubmitButton add_button;

        private bool processing_request;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_addfunds);

            Title = "Add Funds";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            main_layout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            amount_layout = FindViewById<TextInputLayout>(Resource.Id.amountLayout);
            amount_tv = FindViewById<AutoCompleteTextView>(Resource.Id.amountField);

            PopulateAmountAdapter();

            card_number_layout = FindViewById<TextInputLayout>(Resource.Id.cardNumberLayout);
            card_number_layout.EditText.TextChanged += CardNumber_TextChanged;

            expire_layout = FindViewById<TextInputLayout>(Resource.Id.expireLayout);
            expire_layout.EditText.TextChanged += Expire_TextChanged;

            cvc_layout = FindViewById<TextInputLayout>(Resource.Id.CVCLayout);

            add_button = FindViewById<SubmitButton>(Resource.Id.addButton);
            add_button.Touch += Add_button_Touch;
            add_button.Click += Add_button_Click;
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        private void CardNumber_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            card_number_layout.EditText.TextChanged -= CardNumber_TextChanged;

            EditText et = (EditText)sender;

            // Format mechanism.
            if (e.AfterCount > 0)
            {
                string user_input = et.Text.Replace(" ", "");

                string formatted_string = "";
                for (int i = 0; i < user_input.Length; i++)
                {
                    if (i != 0 && i % 4 == 0)
                    {
                        formatted_string += ' ';
                    }

                    formatted_string += user_input[i];
                }

                et.Text = formatted_string;
                et.SetSelection(et.Text.Length);
            }

            // Delete mechanism.
            if (e.BeforeCount > 0 && (e.Start - 1) > 0 && et.Text[e.Start - 1] == ' ')
            {
                // Remove explicitly 2 chars from 'e.Start - 2'.
                // (the start of the whitespace)
                et.Text.Remove(e.Start - 2, 2);

                et.SetSelection(e.Start - 1);
            }

            card_number_layout.EditText.TextChanged += CardNumber_TextChanged;
        }

        private void Expire_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            expire_layout.EditText.TextChanged -= Expire_TextChanged;

            EditText et = (EditText)sender;

            // Format mechanism.
            if (e.AfterCount > 0)
            {
                string user_input = et.Text.Replace("/", "");

                string formatted_string = "";
                for (int i = 0; i < user_input.Length; i++)
                {
                    if (i != 0 && i % 2 == 0)
                    {
                        formatted_string += '/';
                    }

                    formatted_string += user_input[i];
                }

                et.Text = formatted_string;
                et.SetSelection(et.Text.Length);
            }

            // Delete mechanism.
            if (e.BeforeCount > 0 && (e.Start - 1) > 0 && et.Text[e.Start - 1] == '/')
            {
                // Remove any '/'
                et.Text = et.Text.Replace("/", "");

                et.SetSelection(e.Start - 1);
            }

            expire_layout.EditText.TextChanged += Expire_TextChanged;
        }

        // Required since blocking touch event is the only way to
        // prevent the submit button animation from happening.
        // (in-case of input error)
        private void Add_button_Touch(object sender, View.TouchEventArgs e)
        {
            // Don't overload!
            if (processing_request)
            {
                return;
            }

            if (e.Event.Action != MotionEventActions.Up)
            {
                e.Handled = false;
                return;
            }

            // Reset old/unrelevant errors.
            amount_layout.Error = "";
            card_number_layout.Error = "";
            expire_layout.Error = "";
            cvc_layout.Error = "";

            bool failed = false;
            string error = "";

            error = ParamValidation.FundsAmount(amount_tv.Text);
            if (!string.IsNullOrEmpty(error))
            {
                amount_layout.Error = error;
                failed = true;
            }

            error = ParamValidation.CardNumber(card_number_layout.EditText.Text);
            if (!string.IsNullOrEmpty(error))
            {
                card_number_layout.Error = error;
                failed = true;
            }

            error = ParamValidation.CardExpire(expire_layout.EditText.Text);
            if (!string.IsNullOrEmpty(error))
            {
                expire_layout.Error = error;
                failed = true;
            }

            error = ParamValidation.CardCVC(cvc_layout.EditText.Text);
            if (!string.IsNullOrEmpty(error))
            {
                cvc_layout.Error = error;
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
        private void Add_button_Click(object sender, EventArgs e)
        {
            Client runtime_client = RuntimeClient.Get();

            // This value is already validated.
            int value = GetFundValue(amount_tv.Text);

            runtime_client.Coins += value;

            SubmitButton button = (SubmitButton)sender;

            // Temporarily remove the button hook to avoid overload.
            button.Click -= Add_button_Click;

            processing_request = true;

            button.PostDelayed(() => {
                DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
                if (!drawer.IsDrawerOpen(GravityCompat.Start))
                {
                    drawer.OpenDrawer(GravityCompat.Start);
                }

                Helper.FireInputError(this, (ViewGroup)(navigation_menu.GetHeader()), navigation_menu.GetBalanceView(), $"Added {value.ToString("N0")} coins!");

                // Reapply the button hook.
                button.Click += Add_button_Click;

                processing_request = false;
            }, 2300); // 2300ms for the animation duration. (see the layout)
        }

        private void PopulateAmountAdapter()
        {
            // Populate the adapter.
            var adapter = new ArrayAdapter<string>(this, Resource.Layout.duration_layout);
            for (int current_amount = 0; current_amount < AMOUNT_OPTIONS.Length; current_amount++)
            {
                adapter.Add($"{AMOUNT_OPTIONS[current_amount].ToString("N0")} coins");
            }

            amount_tv.Adapter = adapter;
        }

        // Searches and returns an amount index by
        // the given formatted string.
        //
        // Will return -1 if no match has been found.
        private static int FindAmount(string str)
        {
            for (int current_amount = 0; current_amount < AMOUNT_OPTIONS.Length; current_amount++)
            {
                if ($"{AMOUNT_OPTIONS[current_amount].ToString("N0")} coins" == str)
                {
                    return current_amount;
                }
            }

            return -1;
        }

        // Retrieves an integer value of a fund to be added.
        // (whether it's formatted or not)
        // Retrieves -1 on failure.
        public static int GetFundValue(string str)
        {
            int idx = FindAmount(str);
            if (idx != -1)
            {
                return AMOUNT_OPTIONS[idx];
            }

            if (int.TryParse(str, out int amount))
            {
                return amount;
            }

            return -1;
        }

        public void OnTipDismissed(View p0, int p1, bool p2)
        {
        }
    }
}