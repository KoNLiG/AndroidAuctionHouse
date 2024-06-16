using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Telephony;
using Android.Views;
using Android.Widget;

namespace FinalProject
{
    [Activity(Label = "RegisterActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class RegisterActivity : AppCompatActivity
    {
        private static int[] FieldIDS =
        {
            Resource.Id.phoneField,
            Resource.Id.firstNameField,
            Resource.Id.lastNameField,
            Resource.Id.passwordField,
            Resource.Id.confirmPasswordField
        };

        // Fields enumeration.
        private static int FIELD_PHONE = 0;
        private static int FIELD_FIRSTNAME = 1;
        private static int FIELD_LASTNAME = 2;
        private static int FIELD_PASSWORD = 3;
        private static int FIELD_CONFIRMPASSWORD = 4;
        private static int FIELD_MAX = 5;

        RelativeLayout main_layout;

        EditText[] fields = new EditText[FIELD_MAX];
        TextView[] field_comment = new TextView[FIELD_MAX];
        
        // Current client pending to registeration.
        Client new_client;

        ToggleButton toggleButtonHidePassword;

        Button register_button;

        private NavigationMenu navigation_menu;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_register);

            Title = "Create your new account";

            // Initialize global variables.
            main_layout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);

            for (int i = 0; i < FieldIDS.Length; i++)
            {
                fields[i] = FindViewById<EditText>(FieldIDS[i]);
                fields[i].AfterTextChanged += Field_AfterTextChanged;
            }

            toggleButtonHidePassword = FindViewById<ToggleButton>(Resource.Id.toggleButtonHidePassword);
            toggleButtonHidePassword.Click += ToggleButtonHidePassword_Click;

            register_button = FindViewById<Button>(Resource.Id.registerButton);
            register_button.Click += RegisterActivity_Click;

            SetupBasePage();

            new BackgroundManager(this);

            navigation_menu = new NavigationMenu(this);
        }

        private void Field_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            bool should_be_enabled =
                ParamValidation.Phone(fields[FIELD_PHONE].Text) &&
                ParamValidation.Name(fields[FIELD_FIRSTNAME].Text) &&
                ParamValidation.Name(fields[FIELD_LASTNAME].Text) &&
                ParamValidation.Password(fields[FIELD_PASSWORD].Text) &&
                ParamValidation.Password(fields[FIELD_CONFIRMPASSWORD].Text);

            Helper.SetButtonState(this, register_button, should_be_enabled);
            register_button.Enabled = true;
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

        private void ToggleButtonHidePassword_Click(object sender, System.EventArgs e)
        {
            HideEditTextContents(fields[FIELD_PASSWORD], !toggleButtonHidePassword.Checked);
            HideEditTextContents(fields[FIELD_CONFIRMPASSWORD], !toggleButtonHidePassword.Checked);
        }

        public static void HideEditTextContents(EditText et, bool val)
        {
            et.InputType = val ? (Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText) : Android.Text.InputTypes.ClassText;
        }

        void SetupBasePage()
        {
            // CreateFieldCommentViews:
            for (int i = 0; i < field_comment.Length; i++)
            {
                field_comment[i] = new TextView(this);

                RelativeLayout.LayoutParams layout_params = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

                layout_params.AddRule(LayoutRules.AlignStart, FieldIDS[i]);
                layout_params.AddRule(LayoutRules.Below, FieldIDS[i]);
                layout_params.SetMargins(25, 0, 0, 0);

                field_comment[i].LayoutParameters = layout_params;
                field_comment[i].SetTextColor(Android.Graphics.Color.Red);
                field_comment[i].TextSize = 18;

                main_layout.AddView(field_comment[i]);
            }
        }

        private void RegisterActivity_Click(object sender, EventArgs e)
        {
            if (!ValidateRegisterParams(out new_client))
            {
                return;
            }

            if (Client.IsExists(new_client.PhoneNumber))
            {
                // Phone number is already in use, let the client know!
                field_comment[FIELD_PHONE].Text = "This phone number is already in-use";
            }
            else
            {
                PhoneConfirmation phone_confirmation = new PhoneConfirmation(this, new_client.PhoneNumber, PostPhoneVerification);
                phone_confirmation.DisplayDialog();
            }
        }

        void PostPhoneVerification(int phone_number)
        {
            // Attempt to insert the new verified client.
            if (new_client.Insert())
            {
                Toast.MakeText(this, "Verification succeed!", ToastLength.Short).Show();

                // New client has been successfully inserted into the database.
                // Send the client to the login page.
                StartActivity(new Intent(this, typeof(LoginActivity)));
                FinishAffinity();
            }
            // Someone else already registered with the specified 
            // phone number while the client was in the verification stage, sad.
            else
            {
                Toast.MakeText(this, "This phone number is already in-use.", ToastLength.Short).Show();
            }
        }

        private bool ValidateRegisterParams(out Client client)
        {
            // Erase all previous param comments.
            ClearParamComments();

            bool params_validated = true;

            // Phone number.
            if (!ParamValidation.Phone(fields[FIELD_PHONE].Text))
            {
                field_comment[FIELD_PHONE].Text = "Invalid phone number";
                params_validated = false;
            }

            // First name.
            if (!ParamValidation.Name(fields[FIELD_FIRSTNAME].Text))
            {
                field_comment[FIELD_FIRSTNAME].Text = "Invalid first name";
                params_validated = false;
            }

            // Last name.
            if (!ParamValidation.Name(fields[FIELD_LASTNAME].Text))
            {
                field_comment[FIELD_LASTNAME].Text = "Invalid last name";
                params_validated = false;
            }

            // Password.
            string password_str = fields[FIELD_PASSWORD].Text;
            if (!ParamValidation.Password(password_str))
            {
                field_comment[FIELD_PASSWORD].Text = "8 characters, at least 1 lower case, 1 upper case, 1 numeric character";
                params_validated = false;
            }

            // Confirm password.
            string confirm_password_str = fields[FIELD_CONFIRMPASSWORD].Text;
            if (password_str != confirm_password_str)
            {
                field_comment[FIELD_CONFIRMPASSWORD].Text = "Both passwords must be identical";
                params_validated = false;
            }

            if (params_validated)
            {
                client = new Client(
                    int.Parse(fields[FIELD_PHONE].Text), // Phone number.
                    fields[FIELD_FIRSTNAME].Text,        // First name.
                    fields[FIELD_LASTNAME].Text,        // Last name.
                    password_str          // Password.
                    );
            }
            else
            {
                client = null;
            }

            return params_validated;
        }

        void ClearParamComments()
        {
            for (int current_comment = 0; current_comment < field_comment.Length; current_comment++)
            {
                field_comment[current_comment].Text = "";
            }
        }
    }
}