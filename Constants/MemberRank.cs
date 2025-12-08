namespace BurhaniGuards.Api.Constants;

/// <summary>
/// Member rank constants matching the roles in the database
/// </summary>
public static class MemberRank
{
    public const int Member = 1;
    public const int Captain = 2;
    public const int ViceCaptain = 3;
    public const int AsstGroupLeader = 4;
    public const int GroupLeader = 5;
    public const int MajorCaptain = 6;
    public const int ResourceAdmin = 7;
    public const int AssistantCommander = 8;

    /// <summary>
    /// Gets rank text from role ID
    /// </summary>
    public static string GetRankText(int? roleId)
    {
        return roleId switch
        {
            Member => "Member",
            Captain => "Captain",
            ViceCaptain => "Vice Captain",
            AsstGroupLeader => "Asst. Group Leader",
            GroupLeader => "Group Leader",
            MajorCaptain => "Major (Captain)",
            ResourceAdmin => "Resource Admin",
            AssistantCommander => "Assistant Commander",
            _ => "Member"
        };
    }

    /// <summary>
    /// Gets role ID from rank text
    /// </summary>
    public static int? GetRoleId(string rankText)
    {
        return rankText?.Trim() switch
        {
            "Member" => Member,
            "Captain" => Captain,
            "Vice Captain" => ViceCaptain,
            "Asst. Group Leader" => AsstGroupLeader,
            "Group Leader" => GroupLeader,
            "Major (Captain)" => MajorCaptain,
            "Resource Admin" => ResourceAdmin,
            "Assistant Commander" => AssistantCommander,
            _ => null
        };
    }
}

