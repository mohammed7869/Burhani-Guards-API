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

Run the following SQL script to create the `miqaat_members` table:

```sql
CREATE TABLE IF NOT EXISTS `miqaat_members` (
  `member_id` INT NOT NULL,
  `miqaat_id` BIGINT NOT NULL,
  `status` VARCHAR(50) NULL,
  PRIMARY KEY (`member_id`, `miqaat_id`),
  INDEX `IX_miqaat_members_member_id` (`member_id`),
  INDEX `IX_miqaat_members_miqaat_id` (`miqaat_id`),
  CONSTRAINT `FK_miqaat_members_member_id` FOREIGN KEY (`member_id`) REFERENCES `members` (`id`) ON DELETE CASCADE,
  CONSTRAINT `FK_miqaat_members_miqaat_id` FOREIGN KEY (`miqaat_id`) REFERENCES `local_miqaat` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

## Notes

- `admin_approval` can be: 'Pending', 'Approved', 'Rejected'
- `captain_name` stores the full name of the Captain who created the miqaat
- Dates are stored as DATE type for proper date handling
- `volunteer_limit` is stored as INT
- `miqaat_members` table links members to miqaats with a status field
- Composite primary key ensures a member can only be associated once per miqaat
- Foreign keys ensure referential integrity with cascade delete
