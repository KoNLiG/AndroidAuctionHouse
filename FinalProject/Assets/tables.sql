START TRANSACTION;

-- --------------------------------------------------------

--
-- Table structure for table `ah_clients`
--

CREATE TABLE IF NOT EXISTS `ah_clients` (
  `phone` int(11) NOT NULL,
  `first_name` varchar(64) NOT NULL,
  `last_name` varchar(64) NOT NULL,
  `password` varchar(256) NOT NULL,
  `coins` int(11) DEFAULT 500,
  PRIMARY KEY (`phone`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ah_auctions`
--

CREATE TABLE IF NOT EXISTS `ah_auctions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `owner_phone` int(11) DEFAULT NULL,
  `buyer_phone` int(11) DEFAULT NULL,
  `item_name` varchar(64) NOT NULL,
  `item_desc` varchar(256) DEFAULT NULL,
  `start_time` bigint(20) NOT NULL,
  `end_time` bigint(20) NOT NULL,
  `value` int(11) NOT NULL,
  `type` int(11) NOT NULL,
  `status` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  FOREIGN KEY (`owner_phone`) REFERENCES `ah_clients` (`phone`) ON DELETE SET NULL ON UPDATE CASCADE,
  FOREIGN KEY (`buyer_phone`) REFERENCES `ah_clients` (`phone`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ah_bids`
--

CREATE TABLE IF NOT EXISTS `ah_bids` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `auction_id` int(11) NOT NULL,
  `bidder_phone` int(11),
  `value` int(11) NOT NULL,
  `bid_time` bigint(20) NOT NULL,
  `legacy_bid` int(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  FOREIGN KEY (`auction_id`) REFERENCES `ah_auctions` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  FOREIGN KEY (`bidder_phone`) REFERENCES `ah_clients` (`phone`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ah_images`
--

CREATE TABLE IF NOT EXISTS `ah_images` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `auction_id` int(11) NOT NULL,
  `image_data` MEDIUMTEXT NOT NULL,
  PRIMARY KEY (`id`),
  FOREIGN KEY (`auction_id`) REFERENCES `ah_auctions` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ah_stats`
--

CREATE TABLE IF NOT EXISTS `ah_stats` (
  `phone` int(11) NOT NULL,

  -- Buyer stats.
  `auctions_won` int(11) NOT NULL DEFAULT 0,
  `total_bids` int(11) NOT NULL DEFAULT 0,
  `highest_bid` int(11) NOT NULL DEFAULT 0,
  `coins_spent` int(11) NOT NULL DEFAULT 0,

  -- Seller stats.
  `auctions_created` int(11) NOT NULL DEFAULT 0,
  `highest_auction_held` int(11) NOT NULL DEFAULT 0,
  `auctions_completed_with_bids` int(11) NOT NULL DEFAULT 0,
  `auctions_completed_without_bids` int(11) NOT NULL DEFAULT 0,
  `total_coins_earned` int(11) NOT NULL DEFAULT 0,
  `coins_spent_on_fees` int(11) NOT NULL DEFAULT 0,

  PRIMARY KEY (`phone`),
  FOREIGN KEY (`phone`) REFERENCES `ah_clients` (`phone`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Events
--

CREATE OR REPLACE EVENT `TriggerHandleExpiredAuctions` 
ON SCHEDULE EVERY 1 SECOND 
DO 
  SELECT HandleExpiredAuctions();

-- If manually created via phpmyadmin, use delimiter!
CREATE OR REPLACE FUNCTION `HandleExpiredAuctions`() RETURNS int(1)
BEGIN
    DECLARE auction_id INT;
    DECLARE auction_owner INT;
    DECLARE bidder INT;
    DECLARE amount INT;
    DECLARE done BOOLEAN DEFAULT FALSE;

    DECLARE current_auction_id INT DEFAULT 0;
    DECLARE auction_count INT DEFAULT 0;

    DECLARE expired_auctions_cur CURSOR FOR
        SELECT a.id, a.owner_phone, b.bidder_phone, b.value
        FROM ah_auctions a
        LEFT JOIN ah_bids b ON a.id = b.auction_id AND b.legacy_bid = 0
        WHERE a.end_time <= UNIX_TIMESTAMP()
        AND a.status = 0 
        AND a.type = 1
        ORDER BY a.id, b.value DESC;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    OPEN expired_auctions_cur;

    read_loop: LOOP
        FETCH expired_auctions_cur INTO auction_id, auction_owner, bidder, amount;

        IF done THEN
            SET done = FALSE;
            LEAVE read_loop;
        END IF;

        IF auction_id != current_auction_id THEN
            SET current_auction_id = auction_id;
            SET auction_count = 0;
        END IF;
		
		IF ISNULL(bidder) THEN
			UPDATE `ah_stats` 
			SET `auctions_completed_without_bids`= `auctions_completed_without_bids` + 1 
			WHERE `phone` = auction_owner;
		ELSEIF auction_count = 0 THEN
			-- Very important and eseential line for 
			-- the history feature to function properly.
            UPDATE ah_auctions
            SET buyer_phone = bidder
            WHERE id = auction_id;
			
			-- Update stats.
			UPDATE `ah_stats` 
			SET `auctions_won`= `auctions_won` + 1 
			WHERE `phone` = bidder;
			
			-- Update more stats.
			UPDATE `ah_stats` 
			SET `auctions_completed_with_bids`= `auctions_completed_with_bids` + 1 
			WHERE `phone` = auction_owner;
		END IF;

        SET auction_count = auction_count + 1;
        
    END LOOP;

    CLOSE expired_auctions_cur;

    UPDATE 
    ah_auctions
    SET status = 2 
    WHERE end_time <= UNIX_TIMESTAMP() 
    AND `status` = 0;

    RETURN 0;
END;

SET GLOBAL event_scheduler="ON";

COMMIT;