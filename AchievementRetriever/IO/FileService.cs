using System;
using System.Collections.Generic;
using System.IO;

namespace AchievementRetriever.IO;

public class FileService : IFileService
{
    public ResultOfReadingFile<IEnumerable<string>> TryGetFilesFromConfig(string directoryPath, string searchPattern = "*.*", 
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return ResultOfReadingFile<IEnumerable<string>>.Fail(new ArgumentException("Directory path is missing or empty."));

            if (!Directory.Exists(directoryPath))
                return ResultOfReadingFile<IEnumerable<string>>.Fail(new DirectoryNotFoundException($"Directory not found: {directoryPath}"));

            var files = Directory.GetFiles(directoryPath, searchPattern, searchOption);
            return ResultOfReadingFile<IEnumerable<string>>.Ok(files);
        }
        catch (Exception ex)
        {
            return ResultOfReadingFile<IEnumerable<string>>.Fail(ex);
        }
    }
}