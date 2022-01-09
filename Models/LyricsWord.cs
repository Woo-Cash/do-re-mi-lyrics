using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Do_Re_Mi_Lyrics.Models;

public class LyricsWord : INotifyPropertyChanged
{
    internal LyricsLine Line;
    private TimeSpan _endTime;
    private bool _isNotProperTime;
    private bool _isPlaying;
    private bool _isSelected;
    private TimeSpan _startTime;
    private string _word = "";

    public LyricsWord(LyricsLine line)
    {
        Line = line;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string EndTimeText => EndTime.ToString(@"\<mm\:ss\.ff\>");

    public string StartTimeText => (PreviousWord == null || StartTime != PreviousWord.EndTime) && Line.FirstWord != this ? StartTime.ToString(@"\<mm\:ss\.ff\>") : "";

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

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            _isPlaying = value;
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

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            CheckProperTime();
            NextWord?.CheckProperTime();
            OnPropertyChanged();
            if (this == Line.FirstWord)
            {
                Line.UpdateStartTimeText();
            }
            else
            {
                OnPropertyChanged(nameof(StartTimeText));
            }
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

    internal LyricsWord? NextWord => Line.LastWord == this ? Line.NextLine?.FirstWord : Line.Words[Line.Words.IndexOf(this) + 1];

    internal LyricsWord? PreviousWord => Line.FirstWord == this ? Line.PreviousLine?.LastWord : Line.Words[Line.Words.IndexOf(this) - 1];

    public void CheckProperTime()
    {
        IsNotProperTime = EndTime < Line.StartTime || EndTime < PreviousWord?.EndTime;
        Line.CheckProperTime();
    }

    public override string ToString()
    {
        string text = "";
        if (Line.FirstWord != this && (PreviousWord == null || StartTime != PreviousWord.EndTime))
        {
            text = StartTimeText;
        }

        text += $"{Word}{EndTimeText} ";
        return text;
    }

    public void UpdateStartTimeText()
    {
        OnPropertyChanged(nameof(StartTimeText));
    }


    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}