# SQL Server Setup Guide

## What You Need to Provide

To use SQL Server instead of SQLite, you need to provide a **SQL Server connection string**.

### Connection String Format

Here are examples for different SQL Server configurations:

#### 1. SQL Server (Local/On-Premise)
```
Server=localhost;Database=BurhaniGuards;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

#### 2. SQL Server with Windows Authentication
```
Server=localhost;Database=BurhaniGuards;Integrated Security=True;TrustServerCertificate=True;
```

#### 3. SQL Server (Remote Server)
```
Server=your-server-ip,1433;Database=BurhaniGuards;User Id=your-username;Password=your-password;TrustServerCertificate=True;
```

#### 4. Azure SQL Database
```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=BurhaniGuards;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Required Information

Please provide the following:

1. **Server Name/IP**: Where is your SQL Server located?
   - Example: `localhost`, `192.168.1.100`, `your-server.database.windows.net`

2. **Database Name**: What should the database be called?
   - Example: `BurhaniGuards`, `burhani_guards_db`

3. **Authentication Method**:
   - SQL Server Authentication (username/password)
   - Windows Authentication (Integrated Security)

4. **Credentials** (if using SQL Server Auth):
   - Username
   - Password

5. **Port** (if not default 1433):
   - Default: 1433

## Example Connection Strings

### Example 1: Local SQL Server with SQL Auth
```
Server=localhost;Database=BurhaniGuards;User Id=admin;Password=MySecurePassword123;TrustServerCertificate=True;
```

### Example 2: Remote SQL Server
```
Server=192.168.1.50,1433;Database=BurhaniGuards;User Id=burhani_user;Password=SecurePass456;TrustServerCertificate=True;
```

### Example 3: Azure SQL
```
Server=tcp:burhaniguards-sql.database.windows.net,1433;Initial Catalog=BurhaniGuards;Persist Security Info=False;User ID=burhani_admin;Password=YourAzurePassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Database Schema

The API will automatically create the following tables:

1. **captains** - Stores captain/admin credentials
   - `id` (int, primary key, identity)
   - `email` (nvarchar, unique, not null)
   - `password_hash` (nvarchar, not null)
   - `created_at` (datetime, default: GETDATE())
   - `updated_at` (datetime, default: GETDATE())

2. **member_snapshots** - Stores member login history
   - `id` (int, primary key, identity)
   - `email` (nvarchar, unique, not null)
   - `display_name` (nvarchar, nullable)
   - `role` (nvarchar, nullable)
   - `last_login` (datetime, default: GETDATE())

## Next Steps

1. **Provide your connection string** - Share it with the format above
2. **Update appsettings.json** - The connection string will be added to the `SqlServer` section
3. **Run the API** - The database tables will be created automatically on first run
4. **Test the endpoints** - Use Swagger UI to verify everything works

## Security Note

⚠️ **Important**: Never commit connection strings with real passwords to version control. Use:
- `appsettings.Development.json` for local development
- Environment variables for production
- Azure Key Vault or similar for production secrets








