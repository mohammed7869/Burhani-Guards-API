# Push Notifications Setup Guide

This guide explains how to set up push notifications for the Burhani Guards application.

## Overview

When an admin approves a miqaat from the admin panel, push notifications are automatically sent to:

1. **Captain** - who created the miqaat
2. **Members** - who belong to the same jamaat as the miqaat

## Prerequisites

1. Firebase project with Cloud Messaging enabled
2. Service account JSON file from Firebase Console
3. Database table for storing device tokens

## Setup Steps

### 1. Database Setup

Run the SQL script to create the `device_tokens` table:

```sql
-- See SQL_SCRIPTS/create_device_tokens_table.sql
```

The table stores FCM device tokens for each user.

### 2. Firebase Configuration

#### For API (Backend):

1. Go to Firebase Console → Project Settings → Service Accounts
2. Generate a new private key (JSON file)
3. Set the environment variable `GOOGLE_APPLICATION_CREDENTIALS` to point to this JSON file:

**Windows:**

```cmd
setx GOOGLE_APPLICATION_CREDENTIALS "C:\path\to\service-account-key.json"
```

**Linux/Mac:**

```bash
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"
```

**Alternative:** Place the JSON file in the API project root and modify `NotificationService.cs` to load it directly.

#### For Flutter App:

1. Add `google-services.json` (Android) to `android/app/`
2. Add `GoogleService-Info.plist` (iOS) to `ios/Runner/`
3. These files are automatically configured when you add Firebase to your Flutter project

### 3. Flutter App Configuration

The Flutter app automatically:

- Initializes FCM on app startup
- Requests notification permissions
- Registers device token with the API on login
- Handles foreground and background notifications
- Unregisters token on logout

### 4. API Configuration

The API automatically:

- Registers device tokens when users log in
- Sends notifications when miqaats are approved
- Handles token cleanup on logout

## How It Works

### Flow:

1. **User Login** → FCM token is generated and registered with API
2. **Admin Approves Miqaat** → API sends notifications to:
   - Captain (who created the miqaat)
   - All members in the same jamaat
3. **User Receives Notification** → Can tap to open the app

### Notification Content:

**For Captain:**

- Title: "Miqaat Approved"
- Body: "Your miqaat '{miqaatName}' has been approved by the admin."

**For Members:**

- Title: "New Miqaat Available"
- Body: "A new miqaat '{miqaatName}' is now available for enrollment."

## Testing

1. Ensure Firebase is properly configured
2. Login to the Flutter app (token will be registered)
3. Approve a miqaat from the admin panel
4. Check that notifications are received on:
   - Captain's device
   - Members' devices (same jamaat)

## Troubleshooting

### Notifications Not Sending:

1. Check Firebase credentials are properly configured
2. Verify `GOOGLE_APPLICATION_CREDENTIALS` environment variable
3. Check API logs for Firebase initialization errors
4. Verify device tokens are stored in `device_tokens` table

### Notifications Not Received:

1. Check notification permissions are granted (iOS/Android)
2. Verify device token is registered in database
3. Check Firebase Console for delivery status
4. Ensure app is not in "Do Not Disturb" mode

## API Endpoints

- `POST /api/1/notifications/register-token` - Register device token
- `DELETE /api/1/notifications/unregister-token` - Unregister device token

## Notes

- Notifications are sent asynchronously and won't block the approval process
- Failed notifications are logged but don't affect the approval flow
- Device tokens are automatically cleaned up on logout
