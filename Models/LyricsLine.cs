using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Do_Re_Mi_Lyrics.Models;

public class LyricsLine : INotifyPropertyChanged
{
    internal LyricsWord? FirstWord;
    internal LyricsWord? LastWord;
    private bool _isNotProperTime;
    private LyricsLine? _nextLine;
    private LyricsLine? _previousLine;
    private TimeSpan _startTime;
    private ObservableCollection<LyricsWord> _words = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StartTimeText => StartTime.ToString(@"\[mm\:ss\.ff\]");

    public bool IsNotProperTime
    {
        get => _isNotProperTime;
        set
        {
            _isNotProperTime = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            CheckProperTime();
            NextLine?.CheckProperTime();
            foreach (LyricsWord word in Words)
            {
                word.CheckProperTime();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(StartTimeText));
        }
    }

    public ObservableCollection<LyricsWord> Words
    {
        get => _words;
        set
        {
            _words = value;
            OnPropertyChanged();
        }
    }

    internal LyricsLine? NextLine
    {
        get => _nextLine;
        set
        {
            _nextLine = value;
            CheckProperTime();
            NextLine?.CheckProperTime();
        }
    }

    internal LyricsLine? PreviousLine
    {
        get => _previousLine;
        set
        {
            _previousLine = value;
            CheckProperTime();
        }
    }

    public void CheckProperTime()
    {
        IsNotProperTime = PreviousLine?.StartTime > StartTime;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}