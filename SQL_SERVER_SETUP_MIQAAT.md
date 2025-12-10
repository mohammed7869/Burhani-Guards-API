# Local Miqaat Table Setup

## Database Table Creation

Run the following SQL script to create the `local_miqaat` table:

```sql
CREATE TABLE IF NOT EXISTS `local_miqaat` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `miqaat_name` VARCHAR(255) NOT NULL,
  `jamaat` VARCHAR(255) NOT NULL,
  `jamiyat` VARCHAR(255) NOT NULL,
  `from_date` DATE NOT NULL,
  `till_date` DATE NOT NULL,
  `volunteer_limit` INT NOT NULL,
  `about_miqaat` TEXT,
  `admin_approval` VARCHAR(50) DEFAULT 'Pending',
  `captain_name` VARCHAR(255) NOT NULL,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

## Notes

- `admin_approval` can be: 'Pending', 'Approved', 'Rejected'
- `captain_name` stores the full name of the Captain who created the miqaat
- Dates are stored as DATE type for proper date handling
- `volunteer_limit` is stored as INT
