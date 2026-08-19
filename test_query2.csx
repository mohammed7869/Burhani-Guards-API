using MySql.Data.MySqlClient;
using System;
using System.Data;

string connStr = "Server=localhost;Database=burhani_guards_pune;User ID=root;Password=;";
using var connection = new MySqlConnection(connStr);
connection.Open();

string sql = @"
            SELECT 
                m.`id` AS Id,
                m.`full_name` AS FullName,
                SUM(CASE WHEN mm.`status` = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
                SUM(CASE WHEN mm.`status` = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
                SUM(CASE WHEN mm.`status` = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
                COUNT(*) AS TotalDays
            FROM `members` m
            INNER JOIN `miqaat_members` mm ON m.`id` = mm.`member_id`
            WHERE m.`is_active` = 1
            GROUP BY m.`id`, m.`full_name`
";

using var cmd = new MySqlCommand(sql, connection);
using var reader = cmd.ExecuteReader();

int count = 0;
while (reader.Read() && count < 20)
{
    Console.WriteLine($"{reader["Id"]} | {reader["FullName"]} | Appr: {reader["ApprovedCount"]} | Pend: {reader["PendingCount"]}");
    count++;
}
