using MySql.Data.MySqlClient;
using System;

string connStr = "Server=localhost;Database=burhani_guards_pune;User ID=root;Password=;";
using var connection = new MySqlConnection(connStr);
connection.Open();

string sql = @"
    SELECT 
        m.`id`, m.`full_name`,
        SUM(CASE WHEN mm.`status` = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
        SUM(CASE WHEN mm.`status` = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
        SUM(CASE WHEN mm.`status` = 'Pending' THEN 1 ELSE 0 END) AS PendingCount
    FROM `members` m
    INNER JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
    WHERE mm.`miqaat_id` = 1 AND m.`is_active` = 1
    GROUP BY m.`id`, m.`full_name`
";

using var cmd = new MySqlCommand(sql, connection);
using var reader = cmd.ExecuteReader();

while (reader.Read())
{
    Console.WriteLine($"{reader["id"]} - {reader["full_name"]} - Approved: {reader["ApprovedCount"]} - Pending: {reader["PendingCount"]}");
}
