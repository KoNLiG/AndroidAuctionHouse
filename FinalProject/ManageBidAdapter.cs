using Android.App;
using Android.Content;
using Android.Graphics;
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
    class ManageBidAdapter : BaseAdapter<Bid>
    {
        Context context;
        private List<Bid> bids;

        public ManageBidAdapter(Context c, List<Bid> bids)
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
                
                TextView empty_text = empty_bid_layout.FindViewById<TextView>(Resource.Id.bidAmountText);
                empty_text.Text = "You don't have any outstanding bids!";

                return empty_bid_layout;
            }

            View new_bid_layout = ((Activity)context).LayoutInflater.Inflate(Resource.Layout.bid_layout, parent, false);
            
            // In this case it's the auction name.
            TextView auction_name = new_bid_layout.FindViewById<TextView>(Resource.Id.bidderNameText);

            TextView bid_amount = new_bid_layout.FindViewById<TextView>(Resource.Id.bidAmountText);
            TextView bid_title = new_bid_layout.FindViewById<TextView>(Resource.Id.bidTimeText);

            Bid current_bid = this.bids[position];
            if (current_bid == null)
            {
                return null;
            }

            Auction auction = new Auction(current_bid.AuctionId);
            if (auction.RowId == 0)
            {
                return null;
            }

            // Set the auction name.
            auction_name.Text = auction.ItemName;

            // Set the bid amount.
            bid_amount.Text = current_bid.Value.ToString("N0");

            /*
            // Set the bid time.
            //bid_title.Text = $"{Helper.FormatMinutes((int)(Helper.GetUnixTimeStamp() - current_bid.BidTime) / 60)} ago";
            */

            // Side title:
            // 1. Top bid. [green] (running only)
            // 2. Outbidded! [dark red] (running only)
            // 3. Unclaimed [orange]
            // 4. Ended [light red]

            string title_str = "";

            // Auction is currently running.
            if (auction.Status == AuctionStatus.Running)
            {
                // 'Auction::FindTopBid' is guaranteed to not return null. (in this case)
                if (auction.FindTopBid().RowId == current_bid.RowId)
                {
                    // This bid is the top bid!
                    title_str = "Top bid";
                    bid_title.SetTextColor(new Color(50, 205, 50));
                }
                // Someone else outbidded this bid!
                else
                {
                    title_str = "Outbidded!";
                    bid_title.SetTextColor(new Color(146, 0, 2));
                }
            }
            // Auction ended but bidder haven't acknowledged it.
            else if (!current_bid.BidderAcknowledged)
            {
                title_str = "Unclaimed";
                bid_title.SetTextColor(new Color(255, 103, 0));
            }
            // Auction ended and the bidder acknowledged it.
            else
            {
                title_str = "Ended";
                bid_title.SetTextColor(new Color(255, 127, 127));
            }

            bid_title.Text = title_str;

            return new_bid_layout;
        }
    }
}