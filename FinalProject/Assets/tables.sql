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
  `coins` int(11) DEFAULT ?DEFAULT_COINS,
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

COMMIT;