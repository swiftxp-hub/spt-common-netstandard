using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SwiftXP.SPT.Common.NETStd.Json;

public class JsonFileSerializer : IJsonFileSerializer
{
    private static readonly JsonSerializerSettings s_jsonOptions = new()
    {
        Formatting = Formatting.Indented,
    };

    public async Task<T?> DeserializeJsonFileAsync<T>(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("JSON file not found", filePath);

        JsonSerializer serializer = JsonSerializer.Create(s_jsonOptions);

        using FileStream contentStream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        using StreamReader streamReader = new(contentStream);
        using JsonTextReader jsonTextReader = new(streamReader);

        jsonTextReader.FloatParseHandling = FloatParseHandling.Double;

        return await Task.Run(() =>
        {
            return serializer.Deserialize<T>(jsonTextReader);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SerializeJsonFileAsync<T>(
        string filePath,
        T value,
        CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        JsonSerializer serializer = JsonSerializer.Create(s_jsonOptions);

        using FileStream fileStream = new(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        using StreamWriter streamWriter = new(fileStream);
        using JsonTextWriter jsonWriter = new(streamWriter);

        jsonWriter.Formatting = serializer.Formatting;

        await Task.Run(() =>
        {
            serializer.Serialize(jsonWriter, value);
        }, cancellationToken).ConfigureAwait(false);

        await streamWriter.FlushAsync().ConfigureAwait(false);
    }
}