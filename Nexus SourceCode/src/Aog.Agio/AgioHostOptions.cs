using System.ComponentModel.DataAnnotations;

namespace Aog.Agio;

/// <summary>
/// Options that control the AGiO host.
/// </summary>
public sealed class AgioHostOptions
{
    /// <summary>
    /// Gets or sets the backend configuration.
    /// </summary>
    [Required]
    public BackendOptions Backend { get; set; } = new();

    /// <summary>
    /// Configuration for selecting a backend implementation.
    /// </summary>
    public sealed class BackendOptions
    {
        /// <summary>
        /// Gets or sets the assembly name containing the backend implementation.
        /// </summary>
        [Required]
        public string Assembly { get; set; } = "Aog.Agio.Sim";

        /// <summary>
        /// Gets or sets the fully qualified type name of the backend implementation.
        /// </summary>
        [Required]
        public string Type { get; set; } = "Aog.Agio.Sim.SimAgioBackend";
    }
}
