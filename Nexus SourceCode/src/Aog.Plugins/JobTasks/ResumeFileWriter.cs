using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Plugins.JobTasks;

internal static class ResumeFileWriter
{
    public static async Task WriteAsync(JobSnapshot snapshot, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Metadata);
        ArgumentNullException.ThrowIfNull(snapshot.Layout);

        var resumePath = snapshot.Layout.ResumeFile;
        if (string.IsNullOrWhiteSpace(resumePath))
        {
            throw new InvalidOperationException("Job snapshot layout must include a resume file path.");
        }

        var directory = Path.GetDirectoryName(resumePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();
        var now = timeProvider.GetUtcNow();

        builder.AppendLine("# Nexus job resume marker");
        builder.AppendLine($"SavedAt={now:O}");
        builder.AppendLine($"JobId={snapshot.Metadata.JobId}");
        builder.AppendLine($"DisplayName={snapshot.Metadata.DisplayName}");
        builder.AppendLine($"Slug={snapshot.Metadata.Slug}");
        builder.AppendLine($"State={snapshot.Metadata.State.ToSchemaValue()}");
        builder.AppendLine($"UpdatedAt={snapshot.Metadata.UpdatedAt:O}");
        builder.AppendLine($"FarmId={snapshot.Metadata.Context.FarmId}");
        builder.AppendLine($"FieldIds={string.Join(',', snapshot.Metadata.Context.FieldIds)}");

        if (!string.IsNullOrWhiteSpace(snapshot.Metadata.Context.SeasonId))
        {
            builder.AppendLine($"SeasonId={snapshot.Metadata.Context.SeasonId}");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Metadata.Context.WorkOrderId))
        {
            builder.AppendLine($"WorkOrderId={snapshot.Metadata.Context.WorkOrderId}");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Metadata.ActiveSessionId))
        {
            builder.AppendLine($"ActiveSessionId={snapshot.Metadata.ActiveSessionId}");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Metadata.Context.Notes))
        {
            builder.AppendLine($"Notes={snapshot.Metadata.Context.Notes}");
        }

        if (snapshot.Metadata.Tags is { Count: > 0 })
        {
            builder.AppendLine($"Tags={string.Join(',', snapshot.Metadata.Tags)}");
        }

        var sessions = snapshot.Sessions ?? Array.Empty<JobSessionSnapshot>();
        builder.AppendLine($"SessionCount={sessions.Count}");

        foreach (var session in sessions)
        {
            builder.AppendLine();
            builder.AppendLine($"[Session {session.SessionId}]");
            builder.AppendLine($"State={session.State.ToSchemaValue()}");
            builder.AppendLine($"StartedAt={session.StartedAt:O}");
            builder.AppendLine($"LastModifiedAt={session.LastModifiedAt:O}");

            if (session.EndedAt is DateTimeOffset endedAt)
            {
                builder.AppendLine($"EndedAt={endedAt:O}");
            }

            if (!string.IsNullOrWhiteSpace(session.Name))
            {
                builder.AppendLine($"Name={session.Name}");
            }

            if (session.ActiveOperators is { Count: > 0 })
            {
                builder.AppendLine($"Operators={string.Join(',', session.ActiveOperators)}");
            }
        }

        await File.WriteAllTextAsync(resumePath, builder.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }
}
