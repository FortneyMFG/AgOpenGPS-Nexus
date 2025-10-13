using System.IO;

namespace Aog.Core.Legacy;

/// <summary>
/// Defines the contract for importing legacy guidance data into Core-friendly structures.
/// </summary>
public interface ILegacyGuidanceImportService
{
    /// <summary>
    /// Imports legacy AB lines and boundaries into a structured result.
    /// </summary>
    /// <param name="fieldName">Friendly name of the field being imported.</param>
    /// <param name="abLineCsv">Stream containing the legacy AB line CSV export.</param>
    /// <param name="boundaryShapefilePath">Path to the ESRI shapefile containing the field boundary.</param>
    /// <returns>Structured import result ready for Core consumption.</returns>
    LegacyGuidanceImportResult Import(string fieldName, Stream abLineCsv, string boundaryShapefilePath);
}
