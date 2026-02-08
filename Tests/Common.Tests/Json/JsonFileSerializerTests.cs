using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.NETStd.Json;
using Xunit;

namespace SwiftXP.SPT.Common.NETStd.Tests.Json;

public class JsonFileSerializerTests
{
    private sealed class TempDirectory : IDisposable
    {
        public DirectoryInfo DirInfo { get; }

        public TempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            DirInfo = Directory.CreateDirectory(path);
        }

        public string GetPath(string fileName) => Path.Combine(DirInfo.FullName, fileName);

        public void Dispose()
        {
            if (DirInfo.Exists)
            {
                try { DirInfo.Delete(true); } catch { }
            }
        }
    }

    private sealed class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public double Value { get; set; }
    }

    [Fact]
    public async Task SerializeJsonFileAsyncCreatesFileAndWritesData()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("output.json");

        TestData data = new() { Name = "Newtonsoft", Id = 101, Value = 99.9 };
        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);

        Assert.True(File.Exists(filePath));

        string jsonContent = await File.ReadAllTextAsync(filePath);

        Assert.Contains("Newtonsoft", jsonContent);
        Assert.Contains("101", jsonContent);
    }

    [Fact]
    public async Task SerializeJsonFileAsyncCreatesMissingDirectories()
    {
        using TempDirectory temp = new();
        string filePath = Path.Combine(temp.DirInfo.FullName, "sub", "folder", "config.json");
        TestData data = new() { Name = "DeepDirectory" };
        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);

        Assert.True(File.Exists(filePath));
        Assert.True(Directory.Exists(Path.GetDirectoryName(filePath)));
    }

    [Fact]
    public async Task SerializeJsonFileAsyncUsesIndentation()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("format.json");
        TestData data = new() { Name = "A", Id = 1 };
        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);

        string[] lines = await File.ReadAllLinesAsync(filePath);

        Assert.True(lines.Length > 2, "JSON should be formatted (indented), but looks like single line.");
    }

    [Fact]
    public async Task DeserializeJsonFileAsyncReadsDataCorrectly()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("input.json");

        await File.WriteAllTextAsync(filePath, "{ \"Name\": \"Reader\", \"Id\": 555 }");

        JsonFileSerializer serializer = new();

        TestData? result = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(result);
        Assert.Equal("Reader", result.Name);
        Assert.Equal(555, result.Id);
    }

    [Fact]
    public async Task DeserializeJsonFileAsyncHandlesFloatAsDouble()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("float.json");

        await File.WriteAllTextAsync(filePath, "{ \"Value\": 123.456789 }");

        JsonFileSerializer serializer = new();

        TestData? result = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(result);
        Assert.Equal(123.456789, result.Value);
    }

    [Fact]
    public async Task DeserializeJsonFileAsyncThrowsFileNotFoundIfFileMissing()
    {
        using TempDirectory temp = new();
        string missingPath = temp.GetPath("ghost.json");
        JsonFileSerializer serializer = new();

        FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            serializer.DeserializeJsonFileAsync<TestData>(missingPath));

        Assert.Equal("JSON file not found", ex.Message);
    }

    [Fact]
    public async Task SerializeJsonFileAsyncOverwritesExistingFile()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("overwrite.json");
        JsonFileSerializer serializer = new();

        await File.WriteAllTextAsync(filePath, "OLD CONTENT");

        await serializer.SerializeJsonFileAsync(filePath, new TestData { Name = "New" });

        string content = await File.ReadAllTextAsync(filePath);

        Assert.DoesNotContain("OLD CONTENT", content);
        Assert.Contains("New", content);
    }

    [Fact]
    public async Task SerializeAndDeserializeComplexScenario()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("roundtrip.json");
        TestData original = new() { Name = "RoundTrip", Id = 999, Value = 1.23 };
        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, original);
        TestData? loaded = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(loaded);
        Assert.Equal(original.Name, loaded.Name);
        Assert.Equal(original.Id, loaded.Id);
        Assert.Equal(original.Value, loaded.Value);
    }
}