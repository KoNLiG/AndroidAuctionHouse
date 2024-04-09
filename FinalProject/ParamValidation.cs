using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FinalProject
{
    static class ParamValidation
    {
        public static bool Phone(string phone_str)
        {
            Regex phone_rgx = new Regex("^(0?5[0123458])(\\d{7})$");

            return phone_rgx.Match(phone_str).Success;
        }

        public static bool Name(string name)
        {
            // One single regex that will be used to validate both first and last name parameters.
            Regex name_rgx = new Regex("^[\\p{L}'][ \\p{L}'-]*[\\p{L}]$");

            return name_rgx.Match(name).Success;
        }

        // Minimum eight characters, at least one uppercase letter, one lowercase letter and one number:
        public static bool Password(string password)
        {
            Regex name_rgx = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[a-zA-Z\\d]{8,}$");

            return name_rgx.Match(password).Success;
        }

        public static string ItemName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Item name cannot be empty";
            }
            else if (name.Length > Auction.MAX_NAME_LENGTH)
            {
                return "Exceeds maximum length limit";
            }

            return "";
        }

        public static string ItemDesc(string desc)
        {
            if (desc.Length > Auction.MAX_DESC_LENGTH)
            {
                return "Exceeds maximum length limit";
            }

            return "";
        }

        public static string ItemValue(string value, string value_desc)
        {
            if (string.IsNullOrEmpty(value))
            {
                return $"Item {value_desc} cannot be empty";
            }

            int value_int = int.Parse(value);
            if (value_int < Auction.MIN_VALUE)
            {
                return $"Minimum auction {value_desc} is {Auction.MIN_VALUE}";
            }

            int extra_fee = (value_int * Auction.FEE_PERCENT) / 100;

            Client client = RuntimeClient.Get();
            if (client != null && client.Coins < extra_fee)
            {
                return $"Missing coins for extra fee ({(extra_fee - client.Coins).ToString("N0")})";
            }

            return "";
        }

        public static string ItemDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration))
            {
                return "Please select a duration";
            }

            // Shouldn't really ever happen.
            if (Auction.FindDuration(duration) == -1)
            {
                return "Invalid duration has been selected";
            }

            return "";
        }
    }
}