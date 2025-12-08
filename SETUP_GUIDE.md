# Step-by-Step Setup Guide

## Prerequisites Check

1. **Verify .NET 8 SDK is installed:**
   - Open Command Prompt or PowerShell
   - Run: `dotnet --version`
   - You should see version `8.0.x` or higher
   - If not installed, download from: https://dotnet.microsoft.com/download/dotnet/8.0

## Step 1: Navigate to API Directory

Open Command Prompt or PowerShell and navigate to the API folder:

```bash
cd "D:\CCS Projects\Flutter Codes\Burhani Guards\burhaniguardsapi"
```

## Step 2: Restore NuGet Packages

Restore all project dependencies:

```bash
dotnet restore
```

**Expected output:** You should see packages being restored (BCrypt.Net-Next, Dapper, MongoDB.Driver, etc.)

## Step 3: Build the Project

Build the project to check for compilation errors:

```bash
dotnet build
```

**Expected output:** `Build succeeded.` with no errors.

## Step 4: Run the API

Start the API server:

```bash
dotnet run
```

**Expected output:**

- The API will start and display something like:
  ```
  info: Microsoft.Hosting.Lifetime[14]
        Now listening on: https://localhost:5001
        Now listening on: http://localhost:5000
  ```
- The SQLite database will be automatically created in `Data\burhani_guards.db`
- The captain credentials will be automatically seeded

## Step 5: Access Swagger UI

1. Open your web browser
2. Navigate to: `https://localhost:5001/swagger`
   - If you get a security warning, click "Advanced" and "Proceed to localhost"
3. You should see the Swagger UI with two endpoints:
   - `POST /api/auth/captain/login`
   - `POST /api/auth/member/login`

## Step 6: Test Captain Login

1. In Swagger UI, click on `POST /api/auth/captain/login`
2. Click "Try it out"
3. Enter the request body:
   ```json
   {
     "email": "hatimghadiyali53@gmail.com",
     "password": "123456"
   }
   ```
4. Click "Execute"
5. **Expected response:** Status 200 with a JSON response containing:
   ```json
   {
     "email": "hatimghadiyali53@gmail.com",
     "displayName": "Captain",
     "role": "captain",
     "token": "<some-token>"
   }
   ```

## Step 7: Prepare MongoDB for Member Login

Before testing member login, you need to ensure your MongoDB `members` collection has documents with BCrypt-hashed passwords.

### Option A: Using MongoDB Compass (GUI Tool)

1. Download MongoDB Compass: https://www.mongodb.com/try/download/compass
2. Connect using your connection string:
   ```
   mongodb+srv://mustafahussain78651_db_user:dvHQEAg0jz9SB8qb@cluster0.ipyj57z.mongodb.net/?appName=Cluster0
   ```
3. Navigate to `burhaniguards` database → `members` collection
4. Create a test member document with this structure:
   ```json
   {
     "email": "member@example.com",
     "passwordHash": "$2a$11$YourBCryptHashHere",
     "fullName": "Test Member",
     "role": "member",
     "isActive": true
   }
   ```

### Option B: Using Online BCrypt Generator (Easiest)

1. Visit: https://bcrypt-generator.com/
2. Enter your password (e.g., "member123")
3. Set rounds to 11 (default)
4. Click "Generate Hash"
5. Copy the generated hash (starts with `$2a$11$`)
6. Use it in your MongoDB `members` collection document

### Option C: Generate Hash Using C# (Alternative)

If you prefer using code, temporarily add this to your `Program.cs` before `app.Run()`:

```csharp
// Temporary: Generate BCrypt hash
var testPassword = "member123"; // Change to your desired password
var hash = BCrypt.Net.BCrypt.HashPassword(testPassword);
Console.WriteLine($"\nPassword: {testPassword}");
Console.WriteLine($"BCrypt Hash: {hash}\n");
```

Run `dotnet run`, copy the hash from console output, then remove this code.

## Step 8: Test Member Login

1. In Swagger UI, click on `POST /api/auth/member/login`
2. Click "Try it out"
3. Enter the request body (use the email from your MongoDB document):
   ```json
   {
     "email": "member@example.com",
     "password": "member123"
   }
   ```
4. Click "Execute"
5. **Expected response:** Status 200 with member details and token

## Troubleshooting

### Issue: "dotnet command not found"

**Solution:** Install .NET 8 SDK from https://dotnet.microsoft.com/download

### Issue: "Port 5001 already in use"

**Solution:**

- Stop any other applications using port 5001
- Or modify `Properties/launchSettings.json` to use a different port

### Issue: "MongoDB connection failed"

**Solution:**

- Verify your MongoDB Atlas connection string is correct
- Check if your IP is whitelisted in MongoDB Atlas (Network Access)
- Ensure the database name is `burhaniguards` and collection is `members`

### Issue: "Member login returns 401 Unauthorized"

**Solution:**

- Verify the member document exists in MongoDB
- Ensure `passwordHash` field contains a valid BCrypt hash
- Ensure `isActive` field is set to `true`
- Verify the password matches the one used to generate the hash

### Issue: "SQLite database not created"

**Solution:**

- Check if the `Data` folder exists in the API directory
- Ensure the application has write permissions
- Check the console output for any error messages

## Next Steps

Once everything is working:

1. Integrate these endpoints into your Flutter app
2. Store the JWT token securely in your Flutter app
3. Add token validation middleware for protected endpoints
4. Consider adding refresh token functionality
