namespace Project.API.Security;

public static class AccessTokenReader
{
    public static bool TryRead(string? authorization, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return false;
        }

        var value = authorization.Trim();
        while (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            value = value["Bearer ".Length..].Trim();
        }

        if (value.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = value.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        token = value;
        return true;
    }
}
