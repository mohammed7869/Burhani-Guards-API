using BurhaniGuards.Api.BusinessModel;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Contracts.Responses;
using BurhaniGuards.Api.Repositories;

namespace BurhaniGuards.Api.Services;

public class MiqaatService : IMiqaatService
{
    private readonly IMiqaatRepository _miqaatRepository;
    private readonly IMiqaatMemberRepository _miqaatMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    public MiqaatService(
        IMiqaatRepository miqaatRepository, 
        IMiqaatMemberRepository miqaatMemberRepository,
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _miqaatRepository = miqaatRepository;
        _miqaatMemberRepository = miqaatMemberRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            // Try Windows timezone ID first
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Try Linux/Unix timezone ID
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: Create a custom timezone for IST (UTC+5:30)
                return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time");
            }
        }
    }

    private DateTime ConvertUtcToIst(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            // If kind is unspecified, assume it's UTC
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndiaTimeZone);
    }

    private static int CalculateMiqaatDaysInclusive(DateTime fromDate, DateTime tillDate)
    {
        var days = (tillDate.Date - fromDate.Date).Days + 1;
        if (days < 1)
        {
            throw new Exception("Till date must be on or after From date");
        }
        return days;
    }

    /// <summary>
    /// Creates a miqaat (used by Captains from Flutter app).
    /// Captain-created miqaats are always "Local" type and require admin approval.
    /// </summary>
    public async Task<MiqaatResponse> Create(CreateMiqaatRequest request, string captainName)
    {
        var model = new MiqaatModel
        {
            MiqaatName = request.MiqaatName,
            MiqaatType = "Local", // Captains can only create Local miqaats
            Jamaat = request.Jamaat,
            Jamiyat = request.Jamiyat,
            FromDate = request.FromDate,
            TillDate = request.TillDate,
            MiqaatDays = CalculateMiqaatDaysInclusive(request.FromDate, request.TillDate),
            VolunteerLimit = request.VolunteerLimit,
            AboutMiqaat = request.AboutMiqaat,
            AdminApproval = AdminApprovalStatus.Pending,
            CaptainName = captainName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var id = await _miqaatRepository.Add(model);
        var createdMiqaat = await _miqaatRepository.GetById(id);

        if (createdMiqaat == null)
        {
            throw new Exception("Failed to create miqaat");
        }

        // Send email notifications when Captain creates Miqaat
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmails = await _userRepository.GetAdminEmailsAsync();
                var captain = await _userRepository.GetCaptainByFullNameAsync(captainName);
                
                var emailList = new List<string>();
                
                // Add admin emails
                emailList.AddRange(adminEmails);
                
                // Add captain email if found
                if (captain != null && !string.IsNullOrWhiteSpace(captain.Email))
                {
                    emailList.Add(captain.Email);
                }

                if (emailList.Any())
                {
                    var subject = $"New Miqaat Created: {request.MiqaatName}";
                    var body = $@"
                        <html>
                        <body>
                            <h2>New Miqaat Created</h2>
                            <p>A new Miqaat has been created and is pending approval.</p>
                            <p><strong>Details:</strong></p>
                            <ul>
                                <li><strong>Miqaat Name:</strong> {request.MiqaatName}</li>
                                <li><strong>Type:</strong> Local</li>
                                <li><strong>Jamaat:</strong> {request.Jamaat}</li>
                                <li><strong>Jamiyat:</strong> {request.Jamiyat}</li>
                                <li><strong>From Date:</strong> {request.FromDate:yyyy-MM-dd}</li>
                                <li><strong>Till Date:</strong> {request.TillDate:yyyy-MM-dd}</li>
                                <li><strong>Volunteer Limit:</strong> {request.VolunteerLimit}</li>
                                <li><strong>Captain:</strong> {captainName}</li>
                                {(string.IsNullOrWhiteSpace(request.AboutMiqaat) ? "" : $"<li><strong>About:</strong> {request.AboutMiqaat}</li>")}
                            </ul>
                            <p>Status: <strong>Pending Approval</strong></p>
                        </body>
                        </html>";

                    await _emailService.SendBulkEmailAsync(emailList, subject, body);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email notification for miqaat creation: {ex.Message}");
            }
        });

        return MapToResponse(createdMiqaat);
    }

    /// <summary>
    /// Creates a miqaat by Admin from the web panel.
    /// Admin-created miqaats are auto-approved. For International miqaats, email is sent to ALL members.
    /// </summary>
    public async Task<MiqaatResponse> CreateByAdmin(CreateMiqaatRequest request, string adminName)
    {
        var miqaatType = request.MiqaatType ?? "Local";
        if (miqaatType != "Local" && miqaatType != "International")
        {
            throw new Exception("Invalid miqaat type. Must be 'Local' or 'International'");
        }

        var model = new MiqaatModel
        {
            MiqaatName = request.MiqaatName,
            MiqaatType = miqaatType,
            Jamaat = request.Jamaat ?? "",
            Jamiyat = request.Jamiyat ?? "",
            FromDate = request.FromDate,
            TillDate = request.TillDate,
            MiqaatDays = CalculateMiqaatDaysInclusive(request.FromDate, request.TillDate),
            VolunteerLimit = request.VolunteerLimit,
            AboutMiqaat = request.AboutMiqaat,
            AdminApproval = AdminApprovalStatus.Approved, // Auto-approved when admin creates
            CaptainName = adminName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var id = await _miqaatRepository.Add(model);
        var createdMiqaat = await _miqaatRepository.GetById(id);

        if (createdMiqaat == null)
        {
            throw new Exception("Failed to create miqaat");
        }

        // Seed miqaat_members for the jamaat(s)
        if (!string.IsNullOrWhiteSpace(request.Jamaat))
        {
            await _miqaatMemberRepository.UpsertMembersForMiqaat(id, request.Jamaat, AdminApprovalStatus.Pending);
        }

        // Send email notifications
        _ = Task.Run(async () =>
        {
            try
            {
                var typeLabel = miqaatType == "International" ? "International" : "Local";
                var subject = $"New {typeLabel} Miqaat Created: {request.MiqaatName}";

                var bodyBuilder = $@"
                    <html>
                    <body>
                        <h2>New {typeLabel} Miqaat Created</h2>
                        <p>A new {typeLabel} Miqaat has been created and is now available.</p>
                        <p><strong>Details:</strong></p>
                        <ul>
                            <li><strong>Miqaat Name:</strong> {request.MiqaatName}</li>
                            <li><strong>Type:</strong> {typeLabel}</li>
                            {(miqaatType == "Local" ? $"<li><strong>Jamaat:</strong> {request.Jamaat}</li>" : "")}
                            {(miqaatType == "Local" ? $"<li><strong>Jamiyat:</strong> {request.Jamiyat}</li>" : "")}
                            <li><strong>From Date:</strong> {request.FromDate:yyyy-MM-dd}</li>
                            <li><strong>Till Date:</strong> {request.TillDate:yyyy-MM-dd}</li>
                            <li><strong>Volunteer Limit:</strong> {request.VolunteerLimit}</li>
                            <li><strong>Created By:</strong> {adminName}</li>
                            {(string.IsNullOrWhiteSpace(request.AboutMiqaat) ? "" : $"<li><strong>About:</strong> {request.AboutMiqaat}</li>")}
                        </ul>
                        <p>Status: <strong>Approved</strong></p>
                        <p>Please check the app for enrollment details.</p>
                    </body>
                    </html>";

                    // For International, if Jamiyats are selected but no Jamaats restrict, we could send to all. But if Jamaat is selected, send to them.
                    bool sendToAll = string.IsNullOrWhiteSpace(request.Jamaat);

                    if (miqaatType == "International" && sendToAll)
                    {
                        var allMembers = await _userRepository.List();
                        var memberEmails = allMembers
                            .Where(m => !string.IsNullOrWhiteSpace(m.email) && (m.isActive == true || m.isActive == null))
                            .Select(m => m.email)
                            .Distinct()
                            .ToList();

                        if (memberEmails.Any())
                        {
                            await _emailService.SendBulkEmailAsync(memberEmails, subject, bodyBuilder);
                        }
                    }
                    else
                    {
                        var emailList = new List<string>();
                        
                        var adminEmails = await _userRepository.GetAdminEmailsAsync();
                        emailList.AddRange(adminEmails);

                        if (!string.IsNullOrWhiteSpace(request.Jamaat))
                        {
                            // Support comma-separated jamaats
                            var jamaatList = request.Jamaat.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                           .Select(j => j.Trim())
                                                           .ToList();
                                                           
                            foreach (var j in jamaatList)
                            {
                                var members = await _userRepository.GetMembersByJamaatAsync(j);
                                var memberEmails = members
                                    .Where(m => !string.IsNullOrWhiteSpace(m.Email))
                                    .Select(m => m.Email)
                                    .ToList();
                                emailList.AddRange(memberEmails);
                            }
                        }

                        emailList = emailList.Distinct().ToList();
                        if (emailList.Any())
                        {
                            await _emailService.SendBulkEmailAsync(emailList, subject, bodyBuilder);
                        }
                    }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email notification for admin miqaat creation: {ex.Message}");
            }
        });

        return MapToResponse(createdMiqaat);
    }

    public async Task<List<MiqaatResponse>> GetAll()
    {
        var miqaats = await _miqaatRepository.GetAll();
        return miqaats.Select(m => MapToResponse(m, includeReport: true)).ToList();
    }

    public async Task<MiqaatResponse?> GetById(long id)
    {
        var miqaat = await _miqaatRepository.GetById(id);
        if (miqaat == null)
        {
            return null;
        }

        return MapToResponse(miqaat, includeReport: true);
    }

    public async Task Update(long id, UpdateMiqaatRequest request)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        var previousApprovalStatus = existingMiqaat.AdminApproval;

        existingMiqaat.MiqaatName = request.MiqaatName;
        existingMiqaat.MiqaatType = request.MiqaatType ?? existingMiqaat.MiqaatType;
        existingMiqaat.Jamaat = request.Jamaat;
        existingMiqaat.Jamiyat = request.Jamiyat;
        existingMiqaat.FromDate = request.FromDate;
        existingMiqaat.TillDate = request.TillDate;
        existingMiqaat.MiqaatDays = CalculateMiqaatDaysInclusive(request.FromDate, request.TillDate);
        existingMiqaat.VolunteerLimit = request.VolunteerLimit;
        existingMiqaat.AboutMiqaat = request.AboutMiqaat;
        
        // Update approval status if provided
        AdminApprovalStatus? newApprovalStatus = null;
        if (!string.IsNullOrWhiteSpace(request.AdminApproval))
        {
            newApprovalStatus = ParseApprovalStatus(request.AdminApproval);
            existingMiqaat.AdminApproval = newApprovalStatus.Value;
        }
        
        existingMiqaat.UpdatedAt = DateTime.UtcNow;

        await _miqaatRepository.Update(existingMiqaat);

        // Only seed miqaat_members after admin approval is set to Approved
        // UpsertMembersForMiqaat uses ON DUPLICATE KEY UPDATE, so it's safe to call multiple times
        if (newApprovalStatus.HasValue && newApprovalStatus.Value == AdminApprovalStatus.Approved)
        {
            await _miqaatMemberRepository.UpsertMembersForMiqaat(id, existingMiqaat.Jamaat, AdminApprovalStatus.Pending);

            // Auto-approve captain who created the miqaat for all days
            var captain = await _userRepository.GetCaptainByFullNameAsync(existingMiqaat.CaptainName);
            if (captain != null)
            {
                // Upsert captain rows as Approved (in case UpsertMembersForMiqaat added them as Pending
                // or captain belongs to a different jamaat and wasn't seeded at all)
                await _miqaatMemberRepository.UpsertCaptainForMiqaat((int)captain.Id, id, AdminApprovalStatus.Approved);
            }
        }
    }

    public async Task UpdateApprovalStatus(long id, string status)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        var previousApprovalStatus = existingMiqaat.AdminApproval;
        var parsedStatus = ParseApprovalStatus(status);
        existingMiqaat.AdminApproval = parsedStatus;
        existingMiqaat.UpdatedAt = DateTime.UtcNow;

        await _miqaatRepository.Update(existingMiqaat);

        // Only seed miqaat_members after admin approval is set to Approved
        // UpsertMembersForMiqaat uses ON DUPLICATE KEY UPDATE, so it's safe to call multiple times
        if (parsedStatus == AdminApprovalStatus.Approved)
        {
            await _miqaatMemberRepository.UpsertMembersForMiqaat(id, existingMiqaat.Jamaat, AdminApprovalStatus.Pending);

            // Auto-approve captain who created the miqaat for all days.
            // We use UpsertCaptainForMiqaat which handles INSERT ... ON DUPLICATE KEY UPDATE,
            // so it works regardless of whether the captain is in the same jamaat or not.
            var captain = await _userRepository.GetCaptainByFullNameAsync(existingMiqaat.CaptainName);
            if (captain != null)
            {
                await _miqaatMemberRepository.UpsertCaptainForMiqaat((int)captain.Id, id, AdminApprovalStatus.Approved);
            }
        }

        // Send email notifications when Admin approves/rejects Miqaat
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmails = await _userRepository.GetAdminEmailsAsync();
                var captain = await _userRepository.GetCaptainByFullNameAsync(existingMiqaat.CaptainName);
                
                var emailList = new List<string>();
                
                // Add admin emails
                emailList.AddRange(adminEmails);
                
                // Add captain email if found
                if (captain != null && !string.IsNullOrWhiteSpace(captain.Email))
                {
                    emailList.Add(captain.Email);
                }

                if (emailList.Any())
                {
                    var statusText = parsedStatus == AdminApprovalStatus.Approved ? "Approved" : 
                                    parsedStatus == AdminApprovalStatus.Rejected ? "Rejected" : "Pending";
                    var subject = $"Miqaat {statusText}: {existingMiqaat.MiqaatName}";
                    var body = $@"
                        <html>
                        <body>
                            <h2>Miqaat {statusText}</h2>
                            <p>The Miqaat has been <strong>{statusText}</strong> by Admin.</p>
                            <p><strong>Details:</strong></p>
                            <ul>
                                <li><strong>Miqaat Name:</strong> {existingMiqaat.MiqaatName}</li>
                                <li><strong>Type:</strong> {existingMiqaat.MiqaatType}</li>
                                <li><strong>Jamaat:</strong> {existingMiqaat.Jamaat}</li>
                                <li><strong>Jamiyat:</strong> {existingMiqaat.Jamiyat}</li>
                                <li><strong>From Date:</strong> {existingMiqaat.FromDate:yyyy-MM-dd}</li>
                                <li><strong>Till Date:</strong> {existingMiqaat.TillDate:yyyy-MM-dd}</li>
                                <li><strong>Volunteer Limit:</strong> {existingMiqaat.VolunteerLimit}</li>
                                <li><strong>Captain:</strong> {existingMiqaat.CaptainName}</li>
                                {(string.IsNullOrWhiteSpace(existingMiqaat.AboutMiqaat) ? "" : $"<li><strong>About:</strong> {existingMiqaat.AboutMiqaat}</li>")}
                            </ul>
                            <p>Status: <strong>{statusText}</strong></p>
                        </body>
                        </html>";

                    await _emailService.SendBulkEmailAsync(emailList, subject, body);

                    if (parsedStatus == AdminApprovalStatus.Approved && !string.IsNullOrWhiteSpace(existingMiqaat.Jamaat))
                    {
                        var allMemberEmails = new HashSet<string>();
                        // Split comma separated jamaats
                        var jamaatList = existingMiqaat.Jamaat.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(j => j.Trim())
                                                              .ToList();
                        foreach (var j in jamaatList)
                        {
                            var members = await _userRepository.GetMembersByJamaatAsync(j);
                            var memberEmails = members
                                .Where(m => !string.IsNullOrWhiteSpace(m.Email))
                                .Select(m => m.Email)
                                .ToList();
                                
                            foreach (var email in memberEmails) 
                            { 
                                if (!allMemberEmails.Contains(email))
                                    allMemberEmails.Add(email);
                            }
                        }

                        var finalEmails = allMemberEmails.ToList();
                        if (captain != null && !string.IsNullOrWhiteSpace(captain.Email) && !finalEmails.Contains(captain.Email))
                        {
                            finalEmails.Add(captain.Email);
                        }

                        if (finalEmails.Any())
                        {
                            var memberSubject = $"New Miqaat Approved: {existingMiqaat.MiqaatName}";
                            var memberBody = $@"
                                <html>
                                <body>
                                    <h2>New Miqaat Approved</h2>
                                    <p>A new Miqaat has been approved and is now available for enrollment.</p>
                                    <p><strong>Details:</strong></p>
                                    <ul>
                                        <li><strong>Miqaat Name:</strong> {existingMiqaat.MiqaatName}</li>
                                        <li><strong>Type:</strong> {existingMiqaat.MiqaatType}</li>
                                        <li><strong>Jamaat:</strong> {existingMiqaat.Jamaat}</li>
                                        <li><strong>Jamiyat:</strong> {existingMiqaat.Jamiyat}</li>
                                        <li><strong>From Date:</strong> {existingMiqaat.FromDate:yyyy-MM-dd}</li>
                                        <li><strong>Till Date:</strong> {existingMiqaat.TillDate:yyyy-MM-dd}</li>
                                        <li><strong>Volunteer Limit:</strong> {existingMiqaat.VolunteerLimit}</li>
                                        <li><strong>Captain:</strong> {existingMiqaat.CaptainName}</li>
                                        {(string.IsNullOrWhiteSpace(existingMiqaat.AboutMiqaat) ? "" : $"<li><strong>About:</strong> {existingMiqaat.AboutMiqaat}</li>")}
                                    </ul>
                                    <p>Please check the app for enrollment details.</p>
                                </body>
                                </html>";

                            await _emailService.SendBulkEmailAsync(finalEmails, memberSubject, memberBody);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email notification for miqaat approval: {ex.Message}");
            }
        });
    }

    public async Task Delete(long id)
    {
        var existingMiqaat = await _miqaatRepository.GetById(id);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        await _miqaatRepository.Delete(id);
    }

    public async Task<List<MiqaatResponse>> GetMiqaatsByMemberId(int memberId)
    {
        var miqaats = await _miqaatMemberRepository.GetMiqaatsByMemberId(memberId);
        return miqaats.Select(m => new MiqaatResponse(
            m.Id,
            m.MiqaatName,
            m.MiqaatType,
            m.Jamaat,
            m.Jamiyat,
            m.FromDate,
            m.TillDate,
            m.MiqaatDays,
            m.VolunteerLimit,
            m.AboutMiqaat,
            m.AdminApproval.ToString(),
            m.CaptainName,
            ConvertUtcToIst(m.CreatedAt),
            ConvertUtcToIst(m.UpdatedAt),
            m.MemberStatus,
            m.FinalStatus,
            null,
            null,
            null,
            null,
            false
        )).ToList();
    }

    public async Task<List<MiqaatResponse>> GetMiqaatsByCaptainName(string captainName)
    {
        var miqaats = await _miqaatRepository.GetByCaptainName(captainName);
        return miqaats.Select(m => MapToResponse(m, includeReport: true)).ToList();
    }

    public async Task<List<MiqaatResponse>> GetMiqaatsForCurrentUser(int userId, int? userRole, string? captainName)
    {
        // If user is Captain (role = 2), return all miqaats created by them
        if (userRole == 2 && !string.IsNullOrWhiteSpace(captainName))
        {
            return await GetMiqaatsByCaptainName(captainName);
        }
        // If user is Member (role = 1), return miqaats from miqaat_members table
        else
        {
            return await GetMiqaatsByMemberId(userId);
        }
    }

    public async Task UpdateMemberMiqaatStatus(int memberId, long miqaatId, string status, IReadOnlyCollection<int>? days)
    {
        // Validate status
        if (status != "Approved" && status != "Rejected" && status != "Pending")
        {
            throw new Exception("Invalid status. Must be 'Approved', 'Rejected', or 'Pending'");
        }

        if (days != null && days.Count > 0)
        {
            if (days.Any(d => d < 1))
            {
                throw new Exception("Day must be >= 1");
            }

            var miqaat = await _miqaatRepository.GetById(miqaatId);
            if (miqaat == null)
            {
                throw new Exception("Miqaat not found");
            }

            if (days.Any(d => d > miqaat.MiqaatDays))
            {
                throw new Exception($"Day must be between 1 and {miqaat.MiqaatDays}");
            }
        }

        await _miqaatMemberRepository.UpdateMemberMiqaatStatus(memberId, miqaatId, status, days);
    }

    public async Task<List<MemberPointsResponse>> GetMemberPointsByJamaat(string jamaat)
    {
        if (string.IsNullOrWhiteSpace(jamaat))
        {
            throw new Exception("Jamaat is required");
        }

        var members = await _miqaatMemberRepository.GetMemberPointsByJamaat(jamaat);
        return members.Select(m => new MemberPointsResponse(
            m.Id,
            m.FullName,
            m.ItsId,
            m.TotalPoints
        )).ToList();
    }

    public async Task<MemberPointsResponse> GetMemberPointsByMemberId(int memberId)
    {
        var member = await _miqaatMemberRepository.GetMemberPointsByMemberId(memberId);
        return new MemberPointsResponse(
            member.Id,
            member.FullName,
            member.ItsId,
            member.TotalPoints
        );
    }

    public async Task<List<AdminMemberPointsResponse>> GetAllMemberPointsForAdmin()
    {
        var members = await _miqaatMemberRepository.GetAllMemberPoints();
        return members.Select(m => new AdminMemberPointsResponse(
            m.Id,
            m.FullName,
            m.ItsId,
            m.Jamaat,
            m.TotalPoints
        )).ToList();
    }

    public async Task<List<EnrolledMemberResponse>> GetEnrolledMembersByMiqaatId(long miqaatId)
    {
        var members = await _miqaatMemberRepository.GetEnrolledMembersByMiqaatId(miqaatId);
        var finalStatuses = await _miqaatMemberRepository.GetFinalStatusesByMiqaatId(miqaatId);
        // Defaulting to day 1 for enrolled-members list (this endpoint is not day-specific)
        var attendanceStatuses = await _miqaatMemberRepository.GetAttendanceStatusesByMiqaatId(miqaatId, 1);
        
        return members.Select(m => new EnrolledMemberResponse(
            m.Id,
            m.FullName,
            m.Email,
            m.Contact,
            m.Rank,
            m.Jamaat,
            m.Jamiyat,
            finalStatuses.GetValueOrDefault(m.Id),
            m.ItsId,
            attendanceStatuses.GetValueOrDefault(m.Id),
            "Enrolled"  // These members have at least one approved day
        )).ToList();
    }

    public async Task<List<EnrolledMemberResponse>> GetAllMembersByMiqaatId(long miqaatId)
    {
        var membersWithStatus = await _miqaatMemberRepository.GetAllMembersByMiqaatId(miqaatId);
        var finalStatuses = await _miqaatMemberRepository.GetFinalStatusesByMiqaatId(miqaatId);
        var attendanceStatuses = await _miqaatMemberRepository.GetAttendanceStatusesByMiqaatId(miqaatId, 1);
        
        return membersWithStatus.Select(ms => new EnrolledMemberResponse(
            ms.Member.Id,
            ms.Member.FullName,
            ms.Member.Email,
            ms.Member.Contact,
            ms.Member.Rank,
            ms.Member.Jamaat,
            ms.Member.Jamiyat,
            finalStatuses.GetValueOrDefault(ms.Member.Id),
            ms.Member.ItsId,
            attendanceStatuses.GetValueOrDefault(ms.Member.Id),
            ms.StatusCategory
        )).ToList();
    }

    public async Task<List<EnrolledMemberResponse>> GetApprovedMembersForAttendance(long miqaatId, int day)
    {
        if (day < 1)
        {
            throw new Exception("Day must be >= 1");
        }

        var miqaat = await _miqaatRepository.GetById(miqaatId);
        if (miqaat == null)
        {
            throw new Exception("Miqaat not found");
        }
        if (day > miqaat.MiqaatDays)
        {
            throw new Exception($"Day must be between 1 and {miqaat.MiqaatDays}");
        }

        var members = await _miqaatMemberRepository.GetApprovedMembersForAttendance(miqaatId, day);
        var attendanceStatuses = await _miqaatMemberRepository.GetAttendanceStatusesByMiqaatId(miqaatId, day);
        
        return members.Select(m => new EnrolledMemberResponse(
            m.Id,
            m.FullName,
            m.Email,
            m.Contact,
            m.Rank,
            m.Jamaat,
            m.Jamiyat,
            "Approved", // All members from this method already have final_status = 'Approved'
            m.ItsId,
            attendanceStatuses.GetValueOrDefault(m.Id),
            "Enrolled"  // These members are fully approved
        )).ToList();
    }
    
    public async Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus, IReadOnlyCollection<int>? days)
    {
        // Validate final status
        if (finalStatus != "Approved" && finalStatus != "Rejected")
        {
            throw new Exception("Invalid final status. Must be 'Approved' or 'Rejected'");
        }

        await _miqaatMemberRepository.UpdateFinalStatus(memberId, miqaatId, finalStatus, days);
    }

    public async Task MarkAttendanceBatch(long miqaatId, int day, List<int> memberIds)
    {
        if (memberIds == null || !memberIds.Any())
        {
            throw new Exception("Member IDs list cannot be empty");
        }

        if (day < 1)
        {
            throw new Exception("Day must be >= 1");
        }

        var miqaat = await _miqaatRepository.GetById(miqaatId);
        if (miqaat == null)
        {
            throw new Exception("Miqaat not found");
        }
        if (day > miqaat.MiqaatDays)
        {
            throw new Exception($"Day must be between 1 and {miqaat.MiqaatDays}");
        }

        await _miqaatMemberRepository.MarkAttendanceBatch(miqaatId, day, memberIds);
    }

    public async Task<MemberMiqaatAttendanceHistoryResponse> GetMemberAttendanceHistory(int memberId)
    {
        var result = await _miqaatMemberRepository.GetMemberAttendanceHistory(memberId);
        var member = result.Member;
        var items = result.Items;
        var totalPoints = result.TotalPoints;

        return new MemberMiqaatAttendanceHistoryResponse(
            member.Id,
            member.FullName,
            member.ItsId,
            totalPoints,
            items.Select(i => new MemberMiqaatAttendanceItemResponse(
                i.Id,
                i.MiqaatName,
                i.FromDate,
                i.TillDate,
                i.MiqaatDays,
                i.MiqaatDay ?? 1,
                i.IsAttended ?? false,
                i.Points
            )).ToList()
        );
    }

    private static AdminApprovalStatus ParseApprovalStatus(string status)
    {
        if (Enum.TryParse<AdminApprovalStatus>(status, true, out var parsed))
        {
            return parsed;
        }

        throw new Exception("Invalid approval status. Must be 'Pending', 'Approved', or 'Rejected'");
    }

    public async Task UpdateMiqaatReport(long miqaatId, string? image1, string? image2, string? notes, string? khidmatDone)
    {
        var existingMiqaat = await _miqaatRepository.GetById(miqaatId);
        if (existingMiqaat == null)
        {
            throw new Exception("Miqaat not found");
        }

        await _miqaatRepository.UpdateMiqaatReport(miqaatId, image1, image2, notes, khidmatDone);
    }

    public async Task<bool> HasExistingReport(long miqaatId)
    {
        var miqaat = await _miqaatRepository.GetById(miqaatId);
        if (miqaat == null)
        {
            return false;
        }

        // Check if any report fields are filled
        return !string.IsNullOrWhiteSpace(miqaat.MiqaatImage1) ||
               !string.IsNullOrWhiteSpace(miqaat.MiqaatImage2) ||
               !string.IsNullOrWhiteSpace(miqaat.Notes);
    }

    public async Task<List<MemberEnrollmentDayResponse>> GetMemberEnrollmentDays(long miqaatId, int memberId)
    {
        var enrollmentDays = await _miqaatMemberRepository.GetMemberEnrollmentDays(miqaatId, memberId);
        return enrollmentDays.Select(d => new MemberEnrollmentDayResponse(
            d.MiqaatDay,
            d.Status,
            d.FinalStatus,
            d.MiqaatDate.ToString("dd MMM yyyy")
        )).ToList();
    }

    /// <summary>
    /// Helper to convert MiqaatModel to MiqaatResponse consistently
    /// </summary>
    private MiqaatResponse MapToResponse(MiqaatModel m, bool includeReport = false)
    {
        return new MiqaatResponse(
            m.Id,
            m.MiqaatName,
            m.MiqaatType,
            m.Jamaat,
            m.Jamiyat,
            m.FromDate,
            m.TillDate,
            m.MiqaatDays,
            m.VolunteerLimit,
            m.AboutMiqaat,
            m.AdminApproval.ToString(),
            m.CaptainName,
            ConvertUtcToIst(m.CreatedAt),
            ConvertUtcToIst(m.UpdatedAt),
            m.MemberStatus,
            m.FinalStatus,
            includeReport ? m.MiqaatImage1 : null,
            includeReport ? m.MiqaatImage2 : null,
            includeReport ? m.Notes : null,
            includeReport ? m.KhidmatDone : null,
            includeReport && m.IsReportSubmitted
        );
    }

    public async Task<MiqaatInsightsResponse> GetInsights()
    {
        var overallTask = _miqaatMemberRepository.GetMiqaatOverallStatsAsync();
        var detailTask = _miqaatMemberRepository.GetMiqaatDetailStatsAsync();
        var jamaatTask = _miqaatMemberRepository.GetJamaatInsightsAsync();
        var trendTask = _miqaatMemberRepository.GetMonthlyTrendAsync();

        await Task.WhenAll(overallTask, detailTask, jamaatTask, trendTask);

        var overall = overallTask.Result;
        var details = detailTask.Result;
        var jamaats = jamaatTask.Result;
        var trend = trendTask.Result;

        var overallStats = new MiqaatOverallStats(
            overall.TotalMiqaats,
            overall.ApprovedMiqaats,
            overall.PendingMiqaats,
            overall.RejectedMiqaats,
            overall.TotalEnrolled,
            overall.TotalAttended,
            overall.TotalLocal,
            overall.TotalInternational,
            overall.ReportsSubmitted
        );

        var miqaatItems = details.Select(d => new MiqaatInsightItem(
            d.Id,
            d.MiqaatName,
            d.MiqaatType,
            d.Jamaat,
            d.Jamiyat,
            d.FromDate.ToString("yyyy-MM-dd"),
            d.TillDate.ToString("yyyy-MM-dd"),
            d.MiqaatDays,
            d.VolunteerLimit,
            d.AdminApproval,
            d.CaptainName,
            d.TotalEnrolled,
            d.TotalApproved,
            d.TotalAttended,
            d.TotalPending,
            d.TotalRejected,
            d.IsReportSubmitted
        )).ToList();

        var jamaatItems = jamaats.Select(j => new JamaatInsightItem(
            j.Jamaat,
            j.TotalMiqaats,
            j.ApprovedMiqaats,
            j.TotalEnrolled,
            j.TotalAttended
        )).ToList();

        var monthlyTrend = trend.Select(t => new MonthlyMiqaatCount(t.MonthLabel, t.Count)).ToList();

        return new MiqaatInsightsResponse(overallStats, miqaatItems, jamaatItems, monthlyTrend);
    }

    public async Task<MiqaatDetailedInsightsResponse> GetMiqaatDetailedInsights(long miqaatId)
    {
        var miqaat = await _miqaatRepository.GetById(miqaatId);
        if (miqaat == null)
            throw new Exception("Miqaat not found");

        var rows = await _miqaatMemberRepository.GetAllMemberDayRowsForMiqaatAsync(miqaatId);

        // ── Day-wise stats ──────────────────────────────────────────────────────
        var dayStats = Enumerable.Range(1, miqaat.MiqaatDays).Select(day =>
        {
            var dayRows = rows.Where(r => r.Day == day).ToList();
            var dayDate = miqaat.FromDate.AddDays(day - 1).ToString("dd MMM yyyy");
            return new MiqaatDayStat(
                day,
                dayDate,
                dayRows.Count(r => r.Status == "Approved"),
                dayRows.Count(r => r.Status == "Pending"),
                dayRows.Count(r => r.Status == "Rejected"),
                dayRows.Count(r => r.IsAttended)
            );
        }).ToList();

        // ── Member-day matrix ────────────────────────────────────────────────────
        var memberGroups = rows
            .GroupBy(r => r.MemberId)
            .Select(g =>
            {
                var first = g.First();
                var dayEntries = g.OrderBy(r => r.Day).Select(r => new MiqaatMemberDayEntry(
                    r.Day,
                    miqaat.FromDate.AddDays(r.Day - 1).ToString("dd MMM yyyy"),
                    r.Status,
                    r.FinalStatus,
                    r.IsAttended
                )).ToList();

                return new MiqaatMemberDayDetail(
                    first.MemberId,
                    first.FullName,
                    first.ItsId,
                    first.Rank,
                    first.Jamaat,
                    first.Contact,
                    dayEntries
                );
            })
            .OrderBy(m => m.FullName)
            .ToList();

        // ── Summary counts ───────────────────────────────────────────────────────
        var uniqueMemberIds = rows.Select(r => r.MemberId).Distinct().ToHashSet();
        int totalUniqueMembers = uniqueMemberIds.Count;
        int totalEnrolledMembers = rows
            .Where(r => r.Status == "Approved")
            .Select(r => r.MemberId)
            .Distinct()
            .Count();
        int totalApprovedSlots = rows.Count(r => r.Status == "Approved");
        int totalAttendedSlots = rows.Count(r => r.IsAttended);
        int totalPendingSlots  = rows.Count(r => r.Status == "Pending");
        int totalRejectedSlots = rows.Count(r => r.Status == "Rejected");

        var summary = new MiqaatDetailSummary(
            miqaat.Id,
            miqaat.MiqaatName,
            miqaat.MiqaatType,
            miqaat.Jamaat ?? "",
            miqaat.Jamiyat ?? "",
            miqaat.FromDate.ToString("yyyy-MM-dd"),
            miqaat.TillDate.ToString("yyyy-MM-dd"),
            miqaat.MiqaatDays,
            miqaat.VolunteerLimit,
            miqaat.AdminApproval.ToString(),
            miqaat.CaptainName ?? "",
            miqaat.IsReportSubmitted,
            totalUniqueMembers,
            totalEnrolledMembers,
            totalApprovedSlots,
            totalAttendedSlots,
            totalPendingSlots,
            totalRejectedSlots
        );

        return new MiqaatDetailedInsightsResponse(summary, dayStats, memberGroups);
    }
}


