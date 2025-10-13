using System.Globalization;
using System.Reflection;

namespace Aog.Agio;

/// <summary>
/// Reflection-based loader that instantiates AGiO backends from configuration.
/// </summary>
public static class AgioBackendLoader
{
    /// <summary>
    /// Loads the backend described by <paramref name="options"/>.
    /// </summary>
    /// <param name="options">Backend selection options.</param>
    /// <returns>The instantiated backend.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the backend cannot be loaded.</exception>
    public static IAgioBackend Load(AgioHostOptions.BackendOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Assembly))
        {
            throw new InvalidOperationException("AGiO backend assembly must be specified.");
        }

        if (string.IsNullOrWhiteSpace(options.Type))
        {
            throw new InvalidOperationException("AGiO backend type must be specified.");
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(new AssemblyName(options.Assembly));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Failed to load AGiO backend assembly '{0}'.", options.Assembly),
                ex);
        }

        var backendType = assembly.GetType(options.Type, throwOnError: false);
        if (backendType is null)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Type '{0}' was not found in assembly '{1}'.", options.Type, options.Assembly));
        }

        if (!typeof(IAgioBackend).IsAssignableFrom(backendType))
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Type '{0}' does not implement {1}.", options.Type, nameof(IAgioBackend)));
        }

        try
        {
            return (IAgioBackend)Activator.CreateInstance(backendType)!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Failed to instantiate AGiO backend type '{0}'.", options.Type),
                ex);
        }
    }
}
