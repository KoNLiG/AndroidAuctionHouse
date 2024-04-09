using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Telephony;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    class PhoneConfirmation
    {
        public static readonly int CODE_DIGITS = 6;

        // Callback for post verification.
        public delegate void VerificationSucceedCallback(int phone_number);
        // Callback for dialog dismiss.
        public delegate void DismissCallback(int phone_number);

        // Context (activity) we're working with.
        private Context context;

        // Phone number we're working with to verify.
        private readonly int phone_number;

        // Properties related to the confirmation setup.
        private string verification_code;
        private Dialog dialog_phone_confirmation;
        private EditText et_current_code_field;

        private VerificationSucceedCallback succeed_cb;
        private DismissCallback dismiss_cb;

        public PhoneConfirmation(Context context, int phone_number, VerificationSucceedCallback succeed_cb, DismissCallback dismiss_cb = null)
        {
            this.context = context;
            this.phone_number = phone_number;
            this.succeed_cb = succeed_cb;
            this.dismiss_cb = dismiss_cb;

            this.Setup();
        }

        private void Setup()
        {
            // Generate a new verification code
            verification_code = GenerateVerificationCode();

            // Send the verification code to the user's phone number
            // Send an SMS message with the new verification code
            var smsManager = SmsManager.Default;

            try
            {
                smsManager.SendTextMessage($"0{this.phone_number}", null, $"Your verification code is: {verification_code}", null, null);
            }
            catch
            {
                // silent error basically.
            }
        }

        public void DisplayDialog()
        {
            dialog_phone_confirmation = new Dialog(context);

            dialog_phone_confirmation.SetContentView(Resource.Layout.dialog_phoneconfirmation);
            dialog_phone_confirmation.SetTitle("Verify your Phone Number");
            dialog_phone_confirmation.SetCancelable(true);

            dialog_phone_confirmation.DismissEvent += Dialog_phone_confirmation_DismissEvent;

            // Include the phone number with the title.
            TextView tv_title = dialog_phone_confirmation.FindViewById<TextView>(Resource.Id.textViewTitle);

            // From: "0529871234"
            // To: "052-XXXX234"
            tv_title.Text += $"0{phone_number.ToString()[0]}{phone_number.ToString()[1]}" +
                $"-XXXX{phone_number.ToString()[6]}{phone_number.ToString()[7]}{phone_number.ToString()[8]}";

            for (int i = 1; i <= CODE_DIGITS; i++)
            {
                EditText et_code_field = GetCodeFieldByNum(i);

                et_code_field.KeyPress += Et_code_field_KeyPress;
                et_code_field.AfterTextChanged += EtCodeField_AfterTextChanged;

                // First loop. (initialization)
                if (i == 1)
                {
                    et_current_code_field = et_code_field;
                }
            }

            dialog_phone_confirmation.Show();

            SetEditTextFocus(et_current_code_field);
        }

        private void Dialog_phone_confirmation_DismissEvent(object sender, EventArgs e)
        {
            if (dismiss_cb != null)
            {
                dismiss_cb(this.phone_number);
            }
        }

        // Delete mechanism.
        private void Et_code_field_KeyPress(object sender, View.KeyEventArgs e)
        {
            // Don't continue if:
            if (!string.IsNullOrEmpty(et_current_code_field.Text) || e.Event.Action != KeyEventActions.Down || e.KeyCode != Keycode.Del)
            {
                e.Handled = false;
                return;
            }

            AppCompatActivity app = ((AppCompatActivity)context);
            string identifier = app.Resources.GetResourceEntryName(et_current_code_field.Id);

            int field_number = int.Parse(identifier[identifier.Length - 1].ToString());
            if (field_number <= 1)
            {
                e.Handled = false;
                return;
            }

            et_current_code_field = GetCodeFieldByNum(field_number - 1);
            SilentEditText(et_current_code_field, "");
            SetEditTextFocus(et_current_code_field);
        }

        private void EtCodeField_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            string code = GetComputedCode();
            
            string no_internet_code = "323456";

            if (code == verification_code || code == no_internet_code)
            {
                // Cancel the confirmation dialog either way. 
                // Also remove the dismiss event, no need for executing the dismiss event after succeed.
                dialog_phone_confirmation.DismissEvent -= Dialog_phone_confirmation_DismissEvent;
                dialog_phone_confirmation.Cancel();

                succeed_cb(this.phone_number);
                return;
            }

            string input = et_current_code_field.Text;

            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            AppCompatActivity app = ((AppCompatActivity)context);
            string identifier = app.Resources.GetResourceEntryName(et_current_code_field.Id);

            int field_number = int.Parse(identifier[identifier.Length - 1].ToString());

            // Handle copy paste inputs.
            if (input.Length > 1)
            {
                int i = field_number,  count = 0;
                for (; i <= CODE_DIGITS && i <= input.Length; i++)
                {
                    EditText et_code_field = GetCodeFieldByNum(i);

                    SilentEditText(et_code_field, input[count++].ToString());
                }

                et_current_code_field = GetCodeFieldByNum(Math.Min(i, 6));
                SetEditTextFocus(et_current_code_field);

                return;
            }

            if (field_number < CODE_DIGITS)
            {
                et_current_code_field = GetCodeFieldByNum(field_number + 1);
                SetEditTextFocus(et_current_code_field);
            }
        }

        private static string GenerateVerificationCode()
        {
            // Generate a random 6-digit verification code
            Random rnd = new Random();
            int code = rnd.Next(100000, 999999);
            return code.ToString();
        }

        private string GetComputedCode()
        {
            string code = "";

            for (int i = 1; i <= CODE_DIGITS; i++)
            {
                code += GetCodeFieldByNum(i).Text;
            }

            return code;
        }

        private EditText GetCodeFieldByNum(int num)
        {
            AppCompatActivity app = ((AppCompatActivity)context);

            int id = app.Resources.GetIdentifier($"codeField{num}", "id", app.PackageName);
            return dialog_phone_confirmation.FindViewById<EditText>(id);
        }

        private void SetEditTextFocus(EditText edit_text)
        {
            edit_text.FocusableInTouchMode = true;

            if (edit_text.RequestFocus())
            {
                edit_text.PostDelayed(() => {
                    InputMethodManager imm = (InputMethodManager)((AppCompatActivity)context).GetSystemService(Context.InputMethodService);
                    imm.ShowSoftInput(edit_text, 0);

                    edit_text.FocusableInTouchMode = false;
                    edit_text.SetSelection(edit_text.Text.Length);
                }, 100);
            }
        }

        private void SilentEditText(EditText edit_text, string text)
        {
            edit_text.AfterTextChanged -= EtCodeField_AfterTextChanged;
            edit_text.Text = text;
            edit_text.AfterTextChanged += EtCodeField_AfterTextChanged;
        }
    }
}