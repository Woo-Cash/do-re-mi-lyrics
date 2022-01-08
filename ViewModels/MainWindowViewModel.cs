using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using Do_Re_Mi_Lyrics.Models;
using Do_Re_Mi_Lyrics.Properties;
using Do_Re_Mi_Lyrics.Views;
using Microsoft.Win32;
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;

namespace Do_Re_Mi_Lyrics.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private const string PatternLine = @"\[([0-9]{2})\:([0-9]{2})\.([0-9]{2})\](.*)";
    private const string PatternWord = @"(.*)\<([0-9]{2})\:([0-9]{2})\.([0-9]{2})\>";
    private const int SkipInterval = 5;
    private readonly DispatcherTimer _playTimer = new(DispatcherPriority.Send) {Interval = new TimeSpan(0, 0, 0, 0, 100)};
    private readonly Window _window;
    private string _audioFilePath = "Open audio file";
    private LyricsLine? _currentLine;
    private LyricsWord? _currentWord;
    private LyricsLine? _firstLine;
    private bool _isSaved = true;
    private string _lyricsFilePath = "Open or paste lyrics";
    private ObservableCollection<LyricsLine> _lyricsLines = new();
    private string _playPauseIconPath = @"..\Images\play.png";
    private string _playPauseText = "Play (Space)";
    private SoundTouchWaveStream? _processorStream;
    private WaveChannel32? _waveChannel;

    private IWavePlayer _waveOut = new WaveOutEvent
    {
        DesiredLatency = 100
    };

    public MainWindowViewModel(Window window)
    {
        _window = window;
        _playTimer.Tick += PlayTimerTick;
        if (Settings.Default.AudioFilePath != "")
        {
            AudioFilePath = Settings.Default.AudioFilePath;
            OpenAudio();
        }

        if (Settings.Default.LyricsFilePath != "" && File.Exists(Settings.Default.LyricsFilePath))
        {
            LyricsFilePath = Settings.Default.LyricsFilePath;
            ParseLyricsFromFile();
        }
        else
        {
            NewLyrics();
        }

        SelectFirstParagraph();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentTimeText => _waveChannel?.CurrentTime.ToString(@"mm\:ss") ?? "00:00";

    public bool IsAudioFileLoaded => AudioFilePath != "Open audio file";
    public bool IsLyricsFileLoaded => LyricsFilePath != "Open or paste lyrics";

    public int PlaySliderMaximum => (int) (_waveChannel?.TotalTime.TotalMilliseconds ?? 0);

    public string PlayTempoText => $"Tempo: {PlayTempo:0.0}x";
    public string PlayVolumeText => $"Volume: {PlayVolume * 100:0}%";

    public string TotalTimeText => _waveChannel?.TotalTime.ToString(@"mm\:ss") ?? "00:00";

    public string AudioFilePath
    {
        get => _audioFilePath;
        set
        {
            _audioFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAudioFileLoaded));
        }
    }

    public string LyricsFilePath
    {
        get => _lyricsFilePath;
        set
        {
            _lyricsFilePath = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LyricsLine> LyricsLines
    {
        get => _lyricsLines;
        set
        {
            _lyricsLines = value;
            OnPropertyChanged();
        }
    }

    public string PlayPauseIconPath
    {
        get => _playPauseIconPath;
        set
        {
            _playPauseIconPath = value;
            OnPropertyChanged();
        }
    }

    public string PlayPauseText
    {
        get => _playPauseText;
        set
        {
            _playPauseText = value;
            OnPropertyChanged();
        }
    }

    public long PlaySliderPosition
    {
        get => (int) (_waveChannel?.CurrentTime.TotalMilliseconds ?? 0);
        set
        {
            if (_waveChannel != null)
            {
                _waveChannel.CurrentTime = TimeSpan.FromMilliseconds(value);
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTimeText));
        }
    }

    public double PlayTempo
    {
        get => _processorStream?.Tempo ?? 1;
        set
        {
            if (_processorStream != null)
            {
                _processorStream.Tempo = value;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayTempoText));
        }
    }

    public float PlayVolume
    {
        get => _waveChannel?.Volume ?? 1;
        set
        {
            if (_waveChannel != null)
            {
                _waveChannel.Volume = value;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayVolumeText));
        }
    }

    public void Dispose()
    {
        _processorStream?.Dispose();
        _waveChannel?.Dispose();
        _waveOut.Dispose();
    }

    public void OpenAudioFile()
    {
        try
        {
            OpenFileDialog ofd = new()
            {
                Filter = "Audio (*.flac,*.mp3,*.wav)|*.flac;*.mp3;*.wav",
                Multiselect = false,
                InitialDirectory = Settings.Default.AudioFilesPath
            };
            if (!ofd.ShowDialog(_window)!.Value)
            {
                return;
            }

            AudioFilePath = ofd.FileName;
            Settings.Default.AudioFilePath = AudioFilePath;
            Settings.Default.AudioFilesPath = Path.GetDirectoryName(AudioFilePath);
            Settings.Default.Save();
            OpenAudio();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void OpenLyricsFile()
    {
        try
        {
            if (!CheckIfSaved())
            {
                return;
            }

            OpenFileDialog ofd = new()
            {
                Filter = "Lyrics (*.lrc,*.txt)|*.lrc;*.txt",
                Multiselect = false,
                InitialDirectory = Settings.Default.LyricsFilesPath
            };
            if (!ofd.ShowDialog(_window)!.Value)
            {
                return;
            }

            LyricsFilePath = ofd.FileName;
            Settings.Default.LyricsFilePath = LyricsFilePath;
            Settings.Default.LyricsFilesPath = Path.GetDirectoryName(LyricsFilePath);
            Settings.Default.Save();
            ParseLyricsFromFile();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public bool SaveLyrics()
    {
        try
        {
            if (!IsLyricsFileLoaded)
            {
                return SaveLyricsToNewFile();
            }

            string text = GetLyricsText();
            File.WriteAllText(LyricsFilePath, text, Encoding.UTF8);
            _isSaved = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return false;
        }

        return true;
    }

    public bool SaveLyricsToNewFile()
    {
        try
        {
            SaveFileDialog sfd = new()
            {
                Filter = "Lyrics (*.lrc)|*.lrc",
                FileName = IsLyricsFileLoaded ? Path.GetFileNameWithoutExtension(LyricsFilePath) : IsAudioFileLoaded ? Path.GetFileNameWithoutExtension(AudioFilePath) : ""
            };
            if (!sfd.ShowDialog(_window)!.Value)
            {
                return false;
            }

            string text = GetLyricsText();
            File.WriteAllText(sfd.FileName, text, Encoding.UTF8);
            LyricsFilePath = sfd.FileName;
            Settings.Default.LyricsFilePath = LyricsFilePath;
            Settings.Default.LyricsFilesPath = Path.GetDirectoryName(LyricsFilePath);
            Settings.Default.Save();
            _isSaved = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return false;
        }

        return true;
    }

    public void ParseLyrics(string text)
    {
        try
        {
            _firstLine = null;
            _currentLine = null;
            _currentWord = null;
            LyricsLine? previousLine = null;
            LyricsWord? previousWord = null;
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
                Regex regex = new(PatternLine);
                Match match = regex.Match(line);
                string restOfLine = line;
                if (match.Success)
                {
                    lyricsLine.StartTime = new TimeSpan(0, 0, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) * 10);
                    restOfLine = match.Groups[4].Value;
                }

                List<string> words = restOfLine.Split(' ').ToList();
                foreach (string word in words)
                {
                    if (string.IsNullOrWhiteSpace(word) && !string.IsNullOrWhiteSpace(restOfLine))
                    {
                        continue;
                    }

                    LyricsWord lyricsWord = new()
                    {
                        Line = lyricsLine
                    };
                    regex = new Regex(PatternWord);
                    match = regex.Match(word);
                    string restOfWord = word;
                    if (match.Success)
                    {
                        lyricsWord.EndTime = new TimeSpan(0, 0, int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value) * 10);
                        restOfWord = match.Groups[1].Value;
                    }

                    if (string.IsNullOrWhiteSpace(restOfWord))
                    {
                        restOfWord = " ";
                    }

                    lyricsWord.Word = restOfWord;
                    lyricsLine.Words.Add(lyricsWord);
                    lyricsLine.FirstWord ??= lyricsWord;
                    if (previousWord != null)
                    {
                        previousWord.NextWord = lyricsWord;
                        lyricsWord.PreviousWord = previousWord;
                    }

                    previousWord = lyricsWord;
                }

                lyricsLine.LastWord ??= previousWord;

                LyricsLines.Add(lyricsLine);
                _firstLine ??= lyricsLine;
                if (previousLine != null)
                {
                    previousLine.NextLine = lyricsLine;
                    lyricsLine.PreviousLine = previousLine;
                }

                previousLine = lyricsLine;
            }

            if ((LyricsLines[^1].FirstWord != LyricsLines[^1].LastWord || LyricsLines[^1].FirstWord?.Word != " ") && _waveChannel != null)
            {
                LyricsWord newWord = new()
                {
                    EndTime = _waveChannel.TotalTime,
                    Word = " ",
                    PreviousWord = previousWord
                };
                LyricsLine newLine = new()
                {
                    StartTime = new TimeSpan(0, 0, 0),
                    PreviousLine = previousLine,
                    FirstWord = newWord,
                    LastWord = newWord
                };
                newWord.Line = newLine;
                newLine.Words.Add(newWord);
                if (previousWord != null)
                {
                    previousWord.NextWord = newWord;
                }

                if (previousLine != null)
                {
                    previousLine.NextLine = newLine;
                }

                LyricsLines.Add(newLine);
            }

            SelectFirstParagraph();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void PlayOrPause()
    {
        if (!IsAudioFileLoaded)
        {
            return;
        }

        if (PlayPauseText == "Play (Space)")
        {
            Play();
            ChangeButtonToPause();
        }
        else
        {
            Pause();
            ChangeButtonToPlay();
        }
    }

    public void Rewind()
    {
        try
        {
            if (_waveChannel == null)
            {
                return;
            }

            if (_waveChannel.CurrentTime.TotalSeconds < SkipInterval)
            {
                _waveChannel.Position = 0;
            }
            else
            {
                _waveChannel.CurrentTime -= new TimeSpan(0, 0, SkipInterval);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void FastForward()
    {
        try
        {
            if (_waveChannel == null)
            {
                return;
            }

            if (_waveChannel.CurrentTime > _waveChannel.TotalTime - new TimeSpan(0, 0, SkipInterval))
            {
                _waveChannel.CurrentTime = _waveChannel.TotalTime;
                Pause();
            }
            else
            {
                _waveChannel.CurrentTime += new TimeSpan(0, 0, SkipInterval);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void Stop()
    {
        try
        {
            if (_processorStream == null)
            {
                return;
            }

            _waveOut.Stop();
            _playTimer.Stop();
            _processorStream.Position = 0;
            _processorStream.Flush();
            PlaySliderPosition = 0;
            ChangeButtonToPlay();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void RemoveTimeInPreviousWord()
    {
        try
        {
            if (_currentWord?.PreviousWord == null)
            {
                return;
            }

            SelectPreviousWord();
            if (_currentLine != null && _currentWord == _currentLine.FirstWord)
            {
                _currentLine.StartTime = new TimeSpan(0, 0, 0);
            }
            else if (_currentWord?.PreviousWord != null)
            {
                _currentWord.PreviousWord.EndTime = new TimeSpan(0, 0, 0);
            }

            _isSaved = false;
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
            if (_waveChannel == null || _currentLine?.PreviousLine == null || _currentWord?.PreviousWord == null || _currentWord != _currentLine.FirstWord)
            {
                return;
            }

            TimeSpan timeSpan = _waveChannel.CurrentTime;
            LyricsWord newWord = new()
            {
                EndTime = _currentLine.StartTime,
                Word = " ",
                PreviousWord = _currentWord.PreviousWord,
                NextWord = _currentWord
            };
            LyricsLine newLine = new()
            {
                StartTime = timeSpan,
                PreviousLine = _currentLine.PreviousLine,
                NextLine = _currentLine,
                FirstWord = newWord,
                LastWord = newWord
            };
            newWord.Line = newLine;
            newLine.Words.Add(newWord);
            _currentWord.PreviousWord.EndTime = timeSpan;
            _currentWord.PreviousWord.NextWord = newWord;
            _currentWord.PreviousWord = newWord;
            _currentLine.PreviousLine.NextLine = newLine;
            _currentLine.PreviousLine = newLine;
            LyricsLines.Insert(LyricsLines.IndexOf(_currentLine), newLine);
            _isSaved = false;
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
            if (_waveChannel == null)
            {
                return;
            }

            if (_currentLine != null && _currentWord == _currentLine.FirstWord)
            {
                _currentLine.StartTime = _waveChannel.CurrentTime;
            }

            if (_currentWord?.PreviousWord != null)
            {
                _currentWord.PreviousWord.EndTime = _waveChannel.CurrentTime;
            }

            if (_currentWord == _firstLine?.FirstWord && PlaySliderPosition > 0)
            {
                LyricsWord newWord = new()
                {
                    EndTime = _waveChannel.CurrentTime,
                    NextWord = _currentWord,
                    Word = " "
                };

                LyricsLine newLine = new()
                {
                    StartTime = new TimeSpan(0, 0, 0),
                    NextLine = _currentLine,
                    FirstWord = newWord,
                    LastWord = newWord
                };

                newLine.Words.Add(newWord);
                newWord.Line = newLine;

                if (_currentWord != null)
                {
                    _currentWord.PreviousWord = newWord;
                }

                if (_currentLine != null)
                {
                    _currentLine.PreviousLine = newLine;
                }

                LyricsLines.Insert(0, newLine);
            }

            SelectNextWord();

            _isSaved = false;
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

    public bool CheckIfSaved()
    {
        try
        {
            if (_isSaved)
            {
                return true;
            }

            MessageBoxResult result = MessageBox.Show(_window, "Zapisać zmiany?", "Zmiany", MessageBoxButton.YesNoCancel);
            return result switch
            {
                MessageBoxResult.Yes => SaveLyricsToNewFile(),
                MessageBoxResult.Cancel => false,
                _ => true
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return false;
        }
    }

    public void SetTempoUp()
    {
        try
        {
            if (!IsAudioFileLoaded || _processorStream == null)
            {
                return;
            }

            if (PlayTempo < 2)
            {
                PlayTempo += 0.1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SetTempoDown()
    {
        try
        {
            if (!IsAudioFileLoaded || _processorStream == null)
            {
                return;
            }

            if (PlayTempo > 0.1)
            {
                PlayTempo -= 0.1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SetVolumeUp()
    {
        try
        {
            if (!IsAudioFileLoaded || _waveChannel == null)
            {
                return;
            }

            if (PlayVolume < 1)
            {
                PlayVolume += 0.1f;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void SetVolumeDown()
    {
        try
        {
            if (!IsAudioFileLoaded || _waveChannel == null)
            {
                return;
            }

            if (PlayVolume > 0.1)
            {
                PlayVolume -= 0.1f;
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

            LyricsLine newLine = new()
            {
                StartTime = _currentWord.PreviousWord.EndTime,
                PreviousLine = _currentLine,
                NextLine = _currentLine.NextLine,
                FirstWord = _currentWord,
                LastWord = _currentWord
            };

            LyricsWord word = _currentWord;
            while (word.Line == _currentLine)
            {
                newLine.Words.Add(word);
                _currentLine.Words.Remove(word);
                word.Line = newLine;
                if (word.NextWord == null)
                {
                    break;
                }

                newLine.LastWord = word;
                word = word.NextWord;
            }

            if (_currentLine?.NextLine != null)
            {
                _currentLine.NextLine.PreviousLine = newLine;
                LyricsLines.Insert(LyricsLines.IndexOf(_currentLine.NextLine), newLine);
            }
            else
            {
                LyricsLines.Add(newLine);
            }

            if (_currentLine != null)
            {
                _currentLine.NextLine = newLine;
                _currentLine.LastWord = _currentWord.PreviousWord;
            }

            _currentLine = newLine;
            _isSaved = false;
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

            LyricsLine previousLine = _currentLine.PreviousLine;

            LyricsWord word = _currentWord;
            while (word.Line == _currentLine)
            {
                previousLine.Words.Add(word);
                _currentLine.Words.Remove(word);
                word.Line = previousLine;
                if (word.NextWord == null)
                {
                    break;
                }

                previousLine.LastWord = word;
                word = word.NextWord;
            }

            if (_currentLine.PreviousLine != null)
            {
                _currentLine.PreviousLine.NextLine = _currentLine.NextLine;
            }

            if (_currentLine.NextLine != null)
            {
                _currentLine.NextLine.PreviousLine = previousLine;
            }

            LyricsLines.Remove(_currentLine);

            _currentLine = previousLine;
            _isSaved = false;
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
                _currentLine?.Words.Add(word);
                nextLine.Words.Remove(word);
                word.Line = _currentLine;
                if (word.NextWord == null)
                {
                    break;
                }

                if (_currentLine != null)
                {
                    _currentLine.LastWord = word;
                }

                word = word.NextWord;
            }

            if (_currentLine != null)
            {
                _currentLine.NextLine = nextLine.NextLine;
            }

            if (_currentLine?.NextLine != null)
            {
                _currentLine.NextLine.PreviousLine = _currentLine;
            }

            LyricsLines.Remove(nextLine);

            _isSaved = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void MovePlaySliderToWord(LyricsWord word)
    {
        if (word.Line == null)
        {
            return;
        }

        TimeSpan timeSpan = word.Line.StartTime;
        if (word != word.Line.FirstWord && word.PreviousWord != null)
        {
            timeSpan = word.PreviousWord.EndTime;
        }

        PlaySliderPosition = (long) (timeSpan - TimeSpan.FromSeconds(3)).TotalMilliseconds;
        Play();
    }

    public void NewLyrics()
    {
        if (!CheckIfSaved())
        {
            return;
        }

        ParseLyrics("");
        LyricsFilePath = "Open or paste lyrics";
    }

    public void ParseLyricsFromClipboard()
    {
        if (!CheckIfSaved())
        {
            return;
        }

        string text = Clipboard.GetText();
        ParseLyrics(text);
    }

    public void ShowAboutWindow()
    {
        AboutWindow aboutWindow = new()
        {
            Owner = _window
        };
        aboutWindow.ShowDialog();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void PlayTimerTick(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentTimeText));
        OnPropertyChanged(nameof(PlaySliderPosition));
    }

    private string GetLyricsText()
    {
        string text = "";
        try
        {
            foreach (LyricsLine lyricsLine in LyricsLines)
            {
                text += lyricsLine.StartTimeText;
                text = lyricsLine.Words.Aggregate(text, (current, word) => current + $"{word.Word}{word.EndTimeText} ");
                text += Environment.NewLine;
            }

            text.Remove(text[^1]);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return "";
        }

        return text;
    }

    private void ParseLyricsFromFile()
    {
        string text = File.ReadAllText(LyricsFilePath);
        ParseLyrics(text);
    }

    private void ChangeButtonToPlay()
    {
        PlayPauseText = "Play (Space)";
        PlayPauseIconPath = @"..\Images\play.png";
    }

    private void ChangeButtonToPause()
    {
        PlayPauseText = "Pause (Space)";
        PlayPauseIconPath = @"..\Images\pause.png";
    }

    private void Play()
    {
        try
        {
            if (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            _waveOut.Play();
            _playTimer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Pause()
    {
        try
        {
            if (_waveOut.PlaybackState != PlaybackState.Playing)
            {
                return;
            }

            _waveOut.Pause();
            _playTimer.Stop();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void CloseWaveOut()
    {
        _waveOut.Stop();
        _waveOut.Dispose();

        _processorStream?.Dispose();
    }

    private void OpenAudio()
    {
        CloseWaveOut();

        try
        {
            WaveStream reader;
            if (Path.GetExtension(AudioFilePath) == ".flac")
            {
                MediaFoundationReader mediaFoundationReader = new(AudioFilePath);
                WaveFormat outFormat = new(44100, mediaFoundationReader.WaveFormat.Channels);

                using MediaFoundationResampler resampler = new(mediaFoundationReader, outFormat);
                WaveFileWriter.CreateWaveFile($"{Path.GetTempPath()}temp.wav", resampler);
                reader = new AudioFileReader($"{Path.GetTempPath()}temp.wav");
            }
            else
            {
                reader = new AudioFileReader(AudioFilePath);
            }

            _waveChannel = new WaveChannel32(reader)
            {
                PadWithZeroes = false
            };

            OnPropertyChanged(nameof(TotalTimeText));
            OnPropertyChanged(nameof(PlaySliderMaximum));

            _processorStream = new SoundTouchWaveStream(_waveChannel);

            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100
            };

            _waveOut.Init(_processorStream);
            _waveOut.PlaybackStopped += OnPlaybackStopped;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
    {
        if (_waveOut.PlaybackState == PlaybackState.Stopped)
        {
            Stop();
        }
    }

    private void SelectFirstParagraph()
    {
        try
        {
            _currentLine = _firstLine;
            SelectWord(_currentLine?.FirstWord);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}