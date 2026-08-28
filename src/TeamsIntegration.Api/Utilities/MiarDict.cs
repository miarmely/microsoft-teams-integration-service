namespace TeamsIntegration.Api.Utilities;

public static class MiarDict
{
    /// <summary>
    /// Convert dict to URL encoded string.
    /// </summary>
    /// <typeparam name="TDictVal"></typeparam>
    /// <param name="dict"></param>
    /// <returns></returns>
    public static string ConvertDictToUrlQuery<TDictVal>(
        Dictionary<string, TDictVal> dict)
    {
        // convert dict to "["k1=v1", "k2=v2", "k3=v3"...]"
        var keyValueList = dict
            .Where(pair => pair.Key != null
                && !string.IsNullOrWhiteSpace(pair.Key.ToString()))
            .Select(pair =>
            {
                var queryKey = Uri.EscapeDataString(pair.Key!.ToString()!);
                var queryVal = Uri.EscapeDataString(pair.Value?.ToString() ?? string.Empty);

                return $"{queryKey}={queryVal}";
            });

        return string.Join(
            "&",
            keyValueList);
    }
}
