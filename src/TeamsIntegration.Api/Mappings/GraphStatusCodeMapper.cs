namespace TeamsIntegration.Api.Mappings;

public static class GraphStatusCodeMapper
{
    /// <summary>
    /// Convert "status code" of Teams Api responses to suitable for the application status code.
    /// Example, if Teams Api Service responses 429(To Many Request) status code, it shouldn't be the 
    /// application status code. The Application should return 502(Bad Gateway) response to client. 
    /// This function provides this.
    /// </summary>
    /// <param name="graphStatusCode"></param>
    /// <returns></returns>
    public static int Map(int graphStatusCode)
    {
        return graphStatusCode switch
        {
            400 => StatusCodes.Status400BadRequest,
            404 => StatusCodes.Status404NotFound,
            429 => StatusCodes.Status503ServiceUnavailable,
            401 or 403 => StatusCodes.Status502BadGateway,
            >= 500 and <= 599 => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway
        };
    }
}
