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

    public async Task<MiqaatResponse> Create(CreateMiqaatRequest request, string captainName)
    {
        var model = new MiqaatModel
        {
            MiqaatName = request.MiqaatName,
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

        return new MiqaatResponse(
            createdMiqaat.Id,
            createdMiqaat.MiqaatName,
            createdMiqaat.Jamaat,
            createdMiqaat.Jamiyat,
            createdMiqaat.FromDate,
            createdMiqaat.TillDate,
            createdMiqaat.MiqaatDays,
            createdMiqaat.VolunteerLimit,
            createdMiqaat.AboutMiqaat,
            createdMiqaat.AdminApproval.ToString(),
            createdMiqaat.CaptainName,
            ConvertUtcToIst(createdMiqaat.CreatedAt),
            ConvertUtcToIst(createdMiqaat.UpdatedAt),
            null,
            null,
            null,
            null,
            null,
            null,
            false
        );
    }

    public async Task<List<MiqaatResponse>> GetAll()
    {
        var miqaats = await _miqaatRepository.GetAll();
        return miqaats.Select(m => new MiqaatResponse(
            m.Id,
            m.MiqaatName,
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
            null,
            null,
            m.MiqaatImage1,
            m.MiqaatImage2,
            m.Notes,
            m.KhidmatDone,
            m.IsReportSubmitted
        )).ToList();
    }

    public async Task<MiqaatResponse?> GetById(long id)
    {
        var miqaat = await _miqaatRepository.GetById(id);
        if (miqaat == null)
        {
            return null;
        }

        return new MiqaatResponse(
            miqaat.Id,
            miqaat.MiqaatName,
            miqaat.Jamaat,
            miqaat.Jamiyat,
            miqaat.FromDate,
            miqaat.TillDate,
            miqaat.MiqaatDays,
            miqaat.VolunteerLimit,
            miqaat.AboutMiqaat,
            miqaat.AdminApproval.ToString(),
            miqaat.CaptainName,
            ConvertUtcToIst(miqaat.CreatedAt),
            ConvertUtcToIst(miqaat.UpdatedAt),
            null,
            null,
            miqaat.MiqaatImage1,
            miqaat.MiqaatImage2,
            miqaat.Notes,
            miqaat.KhidmatDone,
            miqaat.IsReportSubmitted
        );
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
            
            // Auto-approve captain who created the miqaat for all days
            var captain = await _userRepository.GetCaptainByFullNameAsync(existingMiqaat.CaptainName);
            if (captain != null)
            {
                try
                {
                    await _miqaatMemberRepository.UpdateMemberMiqaatStatus((int)captain.Id, id, "Approved", null);
                }
                catch
                {
                    // Ignore if captain is not in miqaat_members (e.g., different jamaat)
                }
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

                    // If approved, also send email to all members from the same jamiyat
                    if (parsedStatus == AdminApprovalStatus.Approved && !string.IsNullOrWhiteSpace(existingMiqaat.Jamaat))
                    {
                        var members = await _userRepository.GetMembersByJamaatAsync(existingMiqaat.Jamaat);
                        var memberEmails = members
                            .Where(m => !string.IsNullOrWhiteSpace(m.Email))
                            .Select(m => m.Email)
                            .ToList();

                        // Add captain email to member list if not already included
                        if (captain != null && !string.IsNullOrWhiteSpace(captain.Email) && !memberEmails.Contains(captain.Email))
                        {
                            memberEmails.Add(captain.Email);
                        }

                        if (memberEmails.Any())
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

                            await _emailService.SendBulkEmailAsync(memberEmails, memberSubject, memberBody);
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
        return miqaats.Select(m => new MiqaatResponse(
            m.Id,
            m.MiqaatName,
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
            null,
            null,
            m.MiqaatImage1,
            m.MiqaatImage2,
            m.Notes,
            m.KhidmatDone,
            m.IsReportSubmitted
        )).ToList();
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
    
    public async Task UpdateFinalStatus(int memberId, long miqaatId, string finalStatus)
    {
        // Validate final status
        if (finalStatus != "Approved" && finalStatus != "Rejected")
        {
            throw new Exception("Invalid final status. Must be 'Approved' or 'Rejected'");
        }

        await _miqaatMemberRepository.UpdateFinalStatus(memberId, miqaatId, finalStatus);
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
}

