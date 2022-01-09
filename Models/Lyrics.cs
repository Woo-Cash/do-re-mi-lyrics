using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace Do_Re_Mi_Lyrics.Models;

public class Lyrics : INotifyPropertyChanged
{
    private const string PatternLine = @"\[([0-9]{2})\:([0-9]{2})\.([0-9]{2})\](.*)";
    private const string PatternWord = @"\<([0-9]{2})\:([0-9]{2})\.([0-9]{2})\>(.*)\<([0-9]{2})\:([0-9]{2})\.([0-9]{2})\>";
    private const string PatternWordEnd = @"(.*)\<([0-9]{2})\:([0-9]{2})\.([0-9]{2})\>";
    private const string PatternWordStart = @"\<([0-9]{2})\:([0-9]{2})\.([0-9]{2})\>(.*)";
    private LyricsLine? _currentLine;
    private LyricsWord? _currentPlayingWord;
    private LyricsWord? _currentWord;
    private ObservableCollection<LyricsLine> _lyricsLines = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    public LyricsLine? FirstLine => LyricsLines.Count > 0 ? LyricsLines[0] : null;
    public LyricsLine? LastLine => LyricsLines.Count > 0 ? LyricsLines[^1] : null;

    public ObservableCollection<LyricsLine> LyricsLines
    {
        get => _lyricsLines;
        set
        {
            _lyricsLines = value;
            OnPropertyChanged();
        }
    }

    public void ParseLyrics(string text)
    {
        try
        {
            _currentLine = null;
            _currentWord = null;
            text = text.Replace("\r", "");
            LyricsLines.Clear();
            List<string> lines = text.Split('\n').ToList();
            while (lines.Count > 1 && string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            foreach (string line in lines)
            {
                LyricsLine lyricsLine = new();
                LyricsLines.Add(lyricsLine);
                Regex regex = new(PatternLine);
                Match match = regex.Match(line);
                string restOfLine = line;
                TimeSpan lineStartTime = TimeSpan.Zero;
                if (match.Success)
                {
                    lineStartTime = new TimeSpan(0, 0, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) * 10);

                    restOfLine = match.Groups[4].Value;
                }

                List<string> words = restOfLine.Split(' ').ToList();
                foreach (string word in words)
                {
                    if (string.IsNullOrWhiteSpace(word) && !string.IsNullOrWhiteSpace(restOfLine))
                    {
                        continue;
                    }

                    LyricsWord lyricsWord = new(lyricsLine);
                    lyricsLine.AddWord(lyricsWord);
                    string restOfWord = word;

                    regex = new Regex(PatternWord);
                    match = regex.Match(word);
                    if (match.Success)
                    {
                        lyricsWord.StartTime = new TimeSpan(0, 0, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) * 10);
                        lyricsWord.EndTime = new TimeSpan(0, 0, int.Parse(match.Groups[5].Value), int.Parse(match.Groups[6].Value), int.Parse(match.Groups[7].Value) * 10);
                        restOfWord = match.Groups[4].Value;
                    }
                    else
                    {
                        regex = new Regex(PatternWordEnd);
                        match = regex.Match(word);
                        if (match.Success)
                        {
                            lyricsWord.StartTime = lyricsLine.FirstWord == lyricsWord ? lineStartTime : lyricsWord.PreviousWord?.EndTime ?? TimeSpan.Zero;
                            lyricsWord.EndTime = new TimeSpan(0, 0, int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value) * 10);
                            restOfWord = match.Groups[1].Value;
                        }
                        else
                        {
                            regex = new Regex(PatternWordStart);
                            match = regex.Match(word);
                            if (match.Success)
                            {
                                lyricsWord.StartTime = new TimeSpan(0, 0, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value),
                                    int.Parse(match.Groups[3].Value) * 10);
                                if (lyricsWord.PreviousWord?.EndTime == TimeSpan.Zero)
                                {
                                    lyricsWord.PreviousWord.EndTime = lyricsWord.StartTime;
                                }

                                lyricsWord.EndTime = TimeSpan.Zero;
                                restOfWord = match.Groups[4].Value;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(restOfWord))
                    {
                        restOfWord = "";
                    }

                    lyricsWord.Word = restOfWord;

                    lyricsWord.CheckProperTime();
                }

                lyricsLine.CheckProperTime();
            }

            if (LyricsLines[^1].FirstWord != LyricsLines[^1].LastWord || LyricsLines[^1].FirstWord?.Word != "")
            {
                LyricsLine newLine = new();
                LyricsLines.Add(newLine);
                LyricsWord newWord = new(newLine)
                {
                    Word = ""
                };
                newLine.AddWord(newWord);
                newWord.EndTime = Application.Audio.TotalTime;
            }

            SelectFirstLine();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void RemoveStartTime()
    {
        try
        {
            if (_currentWord?.PreviousWord == null)
            {
                return;
            }

            SelectPreviousWord();

            if (_currentWord?.PreviousWord != null)
            {
                _currentWord.PreviousWord.EndTime = new TimeSpan(0, 0, 0);
            }

            if (_currentWord != null)
            {
                _currentWord.StartTime = new TimeSpan(0, 0, 0);
            }

            Application.IsSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SetEndingTimeToPreviousLine()
    {
        try
        {
            TimeSpan timeSpan = Application.Audio.CurrentTime;

            if (_currentWord?.PreviousWord != null)
            {
                _currentWord.PreviousWord.EndTime = timeSpan;
            }

            if (_currentWord?.StartTime < timeSpan)
            {
                _currentWord.StartTime = timeSpan;
            }

            _currentWord?.UpdateStartTimeText();

            Application.IsSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SetTimeToCurrentWord()
    {
        try
        {
            TimeSpan currentTime = Application.Audio.CurrentTime;
            if (_currentWord?.PreviousWord != null && _currentLine?.PreviousLine != null && _currentWord == _currentLine?.FirstWord &&
                _currentWord.PreviousWord.EndTime > TimeSpan.Zero && currentTime - _currentWord.PreviousWord.EndTime > TimeSpan.FromSeconds(1.5))
            {
                InsertEmptyLineBeforeCurrent(currentTime);
            }

            if (_currentWord?.PreviousWord != null &&
                (_currentWord.PreviousWord.EndTime == TimeSpan.Zero || currentTime - _currentWord.PreviousWord.EndTime < TimeSpan.FromSeconds(0.5)))
            {
                _currentWord.PreviousWord.EndTime = currentTime;
            }

            if (_currentWord != null)
            {
                _currentWord.StartTime = currentTime;

                if (_currentWord == FirstLine?.FirstWord && currentTime > TimeSpan.Zero)
                {
                    InsertEmptyLineAtBeginning(currentTime);
                }
            }

            SelectNextWord();

            Application.IsSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SelectWord(LyricsWord? word)
    {
        try
        {
            if (_currentWord != null)
            {
                _currentWord.IsSelected = false;
            }

            _currentWord = word;
            _currentLine = word?.Line;
            if (_currentWord != null)
            {
                _currentWord.IsSelected = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SelectNextWord()
    {
        try
        {
            if (_currentWord?.NextWord != null)
            {
                SelectWord(_currentWord?.NextWord);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SelectPreviousWord()
    {
        try
        {
            if (_currentWord?.PreviousWord != null)
            {
                SelectWord(_currentWord?.PreviousWord);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SelectNextLine()
    {
        try
        {
            if (_currentLine?.NextLine == null)
            {
                return;
            }

            SelectWord(_currentLine.NextLine?.FirstWord);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SelectPreviousLine()
    {
        try
        {
            if (_currentLine?.PreviousLine == null)
            {
                return;
            }

            SelectWord(_currentLine?.PreviousLine?.FirstWord);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void MoveWordToNewLine()
    {
        try
        {
            if (_currentWord == _currentLine?.FirstWord || _currentWord?.PreviousWord == null || _currentLine == null)
            {
                return;
            }

            LyricsLine newLine = new();
            if (_currentLine.NextLine != null)
            {
                LyricsLines.Insert(LyricsLines.IndexOf(_currentLine.NextLine), newLine);
            }
            else
            {
                LyricsLines.Add(newLine);
            }


            if (_currentLine.LastWord != null)
            {
                LyricsWord word = _currentLine.LastWord;
                do
                {
                    _currentLine.RemoveWord(word);
                    newLine.InsertWord(0, word);
                    word.Line = newLine;
                    if (word.PreviousWord == null)
                    {
                        break;
                    }

                    word = word.PreviousWord;
                } while (word != _currentWord.PreviousWord);
            }

            _currentLine = newLine;
            Application.IsSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void MoveLineToPrevious()
    {
        try
        {
            if (_currentWord != _currentLine?.FirstWord || _currentWord?.PreviousWord == null || _currentLine?.PreviousLine == null)
            {
                return;
            }

            LyricsLine deletedLine = _currentLine;
            LyricsLine previousLine = _currentLine.PreviousLine;

            LyricsWord word = _currentWord;
            while (word.Line == _currentLine)
            {
                previousLine.AddWord(word);
                deletedLine.RemoveWord(word);
                word.Line = previousLine;
                if (word.NextWord == null)
                {
                    break;
                }

                word = word.NextWord;
            }

            LyricsLines.Remove(deletedLine);

            _currentLine = previousLine;

            RemoveEmptyWords();

            Application.IsSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void MoveNextLineToCurrent()
    {
        try
        {
            if (_currentWord != _currentLine?.LastWord || _currentWord?.NextWord == null || _currentLine?.NextLine == null)
            {
                return;
            }

            LyricsLine nextLine = _currentLine.NextLine;
            LyricsWord word = _currentWord.NextWord;
            while (word.Line == nextLine)
            {
                _currentLine.AddWord(word);
                nextLine.RemoveWord(word);
                word.Line = _currentLine;
                if (word.NextWord == null)
                {
                    break;
                }

                word = word.NextWord;
            }

            LyricsLines.Remove(nextLine);

            RemoveEmptyWords();

            Application.IsSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal string GetLyricsText()
    {
        string text = "";
        try
        {
            foreach (LyricsLine lyricsLine in LyricsLines)
            {
                text += lyricsLine.ToString();
                text += Environment.NewLine;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return "";
        }

        return text;
    }

    internal void SelectFirstLine()
    {
        try
        {
            _currentLine = FirstLine;
            SelectWord(_currentLine?.FirstWord);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal TimeSpan GetCurrentWordStartTime()
    {
        if (_currentWord == null)
        {
            return TimeSpan.Zero;
        }

        LyricsWord word = _currentWord;

        while (word.StartTime == TimeSpan.Zero && word != FirstLine?.FirstWord)
        {
            if (word.PreviousWord != null)
            {
                word = word.PreviousWord;
            }
        }

        TimeSpan timeSpan = word.Line.StartTime;
        return timeSpan;
    }

    internal void ChangePlayingWord()
    {
        if (_currentPlayingWord != null && Application.Audio.CurrentTime >= _currentPlayingWord.StartTime && Application.Audio.CurrentTime <= _currentPlayingWord.EndTime)
        {
            return;
        }

        if (_currentPlayingWord != null)
        {
            _currentPlayingWord.IsPlaying = false;
            _currentPlayingWord = null;
        }

        LyricsWord? word = FirstLine?.FirstWord;

        while (word != null)
        {
            if (Application.Audio.CurrentTime >= word.StartTime && Application.Audio.CurrentTime <= word.EndTime)
            {
                word.IsPlaying = true;
                _currentPlayingWord = word;
                break;
            }

            word = word.NextWord;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RemoveEmptyWords()
    {
        LyricsWord? word = FirstLine?.FirstWord;

        if (word == null)
        {
            return;
        }

        do
        {
            if (string.IsNullOrWhiteSpace(word.Word) && word.Line.FirstWord != word.Line.LastWord)
            {
                RemoveWord(word);
            }

            word = word.NextWord;
        } while (word != null);
    }

    private void InsertEmptyLineBeforeCurrent(TimeSpan timeSpan)
    {
        if (_currentWord?.PreviousWord == null || _currentLine?.PreviousLine == null)
        {
            return;
        }

        LyricsLine newLine = new();
        LyricsLines.Insert(LyricsLines.IndexOf(_currentLine), newLine);
        LyricsWord newWord = new(newLine)
        {
            Word = ""
        };
        newLine.AddWord(newWord);
        if (newWord.PreviousWord != null)
        {
            newWord.StartTime = newWord.PreviousWord.EndTime;
        }

        newWord.EndTime = timeSpan;
    }

    private void InsertEmptyLineAtBeginning(TimeSpan timeSpan)
    {
        LyricsLine newLine = new();
        LyricsLines.Insert(0, newLine);
        LyricsWord newWord = new(newLine)
        {
            Word = ""
        };

        newLine.AddWord(newWord);
        newWord.EndTime = timeSpan;
    }

    private void RemoveWord(LyricsWord word)
    {
        word.Line.RemoveWord(word);
        if (word.Line.Words.Count == 0)
        {
            LyricsLines.Remove(word.Line);
        }
    }
}