using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BurhaniGuards.Api.Services;

/// <summary>
/// Interface for Firebase Cloud Messaging push notification delivery.
/// </summary>
public interface IFcmPushService
{
    /// <summary>
    /// Send a push notification to a single device via its FCM token.
    /// </summary>
    Task<bool> SendAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null, string? imageUrl = null);

    /// <summary>
    /// Send a push notification to multiple devices via their FCM tokens.
    /// Returns the count of successfully sent messages.
    /// </summary>
    Task<int> SendToMultipleAsync(IEnumerable<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null, string? imageUrl = null);
}

/// <summary>
/// Firebase Cloud Messaging push service.
/// Initializes the Firebase Admin SDK and sends push notifications to Android devices.
/// Push notifications are delivered by Google Play Services even when the app is killed.
/// </summary>
public class FcmPushService : IFcmPushService
{
    private readonly ILogger<FcmPushService> _logger;
    private readonly bool _isInitialized;

    public FcmPushService(IConfiguration configuration, ILogger<FcmPushService> logger)
    {
        _logger = logger;

        try
        {
            var serviceAccountPath = configuration["Firebase:ServiceAccountPath"];

            if (string.IsNullOrEmpty(serviceAccountPath) || !File.Exists(serviceAccountPath))
            {
                _logger.LogWarning(
                    "Firebase service account file not found at '{Path}'. FCM push notifications will be disabled.",
                    serviceAccountPath);
                _isInitialized = false;
                return;
            }

            // Initialize Firebase Admin SDK (only once per application lifetime)
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(serviceAccountPath)
                });
                _logger.LogInformation("Firebase Admin SDK initialized successfully");
                LogToFile("Firebase Admin SDK initialized successfully with path: " + serviceAccountPath);
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
            LogToFile("Failed to initialize Firebase Admin SDK: " + ex.ToString());
            _isInitialized = false;
        }
    }

    private void LogToFile(string message)
    {
        try
        {
            File.AppendAllText(@"C:\inetpub\wwwroot\bgp_api\fcm_debug_log.txt", $"{DateTime.Now}: {message}\n");
        }
        catch { /* ignore if no permission */ }
    }

    public async Task<bool> SendAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null, string? imageUrl = null)
    {
        if (!_isInitialized || string.IsNullOrEmpty(fcmToken))
            return false;

        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                    ImageUrl = imageUrl
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                        ChannelId = "bgp_notifications",
                    }
                },
                Data = data ?? new Dictionary<string, string>()
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogDebug("FCM: Sent to token {Token}: {Response}", fcmToken[..20], response);
            LogToFile($"SUCCESS: Sent FCM to token {fcmToken.Substring(0, Math.Min(20, fcmToken.Length))}... Response: {response}");
            return true;
        }
        catch (FirebaseMessagingException ex) when (
            ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
            ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
        {
            // Token is invalid or expired — the device uninstalled the app or token rotated
            _logger.LogWarning("FCM: Invalid/expired token {Token}: {Error}", fcmToken[..20], ex.MessagingErrorCode);
            LogToFile($"WARNING: Invalid/expired token {fcmToken.Substring(0, Math.Min(20, fcmToken.Length))}... Error: {ex.MessagingErrorCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM: Error sending to token {Token}", fcmToken[..20]);
            LogToFile($"ERROR: Failed to send FCM to token {fcmToken.Substring(0, Math.Min(20, fcmToken.Length))}... Exception: {ex}");
            return false;
        }
    }

    public async Task<int> SendToMultipleAsync(IEnumerable<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null, string? imageUrl = null)
    {
        if (!_isInitialized)
            return 0;

        var tokens = fcmTokens.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
        if (tokens.Count == 0)
            return 0;

        var successCount = 0;

        // Firebase supports MulticastMessage for up to 500 tokens at a time
        foreach (var batch in tokens.Chunk(500))
        {
            try
            {
                var multicast = new MulticastMessage
                {
                    Tokens = batch.ToList(),
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body,
                        ImageUrl = imageUrl
                    },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                            ChannelId = "bgp_notifications",
                        }
                    },
                    Data = data ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicast);
                successCount += response.SuccessCount;

                if (response.FailureCount > 0)
                {
                    _logger.LogWarning("FCM: {Failures}/{Total} messages failed in batch",
                        response.FailureCount, batch.Length);
                    LogToFile($"WARNING: {response.FailureCount}/{batch.Length} multicast messages failed.");
                }
                else
                {
                    LogToFile($"SUCCESS: Sent multicast batch of {batch.Length} tokens.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM: Error sending multicast batch");
                LogToFile($"ERROR: Multicast batch failed. Exception: {ex}");
            }
        }

        _logger.LogInformation("FCM: Sent to {Success}/{Total} devices", successCount, tokens.Count);
        return successCount;
    }
}
