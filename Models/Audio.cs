using System;
using System.IO;
using System.Windows;
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;

namespace Do_Re_Mi_Lyrics.Models;

public class Audio : IDisposable
{
    private const int SkipInterval = 5;
    private SoundTouchWaveStream? _processorStream;
    private WaveStream? _reader;
    private double _tempo;
    private float _volume;
    private WaveChannel32? _waveChannel;

    private IWavePlayer _waveOut = new WaveOutEvent
    {
        DesiredLatency = 100
    };

    public string CurrentTimeText => (_waveChannel?.CurrentTime ?? TimeSpan.Zero).ToString(@"mm\:ss\.ff");

    internal TimeSpan TotalTime => _waveChannel?.TotalTime ?? TimeSpan.Zero;

    internal TimeSpan CurrentTime
    {
        get => TimeSpan.FromMilliseconds((_waveChannel?.CurrentTime ?? TimeSpan.Zero).TotalMilliseconds - (_waveChannel?.CurrentTime ?? TimeSpan.Zero).TotalMilliseconds % 10);
        set
        {
            if (_waveChannel != null)
            {
                _waveChannel.CurrentTime = value;
            }
        }
    }

    internal double Tempo
    {
        get => _processorStream?.Tempo ?? 1;
        set
        {
            _tempo = value;
            if (_processorStream != null)
            {
                _processorStream.Tempo = value;
            }
        }
    }

    internal float Volume
    {
        get => _waveChannel?.Volume ?? 1;
        set
        {
            _volume = value;
            if (_waveChannel != null)
            {
                _waveChannel.Volume = value;
            }
        }
    }

    public void Dispose()
    {
        _processorStream?.Dispose();
        _waveChannel?.Dispose();
        _waveOut.Dispose();
    }

    internal void Rewind()
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

    internal void FastForward()
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

    internal void Stop()
    {
        try
        {
            if (_processorStream == null)
            {
                return;
            }

            _waveOut.Stop();
            Application.MainWindowViewModel.StopTimer();
            if (_processorStream.CanSeek)
            {
                _processorStream.Position = 0;
                Application.MainWindowViewModel.PlaySliderPosition = 0;
            }

            _processorStream.Flush();
            Application.MainWindowViewModel.ChangeButtonToPlay();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }


    internal void SetTempoUp()
    {
        try
        {
            if (!Application.MainWindowViewModel.IsAudioFileLoaded || _processorStream == null)
            {
                return;
            }

            if (Application.MainWindowViewModel.PlayTempo < 2)
            {
                Application.MainWindowViewModel.PlayTempo += 0.1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal void SetTempoDown()
    {
        try
        {
            if (!Application.MainWindowViewModel.IsAudioFileLoaded || _processorStream == null)
            {
                return;
            }

            if (Application.MainWindowViewModel.PlayTempo > 0.1)
            {
                Application.MainWindowViewModel.PlayTempo -= 0.1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal void SetVolumeUp()
    {
        try
        {
            if (!Application.MainWindowViewModel.IsAudioFileLoaded || _waveChannel == null)
            {
                return;
            }

            if (Application.MainWindowViewModel.PlayVolume < 1)
            {
                Application.MainWindowViewModel.PlayVolume += 0.1f;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal void SetVolumeDown()
    {
        try
        {
            if (!Application.MainWindowViewModel.IsAudioFileLoaded || _waveChannel == null)
            {
                return;
            }

            if (Application.MainWindowViewModel.PlayVolume > 0.1)
            {
                Application.MainWindowViewModel.PlayVolume -= 0.1f;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal void Play()
    {
        try
        {
            if (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            _waveOut.Play();
            Application.MainWindowViewModel.StartTimer();
            Application.MainWindowViewModel.ChangeButtonToPause();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal void Pause()
    {
        try
        {
            if (_waveOut.PlaybackState != PlaybackState.Playing)
            {
                return;
            }

            _waveOut.Pause();
            Application.MainWindowViewModel.StopTimer();
            Application.MainWindowViewModel.ChangeButtonToPlay();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    internal void OpenAudio()
    {
        CloseWaveOut();

        try
        {
            if (Path.GetExtension(Application.MainWindowViewModel.AudioFilePath) == ".flac")
            {
                MediaFoundationReader mediaFoundationReader = new(Application.MainWindowViewModel.AudioFilePath);
                WaveFormat outFormat = new(44100, mediaFoundationReader.WaveFormat.Channels);

                using MediaFoundationResampler resampler = new(mediaFoundationReader, outFormat);
                WaveFileWriter.CreateWaveFile($"{Path.GetTempPath()}temp.wav", resampler);
                _reader = new AudioFileReader($"{Path.GetTempPath()}temp.wav");
            }
            else
            {
                _reader = new AudioFileReader(Application.MainWindowViewModel.AudioFilePath);
            }

            _waveChannel = new WaveChannel32(_reader)
            {
                PadWithZeroes = false,
                Volume = _volume
            };


            _processorStream = new SoundTouchWaveStream(_waveChannel)
            {
                Tempo = _tempo
            };
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

    private void CloseWaveOut()
    {
        _waveOut.Stop();
        _waveOut.Dispose();

        _processorStream?.Dispose();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
    {
        if (_waveOut.PlaybackState == PlaybackState.Stopped)
        {
            Stop();
        }
    }
}