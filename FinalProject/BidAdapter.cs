using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    class BidAdapter : BaseAdapter<Bid>
    {
        Context context;
        private List<Bid> bids;

        public BidAdapter(Context c, List<Bid> bids)
        {
            context = c;
            this.bids = bids;
        }

        public override int Count 
        { 
            get
            {
                // If there are no bids, return 1 for the "empty message row" view.
                if (bids.Count == 0)
                {
                    return 1;
                }
                
                return bids.Count;
            } 
        }

        public override Bid this[int position]
        {
            get
            {
                return this.bids[position];
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            if (bids.Count == 0)
            {
                return 0;
            }

            return bids[position].RowId;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            // Display a message in a case where there are no bids,
            // and the bids ListView is empty.
            if (bids.Count == 0)
            {
                View empty_bid_layout = ((Activity)context).LayoutInflater.Inflate(Resource.Layout.bid_layout, parent, false);

                TextView empty_text = empty_bid_layout.FindViewById<TextView>(Resource.Id.bidderNameText);
                empty_text.Text = "No bids were found, be the first!";

                return empty_bid_layout;
            }

            View new_bid_layout = ((Activity)context).LayoutInflater.Inflate(Resource.Layout.bid_layout, parent, false);

            TextView bidder_name = new_bid_layout.FindViewById<TextView>(Resource.Id.bidderNameText);
            TextView bid_amount = new_bid_layout.FindViewById<TextView>(Resource.Id.bidAmountText);
            TextView bid_time_left = new_bid_layout.FindViewById<TextView>(Resource.Id.bidTimeText);

            Bid current_bid = this.bids[position];
            if (current_bid == null)
            {
                return null;
            }

            // Set the bidder name. (Will show "N/A" if unavailable)
            Client bidder = new Client(current_bid.BidderPhone);
            string bidder_name_str = bidder == null ? "N/A" : bidder.FirstName;
            bidder_name.Text = bidder_name_str;

            // Set the bid amount.
            bid_amount.Text = current_bid.Value.ToString("N0");

            // Set the bid time.
            bid_time_left.Text = $"{Helper.FormatMinutes((int)(Helper.GetUnixTimeStamp() - current_bid.BidTime) / 60)} ago";
            
            return new_bid_layout;
        }
    }
}