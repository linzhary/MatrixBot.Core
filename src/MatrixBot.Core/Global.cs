using System.Text.Json;

namespace MatrixBot.Core;

internal class Global
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

}
