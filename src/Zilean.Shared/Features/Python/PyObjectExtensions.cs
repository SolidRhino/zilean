namespace Zilean.Shared.Features.Python;

/// <summary>
/// Provides extension methods for <see cref="PyObject"/> interop helpers.
/// </summary>
public static class PyObjectExtensions
{
    /// <summary>
    /// Determines whether the given Python dict-like object contains the specified key.
    /// </summary>
    /// <param name="dict">The Python object to check (expected to support <c>__contains__</c>).</param>
    /// <param name="key">The key to look up.</param>
    /// <returns><see langword="true"/> if the key is present; otherwise <see langword="false"/>.</returns>
    public static bool HasKey(this PyObject dict, string key) =>
        dict.InvokeMethod("__contains__", new PyString(key)).As<bool>();
}