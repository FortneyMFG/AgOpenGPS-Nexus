using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model that coordinates session lifecycle actions in the job drawer per ADR-041.
/// </summary>
public sealed class SessionLifecyclePanelViewModel : ObservableObject
{
    private readonly Func<DateTimeOffset> _clock;
    private readonly ObservableCollection<SessionTimelineEntryViewModel> _timeline;
    private readonly DelegateCommand _startSessionCommand;
    private readonly DelegateCommand _pauseSessionCommand;
    private readonly DelegateCommand _resumeSessionCommand;
    private readonly DelegateCommand _completeSessionCommand;
    private SessionTimelineEntryViewModel? _activeSession;
    private string _statusMessage = "Ready to start a session.";
    private bool _hasError;
    private int _sessionSequence;
    private string _sessionName;
    private string _sessionNotes = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLifecyclePanelViewModel"/> class.
    /// </summary>
    /// <param name="jobId">Identifier of the job currently mounted.</param>
    /// <param name="fields">Fields available for the job.</param>
    /// <param name="clock">Clock used for deterministic testing.</param>
    public SessionLifecyclePanelViewModel(
        string jobId,
        IEnumerable<JobFieldDefinition> fields,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);

        JobId = jobId;
        FieldSelector = new MultiFieldJobSelectorViewModel(fields ?? throw new ArgumentNullException(nameof(fields)));
        FieldSelector.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MultiFieldJobSelectorViewModel.HasSelection))
            {
                RefreshCommandStates();
            }
        };

        _clock = clock ?? (() => DateTimeOffset.UtcNow);

        _timeline = new ObservableCollection<SessionTimelineEntryViewModel>();
        _startSessionCommand = new DelegateCommand(_ => StartSession(), _ => CanStartSession());
        _pauseSessionCommand = new DelegateCommand(_ => PauseActiveSession(), _ => CanPauseSession());
        _resumeSessionCommand = new DelegateCommand(_ => ResumeActiveSession(), _ => CanResumeSession());
        _completeSessionCommand = new DelegateCommand(_ => CompleteActiveSession(), _ => CanCompleteSession());

        _sessionSequence = 0;
        _sessionName = CreateSessionName(_sessionSequence + 1);
    }

    /// <summary>Gets the job identifier for which sessions are managed.</summary>
    public string JobId { get; }

    /// <summary>Gets the selector providing multi-field job awareness.</summary>
    public MultiFieldJobSelectorViewModel FieldSelector { get; }

    /// <summary>Gets the session timeline entries in chronological order.</summary>
    public IReadOnlyList<SessionTimelineEntryViewModel> SessionTimeline => _timeline;

    /// <summary>Gets or sets the active session input name prior to creation.</summary>
    public string SessionName
    {
        get => _sessionName;
        set => SetProperty(ref _sessionName, value);
    }

    /// <summary>Gets or sets the operator notes captured when starting or completing a session.</summary>
    public string SessionNotes
    {
        get => _sessionNotes;
        set => SetProperty(ref _sessionNotes, value);
    }

    /// <summary>Gets the currently active session, if any.</summary>
    public SessionTimelineEntryViewModel? ActiveSession
    {
        get => _activeSession;
        private set
        {
            if (_activeSession is not null)
            {
                _activeSession.PropertyChanged -= OnActiveSessionPropertyChanged;
            }

            if (SetProperty(ref _activeSession, value))
            {
                if (_activeSession is not null)
                {
                    _activeSession.PropertyChanged += OnActiveSessionPropertyChanged;
                }

                OnPropertyChanged(nameof(HasActiveSession));
                RefreshCommandStates();
            }
        }
    }

    /// <summary>Gets a value indicating whether a session is currently active or paused.</summary>
    public bool HasActiveSession => ActiveSession is not null;

    /// <summary>Gets the banner message presented to the operator.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets a value indicating whether the banner message represents an error.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>Gets the command that starts a new session.</summary>
    public DelegateCommand StartSessionCommand => _startSessionCommand;

    /// <summary>Gets the command that pauses the active session.</summary>
    public DelegateCommand PauseSessionCommand => _pauseSessionCommand;

    /// <summary>Gets the command that resumes a paused session.</summary>
    public DelegateCommand ResumeSessionCommand => _resumeSessionCommand;

    /// <summary>Gets the command that completes the active session.</summary>
    public DelegateCommand CompleteSessionCommand => _completeSessionCommand;

    /// <summary>
    /// Adds a note to the active session using the supplied text. No-op when no session is active.
    /// </summary>
    public void AddNoteToActiveSession(string note)
    {
        if (ActiveSession is null || string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        ActiveSession.AddNote(_clock(), note.Trim());
    }

    private bool CanStartSession()
    {
        return ActiveSession is null && FieldSelector.HasSelection;
    }

    private bool CanPauseSession()
    {
        return ActiveSession is { State: SessionLifecycleState.Active };
    }

    private bool CanResumeSession()
    {
        return ActiveSession is { State: SessionLifecycleState.Paused };
    }

    private bool CanCompleteSession()
    {
        return ActiveSession is not null;
    }

    private void StartSession()
    {
        if (!FieldSelector.HasSelection)
        {
            SetError("Select at least one field before starting a session.");
            return;
        }

        var now = _clock();
        _sessionSequence++;

        var name = string.IsNullOrWhiteSpace(SessionName)
            ? CreateSessionName(_sessionSequence)
            : SessionName.Trim();

        var notes = string.IsNullOrWhiteSpace(SessionNotes) ? null : SessionNotes.Trim();

        var selectedFields = FieldSelector.CaptureSelectedFields();
        var session = new SessionTimelineEntryViewModel(
            id: $"session:{_sessionSequence}",
            name: name,
            startedAt: now,
            selectedFields: selectedFields,
            initialNote: notes);

        _timeline.Add(session);
        ActiveSession = session;

        FieldSelector.SetStatus($"Mounted {selectedFields.Count} field(s) for {name}.");
        SetStatus($"Started {name}.");

        SessionNotes = string.Empty;
        SessionName = CreateSessionName(_sessionSequence + 1);
    }

    private void PauseActiveSession()
    {
        if (ActiveSession is null)
        {
            SetError("No active session to pause.");
            return;
        }

        if (!CanPauseSession())
        {
            SetError("Sessions can only be paused while active.");
            return;
        }

        ActiveSession.Pause(_clock());
        SetStatus($"{ActiveSession.Name} paused.");
    }

    private void ResumeActiveSession()
    {
        if (ActiveSession is null)
        {
            SetError("No session is available to resume.");
            return;
        }

        if (!CanResumeSession())
        {
            SetError("Sessions can only be resumed after being paused.");
            return;
        }

        ActiveSession.Resume(_clock());
        SetStatus($"{ActiveSession.Name} resumed.");
    }

    private void CompleteActiveSession()
    {
        if (ActiveSession is null)
        {
            SetError("No active session to complete.");
            return;
        }

        var closingNote = string.IsNullOrWhiteSpace(SessionNotes) ? null : SessionNotes.Trim();
        ActiveSession.Complete(_clock(), closingNote);

        SetStatus($"{ActiveSession.Name} completed.");
        FieldSelector.SetStatus("Session closed. Select fields for the next outing when ready.");

        ActiveSession = null;
        SessionNotes = string.Empty;
        SessionName = CreateSessionName(_sessionSequence + 1);
    }

    private void SetStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StatusMessage = message;
        HasError = false;
    }

    private void SetError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StatusMessage = message;
        HasError = true;
    }

    private void RefreshCommandStates()
    {
        _startSessionCommand.RaiseCanExecuteChanged();
        _pauseSessionCommand.RaiseCanExecuteChanged();
        _resumeSessionCommand.RaiseCanExecuteChanged();
        _completeSessionCommand.RaiseCanExecuteChanged();
    }

    private void OnActiveSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SessionTimelineEntryViewModel.State))
        {
            RefreshCommandStates();
        }
    }

    private static string CreateSessionName(int sequence)
    {
        return $"Session {sequence}";
    }
}

/// <summary>Represents a single session entry in the lifecycle timeline.</summary>
public sealed class SessionTimelineEntryViewModel : ObservableObject
{
    private SessionLifecycleState _state;
    private DateTimeOffset? _endedAt;
    private TimeSpan _activeDuration;
    private int _pauseCount;
    private int _resumeCount;
    private DateTimeOffset _lastTransitionAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTimelineEntryViewModel"/> class.
    /// </summary>
    public SessionTimelineEntryViewModel(
        string id,
        string name,
        DateTimeOffset startedAt,
        IReadOnlyList<JobFieldSelectionSnapshot> selectedFields,
        string? initialNote)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(selectedFields);

        if (selectedFields.Count == 0)
        {
            throw new ArgumentException("At least one field must be associated with a session.", nameof(selectedFields));
        }

        Id = id;
        Name = name;
        StartedAt = startedAt;
        Fields = selectedFields;
        Notes = new ObservableCollection<SessionTimelineNoteViewModel>();
        _state = SessionLifecycleState.Active;
        _lastTransitionAt = startedAt;

        if (!string.IsNullOrWhiteSpace(initialNote))
        {
            Notes.Add(new SessionTimelineNoteViewModel(startedAt, initialNote.Trim()));
        }
    }

    /// <summary>Gets the session identifier.</summary>
    public string Id { get; }

    /// <summary>Gets or sets the friendly session name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the timestamp when the session started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Gets the timestamp when the session ended, if completed.</summary>
    public DateTimeOffset? EndedAt
    {
        get => _endedAt;
        private set
        {
            if (SetProperty(ref _endedAt, value))
            {
                OnPropertyChanged(nameof(EndedAtDisplay));
            }
        }
    }

    /// <summary>Gets the fields mounted when the session started.</summary>
    public IReadOnlyList<JobFieldSelectionSnapshot> Fields { get; }

    /// <summary>Gets the notes captured during the session lifecycle.</summary>
    public ObservableCollection<SessionTimelineNoteViewModel> Notes { get; }

    /// <summary>Gets the current session state.</summary>
    public SessionLifecycleState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(StateDisplay));
                OnPropertyChanged(nameof(ActivitySummary));
            }
        }
    }

    /// <summary>Gets the accumulated active duration.</summary>
    public TimeSpan ActiveDuration
    {
        get => _activeDuration;
        private set
        {
            if (SetProperty(ref _activeDuration, value))
            {
                OnPropertyChanged(nameof(ActiveDurationDisplay));
                OnPropertyChanged(nameof(ActivitySummary));
            }
        }
    }

    /// <summary>Gets the number of times the session was paused.</summary>
    public int PauseCount
    {
        get => _pauseCount;
        private set => SetProperty(ref _pauseCount, value);
    }

    /// <summary>Gets the number of times the session resumed after a pause.</summary>
    public int ResumeCount
    {
        get => _resumeCount;
        private set => SetProperty(ref _resumeCount, value);
    }

    /// <summary>Gets the timestamp of the last lifecycle transition.</summary>
    public DateTimeOffset LastTransitionAt
    {
        get => _lastTransitionAt;
        private set
        {
            if (SetProperty(ref _lastTransitionAt, value))
            {
                OnPropertyChanged(nameof(LastTransitionDisplay));
            }
        }
    }

    /// <summary>Gets a value indicating whether the session is active.</summary>
    public bool IsActive => State == SessionLifecycleState.Active;

    /// <summary>Gets a value indicating whether the session is paused.</summary>
    public bool IsPaused => State == SessionLifecycleState.Paused;

    /// <summary>Gets a value indicating whether the session has been completed.</summary>
    public bool IsCompleted => State == SessionLifecycleState.Completed;

    /// <summary>Gets a formatted representation of the current state.</summary>
    public string StateDisplay => State switch
    {
        SessionLifecycleState.Active => "Active",
        SessionLifecycleState.Paused => "Paused",
        SessionLifecycleState.Completed => "Completed",
        _ => "Unknown",
    };

    /// <summary>Gets a formatted string representing the field selection.</summary>
    public string FieldSummary => BuildFieldSummary();

    /// <summary>Gets a formatted representation of the active duration.</summary>
    public string ActiveDurationDisplay => FormatDuration(ActiveDuration);

    /// <summary>Gets a formatted representation of the session end time.</summary>
    public string EndedAtDisplay => EndedAt.HasValue ? EndedAt.Value.ToString("MMM d, yyyy HH:mm") : "—";

    /// <summary>Gets a formatted representation of the last transition timestamp.</summary>
    public string LastTransitionDisplay => LastTransitionAt.ToString("MMM d, yyyy HH:mm");

    /// <summary>Gets a short summary describing the current lifecycle state and duration.</summary>
    public string ActivitySummary => $"{StateDisplay} · {ActiveDurationDisplay}";

    /// <summary>Records a pause transition.</summary>
    public void Pause(DateTimeOffset timestamp)
    {
        if (State != SessionLifecycleState.Active)
        {
            return;
        }

        ActiveDuration += timestamp - LastTransitionAt;
        LastTransitionAt = timestamp;
        PauseCount++;
        State = SessionLifecycleState.Paused;
    }

    /// <summary>Records a resume transition.</summary>
    public void Resume(DateTimeOffset timestamp)
    {
        if (State != SessionLifecycleState.Paused)
        {
            return;
        }

        LastTransitionAt = timestamp;
        ResumeCount++;
        State = SessionLifecycleState.Active;
    }

    /// <summary>Records a completion transition.</summary>
    public void Complete(DateTimeOffset timestamp, string? closingNote)
    {
        if (State == SessionLifecycleState.Active)
        {
            ActiveDuration += timestamp - LastTransitionAt;
        }

        LastTransitionAt = timestamp;
        EndedAt = timestamp;
        State = SessionLifecycleState.Completed;

        if (!string.IsNullOrWhiteSpace(closingNote))
        {
            Notes.Add(new SessionTimelineNoteViewModel(timestamp, closingNote.Trim()));
        }
    }

    /// <summary>Renames the session entry.</summary>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (Name.Equals(name, StringComparison.Ordinal))
        {
            return;
        }

        Name = name;
        OnPropertyChanged(nameof(Name));
    }

    /// <summary>Adds a note to the session timeline.</summary>
    public void AddNote(DateTimeOffset timestamp, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        Notes.Add(new SessionTimelineNoteViewModel(timestamp, note.Trim()));
    }

    private string BuildFieldSummary()
    {
        if (Fields.Count == 1)
        {
            return Fields[0].Name;
        }

        if (Fields.Count == 2)
        {
            return string.Join(" & ", Fields.Select(field => field.Name));
        }

        return $"{Fields[0].Name}, {Fields[1].Name} + {Fields.Count - 2} more";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return "0m";
        }

        var parts = new List<string>();

        if (duration.Hours > 0)
        {
            parts.Add($"{duration.Hours}h");
        }

        if (duration.Minutes > 0)
        {
            parts.Add($"{duration.Minutes}m");
        }

        if (parts.Count == 0)
        {
            parts.Add($"{duration.Seconds}s");
        }

        return string.Join(" ", parts);
    }
}

/// <summary>Represents a note captured during a session lifecycle.</summary>
public sealed class SessionTimelineNoteViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTimelineNoteViewModel"/> class.
    /// </summary>
    public SessionTimelineNoteViewModel(DateTimeOffset timestamp, string text)
    {
        Timestamp = timestamp;
        Text = text;
    }

    /// <summary>Gets the note timestamp.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the note text.</summary>
    public string Text { get; }

    /// <summary>Gets a formatted timestamp for display.</summary>
    public string TimestampDisplay => Timestamp.ToString("MMM d, yyyy HH:mm");
}

/// <summary>Enumerates session lifecycle states surfaced in the UI.</summary>
public enum SessionLifecycleState
{
    Active,
    Paused,
    Completed,
}
