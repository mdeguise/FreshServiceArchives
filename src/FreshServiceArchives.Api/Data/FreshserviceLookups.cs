using FreshServiceArchives.Api.Models;

namespace FreshServiceArchives.Api.Data;

/// <summary>
/// Maps Freshservice numeric enum codes to human-readable text.
/// Values follow the Freshservice API v2 defaults.
/// </summary>
public static class FreshserviceLookups
{
    public static readonly IReadOnlyDictionary<int, string> Statuses = new Dictionary<int, string>
    {
        [2] = "Open",
        [3] = "Pending",
        [4] = "Resolved",
        [5] = "Closed",
    };

    public static readonly IReadOnlyDictionary<int, string> Priorities = new Dictionary<int, string>
    {
        [1] = "Low",
        [2] = "Medium",
        [3] = "High",
        [4] = "Urgent",
    };

    public static readonly IReadOnlyDictionary<int, string> Sources = new Dictionary<int, string>
    {
        [1] = "Email",
        [2] = "Portal",
        [3] = "Phone",
        [4] = "Chat",
        [5] = "Feedback Widget",
        [6] = "Yammer",
        [7] = "AWS Cloudwatch",
        [8] = "Pagerduty",
        [9] = "Walk-up",
        [10] = "Slack",
    };

    public static string? StatusText(int? code) => Text(Statuses, code);
    public static string? PriorityText(int? code) => Text(Priorities, code);
    public static string? SourceText(int? code) => Text(Sources, code);

    private static string? Text(IReadOnlyDictionary<int, string> map, int? code)
    {
        if (code is null) return null;
        return map.TryGetValue(code.Value, out var text) ? text : code.Value.ToString();
    }

    public static IEnumerable<CodeLookup> AsLookups(IReadOnlyDictionary<int, string> map) =>
        map.Select(kv => new CodeLookup { Value = kv.Key, Text = kv.Value });
}
