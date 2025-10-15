using System;
using System.Collections.Generic;

namespace Aog.Core.Jobs;

/// <summary>
/// Enumerates lifecycle states for an individual session inside a job. Values
/// map directly to ADR-041 terminology so persisted documents align with the
/// <c>Session.v1</c> schema.
/// </summary>
public enum SessionLifecycleState
{
    Planned = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4,
}

/// <summary>
/// Represents a note captured during a session. Notes may be free-form text or
/// structured payloads supplied by plugins.
/// </summary>
public sealed record SessionNote
{
    public SessionNote(
        DateTimeOffset timestamp,
        string author,
        string type,
        string? text = null,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type is required.", nameof(type));

        Timestamp = timestamp;
        Author = author;
        Type = type;
        Text = text;
        Payload = payload;
    }

    /// <summary>UTC timestamp when the note was recorded.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Actor identifier (<c>user:*</c> or <c>system:*</c>).</summary>
    public string Author { get; init; }

    /// <summary>Classifier for the note (<c>freeform</c>, <c>checklist</c>, etc.).</summary>
    public string Type { get; init; }

    /// <summary>Optional human readable text.</summary>
    public string? Text { get; init; }

    /// <summary>Optional structured payload supplied by plugins.</summary>
    public IReadOnlyDictionary<string, object?>? Payload { get; init; }
}

/// <summary>
/// Snapshot of session metadata persisted to disk during autosave operations.
/// The shape mirrors <c>schemas/Session.v1.json</c> while omitting optional
/// sections managed by specialised services (weather snapshots, attachments,
/// etc.).
/// </summary>
public sealed record SessionDocument
{
    public const string DefaultSchemaVersion = "1.0.0";

    public SessionDocument(
        string jobId,
        string sessionId,
        SessionLifecycleState state,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        DateTimeOffset createdAt,
        string createdBy,
        DateTimeOffset lastModifiedAt,
        IReadOnlyList<string> operators,
        IReadOnlyList<SessionNote> notes,
        IReadOnlyDictionary<string, object?> extensions,
        string? name = null,
        string schemaVersion = DefaultSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job identifier is required.", nameof(jobId));

        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session identifier is required.", nameof(sessionId));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created-by actor is required.", nameof(createdBy));

        if (string.IsNullOrWhiteSpace(schemaVersion))
            throw new ArgumentException("Schema version is required.", nameof(schemaVersion));

        JobId = jobId;
        SessionId = sessionId;
        State = state;
        StartedAt = startedAt;
        EndedAt = endedAt;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        LastModifiedAt = lastModifiedAt;
        Operators = operators ?? Array.Empty<string>();
        Notes = notes ?? Array.Empty<SessionNote>();
        Extensions = extensions ?? new Dictionary<string, object?>();
        Name = name;
        SchemaVersion = schemaVersion;
    }

    public string JobId { get; init; }

    public string SessionId { get; init; }

    public string? Name { get; init; }

    public SessionLifecycleState State { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? EndedAt { get; init; }

    public string CreatedBy { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastModifiedAt { get; init; }

    public IReadOnlyList<string> Operators { get; init; }

    public IReadOnlyList<SessionNote> Notes { get; init; }

    public IReadOnlyDictionary<string, object?> Extensions { get; init; }

    public string SchemaVersion { get; init; }
}

/// <summary>
/// Journal entry persisted alongside autosaved session snapshots. Entries hold
/// high-frequency operations (coverage tile flushes, telemetry segments, etc.).
/// </summary>
public sealed record SessionJournalEntry
{
    public SessionJournalEntry(
        string entryType,
        DateTimeOffset recordedAt,
        int approximateSizeBytes,
        IReadOnlyDictionary<string, object?>? payload = null)
    {
        if (string.IsNullOrWhiteSpace(entryType))
            throw new ArgumentException("Entry type is required.", nameof(entryType));

        if (approximateSizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(approximateSizeBytes), "Size must be non-negative.");

        EntryType = entryType;
        RecordedAt = recordedAt;
        ApproximateSizeBytes = approximateSizeBytes;
        Payload = payload;
    }

    public string EntryType { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public int ApproximateSizeBytes { get; init; }

    public IReadOnlyDictionary<string, object?>? Payload { get; init; }
}
