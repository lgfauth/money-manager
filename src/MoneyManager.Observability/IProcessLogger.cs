namespace MoneyManager.Observability;

public interface IProcessLogger
{
    /// <summary>
    /// Starts a new process log with the given name.
    /// </summary>
    void Start(string processName, Dictionary<string, object?>? context = null);

    /// <summary>
    /// Adds an informational step to the current process log.
    /// </summary>
    void AddStep(string message, Dictionary<string, object?>? data = null);

    /// <summary>
    /// Adds a warning step to the current process log.
    /// </summary>
    void AddWarning(string message, Dictionary<string, object?>? data = null);

    /// <summary>
    /// Adds an error step to the current process log.
    /// </summary>
    void AddError(string message, Exception? exception = null, Dictionary<string, object?>? data = null);

    /// <summary>
    /// Adds or updates a key in the context dictionary after Start().
    /// </summary>
    void AddContext(string key, object? value);

    /// <summary>
    /// Finishes the current process log and flushes the structured JSON.
    /// </summary>
    void Finish(bool success = true, Exception? exception = null);
}
