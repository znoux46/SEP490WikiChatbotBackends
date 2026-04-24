using Newtonsoft.Json;

namespace WikiChatbotBackends.Application.DTOs;

public class WikipediaParseResponse
{
    [JsonProperty("parse")]
    public WikipediaParseData? Parse { get; set; }
}

public class WikipediaParseData
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty("pageid")]
    public int PageId { get; set; }
    [JsonProperty("text")]
    public WikipediaTextContent? Text { get; set; }
    [JsonProperty("images")]
    public List<string>? Images { get; set; }
    [JsonProperty("displaytitle")]
    public string? DisplayTitle { get; set; }
}

public class WikipediaTextContent
{
    [JsonProperty("*")]
    public string Content { get; set; } = string.Empty;
}
