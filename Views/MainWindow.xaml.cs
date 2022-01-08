using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Do_Re_Mi_Lyrics.Models;
using Do_Re_Mi_Lyrics.ViewModels;

namespace Do_Re_Mi_Lyrics.Views;

public partial class MainWindow

{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(this);
        DataContext = _viewModel;
    }

    private void OpenAudioClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenAudioFile();
    }

    private void OpenLyricsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenLyricsFile();
    }

    private void SaveLyricsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveLyrics();
    }

    private void PlayPauseClick(object sender, RoutedEventArgs e)
    {
        _viewModel.PlayOrPause();
    }

    private void RewindClick(object sender, RoutedEventArgs e)
    {
        _viewModel.Rewind();
    }

    private void StopClick(object sender, RoutedEventArgs e)
    {
        _viewModel.Stop();
    }

    private void ForwardClick(object sender, RoutedEventArgs e)
    {
        _viewModel.FastForward();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (e.Key)
        {
            case Key.F1 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.ShowAboutWindow();
                break;
            case Key.F2 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.NewLyrics();
                break;
            case Key.F3 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.OpenAudioFile();
                break;
            case Key.F4 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.OpenLyricsFile();
                break;
            case Key.F5 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.SetTimeToCurrentWord();
                break;
            case Key.F6 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.SetEndingTimeToPreviousLine();
                break;
            case Key.F8 when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.RemoveTimeInPreviousWord();
                break;
            case Key.S when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                _viewModel.SaveLyricsToNewFile();
                break;
            case Key.S when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.SaveLyrics();
                break;
            case Key.V when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.ParseLyricsFromClipboard();
                break;
            case Key.Left when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                _viewModel.Rewind();
                break;
            case Key.Right when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                _viewModel.FastForward();
                break;
            case Key.Left when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.SelectPreviousWord();
                break;
            case Key.Right when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.SelectNextWord();
                break;
            case Key.Up when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.SetTempoUp();
                break;
            case Key.Down when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.SetTempoDown();
                break;
            case Key.Up when Keyboard.Modifiers == ModifierKeys.Shift:
                _viewModel.SetVolumeUp();
                break;
            case Key.Down when Keyboard.Modifiers == ModifierKeys.Shift:
                _viewModel.SetVolumeDown();
                break;
            case Key.Up when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.SelectPreviousLine();
                break;
            case Key.Down when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.SelectNextLine();
                break;
            case Key.Enter when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.MoveWordToNewLine();
                break;
            case Key.Back when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.MoveLineToPrevious();
                break;
            case Key.Delete when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.MoveNextLineToCurrent();
                break;
            case Key.Space when Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.PlayOrPause();
                break;
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_viewModel.CheckIfSaved())
        {
            e.Cancel = true;
        }

        _viewModel.Dispose();

        if (File.Exists($"{Path.GetTempPath()}temp.wav"))
        {
            try
            {
                File.Delete($"{Path.GetTempPath()}temp.wav");
            }
            catch
            {
                // ignored
            }
        }
    }

    private void Word_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            TextBlock wordTextBlock = (TextBlock) sender;
            LyricsWord word = (LyricsWord) wordTextBlock.DataContext;
            _viewModel.SelectWord(word);
            if (e.ClickCount == 2)
            {
                _viewModel.MovePlaySliderToWord(word);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void SaveLyricsAsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveLyricsToNewFile();
    }

    private void NewLyricsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.NewLyrics();
    }

    private void HelpClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowAboutWindow();
    }

    private void EditClick(object sender, RoutedEventArgs e)
    {
    }
}