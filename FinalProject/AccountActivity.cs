using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content.Res;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Com.Tomergoldst.Tooltips;
using Google.Android.Material.Snackbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    [Activity(Label = "AccountActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class AccountActivity : AppCompatActivity, ToolTipsManager.ITipListener
    {
        private NavigationMenu navigation_menu;

        private RelativeLayout main_layout;

        private EditText first_name_et, last_name_et, phone_et, password_et;
        private Button save_button, delete_account_button;

        // Temp data related to the change of account information. (first name, last name, phone)
        private bool changed_first_name = false, changed_last_name = false, changed_phone = false;
        private int phone_number; // phone number to change to.

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_account);

            Title = "My Account";
            new BackgroundManager(this);
            navigation_menu = new NavigationMenu(this);

            // Reset ground variables.
            changed_first_name = false;
            changed_last_name = false;
            changed_phone = false;
            this.phone_number = 0;
            // -----

            main_layout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);

            FindViewById<TextView>(Resource.Id.textViewTitle).PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            first_name_et = FindViewById<EditText>(Resource.Id.firstNameField);
            new ClearableEditText(this, first_name_et);
            first_name_et.AfterTextChanged += Field_AfterTextChanged;

            last_name_et = FindViewById<EditText>(Resource.Id.lastNameField);
            new ClearableEditText(this, last_name_et);
            last_name_et.AfterTextChanged += Field_AfterTextChanged;

            phone_et = FindViewById<EditText>(Resource.Id.phoneField);
            new ClearableEditText(this, phone_et);
            phone_et.AfterTextChanged += Field_AfterTextChanged;

            password_et = FindViewById<EditText>(Resource.Id.passwordField);
            password_et.Touch += Password_et_Touch;

            save_button = FindViewById<Button>(Resource.Id.buttonSave);
            save_button.Click += Save_button_Click;
            Helper.SetButtonState(this, save_button, false);

            delete_account_button = FindViewById<Button>(Resource.Id.buttonDeleteAccount);
            delete_account_button.Click += Delete_account_button_Click;

            PopulatePageData();
        }

        // Called once the user has triggered the "back" operation by left swiping, etc..
        // If the navigation menu is open, override the action and close it first.
        public override void OnBackPressed()
        {
            navigation_menu.OnBackPressed(base.OnBackPressed);
        }

        private void Field_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            bool should_be_enabled = (first_name_et.Text != first_name_et.Hint) || (last_name_et.Text != last_name_et.Hint) || (phone_et.Text != phone_et.Hint);
            if (save_button.Enabled != should_be_enabled)
            {
                Helper.SetButtonState(this, save_button, should_be_enabled);
            }
        }

        private void Delete_account_button_Click(object sender, EventArgs e)
        {
            Client runtime_client = RuntimeClient.Get();

            PhoneConfirmation phone_confirmation = new PhoneConfirmation(this, runtime_client.PhoneNumber, PostDeletePhoneVerification);
            phone_confirmation.DisplayDialog();
        }

        void PostDeletePhoneVerification(int phone_number)
        {
            Client runtime_client = RuntimeClient.Get();
            runtime_client.Delete();

            // Logout, delete the current runtime client.
            RuntimeClient.Erase();

            // Send the user back to the main activity page.
            // Reloads the page if user is already on main page.
            StartActivity(new Intent(this, typeof(MainActivity)));
            FinishAffinity();
        }

        private void Save_button_Click(object sender, EventArgs e)
        {
            Client runtime_client = RuntimeClient.Get();

            // Handle first name changes.
            if (first_name_et.Text != first_name_et.Hint)
            {
                if (!ParamValidation.Name(first_name_et.Text))
                {
                    Helper.FireInputError(this, main_layout, first_name_et, "Invalid first name");
                }
                else
                {
                    runtime_client.FirstName = first_name_et.Text;
                    changed_first_name = true;
                }
            }

            // Handle last name changes.
            if (last_name_et.Text != last_name_et.Hint)
            {
                if (!ParamValidation.Name(last_name_et.Text))
                {
                    Helper.FireInputError(this, main_layout, last_name_et, "Invalid last name");
                }
                else
                {
                    runtime_client.LastName = last_name_et.Text;
                    changed_last_name = true;
                }
            }

            // If we did change the phone, perform the next steps:
            // 1. Verify the CURRENT account phone numer.
            // 2. Verify the new desired phone number.
            // 3. Update to the new phone number only if the 2 verifications passed.
            bool invalid = !int.TryParse(phone_et.Text, out int phone_number);
            int.TryParse(phone_et.Hint, out int old_phone_number);
            if (phone_number != old_phone_number)
            {
                if (invalid)
                {
                    Helper.FireInputError(this, main_layout, phone_et, "Invalid phone number");
                }
                else
                {
                    changed_phone = true;
                    this.phone_number = phone_number;

                    PhoneConfirmation phone_confirmation = new PhoneConfirmation(this, runtime_client.PhoneNumber, PostOldPhoneVerification, OnDialogDismiss);
                    phone_confirmation.DisplayDialog();
                }
            }

            // Attempt to reload the page only if we haven't changed the phone,
            // if we did change the phone, reload the page after verification.
            if (!changed_phone && (changed_first_name || changed_last_name))
            {
                Snackbar snackbar = Snackbar.Make(main_layout, $"Successfully updated your account information", Snackbar.LengthLong);
                snackbar.SetTextColor(new Color(100, 231, 100));
                snackbar.Show();

                PopulatePageData();

                navigation_menu.SetTitles();
            }
        }

        // Step 1 is completed, perform step 2.
        void PostOldPhoneVerification(int phone_number)
        {
            PhoneConfirmation phone_confirmation = new PhoneConfirmation(this, this.phone_number, PostNewPhoneVerification, OnDialogDismiss);
            phone_confirmation.DisplayDialog();
        }

        void OnDialogDismiss(int phone_number)
        {
            if (changed_first_name || changed_last_name)
            {
                PopulatePageData();
            }
        }

        // All required steps to change a phone number are completed.
        void PostNewPhoneVerification(int phone_number)
        {
            // Update runtime client phone number.
            Client runtime_client = RuntimeClient.Get();
            runtime_client.PhoneNumber = phone_number;

            ForceRelog(phone_number);
        }

        private void Password_et_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action != MotionEventActions.Down)
            {
                return;
            }

            Client runtime_client = RuntimeClient.Get();

            PhoneConfirmation phone_confirmation = new PhoneConfirmation(this, runtime_client.PhoneNumber, PostPasswordPhoneVerification);
            phone_confirmation.DisplayDialog();
        }

        void PostPasswordPhoneVerification(int phone_number)
        {
            ChangePassword change_password = new ChangePassword(phone_number, PostPasswordChange);
            change_password.DisplayDialog(this);
        }

        void PostPasswordChange(int phone_number, string new_password)
        {
            ForceRelog(phone_number);
        }

        private void ForceRelog(int phone_number)
        {
            // Logout from the system, a relog is required.
            RuntimeClient.Erase();

            // Send the client back to the login page, with an extra data
            // containing the phone number.
            // Check LoginActivity.cs for implementation.
            Intent intent = new Intent(this, typeof(LoginActivity));
            intent.PutExtra("phone", phone_number);
            StartActivity(intent);
            FinishAffinity();
        }

        void PopulatePageData()
        {
            Client runtime_client = RuntimeClient.Get();

            first_name_et.Hint = runtime_client.FirstName;
            first_name_et.Text = runtime_client.FirstName;
            first_name_et.ClearFocus();

            last_name_et.Hint = runtime_client.LastName;
            last_name_et.Text = runtime_client.LastName;
            last_name_et.ClearFocus();

            phone_et.Hint = $"0{runtime_client.PhoneNumber}";
            phone_et.Text = $"0{runtime_client.PhoneNumber}";
            phone_et.ClearFocus();
        }

        public void OnTipDismissed(View p0, int p1, bool p2)
        {
            Helper.SetButtonState(this, save_button, false);
        }
    }
}