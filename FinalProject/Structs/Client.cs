using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MySqlConnector;
using BC = BCrypt.Net.BCrypt;

namespace FinalProject
{
    public class Client
    {
        // Various client statistics.
        public class Stats
        {
            // Phone number we're working with.
            private int phone_number;

            // Buyer stats.
            private int auctions_won;
            private int total_bids;

            private int highest_bid;
            private int coins_spent;

            // Seller stats.
            private int auctions_created;
            private int auctions_completed_with_bids;
            private int auctions_completed_without_bids;

            private int highest_auction_held;
            private int total_coins_earned;
            private int coins_spent_on_fees;

            // Stats constructor.
            public Stats()
            {
                this.auctions_won = 0;
                this.total_bids = 0;
                this.highest_bid = 0;
                this.coins_spent = 0;

                this.auctions_created = 0;
                this.auctions_completed_with_bids = 0;
                this.auctions_completed_without_bids = 0;
                this.highest_auction_held = 0;
                this.total_coins_earned = 0;
                this.coins_spent_on_fees = 0;
            }

            // Stats constructor that implements database loading.
            public Stats(MySqlConnection db, int phone_number)
            {
                this.phone_number = phone_number;

                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{Helper.DB.STATS_TBL_NAME}` WHERE `phone` = {phone_number}", db);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    // Loop through the rows of the reader
                    if (reader.Read())
                    {
                        this.auctions_won = reader.GetInt32("auctions_won");
                        this.total_bids = reader.GetInt32("total_bids");
                        this.highest_bid = reader.GetInt32("highest_bid");
                        this.coins_spent = reader.GetInt32("coins_spent");

                        this.auctions_created = reader.GetInt32("auctions_created");
                        this.auctions_completed_with_bids = reader.GetInt32("auctions_completed_with_bids");
                        this.auctions_completed_without_bids = reader.GetInt32("auctions_completed_without_bids");
                        this.highest_auction_held = reader.GetInt32("highest_auction_held");
                        this.total_coins_earned = reader.GetInt32("total_coins_earned");
                        this.coins_spent_on_fees = reader.GetInt32("coins_spent_on_fees");
                    }
                }

                db.Close();
            }

            // Attempts to insert an empty stats row.
            public bool Insert(MySqlConnection db, int phone_number)
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO `{Helper.DB.STATS_TBL_NAME}` (phone) VALUES(?phone_number)", db);
                    cmd.Parameters.AddWithValue("?phone_number", phone_number);
                    cmd.ExecuteNonQuery();

                    db.Close();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // This will get updated automatically in the db function,
            // and the reason is has a setter is purely for BIN auctions.
            public int AuctionsWon
            {
                get { return this.auctions_won; }
                set
                {
                    this.auctions_won = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `auctions_won` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            public int TotalBids
            {
                get { return this.total_bids; }
                set
                {
                    this.total_bids = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `total_bids` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            public int HighestBid
            {
                get { return this.highest_bid; }
                set
                {
                    this.highest_bid = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `highest_bid` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            public int CoinsSpent
            {
                get { return this.coins_spent; }
                set
                {
                    this.coins_spent = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `coins_spent` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            public int AuctionsCreated
            {
                get { return this.auctions_created; }
                set
                {
                    this.auctions_created = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `auctions_created` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            // This gets updated automatically through the db function,
            // therefore no need for a setter.
            public int AuctionsCompletedWithBids
            {
                get { return this.auctions_completed_with_bids; }
            }

            // This gets updated automatically through the db function,
            // therefore no need for a setter.
            public int AuctionsCompletedWithoutBids
            {
                get { return this.auctions_completed_without_bids; }
            }

            public int HighestAuctionHeld
            {
                get { return this.highest_auction_held; }
                set
                {
                    this.highest_auction_held = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `highest_auction_held` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            public int TotalCoinsEarned
            {
                get { return this.total_coins_earned; }
                set
                {
                    this.total_coins_earned = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `total_coins_earned` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }

            public int CoinsSpentOnFees
            {
                get { return this.coins_spent_on_fees; }
                set
                {
                    this.coins_spent_on_fees = value;

                    try
                    {
                        MySqlConnection db = Helper.DB.ConnectDatabase();

                        MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.STATS_TBL_NAME}` SET `coins_spent_on_fees` = {value} WHERE `phone` = {phone_number}", db);
                        cmd.ExecuteNonQuery();

                        db.Close();
                    }
                    catch
                    {
                        // silent error.
                    }
                }
            }
        }

        public static readonly int DEFAULT_COINS = 250;

        // First 9 digits of the phone number. (excluding the 0 prefix)
        private int phone_number;

        // Full user name.
        private string first_name;
        private string last_name;

        // Login cederials.
        private string password;

        // Coins, default is 100. (set by the constant 'DEFAULT_COINS')
        private int coins;

        // An object stores all the stats relevant to this client.
        private Stats stats;

        // Client constructor.
        public Client(int phone_number, string first_name, string last_name, string password, int coins = 0)
        {
            this.phone_number = phone_number;
            this.first_name = first_name;
            this.last_name = last_name;
            this.password = password;
            this.coins = coins;

            this.stats = new Stats();
        }

        // Client constructor that implements database loading.
        public Client(int phone_number)
        {
            MySqlConnection db = Helper.DB.ConnectDatabase();

            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{Helper.DB.CLIENTS_TBL_NAME}` WHERE `phone` = {phone_number}", db);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                // Loop through the rows of the reader
                if (reader.Read())
                {
                    this.phone_number = reader.GetInt32("phone");
                    this.first_name = reader.GetString("first_name");
                    this.last_name = reader.GetString("last_name");
                    this.password = reader.GetString("password");
                    this.coins = reader.GetInt32("coins");
                }
            }

            this.stats = new Stats(db, phone_number);

            db.Close();
        }

        public static bool operator ==(Client obj, int value)
        {
            if (ReferenceEquals(obj, null))
                return false;

            return obj.PhoneNumber == value;
        }

        public static bool operator !=(Client obj, int value)
        {
            if (ReferenceEquals(obj, null))
                return false;

            return obj.PhoneNumber != value;
        }

        public static bool IsExists(int phone_number)
        {
            MySqlConnection db = Helper.DB.ConnectDatabase();

            MySqlCommand cmd = new MySqlCommand($"SELECT `password` FROM `{Helper.DB.CLIENTS_TBL_NAME}` WHERE `phone` = {phone_number}", db);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                bool ret = reader.Read();
                db.Close();
                return ret;
            }
        }

        // Takes in a phone number and a password.
        // Retrieves true if a user with the matched data appears to exist on the database, 
        // false otherwise.
        public static bool IsValidLoginSession(int phone_number, string password)
        {
            MySqlConnection db = Helper.DB.ConnectDatabase();

            MySqlCommand cmd = new MySqlCommand($"SELECT `password` FROM `{Helper.DB.CLIENTS_TBL_NAME}` WHERE `phone` = {phone_number}", db);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    db.Close();
                    return false;
                }

                string password_hash = reader.GetString("password");
                db.Close();

                return BC.Verify(password, password_hash);
            }
        }

        // Attempts to insert a client data row into the database.
        // Retrieves true if succeed, or false if a data with identical phone number was found.
        // Phone number is the key, therefore there cannot be 2 rows with a shared phone number.
        public bool Insert()
        {
            // We could potentialy implement an "ExistsInDB" query before executing the insert query, 
            // but that will result in 2 queries executed instead of 1.

            try
            {
                // Default salt (salt = work factor) is 11, therefore the encryption should
                // remain inside the "try" bracket in-case it fails.
                string password_hash = BC.HashPassword(this.password);

                MySqlConnection db = Helper.DB.ConnectDatabase();

                MySqlCommand cmd = new MySqlCommand($"INSERT INTO `{Helper.DB.CLIENTS_TBL_NAME}` (phone, first_name, last_name, password) VALUES(?phone_number, ?first_name, ?last_name, ?password_hash)", db);

                cmd.Parameters.AddWithValue("?phone_number", this.phone_number);
                cmd.Parameters.AddWithValue("?first_name", this.first_name);
                cmd.Parameters.AddWithValue("?last_name", this.last_name);
                cmd.Parameters.AddWithValue("?password_hash", password_hash);

                cmd.ExecuteNonQuery();

                this.stats.Insert(db, this.phone_number);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Delete()
        {
            try
            {
                MySqlConnection db = Helper.DB.ConnectDatabase();

                MySqlCommand cmd = new MySqlCommand($"DELETE FROM `{Helper.DB.CLIENTS_TBL_NAME}` WHERE `phone` = {phone_number}", db);
                cmd.ExecuteNonQuery();

                db.Close();
            }
            catch
            {
                // silent error.
            }
        }

        //====//

        public int PhoneNumber
        {
            get { return this.phone_number; }
            set 
            {
                this.phone_number = value;

                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.CLIENTS_TBL_NAME}` SET `phone` = {value} WHERE `phone` = {phone_number}", db);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }

        public string FirstName
        {
            get { return this.first_name; }
            set
            {
                this.first_name = value;

                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.CLIENTS_TBL_NAME}` SET `first_name` = ?first_name WHERE `phone` = {phone_number}", db);
                    cmd.Parameters.AddWithValue("?first_name", value);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }

        public string LastName
        {
            get { return this.last_name; }
            set
            {
                this.last_name = value;

                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.CLIENTS_TBL_NAME}` SET `last_name` = ?last_name WHERE `phone` = {phone_number}", db);
                    cmd.Parameters.AddWithValue("?last_name", value);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }

        // Get returns hash.
        // Set retrieves raw password.
        public string Password
        {
            get { return this.password; }
            set
            {
                try
                {
                    // Default salt (salt = work factor) is 11, therefore the encryption should
                    // remain inside the "try" bracket in-case it fails.
                    string password_hash = BC.HashPassword(value);

                    this.password = password_hash;

                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.CLIENTS_TBL_NAME}` SET `password` = {password_hash} WHERE `phone` = {phone_number}", db);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }
        
        public int Coins
        {
            get { return this.coins; }
            set
            {
                this.coins = value;

                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    MySqlCommand cmd = new MySqlCommand($"UPDATE `{Helper.DB.CLIENTS_TBL_NAME}` SET `coins` = {value} WHERE `phone` = {phone_number}", db);
                    cmd.ExecuteNonQuery();

                    db.Close();
                }
                catch
                {
                    // silent error.
                }
            }
        }

        public Stats Statistics
        {
            get { return this.stats; }
        }

        // Retrieves the amount of active auctions this client currently have.
        public int ActiveAuctions
        {
            get
            {
                try
                {
                    MySqlConnection db = Helper.DB.ConnectDatabase();

                    long unix_time = Helper.GetUnixTimeStamp(); // Current unix timestamp

                    MySqlCommand cmd = new MySqlCommand($"SELECT COUNT(*) FROM `ah_auctions` WHERE `owner_phone` = {this.phone_number} AND `buyer_phone` IS NULL AND `end_time` > {unix_time};", db);

                    object result = cmd.ExecuteScalar();
                    result = (result == DBNull.Value) ? null : result;
                    int count = Convert.ToInt32(result);

                    db.Close();

                    return count;
                }
                catch
                {
                    // silent error.
                    return 0;
                }
            }
        }
    }
}