using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Aog.Tools.RadioBridge;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        if (!string.Equals(args[0], "provision", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("error: expected 'provision' command.");
            PrintUsage();
            return 1;
        }

        try
        {
            var options = ParseProvisionArguments(args);
            var profile = CreateProfile(options);
            return WriteProfile(profile, options.OutputPath, options.Overwrite);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static ProvisionOptions ParseProvisionArguments(IReadOnlyList<string> args)
    {
        var options = new ProvisionOptions();

        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--device-id":
                    options.DeviceId = RequireNext(args, ref i, "--device-id");
                    break;
                case "--label":
                    options.Label = RequireNext(args, ref i, "--label");
                    break;
                case "--output":
                case "-o":
                    options.OutputPath = RequireNext(args, ref i, arg);
                    break;
                case "--key-bytes":
                    options.KeyBytes = ParsePositiveInt(RequireNext(args, ref i, "--key-bytes"));
                    break;
                case "--key":
                    options.HexKey = RequireNext(args, ref i, "--key");
                    break;
                case "--force":
                    options.Overwrite = true;
                    break;
                default:
                    if (string.IsNullOrEmpty(options.OutputPath))
                    {
                        options.OutputPath = arg;
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected argument '{arg}'.");
                    }

                    break;
            }
        }

        return options;
    }

    private static RadioBridgeProvisioningProfile CreateProfile(ProvisionOptions options)
    {
        var deviceId = string.IsNullOrWhiteSpace(options.DeviceId)
            ? $"bridge.elrs.{Guid.NewGuid():N}".Substring(0, 24)
            : options.DeviceId.Trim();

        var label = string.IsNullOrWhiteSpace(options.Label) ? deviceId : options.Label.Trim();

        byte[] keyMaterial;
        if (!string.IsNullOrWhiteSpace(options.HexKey))
        {
            keyMaterial = Convert.FromHexString(options.HexKey.Trim());
        }
        else
        {
            var length = options.KeyBytes ?? 32;
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.KeyBytes), "Key length must be positive.");
            }

            keyMaterial = RandomNumberGenerator.GetBytes(length);
        }

        return new RadioBridgeProvisioningProfile
        {
            DeviceId = deviceId,
            Label = label,
            PreSharedKey = Convert.ToHexString(keyMaterial),
            CreatedAt = DateTimeOffset.UtcNow,
            Topics = new[]
            {
                new RadioBridgeProvisioningProfile.TopicGrant("*", "*", "all"),
            },
        };
    }

    private static int WriteProfile(RadioBridgeProvisioningProfile profile, string? outputPath, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var json = JsonSerializer.Serialize(profile, RadioBridgeProvisioningProfile.SerializerOptions);
            Console.Out.WriteLine(json);
            return 0;
        }

        var path = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(path) && !overwrite)
        {
            throw new IOException($"File '{path}' already exists. Use --force to overwrite.");
        }

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, profile, RadioBridgeProvisioningProfile.SerializerOptions);
        Console.Out.WriteLine(path);
        return 0;
    }

    private static string RequireNext(IReadOnlyList<string> args, ref int index, string name)
    {
        index++;
        if (index >= args.Count)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        return args[index];
    }

    private static int ParsePositiveInt(string value)
    {
        if (!int.TryParse(value, out var result) || result <= 0)
        {
            throw new ArgumentException($"Invalid integer value '{value}'.");
        }

        return result;
    }

    private static bool IsHelp(string value)
    {
        return string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: radiobridge provision [--device-id <id>] [--label <name>] [--output <file>] [--key-bytes <n>] [--key <hex>] [--force]");
        Console.WriteLine();
        Console.WriteLine("Generates a provisioning profile JSON document for RadioBridge devices. When --output is omitted the profile is written to stdout.");
    }

    private sealed class ProvisionOptions
    {
        public string? DeviceId { get; set; }
        public string? Label { get; set; }
        public string? OutputPath { get; set; }
        public int? KeyBytes { get; set; }
        public string? HexKey { get; set; }
        public bool Overwrite { get; set; }
    }
}
