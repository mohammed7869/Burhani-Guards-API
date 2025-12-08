using System.ComponentModel.DataAnnotations.Schema;

namespace BurhaniGuards.Api.ViewModel;

[Table("users")]
public class UserViewModel
{
    public int id { get; set; }
    public string? profile { get; set; }
    public string? itsId { get; set; }
    public string rank { get; set; } = string.Empty;
    public int? roles { get; set; }
    public string? jamiyat { get; set; }
    public string? jamaat { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string? gender { get; set; }
    public string email { get; set; } = string.Empty;
    public int? age { get; set; }
    public string? contact { get; set; }
    public string? passwordHash { get; set; }
    public string? newPasswordHash { get; set; }
    public bool isActive { get; set; } = true;
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}

public class UserListViewModel
{
    public int id { get; set; }
    public string? itsId { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string rank { get; set; } = string.Empty;
    public int? roles { get; set; }
    public bool isActive { get; set; }
    public DateTime createdAt { get; set; }
}

public class CurrentUserViewModel
{
    public int id { get; set; }
    public string? itsId { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string rank { get; set; } = string.Empty;
    public int? roles { get; set; }
    public bool requiresPasswordChange { get; set; }
}

public class UserCreateViewModel
{
    public string? itsId { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string rank { get; set; } = "Member";
    public int? roles { get; set; }
    public string? jamiyat { get; set; }
    public string? jamaat { get; set; }
    public string? gender { get; set; }
    public int? age { get; set; }
    public string? contact { get; set; }
    public string? password { get; set; }
}

public class UserEditViewModel
{
    public int id { get; set; }
    public string? itsId { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string rank { get; set; } = string.Empty;
    public int? roles { get; set; }
    public string? jamiyat { get; set; }
    public string? jamaat { get; set; }
    public string? gender { get; set; }
    public int? age { get; set; }
    public string? contact { get; set; }
}

