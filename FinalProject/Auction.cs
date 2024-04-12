using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MySqlConnector;

namespace FinalProject
{
    public enum AuctionType
	{
		BIN, // "Buy it now"
		Auction, // Regular auction (bids available)
		// Max
    }

	public enum AuctionStatus
    {
		Running, // The auction is currently active. [0]
		Bought,	 // The auction isn't active. [1]
		Expired, // The auction has expired. (its duration has passed) [2]
		Canceled // The auction has been manually cancaled by the owner. [3]
    }

	// This class represents one bid of a specific auction.
    public class Bid
    {
		// Database unique row id. Used as an identifier to this specific bid.
		private long row_id;

		// Phone number identifier of the user who issued this bid.
		private int bidder_phone;

		// Amount spend for this *specific* bid.
		private int value;

		// Unix timestamp of this bid.
		private long bid_time;

		// Whether this bid data is only saved for lagacy reasons. (history)
		private bool legacy_bid;

		// Constructor.
		public Bid(long row_id, int bidder_phone, int value, long bid_time, bool legacy_bid)
		{
			this.row_id = row_id;
			this.bidder_phone = bidder_phone;
			this.value = value;
			this.bid_time = bid_time;
			this.legacy_bid = legacy_bid;
		}

		//================[ Database ]================//
		public static void Insert(long auction_id, long bidder_phone, int value)
        {
            MySqlConnection db;

            try
            {
                db = Helper.DB.ConnectDatabase();
            }
            catch
            {
                return;
            }

			// Firstly, set all previous bids as legacy. (declare them as old history)
			MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.BIDS_TBL_NAME}` SET `legacy_bid` = 1 WHERE `auction_id` = ?AUCTION_ID AND `bidder_phone` = ?BIDDER_PHONE", db);
			cmd.Parameters.AddWithValue("?AUCTION_ID", auction_id);
			cmd.Parameters.AddWithValue("?BIDDER_PHONE", bidder_phone);

			cmd.ExecuteNonQuery();

			// Secondly, insert the new bid data with a default legacy bid value of false.
			cmd = new MySqlCommand($"INSERT INTO `{Helper.DB.BIDS_TBL_NAME}`(`auction_id`, `bidder_phone`, `value`, `bid_time`) VALUES (?AUCTION_ID, ?BIDDER_PHONE, ?VALUE, ?BID_TIME)", db);
            cmd.Parameters.AddWithValue("?AUCTION_ID", auction_id);
            cmd.Parameters.AddWithValue("?BIDDER_PHONE", bidder_phone);
            cmd.Parameters.AddWithValue("?VALUE", value);

			cmd.Parameters.AddWithValue("?BID_TIME", Helper.GetUnixTimeStamp());

			cmd.ExecuteNonQuery();

            db.Close();
        }

		//================[ Getters and Setters ]================//
		public long RowId
		{
			get { return this.row_id; }
		}

		public int BidderPhone
        {
			get { return this.bidder_phone; }
		}

		public int Value
		{
			get { return this.value; }
		}

		public long BidTime
        {
			get { return this.bid_time; }
		}

		// Price of outbidding |this| bid.
		public int OutBid
		{
			get 
			{ 
				return this.value + (this.value * Auction.OUTBID_PERCENT) / 100;
			}
		}
	}

    // Note that not all properties are always populated!
    // Use the third constructor with a valid `row_id`
    // to get a populated class.
    public class Auction
    {
        // Maximum lengths.
        public static readonly int MAX_NAME_LENGTH = 32;
		public static readonly int MAX_DESC_LENGTH = 128;

		// Minimum item value a user can input.
		public static readonly int MIN_VALUE = 50;

		// Fee percent to charge from the auction item price
		public static readonly int FEE_PERCENT = 5;

		// Maximum allowed auctions to be active at once, PER CLIENT.
		public static readonly int MAX_ACTIVE_AUCTIONS = 3;

		// Percentage of additional coins to add to the outbid price.
		public static readonly int OUTBID_PERCENT = 5;

		// Amount of seconds to increase an auction end time,
		// when someone placed a bid close to its ending.
		public static readonly int EXTEND_SECONDS = 300;

		// Different durations for auction items.
		public static readonly int[] DURATIONS =
		{
			120,  // 2 hours, this will be the default duration.
			360,  // 6 hours.
			720,  // 12 hours.
			1440,  // 1 day.
			2880,  // 2 days.
			10080 // 7 days.
		};

		// Database unique row id. Used as an identifier to this specific auction.
		private long row_id;

		// Auction owner phone identifier.
		private int owner_phone;

		// Auction buyer phone identifier.
		// Will be zero if the auction is still active and running.
		// Will be non-zero if the auction is no longer public, 
		// and was already purchased.
		private int buyer_phone;

		// Auction itfem name and brief description.
		// Both values are custom inputs that we recieve from 
		// users, therefore they must be escaped before
		// inserted into the database.
		private string item_name; // Cannot be empty.
		private string item_desc; // Could be empty.

		// Start/end time. (represented by unix timestamp)
		private long start_time;
		private long end_time;

		// Currency value of the auction item.
		private int value;

		// Auction type. See the enum above.
		private AuctionType type;

		// Auction status. See the enum above.
		private AuctionStatus status;

		// Temporary value to store the duration recieved in the constructor.
		private int temp_duration;

		// Custom images that the owner has uploaded.
		// Will be null if 0 images were uploaded.
		private List<Bitmap> images;

        // Array list which contains all the related bids objects.
        // Will be null if 0 bids were found.
        private List<Bid> bids;

		// Timer handle of the cancel function.
		// Handle end_timer;

		// Constructor. Initializes a new auction. (used for creating and listing a new auctions)
		// 'duration' is minutes represented.
		public Auction(int owner_phone, string item_name, string item_desc, int duration, int value, AuctionType type)
        {
			this.owner_phone = owner_phone;
			this.item_name = item_name;
			this.item_desc = item_desc;
			this.temp_duration = duration;
			this.value = value;
			this.type = type;
		}

		// Used after loading all auctions from database, 
		// and displaying them all via a ListView object.
		// 'base64_image' will be the first image uploaded, 
		// or an empty string if unavailable.
		public Auction(int row_id, string item_name, int value, AuctionType type, string base64_image)
        {
			this.row_id = row_id;
			this.item_name = item_name;
			this.value = value;
			this.type = type;

			// Convert and add the base64 image. (if applicable)
			if (!string.IsNullOrEmpty(base64_image))
			{
				this.images = new List<Bitmap>();
				this.images.Add(Helper.Base64ToBitmap(base64_image));
			}
		}

		// Auction constructor that implements database loading.
		// This populates the entirety of this class. (unlike other constructors)
		public Auction(long row_id)
        {
			MySqlConnection db;

			try
			{
				db = Helper.DB.ConnectDatabase();
			}
			catch
			{
				return;
			}

			// Firstly - fetch base data from `ah_auctions`
			MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{Helper.DB.AUCTIONS_TBL_NAME}` WHERE `id` = ?ID", db);
			cmd.Parameters.AddWithValue("?ID", row_id);

			using (MySqlDataReader reader = cmd.ExecuteReader())
			{
				// Parse all columns one by one.
				if (reader.Read())
				{
					this.row_id = row_id;

					if (!reader.IsDBNull(reader.GetOrdinal("owner_phone")))
					{
						this.owner_phone = reader.GetInt32("owner_phone");
					}

					if (!reader.IsDBNull(reader.GetOrdinal("buyer_phone")))
					{
						this.buyer_phone = reader.GetInt32("buyer_phone");
					}

					this.item_name = reader.GetString("item_name");

					if (!reader.IsDBNull(reader.GetOrdinal("item_desc")))
					{
						this.item_desc = reader.GetString("item_desc");
					}

					this.start_time = reader.GetInt64("start_time");
					this.end_time = reader.GetInt64("end_time");
					this.value = reader.GetInt32("value");
					this.type = (AuctionType)reader.GetInt32("type");
                    this.status = (AuctionStatus)reader.GetInt32("status");
                }
			}

			// Secondly - fetch all the auction images. (with maximum of 'Auction.MAX_ACTIVE_AUCTIONS')
			cmd = new MySqlCommand($"SELECT image_data FROM `{Helper.DB.IMAGES_TBL_NAME}` WHERE `auction_id` = ?AUCTION_ID", db);
            cmd.Parameters.AddWithValue("?AUCTION_ID", row_id);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
					// Initalize 'images' only once.
					if (this.images == null)
					{
						this.images = new List<Bitmap>();
					}

					images.Add(Helper.Base64ToBitmap(reader.GetString("image_data")));
                }
            }

            // Finally - fetch all bids.
            cmd = new MySqlCommand($"SELECT * FROM `{Helper.DB.BIDS_TBL_NAME}` WHERE `auction_id` = ?AUCTION_ID", db);
            cmd.Parameters.AddWithValue("?AUCTION_ID", row_id);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
					long bid_row_id = reader.GetInt32("id");

					int bidder_phone = 0;
					if (!reader.IsDBNull(reader.GetOrdinal("bidder_phone")))
					{
						bidder_phone = reader.GetInt32("bidder_phone");
					}

					int value = reader.GetInt32("value");
					long bid_time = reader.GetInt64("bid_time");
					bool legacy_bid = reader.GetBoolean("legacy_bid");

					// Initialize 'bids' only once.
                    if (this.bids == null)
					{
						this.bids = new List<Bid>();
					}

					this.bids.Add(new Bid(bid_row_id, bidder_phone, value, bid_time, legacy_bid));
                }
            }

            db.Close();
		}

        // Searches and returns a duration index by
        // the given formatted string.
        //
        // Will return -1 if no match has been found.
        public static int FindDuration(string str)
        {
			for (int current_duration = 0; current_duration < DURATIONS.Length; current_duration++)
            {
				if (Helper.FormatMinutes(DURATIONS[current_duration]) == str)
                {
					return current_duration;
                }
            }

			return -1;
        }

		// Lists (inserts data) the auction.
		// Retrieves true if succeed, false otherwise.
		public bool List()
        {
			long unix_time = Helper.GetUnixTimeStamp();

			this.start_time = unix_time;
			this.end_time = this.start_time + (this.temp_duration * 60);
			
			this.row_id = this.Insert(AddImageAdapter.images);

			return this.row_id != 0;
		}

		// Retrieves the current top bid of the auction, 
		// or null if there isn't any.
		public Bid FindTopBid()
        {
			// No bids available.
			if (this.bids == null || this.bids.Count == 0)
            {
				return null;
            }

			Bid top_bid = this.bids[0];

            // Loop through all bids and sort out the highest value one.
            for (int current_bid = 1; current_bid < this.bids.Count; current_bid++)
            { 
				if (this.bids[current_bid].Value > top_bid.Value)
				{
					top_bid = this.bids[current_bid];
				}
            }

			return top_bid;
        }

		// Same as the function above, only it is client specific.
		public Bid FindTopBid(Client client)
		{
			// Invalid client/no bids available.
			if (client == null || this.bids == null || this.bids.Count == 0)
			{
				return null;
			}

			Bid top_bid = null;

			// Loop through all bids and sort out the highest value one,
			// only with the expection of the given client.
			for (int current_bid = 0; current_bid < this.bids.Count; current_bid++)
            {
				if(client == this.bids[current_bid].BidderPhone && (top_bid == null || this.bids[current_bid].Value > top_bid.Value))
                {
                    top_bid = this.bids[current_bid];
                }
            }

			return top_bid;
		}

		//================[ Database ]================//

		// Attempts to insert this auction data into the database.
		// Retrieves the row id on success, 0 otherwise.
		private long Insert(List<Bitmap> images)
        {
			MySqlConnection db;

			try
			{
				db = Helper.DB.ConnectDatabase();
			}
			catch
            {
				return 0;
            }

			// Declared outside the 'try' scope in order
			// to perform a rollback on failure.
			MySqlTransaction tr = null;
			try
			{
				tr = db.BeginTransaction();

				MySqlCommand cmd = new MySqlCommand($"INSERT INTO `{Helper.DB.AUCTIONS_TBL_NAME}`" +
					$" (owner_phone, item_name, item_desc, start_time, end_time, value, type)" +
					$" VALUES(?owner_phone, ?item_name, ?item_desc, ?start_time, ?end_time, ?value, ?type)",
					db,
					tr);

				// Insert the actual auction data.
				cmd.Parameters.AddWithValue("?owner_phone", this.owner_phone);
				cmd.Parameters.AddWithValue("?item_name", this.item_name);
				cmd.Parameters.AddWithValue("?item_desc", this.item_desc);
				cmd.Parameters.AddWithValue("?start_time", this.start_time);
				cmd.Parameters.AddWithValue("?end_time", this.end_time);
				cmd.Parameters.AddWithValue("?value", this.value);
				cmd.Parameters.AddWithValue("?type", this.type);

				cmd.ExecuteNonQuery();

				long new_row_id = cmd.LastInsertedId;

				// Iterate through the auction images, and insert them.
				foreach (Bitmap image in images)
				{
					cmd = new MySqlCommand($"INSERT INTO `{Helper.DB.IMAGES_TBL_NAME}`(`auction_id`, `image_data`) VALUES({new_row_id}, ?image_data)",
						db,
						tr);
					cmd.Parameters.AddWithValue("?image_data", Helper.BitmapToBase64(image));
					cmd.ExecuteNonQuery();
				}

				tr.Commit();

				return new_row_id;
			}
			catch (MySqlException ex)
			{
				#if DEBUG
					Console.WriteLine(ex.ToString());
				#endif

				tr.Rollback();
				return 0;
			}
		}

		//================[ Getters and Setters ]================//
		public long RowId
		{
			get { return this.row_id; }
		}

		public int OwnerPhone
		{
			get { return this.owner_phone; }
		}

		public int BuyerPhone
        {
			get { return this.buyer_phone; }
            set
            {
                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.AUCTIONS_TBL_NAME}` SET `buyer_phone` = {value} WHERE `id` = {row_id}", db);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }

		public string ItemName
		{
			get { return this.item_name; }
		}

		public string ItemDescription
		{
			get { return this.item_desc; }
		}

		public long StartTime
        {
			get { return this.start_time; }
		}

		public long EndTime
		{
			get { return this.end_time; }
			set
			{
                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.AUCTIONS_TBL_NAME}` SET `end_time` = {value} WHERE `id` = {row_id}", db);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
		}

		// Retrieves the time left for this auction before it's expired. (property wrapper)
		// *Return value is Seconds represented*
		public long RemainingTime
		{
			get
			{
				// Current unix timestamp
				long unix_time = Helper.GetUnixTimeStamp();

				return this.end_time - unix_time; 
			}
		}

		public int Value
		{
			get { return this.value; }
		}

		public AuctionType Type
		{
			get { return this.type; }
		}

		public AuctionStatus Status
		{ 
			get { return this.status; }
            set
            {
                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.AUCTIONS_TBL_NAME}` SET `status` = {(int)value} WHERE `id` = {row_id}", db);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }

		public List<Bitmap> Images
		{
			get { return this.images; }
			set { this.images = value; }
		}

		public List<Bid> Bids
		{
			get { return this.bids; }
			set { this.bids = value; }
		}
	}
}