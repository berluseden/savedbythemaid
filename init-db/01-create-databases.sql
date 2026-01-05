-- Create databases for the application
CREATE DATABASE IF NOT EXISTS `EServiceWeb` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS `SavedByMaidIM` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Grant privileges to appuser
GRANT ALL PRIVILEGES ON `EServiceWeb`.* TO 'appuser'@'%';
GRANT ALL PRIVILEGES ON `SavedByMaidIM`.* TO 'appuser'@'%';
FLUSH PRIVILEGES;
