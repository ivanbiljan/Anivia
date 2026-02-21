using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Tracks;

namespace Anivia.Infrastructure;

public sealed class AniviaPlayer(
    IPlayerProperties<LavalinkPlayer, LavalinkPlayerOptions> properties
) : LavalinkPlayer(properties), ILavalinkPlayerListener
{
    public UnifiedQueue Queue { get; } = new();

    public TrackRepeatMode RepeatMode { get; set; }

    async ValueTask ILavalinkPlayerListener.NotifyTrackEndedAsync(
        LavalinkTrack track,
        TrackEndReason endReason,
        CancellationToken cancellationToken
    )
    {
        if (endReason is not (TrackEndReason.Finished or TrackEndReason.LoadFailed))
        {
            return;
        }

        var next = Queue.Next(repeatMode: RepeatMode);

        if (next is not null)
        {
            await base.PlayAsync(next, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await StopAsync(cancellationToken).ConfigureAwait(false);
            await ClearAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask<LavalinkTrack?> BackAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        var previous = Queue.Previous(count, RepeatMode);

        if (previous is null)
        {
            await StopAsync(cancellationToken).ConfigureAwait(false);

            return null;
        }

        await base.PlayAsync(previous, cancellationToken: cancellationToken).ConfigureAwait(false);

        return previous;
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        Queue.Clear();
        await StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<LavalinkTrack?> JumpToAsync(int index, CancellationToken cancellationToken = default)
    {
        var track = Queue.JumpTo(index);

        if (track is null)
        {
            return null;
        }

        await base.PlayAsync(track, cancellationToken: cancellationToken).ConfigureAwait(false);

        return track;
    }

    public bool MoveTrack(int fromIndex, int toIndex)
    {
        return Queue.MoveTrack(fromIndex, toIndex);
    }

    public async ValueTask<int> PlayAsync(LavalinkTrack track, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);

        Queue.Enqueue(track);

        if (State is PlayerState.NotPlaying || CurrentTrack is null)
        {
            var next = Queue.StartFirst() ?? Queue.Next(repeatMode: RepeatMode);

            if (next is not null)
            {
                await base.PlayAsync(next, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        return Queue.Count - 1;
    }

    public async ValueTask PlayAsync(IEnumerable<LavalinkTrack> tracks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tracks);

        Queue.EnqueueRange(tracks);

        if (State is PlayerState.NotPlaying || CurrentTrack is null)
        {
            var next = Queue.StartFirst() ?? Queue.Next(repeatMode: RepeatMode);

            if (next is not null)
            {
                await base.PlayAsync(next, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public bool Remove(int index)
    {
        return Queue.RemoveAt(index);
    }

    public void Shuffle()
    {
        Queue.Shuffle();
    }

    public async ValueTask<LavalinkTrack?> SkipAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        var next = Queue.Next(count, RepeatMode);

        if (next is null)
        {
            await StopAsync(cancellationToken).ConfigureAwait(false);

            return null;
        }

        await base.PlayAsync(next, cancellationToken: cancellationToken).ConfigureAwait(false);

        return next;
    }
}

public sealed class UnifiedQueue
{
    private readonly List<LavalinkTrack> _tracks = [];

    public int Count => _tracks.Count;

    public LavalinkTrack? Current =>
        Position >= 0 && Position < _tracks.Count ? _tracks[Position] : null;

    public IReadOnlyList<LavalinkTrack> History =>
        Position > 0 ? _tracks.GetRange(0, Position) : [];

    public int Position { get; private set; } = -1;

    public IReadOnlyList<LavalinkTrack> Tracks => _tracks.AsReadOnly();

    public IReadOnlyList<LavalinkTrack> Upcoming =>
        Position + 1 < _tracks.Count ? _tracks.GetRange(Position + 1, _tracks.Count - Position - 1) : [];

    public LavalinkTrack? this[int trackIndex] =>
        trackIndex < 0 || trackIndex >= _tracks.Count ? null : _tracks[trackIndex];

    public void Clear()
    {
        _tracks.Clear();
        Position = -1;
    }

    public void ClearHistory()
    {
        if (Position <= 0)
        {
            return;
        }

        _tracks.RemoveRange(0, Position);
        Position = 0;
    }

    public void ClearUpcoming()
    {
        if (Position + 1 < _tracks.Count)
        {
            _tracks.RemoveRange(Position + 1, _tracks.Count - Position - 1);
        }
    }

    public void Enqueue(LavalinkTrack track)
    {
        ArgumentNullException.ThrowIfNull(track);
        _tracks.Add(track);
    }

    public void EnqueueRange(IEnumerable<LavalinkTrack> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        _tracks.AddRange(tracks);
    }

    public LavalinkTrack? JumpTo(int index)
    {
        if (index < 0 || index >= _tracks.Count)
        {
            return null;
        }

        Position = index;

        return Current;
    }

    public bool MoveTrack(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _tracks.Count)
        {
            return false;
        }

        if (toIndex < 0 || toIndex >= _tracks.Count)
        {
            return false;
        }

        if (fromIndex == toIndex)
        {
            return true;
        }

        var track = _tracks[fromIndex];
        _tracks.RemoveAt(fromIndex);
        _tracks.Insert(toIndex, track);

        return true;
    }

    public LavalinkTrack? Next(int skip = 1, TrackRepeatMode repeatMode = TrackRepeatMode.None)
    {
        if (_tracks.Count == 0)
        {
            return null;
        }

        if (repeatMode == TrackRepeatMode.Track)
        {
            return Current;
        }

        var newPosition = Position + skip;

        if (newPosition >= _tracks.Count)
        {
            if (repeatMode == TrackRepeatMode.Queue)
            {
                newPosition %= _tracks.Count;
            }
            else
            {
                Position = _tracks.Count;

                return null;
            }
        }

        Position = newPosition;

        return Current;
    }

    public LavalinkTrack? Previous(int count = 1, TrackRepeatMode repeatMode = TrackRepeatMode.None)
    {
        if (_tracks.Count == 0)
        {
            return null;
        }

        if (repeatMode == TrackRepeatMode.Track)
        {
            return Current;
        }

        var newPosition = Position - count;

        if (newPosition < 0)
        {
            if (repeatMode == TrackRepeatMode.Queue)
            {
                newPosition = _tracks.Count + newPosition % _tracks.Count;
                if (newPosition == _tracks.Count)
                {
                    newPosition = 0;
                }
            }
            else
            {
                newPosition = 0;
            }
        }

        Position = newPosition;

        return Current;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _tracks.Count)
        {
            return false;
        }

        if (index == Position)
        {
            return false;
        }

        _tracks.RemoveAt(index);

        if (index < Position)
        {
            Position--;
        }

        return true;
    }

    public void Shuffle()
    {
        if (_tracks.Count <= 2)
        {
            return;
        }

        LavalinkTrack? currentTrack = null;
        if (Position >= 0 && Position < _tracks.Count)
        {
            currentTrack = _tracks[Position];
            _tracks.RemoveAt(Position);
        }

        var random = Random.Shared;
        for (var i = _tracks.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (_tracks[i], _tracks[j]) = (_tracks[j], _tracks[i]);
        }

        if (currentTrack is not null)
        {
            _tracks.Insert(Position, currentTrack);
        }
    }

    public LavalinkTrack? StartFirst()
    {
        if (_tracks.Count == 0)
        {
            return null;
        }

        Position = 0;

        return Current;
    }
}