using System;
using System.IO;
using System.Runtime.CompilerServices;
using Aog.Core.V1;
using Google.Protobuf;

namespace Aog.Bridge.Host.AogLink.Legacy;

/// <summary>
/// Provides helper methods for attaching legacy metadata to steer state messages.
/// </summary>
public static class LegacySteerStateExtensions
{
    private const int LegacyHeadingFieldNumber = 65000;

    private sealed class LegacySteerStateMetadataHolder
    {
        public double? HeadingDegrees { get; set; }
    }

    private static readonly ConditionalWeakTable<SteerState, LegacySteerStateMetadataHolder> Metadata = new();

    /// <summary>
    /// Associates the provided legacy heading value (degrees) with the steer state instance.
    /// </summary>
    public static void SetLegacyHeadingDegrees(this SteerState state, double headingDegrees)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var holder = Metadata.GetOrCreateValue(state);
        holder.HeadingDegrees = headingDegrees;

        state.UnknownFields = PersistLegacyHeading(state.UnknownFields, headingDegrees);
    }

    /// <summary>
    /// Attempts to read the legacy heading value (degrees) previously attached to the steer state.
    /// </summary>
    public static bool TryGetLegacyHeadingDegrees(this SteerState? state, out double headingDegrees)
    {
        if (state is null)
        {
            headingDegrees = default;
            return false;
        }

        if (Metadata.TryGetValue(state, out var holder) && holder.HeadingDegrees is double value)
        {
            headingDegrees = value;
            return true;
        }

        if (state.UnknownFields is not null
            && TryReadLegacyHeadingFromUnknownFields(state.UnknownFields, out var persistedHeading))
        {
            Metadata.GetOrCreateValue(state).HeadingDegrees = persistedHeading;
            headingDegrees = persistedHeading;
            return true;
        }

        headingDegrees = default;
        return false;
    }

    /// <summary>
    /// Removes any previously attached legacy heading metadata from the steer state instance.
    /// </summary>
    public static void ClearLegacyHeadingDegrees(this SteerState state)
    {
        if (state is null)
        {
            return;
        }

        Metadata.Remove(state);

        state.UnknownFields = RemoveLegacyHeadingField(state.UnknownFields);
    }

    private static UnknownFieldSet? PersistLegacyHeading(UnknownFieldSet? existingFields, double headingDegrees)
    {
        var filteredFields = RemoveLegacyHeadingField(existingFields);

        using var buffer = new MemoryStream();
        using (var writer = new CodedOutputStream(buffer, leaveOpen: true))
        {
            writer.WriteTag(LegacyHeadingFieldNumber, WireFormat.WireType.Fixed64);
            writer.WriteDouble(headingDegrees);
            writer.Flush();
        }

        buffer.Position = 0;
        using var reader = new CodedInputStream(buffer, leaveOpen: true);
        var tag = reader.ReadTag();
        return tag == 0
            ? filteredFields
            : UnknownFieldSet.MergeFieldFrom(filteredFields, reader);
    }

    private static UnknownFieldSet? RemoveLegacyHeadingField(UnknownFieldSet? existingFields)
    {
        if (existingFields is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        using (var writer = new CodedOutputStream(buffer, leaveOpen: true))
        {
            existingFields.WriteTo(writer);
            writer.Flush();
        }

        buffer.Position = 0;
        using var reader = new CodedInputStream(buffer, leaveOpen: true);
        UnknownFieldSet? filtered = null;
        while (reader.ReadTag() is uint tag and not 0)
        {
            if (WireFormat.GetTagFieldNumber(tag) == LegacyHeadingFieldNumber)
            {
                reader.SkipLastField();
                continue;
            }

            filtered = UnknownFieldSet.MergeFieldFrom(filtered, reader);
        }

        return filtered;
    }

    private static bool TryReadLegacyHeadingFromUnknownFields(
        UnknownFieldSet unknownFields,
        out double headingDegrees)
    {
        using var buffer = new MemoryStream();
        using (var writer = new CodedOutputStream(buffer, leaveOpen: true))
        {
            unknownFields.WriteTo(writer);
            writer.Flush();
        }

        buffer.Position = 0;
        using var reader = new CodedInputStream(buffer, leaveOpen: true);
        while (reader.ReadTag() is uint tag and not 0)
        {
            if (WireFormat.GetTagFieldNumber(tag) != LegacyHeadingFieldNumber)
            {
                reader.SkipLastField();
                continue;
            }

            switch (WireFormat.GetTagWireType(tag))
            {
                case WireFormat.WireType.Fixed64:
                    headingDegrees = reader.ReadDouble();
                    return true;
                case WireFormat.WireType.LengthDelimited:
                {
                    var payload = reader.ReadBytes();
                    if (payload.Length == sizeof(double))
                    {
                        headingDegrees = BitConverter.ToDouble(payload.ToByteArray(), 0);
                        return true;
                    }

                    if (double.TryParse(payload.ToStringUtf8(), out var parsed))
                    {
                        headingDegrees = parsed;
                        return true;
                    }

                    break;
                }
                case WireFormat.WireType.Varint:
                    headingDegrees = BitConverter.Int64BitsToDouble((long)reader.ReadUInt64());
                    return true;
                case WireFormat.WireType.Fixed32:
                    headingDegrees = reader.ReadFloat();
                    return true;
                default:
                    reader.SkipLastField();
                    break;
            }
        }

        headingDegrees = default;
        return false;
    }
}
