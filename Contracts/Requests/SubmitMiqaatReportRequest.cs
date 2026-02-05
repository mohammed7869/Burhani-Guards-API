using Microsoft.AspNetCore.Http;

namespace BurhaniGuards.Api.Contracts.Requests;

public class SubmitMiqaatReportRequest
{
    public IFormFile? Image1 { get; set; }
    public IFormFile? Image2 { get; set; }
    public string? Notes { get; set; }
}
