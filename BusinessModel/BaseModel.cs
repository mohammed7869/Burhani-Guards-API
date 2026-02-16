namespace BurhaniGuards.Api.BusinessModel;

public class BaseModel
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }
}

