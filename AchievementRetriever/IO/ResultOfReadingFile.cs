using System;

namespace AchievementRetriever.IO;

public class ResultOfReadingFile<T>
{
    public bool Success { get; }
    public T Result { get; }
    public Exception Error { get; }
    
    private ResultOfReadingFile(T result)
    {
        Success = true;
        Result = result;
    }

    private ResultOfReadingFile(Exception error)
    {
        Success = false;
        Error = error;
    }
    
    public static ResultOfReadingFile<T> Ok(T value) => new(value);
    public static ResultOfReadingFile<T> Fail(Exception ex) => new(ex);
    
    public void Deconstruct(out bool success, out T result, out Exception error)
    {
        success = Success;
        result = Result;
        error = Error;
    }
    
    public static implicit operator bool(ResultOfReadingFile<T> result) => result.Success;
}