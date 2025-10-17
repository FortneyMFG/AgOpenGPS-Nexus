using System;
using System.Collections;
using System.Reflection;
using Aog.Core.V1;
using Google.Protobuf;

namespace Aog.Bridge.Host.AogLink.Legacy;

/// <summary>
/// Provides helper methods for attaching legacy metadata to steer state messages.
/// </summary>
public static class LegacySteerStateExtensions
{
    private const int LegacyHeadingFieldNumber = 65000;

    private static readonly FieldInfo FieldsField = typeof(UnknownFieldSet)
        .GetField("fields", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Unable to access unknown field map.");

    private static readonly Type UnknownFieldType = typeof(UnknownFieldSet).Assembly.GetType("Google.Protobuf.UnknownField")
        ?? throw new InvalidOperationException("Unable to resolve Google.Protobuf.UnknownField type.");

    private static readonly MethodInfo AddFixed64Method = UnknownFieldType.GetMethod(
            "AddFixed64",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(ulong) },
            modifiers: null)
        ?? throw new InvalidOperationException("Unable to locate AddFixed64 helper.");

    private static readonly FieldInfo Fixed64ListField = UnknownFieldType.GetField(
            "fixed64List",
            BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Unable to access fixed64List field.");

    /// <summary>
    /// Associates the provided legacy heading value (degrees) with the steer state instance.
    /// </summary>
    public static void SetLegacyHeadingDegrees(this SteerState state, double headingDegrees)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        var unknownFields = state.UnknownFields ?? CreateUnknownFieldSet();
        var fields = GetFieldsDictionary(unknownFields);
        fields[LegacyHeadingFieldNumber] = CreateFixed64Field(headingDegrees);
        state.UnknownFields = unknownFields;
    }

    /// <summary>
    /// Attempts to read the legacy heading value (degrees) previously attached to the steer state.
    /// </summary>
    public static bool TryGetLegacyHeadingDegrees(this SteerState? state, out double headingDegrees)
    {
        if (state?.UnknownFields is null)
        {
            headingDegrees = default;
            return false;
        }

        var fields = GetFieldsDictionary(state.UnknownFields);
        if (!fields.Contains(LegacyHeadingFieldNumber))
        {
            headingDegrees = default;
            return false;
        }

        var field = fields[LegacyHeadingFieldNumber];
        if (field is null)
        {
            headingDegrees = default;
            return false;
        }

        var list = Fixed64ListField.GetValue(field) as IList;
        if (list is { Count: > 0 })
        {
            var rawValue = list[0];
            ulong bits = rawValue switch
            {
                ulong unsigned => unsigned,
                long signed => unchecked((ulong)signed),
                _ => Convert.ToUInt64(rawValue),
            };

            headingDegrees = BitConverter.Int64BitsToDouble(unchecked((long)bits));
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
        if (state?.UnknownFields is null)
        {
            return;
        }

        var fields = GetFieldsDictionary(state.UnknownFields);
        if (!fields.Contains(LegacyHeadingFieldNumber))
        {
            return;
        }

        fields.Remove(LegacyHeadingFieldNumber);
        if (fields.Count == 0)
        {
            state.UnknownFields = null;
        }
    }

    private static UnknownFieldSet CreateUnknownFieldSet()
    {
        var instance = Activator.CreateInstance(typeof(UnknownFieldSet), nonPublic: true)
            ?? throw new InvalidOperationException("Unable to construct UnknownFieldSet instance.");
        return (UnknownFieldSet)instance;
    }

    private static IDictionary GetFieldsDictionary(UnknownFieldSet set)
    {
        var fields = (IDictionary?)FieldsField.GetValue(set);
        return fields ?? throw new InvalidOperationException("Unknown field map is unavailable.");
    }

    private static object CreateFixed64Field(double headingDegrees)
    {
        var field = Activator.CreateInstance(UnknownFieldType, nonPublic: true)
            ?? throw new InvalidOperationException("Unable to construct UnknownField instance.");

        var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(headingDegrees));
        AddFixed64Method.Invoke(field, new object[] { bits });
        return field;
    }
}
