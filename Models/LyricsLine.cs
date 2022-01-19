using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Do_Re_Mi_Lyrics.Models;

public class LyricsLine : INotifyPropertyChanged
{
    private readonly Lyrics _lyrics;
    private bool _isNotProperTime;
    private bool _isTooShortTime;
    private ObservableCollection<LyricsWord> _words = new();

    public LyricsLine(Lyrics lyrics)
    {
        _lyrics = lyrics;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TimeSpan StartTime => FirstWord?.StartTime ?? TimeSpan.Zero;

    public string StartTimeText => StartTime.ToString(@"\[mm\:ss\.ff\]");

    public string Text => Words.Aggregate("", (current, word) => $"{current}{word.Word} ");

    public bool IsNotProperTime
    {
        get => _isNotProperTime;
        set
        {
            _isNotProperTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsTooShortTime
    {
        get => _isTooShortTime;
        set
        {
            _isTooShortTime = value;
            OnPropertyChanged();
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

    internal LyricsWord? FirstWord => Words.Count > 0 ? Words[0] : null;
    internal LyricsWord? LastWord => Words.Count > 0 ? Words[^1] : null;

    internal LyricsLine? NextLine => _lyrics.LastLine == this || _lyrics.LastLine == null || _lyrics.LyricsLines.IndexOf(this) == -1
        ? null
        : _lyrics.LyricsLines[_lyrics.LyricsLines.IndexOf(this) + 1];

    internal LyricsLine? PreviousLine => _lyrics.FirstLine == this || _lyrics.FirstLine == null || _lyrics.LyricsLines.IndexOf(this) == -1
        ? null
        : _lyrics.LyricsLines[_lyrics.LyricsLines.IndexOf(this) - 1];

    public string ToString(out int caretIndex)
    {
        caretIndex = 0;
        string result = StartTimeText;
        foreach (LyricsWord word in Words)
        {
            if (word.IsSelected)
            {
                caretIndex = result.Length;
            }

            result += word;
        }

        return result;
    }

    internal void CheckProperTime()
    {
        IsNotProperTime = StartTime - PreviousLine?.LastWord?.EndTime < TimeSpan.Zero;
        IsTooShortTime = !string.IsNullOrWhiteSpace(FirstWord?.Word) && StartTime - PreviousLine?.StartTime < TimeSpan.FromSeconds(1.5) && _lyrics.FirstLine?.NextLine != this;
    }

    internal void AddWord(LyricsWord word)
    {
        Words.Add(word);
        UpdateStartTimeText();
    }

    internal void InsertWord(int index, LyricsWord word)
    {
        Words.Insert(index, word);
        UpdateStartTimeText();
    }

    internal void RemoveWord(LyricsWord word)
    {
        Words.Remove(word);
        UpdateStartTimeText();
    }

    internal void UpdateStartTimeText()
    {
        OnPropertyChanged(nameof(StartTimeText));
    }

    internal LyricsLine Clone(Lyrics lyrics)
    {
        LyricsLine lyricsLine = new(lyrics)
        {
            IsNotProperTime = IsNotProperTime,
            IsTooShortTime = IsTooShortTime
        };
        foreach (LyricsWord lyricsWord in Words)
        {
            LyricsWord newLyricsWord = lyricsWord.Clone(lyricsLine);
            lyricsLine.Words.Add(newLyricsWord);
            newLyricsWord.CheckProperTime();
        }

        return lyricsLine;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}