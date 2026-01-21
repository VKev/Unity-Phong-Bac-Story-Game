using System;

public static class PanelTimelineIdUtility
{
    public static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim().ToLowerInvariant();
    }
}
