using System.Collections.Generic;
using System.IO;

namespace AchievementRetriever.IO;

public interface IFileService
{
    ResultOfReadingFile<IEnumerable<string>> TryGetFilesFromConfig(string directoryPath, string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly);
}