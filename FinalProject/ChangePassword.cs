using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Views;
using Android.Widget;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BC = BCrypt.Net.BCrypt;

namespace FinalProject
{
    class ChangePassword
    {
        // Callback for post verification.
        public delegate void PasswordChangeSucceedCallback(int phone_number, string new_password);

        // Phone number we're working with.
        private readonly int phone_number;

        // Properties related to the change setup.
        private Dialog dialog_change_password;
        private EditText et_password_field, et_confirm_password_field;
        private TextView tv_password_error, tv_confirm_password_error;
        private Button button_change_password;
        private ToggleButton toggle_button_hide_password;

        private PasswordChangeSucceedCallback succeed_cb;

        public ChangePassword(int phone_number, PasswordChangeSucceedCallback succeed_cb)
        {
            this.phone_number = phone_number;
            this.succeed_cb = succeed_cb;
        }

        public void DisplayDialog(Context context)
        {
            dialog_change_password = new Dialog(context);

            dialog_change_password.SetContentView(Resource.Layout.dialog_changepassword);
            dialog_change_password.SetTitle("Change your Password");
            dialog_change_password.SetCancelable(true);

            et_password_field = dialog_change_password.FindViewById<EditText>(Resource.Id.passwordField);
            et_confirm_password_field = dialog_change_password.FindViewById<EditText>(Resource.Id.confirmPasswordField);

            tv_password_error = dialog_change_password.FindViewById<TextView>(Resource.Id.tvPasswordError);
            tv_confirm_password_error = dialog_change_password.FindViewById<TextView>(Resource.Id.tvConfirmPasswordError);

            button_change_password = dialog_change_password.FindViewById<Button>(Resource.Id.changeButton);
            button_change_password.Click += Button_change_password_Click;

            toggle_button_hide_password = dialog_change_password.FindViewById<ToggleButton>(Resource.Id.toggleButtonHidePassword);
            toggle_button_hide_password.Click += Toggle_button_hide_password_Click;

            dialog_change_password.Show();
        }

        private void Toggle_button_hide_password_Click(object sender, EventArgs e)
        {
            HideEditTextContents(et_password_field, !toggle_button_hide_password.Checked);
            HideEditTextContents(et_confirm_password_field, !toggle_button_hide_password.Checked);
        }

        private static void HideEditTextContents(EditText et, bool val)
        {
            et.InputType = val ? (Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText) : Android.Text.InputTypes.ClassText;
        }

        private void Button_change_password_Click(object sender, EventArgs e)
        {
            tv_password_error.Text = "";
            tv_confirm_password_error.Text = "";

            bool params_validated = true;

            // Password.
            string password_str = et_password_field.Text;
            if (!ParamValidation.Password(password_str))
            {
                tv_password_error.Text = "8 characters, at least 1 lower case, 1 upper case, 1 numeric character";
                params_validated = false;
            }

            // Confirm password.
            string confirm_password_str = et_confirm_password_field.Text;
            if (password_str != confirm_password_str)
            {
                tv_confirm_password_error.Text = "Both passwords must be identical";
                params_validated = false;
            }

            if (!params_validated)
            {
                return;
            }

            // Make sure the client's new password isn't identical to his current password.
            Client client = new Client(phone_number);
            if (client != null && BC.Verify(password_str, client.Password))
            {
                tv_password_error.Text = "This password is identical to your current password";
                return;
            }


            dialog_change_password.Cancel();

            // Update to the new password.
            client.Password = password_str;

            succeed_cb(phone_number, password_str);
        }
    }
}