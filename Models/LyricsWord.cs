using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Do_Re_Mi_Lyrics.Models;

public class LyricsWord : INotifyPropertyChanged
{
    internal LyricsLine? Line;
    private TimeSpan _endTime;
    private bool _isNotProperTime;
    private bool _isSelected;
    private LyricsWord? _nextWord;
    private LyricsWord? _previousWord;
    private string _word = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string EndTimeText => EndTime.ToString(@"\<mm\:ss\.ff\>");

    public TimeSpan EndTime
    {
        get => _endTime;
        set
        {
            _endTime = value;
            CheckProperTime();
            NextWord?.CheckProperTime();
            OnPropertyChanged();
            OnPropertyChanged(nameof(EndTimeText));
        }
    }

    public bool IsNotProperTime
    {
        get => _isNotProperTime;
        set
        {
            _isNotProperTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string Word
    {
        get => _word;
        set
        {
            _word = value;
            OnPropertyChanged();
        }
    }

    internal LyricsWord? NextWord
    {
        get => _nextWord;
        set
        {
            _nextWord = value;
            CheckProperTime();
            NextWord?.CheckProperTime();
        }
    }

    internal LyricsWord? PreviousWord
    {
        get => _previousWord;
        set
        {
            _previousWord = value;
            CheckProperTime();
        }
    }

    public void CheckProperTime()
    {
        IsNotProperTime = EndTime < Line?.StartTime || EndTime < PreviousWord?.EndTime;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}