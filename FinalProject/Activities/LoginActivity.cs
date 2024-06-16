using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;

using Android.Runtime;
using Android.Support.V4.Content.Res;
using Android.Support.V7.App;
using Android.Telephony;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

// passive colors:
// text: #6f7d86
// button: #344453
namespace FinalProject
{
    [Activity(Label = "LoginActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class LoginActivity : AppCompatActivity
    {
        ToggleButton toggleButtonHidePassword;
        TextView textViewRegister, textViewForgotPassword;
        EditText editTextPassword, editTextPhoneNumber;
        Button buttonLogIn;
        CheckBox checkBoxRememberMe;

        // Properties related to the forgot password feature(/dialog).
        Dialog dialogForgotPassword;
        EditText etPhoneField;
        Button buttonSendButton;
        TextView textViewError;

        private NavigationMenu navigation_menu;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.activity_login);

            Title = "Login with an existing account";

            buttonLogIn = FindViewById<Button>(Resource.Id.buttonLogIn);
            buttonLogIn.Click += buttonLogIn_Click;

            textViewRegister = FindViewById<TextView>(Resource.Id.textViewRegister);
            textViewRegister.Click += TextViewRegister_Click;
            textViewRegister.PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            textViewForgotPassword = FindViewById<TextView>(Resource.Id.textViewForgotPassword);
            textViewForgotPassword.Click += TextViewForgotPassword_Click;
            textViewForgotPassword.PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            toggleButtonHidePassword = FindViewById<ToggleButton>(Resource.Id.toggleButtonHidePassword);
            toggleButtonHidePassword.Click += ToggleButtonHidePassword_Click;

            editTextPassword = FindViewById<EditText>(Resource.Id.editTextPassword);
            editTextPhoneNumber = FindViewById<EditText>(Resource.Id.editTextPhoneNumber);

            editTextPassword.AfterTextChanged += Field_AfterTextChanged;
            editTextPhoneNumber.AfterTextChanged += Field_AfterTextChanged;

            checkBoxRememberMe = FindViewById<CheckBox>(Resource.Id.checkBoxRememberMe);

            new BackgroundManager(this);

            navigation_menu = new NavigationMenu(this);

            // Implement the special intent parameter "phone".
            // Sent only after updating a phone number.
            int phone_number = Intent.GetIntExtra("phone", 0);
            if (phone_number != 0)
            {
                editTextPhoneNumber.Text = $"0{phone_number}";
                Helper.SetButtonState(this, buttonLogIn, false);

                // Clear sharedpreferences, since they aren't relevant no more.
                ISharedPreferences sp = Helper.SP.Get(this);

                ISharedPreferencesEditor editor = sp.Edit();
                editor.Clear();
                editor.Commit();
            }

            // Check for any sharedpreferences data otherwise.
            else
            {
                // Find whether we need to remember the last connected user.
                ISharedPreferences sp = Helper.SP.Get(this);

                string phone = sp.GetString("phone", null);
                string password = sp.GetString("password", null);

                if (phone != null && password != null)
                {
                    editTextPhoneNumber.Text = phone;
                    editTextPassword.Text = password;

                    checkBoxRememberMe.Checked = true;

                    // Make sure to enable the log-in button since we
                    // edited the fields ourselves.
                    Helper.SetButtonState(this, buttonLogIn, true);
                }
                else
                {
                    Helper.SetButtonState(this, buttonLogIn, false);
                }
            }
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

        // Called when either the phone field or the password field has been edited.
        private void Field_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            bool should_be_enabled = ParamValidation.Phone(editTextPhoneNumber.Text) && ParamValidation.Password(editTextPassword.Text);
            if (buttonLogIn.Enabled != should_be_enabled)
            {
                Helper.SetButtonState(this, buttonLogIn, should_be_enabled);
            }
        }

        private void TextViewForgotPassword_Click(object sender, EventArgs e)
        {
            SetupForgotPassword();
        }

        private void buttonLogIn_Click(object sender, System.EventArgs e)
        {
            if (!int.TryParse(editTextPhoneNumber.Text, out int phone_number))
            {
                return;
            }

            if (!Client.IsValidLoginSession(phone_number, editTextPassword.Text))
            {
                Toast.MakeText(this, "Incorrcet username or password", ToastLength.Long).Show();
            }
            else
            {
                // Update data regarding remember me.
                ISharedPreferences sp = Helper.SP.Get(this);

                ISharedPreferencesEditor editor = sp.Edit();
                editor.PutString("phone", checkBoxRememberMe.Checked ? editTextPhoneNumber.Text : null);
                editor.PutString("password", checkBoxRememberMe.Checked ? editTextPassword.Text : null);
                editor.Commit();

                RuntimeClient.Save(phone_number);

                StartActivity(new Intent(this, typeof(MainActivity)));
                FinishAffinity();
            }
        }

        private void TextViewRegister_Click(object sender, System.EventArgs e)
        {
            StartActivity(new Intent(this, typeof(RegisterActivity)));
        }

        private void ToggleButtonHidePassword_Click(object sender, System.EventArgs e)
        {
            HideEditTextContents(editTextPassword, !toggleButtonHidePassword.Checked);
        }
        
        public static void HideEditTextContents(EditText et, bool val)
        {
            et.InputType = val ? (Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText) : Android.Text.InputTypes.ClassText;
        }

        void SetupForgotPassword()
        {
            dialogForgotPassword = new Dialog(this);

            dialogForgotPassword.SetContentView(Resource.Layout.dialog_forgotpassword);
            dialogForgotPassword.SetTitle("Forgot my Password");
            dialogForgotPassword.SetCancelable(true);

            etPhoneField = dialogForgotPassword.FindViewById<EditText>(Resource.Id.phoneField);
            etPhoneField.Text = editTextPhoneNumber.Text;

            textViewError = dialogForgotPassword.FindViewById<TextView>(Resource.Id.textViewError);

            buttonSendButton = dialogForgotPassword.FindViewById<Button>(Resource.Id.sendButton);
            buttonSendButton.Click += ButtonSendButton_Click;

            dialogForgotPassword.Show();
            
            if (etPhoneField.RequestFocus())
            {
                etPhoneField.PostDelayed(() => {

                    InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
                    imm.ShowSoftInput(etPhoneField, 0);

                }, 100);
            }
        }

        private void ButtonSendButton_Click(object sender, EventArgs e)
        {
            // Empty the error text buffer.
            textViewError.Text = "";

            // Phone number.
            if (!ParamValidation.Phone(etPhoneField.Text))
            {
                textViewError.Text = "Invalid phone number format specified";
                return;
            }

            if (!int.TryParse(etPhoneField.Text, out int numeric_phone_number))
            {
                textViewError.Text = "Phone number must be numeric";
                return; // don't proceeed.
            }

            // Run a db search that will tell us if the phone number is valid. (if succeeed)
            if (!Client.IsExists(numeric_phone_number))
            {
                textViewError.Text = "No user is registered via the specified phone number";
                return; // don't proceeed.
            }

            dialogForgotPassword.Cancel();

            PhoneConfirmation phone_confirmation = new PhoneConfirmation(this, numeric_phone_number, PostPhoneVerification);
            phone_confirmation.DisplayDialog();

            // Toast.MakeText(this, "Your AH user password has been sent to your mobile phone", ToastLength.Long).Show();
        }

        void PostPhoneVerification(int phone_number)
        {
            ChangePassword chagne_password_manager = new ChangePassword(phone_number, ChangePasswordSuceeed);
            chagne_password_manager.DisplayDialog(this);
        }

        void ChangePasswordSuceeed(int phone_number, string new_password)
        {
            Toast.MakeText(this, "Your AH user password has been updated!", ToastLength.Long).Show();
        }
    }
}