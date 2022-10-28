﻿using System.Collections;
using Victoria.Player;

namespace Anivia.CommandModules;

public sealed class CustomLavalinkQueue : IEnumerable<LavaTrack>
{
    private readonly List<LavaTrack> _tracks = new();

    private int _currentIndex;

    public LavaTrack? Current { get; private set; }

    public bool IsCurrentTrackLooped { get; set; }

    public bool IsLooped { get; set; }

    public int Length => _tracks.Count;

    public LavaTrack? Next => _currentIndex > _tracks.Count - 1 ? null : _tracks[_currentIndex];

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<LavaTrack> GetEnumerator() => _tracks.GetEnumerator();

    public void Add(LavaTrack track) => _tracks.Add(track);

    public void Add(IEnumerable<LavaTrack> tracks) => _tracks.AddRange(tracks);

    public void Clear()
    {
        _tracks.Clear();
        _currentIndex = 0;
    }

    public void SkipTracks(int numberOfTracks) => _currentIndex += numberOfTracks;

    public LavaTrack? GetNext()
    {
        if (_currentIndex < _tracks.Count)
        {
            var track = _tracks[_currentIndex];

            if (!IsCurrentTrackLooped)
            {
                ++_currentIndex;
            }

            return Current = track;
        }

        if (!IsLooped)
        {
            return null;
        }

        _currentIndex = 0;

        return Current = _tracks[_currentIndex++];
    }

    public void Move(int fromIndex, int toIndex)
    {
        var originalTrack = _tracks[fromIndex - 1];

        _tracks.RemoveAt(fromIndex - 1);
        _tracks.Insert(toIndex - 1, originalTrack);
    }

    public void Remove(LavaTrack track) => _tracks.Remove(track);

    public LavaTrack? Remove(int index)
    {
        if (index < 0 || index >= _tracks.Count)
        {
            return null;
        }

        var track = _tracks[index];
        _tracks.RemoveAt(index);

        return track;
    }
}