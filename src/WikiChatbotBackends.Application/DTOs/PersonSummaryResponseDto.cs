using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.DTOs;

public class PersonSummaryResponseDto
{
    public string Status { get; set; } = "error";
    public PersonSummaryDataDto? Data { get; set; }
    public string? Message { get; set; }
}

