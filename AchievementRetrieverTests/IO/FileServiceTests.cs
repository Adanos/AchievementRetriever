namespace AchievementRetrieverTests.IO;

using AchievementRetriever.IO;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

[TestFixture]
public class FileServiceTests
{
    private string _tempDir = null!;
    private IFileService _service = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _service = new FileService();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void TryGetFilesFromConfig_ReturnsFiles_WhenDirectoryExists()
    {
        var file1 = Path.Combine(_tempDir, "file1.txt");
        var file2 = Path.Combine(_tempDir, "file2.txt");
        File.WriteAllText(file1, "test");
        File.WriteAllText(file2, "test");
        
        var result = _service.TryGetFilesFromConfig(_tempDir);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Result.Count(), Is.EqualTo(2));
    }

    [Test]
    public void TryGetFilesFromConfig_Fails_WhenDirectoryDoesNotExist()
    {
        var fakePath = Path.Combine(_tempDir, "not-exist");
        
        var result = _service.TryGetFilesFromConfig(fakePath);
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.TypeOf<DirectoryNotFoundException>());
    }

    [Test]
    public void TryGetFilesFromConfig_Fails_WhenPathIsNullOrEmpty()
    {
        var result = _service.TryGetFilesFromConfig("");
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.TypeOf<ArgumentException>());
    }
}