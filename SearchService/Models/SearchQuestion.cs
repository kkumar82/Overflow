using System.Text.Json.Serialization;

namespace SearchService.Models;

public class SearchQuestion
{
    [JsonPropertyName("id")]
    public required string Id { get; set; } // no initialization; we get Id from Question Service
    [JsonPropertyName("title")]
    public required string Title { get; set; } // typesense will index based on this property
    [JsonPropertyName("content")]
    public required string Content { get; set; } //typesense will index based on this property
    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];
    [JsonPropertyName("createdAt")] 
    public long CreatedAt { get; set; } = 0;
    [JsonPropertyName("hasAcceptedAnswer")]
    public bool HasAcceptedAnswer { get; set; }
    [JsonPropertyName("answerCount")]
    public int AnswerCount { get; set; }
}