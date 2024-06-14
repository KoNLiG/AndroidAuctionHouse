using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject
{
    class AuctionAdapter : BaseAdapter<Auction>
    {
        Context context;
        private List<Auction> auctions;

        // Temp value. Only used in manage auctions.
        private int runtime_client_phone;

        public AuctionAdapter(Context c, List<Auction> auctions, int runtime_client_phone = 0)
        {
            context = c;
            this.auctions = auctions;
            this.runtime_client_phone = runtime_client_phone;
        }

        public override int Count 
        { 
            get 
            {
                // If there are no auctions, return 1 for the "empty message row" view.
                if (auctions.Count == 0)
                {
                    return 1;
                }

                return auctions.Count;
            } 
        }

        public override Auction this[int position]
        {
            get
            {
                return this.auctions[position];
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            if (auctions.Count == 0)
            {
                return 0;
            }

            return auctions[position].RowId;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (auctions.Count == 0)
            {
                View empty_auction_layout = ((Activity)context).LayoutInflater.Inflate(Resource.Layout.auction_layout, parent, false);

                TextView empty_text = empty_auction_layout.FindViewById<TextView>(Resource.Id.itemNameText);
                empty_text.Text = "No auctions were found";

                return empty_auction_layout;
            }

            View new_auction_layout = ((Activity)context).LayoutInflater.Inflate(Resource.Layout.auction_layout, parent, false);

            ImageView item_image = new_auction_layout.FindViewById<ImageView>(Resource.Id.itemImage);
            TextView item_name = new_auction_layout.FindViewById<TextView>(Resource.Id.itemNameText);
            TextView item_value_desc = new_auction_layout.FindViewById<TextView>(Resource.Id.itemValueDescText);
            TextView item_value = new_auction_layout.FindViewById<TextView>(Resource.Id.itemValueText);

            Auction current_auction = this.auctions[position];
            if (current_auction == null)
            {
                return null;
            }

            // The owner has uploaded at least 1 image, set it.
            if (current_auction.Images != null)
            {
                item_image.SetImageBitmap(current_auction.Images[0]);
            }
            // No item image is available, set the default one.
            else
            {
                item_image.SetImageResource(Resource.Drawable.unknown_item_image);
            }

            item_name.Text = current_auction.ItemName;

            string item_value_str = "";

            // Regular auction appearance. (main page)
            if (!current_auction.IsManage)
            {
                Bid top_bid = current_auction.FindTopBid();
                item_value_desc.Text = current_auction.Type == AuctionType.BIN ? "Price" : top_bid == null ? "Starting bid" : "Top bid";
                int coins = current_auction.Type == AuctionType.BIN ? current_auction.Value : top_bid == null ? current_auction.Value : top_bid.Value;

                item_value_str = coins.ToString("N0");
            }
            // "Manage auctions" appearance.
            else
            {
                if (current_auction.Status != AuctionStatus.Running)
                {
                    // Auction is unacknowledged!
                    if ((current_auction.OwnerPhone == runtime_client_phone && !current_auction.OwnerAcknowledged) 
                        || (current_auction.BuyerPhone == runtime_client_phone && !current_auction.BuyerAcknowledged))
                    {
                        item_value_str = "Unclaimed";
                        item_value.SetTextColor(new Color(255, 103, 0));
                    }
                    // Auction isn't running and acknowledged!
                    else
                    {
                        item_value_str = "Ended";
                        item_value.SetTextColor(new Color(255, 127, 127));
                    }
                }
                // Auction is running!
                else
                {
                    item_value_str = "Running";
                    item_value.SetTextColor(new Color(50, 205, 50));
                }

                LinearLayout layout = new_auction_layout.FindViewById<LinearLayout>(Resource.Id.valueLayout);
                layout.SetGravity(GravityFlags.Right);

                RelativeLayout.LayoutParams layout_params = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                layout_params.SetMargins(0, 0, 25, 0);
                layout.LayoutParameters = layout_params;
            }

            item_value.Text = item_value_str;

            item_value.PaintFlags = Android.Graphics.PaintFlags.UnderlineText;

            return new_auction_layout;
        }
    }
}