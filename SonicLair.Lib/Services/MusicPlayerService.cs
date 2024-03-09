﻿using LibVLCSharp.Shared;

using Newtonsoft.Json;

using SonicLair.Lib.Infrastructure;
using SonicLair.Lib.Types;
using SonicLair.Lib.Types.SonicLair;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SonicLair.Lib.Services
{
    public class MusicPlayerService : IMusicPlayerService
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly ISubsonicService _client;
        private Playlist _playlist;
        private List<Song> _originalPlaylist;
        private readonly List<EventHandler<CurrentStateChangedEventArgs>> _currentStateListeners;
        private readonly List<EventHandler<MediaPlayerTimeChangedEventArgs>> _playerTimeListeners;
        private readonly List<EventHandler<MediaPlayerVolumeChangedEventArgs>> _playerVolumeListeners;
        private RepeatStatus _repeatStatus;
        private bool _isShuffling;
        public Song _currentTrack { get; private set; }
        public INotifier Notifier { get; set; }

        public MusicPlayerService(ISubsonicService subsonicService)
        {
            _client = subsonicService;
            _isShuffling = false;
            _playlist = new Playlist()
            {
                CoverArt = "",
                Comment = "",
                Created = "",
                Duration = 0,
                Entry = new List<Song>(),
                Id = "",
                Image = "",
                Name = "",
                Owner = "",
                Public = false,
                SongCount = 0
            };
            _originalPlaylist = new List<Song>();
            _currentStateListeners = new List<EventHandler<CurrentStateChangedEventArgs>>();
            _playerTimeListeners = new List<EventHandler<MediaPlayerTimeChangedEventArgs>>();
            _playerVolumeListeners = new List<EventHandler<MediaPlayerVolumeChangedEventArgs>>();
            LibVLCSharp.Shared.Core.Initialize();
            _libVlc = new LibVLC("--quiet");

            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.VolumeChanged += (sender, args) =>
            {
                try
                {
                    foreach (var handler in _playerVolumeListeners)
                    {
                        handler.Invoke(this, args);
                    }
                }
                catch (Exception)
                {
                    // Concurrency is hard
                }
            };
            _mediaPlayer.Playing += (sender, args) =>
            {
                try
                {
                    foreach (var handler in _currentStateListeners)
                    {
                        handler.Invoke(this, new CurrentStateChangedEventArgs()
                        {
                            CurrentState = GetCurrentState()
                        });
                    }
                }
                catch (Exception)
                {
                    // Concurrency is hard
                }
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("MSplay");
                }
            };
            _mediaPlayer.Paused += (sender, args) =>
            {
                try
                {
                    foreach (var handler in _currentStateListeners)
                    {
                        handler.Invoke(this, new CurrentStateChangedEventArgs()
                        {
                            CurrentState = GetCurrentState()
                        });
                    }
                }
                catch (Exception)
                {
                    // Concurrency is hard
                }
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("MSpaused");
                }
            };
            _mediaPlayer.Stopped += (sender, args) =>
            {
                try
                {
                    foreach (var handler in _currentStateListeners)
                    {
                        var currentState = GetCurrentState();
                        currentState.Stopped = true;
                        currentState.IsPlaying = false;
                        handler.Invoke(this, new CurrentStateChangedEventArgs()
                        {
                            CurrentState = currentState
                        });
                    }
                }
                catch (Exception)
                {
                    // Concurrency is hard
                }
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("MSstopped");
                }
            };
            _mediaPlayer.Volume = 100;
            _mediaPlayer.TimeChanged += (sender, args) =>
            {
                try
                {
                    foreach (var handler in _playerTimeListeners)
                    {
                        handler.Invoke(this, args);
                    }
                }
                catch (Exception)
                {
                    // Concurrency is hard
                }
                string t = ((args.Time / _currentTrack.Duration) / 1000d).ToString(new CultureInfo("en-US"));
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("MSprogress", $"{{\"time\": {t}}}");
                }
            };
            _mediaPlayer.EndReached += (sender, args) =>
            {
                // Invoke when the media has finished playing
                ThreadPool.QueueUserWorkItem(_ => Next());
            };
        }

        public void SetNotifier(INotifier notifier)
        {
            Notifier = notifier;
        }

        public void SetVolume(int v, bool relative = false)
        {
            var newVol = v;
            if (relative)
            {
                newVol = (_mediaPlayer.Volume + v).Clamp(0, 100);
            }
            _mediaPlayer.Volume = newVol;
        }

        public CurrentState GetCurrentState()
        {
            return new CurrentState()
            {
                CurrentTrack = _currentTrack,
                Position = (decimal)_mediaPlayer.Position,
                IsPlaying = _mediaPlayer.IsPlaying,
                Stopped = false,
                CurrentPlaylist = _playlist,
                RepeatStatus = _repeatStatus,
                IsShuffled = _isShuffling
            };
        }
        public void ToggleRepeat()
        {
            _repeatStatus = _repeatStatus.Next();
            try
            {
                foreach (var handler in _currentStateListeners)
                {
                    handler.Invoke(this, new CurrentStateChangedEventArgs()
                    {
                        CurrentState = GetCurrentState()
                    });
                }
            }
            catch (Exception)
            {
                // Concurrency is hard
            }
        }
        public void Shuffle()
        {
            if (_isShuffling)
            {
                _playlist.Entry = new List<Song>(_originalPlaylist);
            }
            else
            {
                if (_playlist.Entry.Count == 0)
                {
                    return;
                }
                _playlist.Entry.Shuffle();
                var first = _playlist.Entry[0];
                var index = _playlist.Entry.IndexOf(_currentTrack);
                _playlist.Entry[0] = _currentTrack;
                _playlist.Entry[index] = first;
            }
            _isShuffling = !_isShuffling;
            try
            {
                foreach (var handler in _currentStateListeners)
                {
                    handler.Invoke(this, new CurrentStateChangedEventArgs()
                    {
                        CurrentState = GetCurrentState()
                    });
                }
            }
            catch (Exception)
            {
                // Concurrency is hard
            }
        }

        public void PlayPause()
        {
            if (_mediaPlayer.IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        public void Play()
        {
            if (_playlist.Entry.Count > 0 && _currentTrack == null)
            {
                _currentTrack = _playlist.Entry[0];
                LoadMedia();
            }
            _mediaPlayer.Play();
        }

        public void SkipTo(int index)
        {
            if (index > 0 && index < _playlist.Entry.Count)
            {
                _currentTrack = _playlist.Entry[index];
            }
            LoadMedia();
            Play();
        }

        public void Pause()
        {
            _mediaPlayer.Pause();
        }

        public void Seek(float time, bool relative = false)
        {
            if (!relative && time < 0)
            {
                // Roads? Where we're going we don't need roads.
                return;
            }
            float position = 0;
            if (relative)
            {
                position = _mediaPlayer.Position;
            }
            _mediaPlayer.Position = (position + time).Clamp(0, 1);
        }

        private void LoadMedia()
        {
            var uri = _client.GetSongUri(_currentTrack.Id);
            var media = new Media(_libVlc, uri);
            _mediaPlayer.Media = media;
            if (Notifier != null)
            {
                Notifier.NotifyObservers("MScurrentTrack", $"{{\"currentTrack\": {JsonConvert.SerializeObject(_currentTrack, StaticHelpers.GetJsonSerializerSettings())}}}");
            }
            _client.Scrobble(_currentTrack.Id);
        }

        // intendeded by user
        public void ToggleNext()
        {
            if (_playlist.Entry.IndexOf(_currentTrack) == _playlist.Entry.Count - 1)
            {
                switch (_repeatStatus)
                {
                    case RepeatStatus.None:
                    case RepeatStatus.RepeatOne:
                        return;
                    case RepeatStatus.RepeatAll:
                        _currentTrack = _playlist.Entry[0];
                        LoadMedia();
                        Play();
                        return;
                    default:
                        return;
                }
            }
            _currentTrack = _playlist.Entry[_playlist.Entry.IndexOf(_currentTrack) + 1];
            LoadMedia();
            Play();
        }

        // not intended by user
        public void Next()
        {
            switch (_repeatStatus)
            {
                case RepeatStatus.RepeatOne:
                    LoadMedia();
                    Play();
                    return;
                case RepeatStatus.None:
                case RepeatStatus.RepeatAll:
                default:
                    break;
            }
            ToggleNext();
        }

        public void Prev()
        {
            if (_playlist.Entry.IndexOf(_currentTrack) == 0)
            {
                switch (_repeatStatus)
                {
                    case RepeatStatus.None:
                    case RepeatStatus.RepeatOne:
                        return;
                    case RepeatStatus.RepeatAll:
                        _currentTrack = _playlist.Entry[_playlist.Entry.Count - 1];
                        LoadMedia();
                        Play();
                        return;
                    default:
                        return;
                }
            }
            _currentTrack = _playlist.Entry[_playlist.Entry.IndexOf(_currentTrack) - 1];
            LoadMedia();
            Play();
        }

        private int GetPlaylistDuration(List<Song> songs)
        {
            return songs.Sum(s => s.Duration);
        }

        public void AddToCurrentPlaylist(Song song)
        {
            _playlist.Entry.Add(song);
            _originalPlaylist.Add(song);
            foreach (var handler in _currentStateListeners)
            {
                handler.Invoke(this, new CurrentStateChangedEventArgs()
                {
                    CurrentState = GetCurrentState()
                });
            }
        }

        public async Task PlayPlaylist(string id, int track)
        {
            try
            {
                var playlist = await _client.GetPlaylist(id);
                _playlist = playlist;
                _originalPlaylist = playlist.Entry.ToList();
            }
            catch (SubsonicException ex)
            {
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("EX", ex.Message);
                }
                return;
            }
            LoadImagesAndPlay(track);
        }

        private void LoadImagesAndPlay(int track = 0)
        {
            foreach (var s in _playlist.Entry)
            {
                s.Image = _client.GetCoverArtUri(s.AlbumId);
            }
            _currentTrack = _playlist.Entry[track];
            LoadMedia();
            Play();
        }

        public async Task PlayRadio(string id)
        {
            try
            {
                var songs = await _client.GetSimilarSongs(id);
                var song = await _client.GetSong(id);
                songs = songs.Prepend(song).ToList();
                _playlist = new Playlist(
                    "",
                    $"Radio based on {songs[0].Title}",
                    $"by {songs[0].Artist} from {songs[0].Album}",
                    _client.GetActiveAccount().Username,
                    false,
                    songs.Count + 1,
                    GetPlaylistDuration(songs),
                    "",
                    "",
                    songs
                    );
                _originalPlaylist = _playlist.Entry.ToList();
            }
            catch (SubsonicException ex)
            {
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("EX", ex.Message);
                }
                return;
            }
            LoadImagesAndPlay();
        }

        public async Task PlayAlbum(string id, int track = 0)
        {
            try
            {
                var album = await _client.GetAlbum(id);
                _playlist = new Playlist(
                    "",
                    album.Name,
                    $"by {album.Artist}",
                    _client.GetActiveAccount().Username,
                    false,
                    album.Song.Count + 1,
                    GetPlaylistDuration(album.Song),
                    "",
                    "",
                    album.Song
                    );
                _originalPlaylist = _playlist.Entry.ToList();
            }
            catch (SubsonicException ex)
            {
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("EX", ex.Message);
                }
                return;
            }

            LoadImagesAndPlay(track);
        }

        public void RegisterPlayerVolumeHandler(EventHandler<MediaPlayerVolumeChangedEventArgs> handler)
        {
            _playerVolumeListeners.Add(handler);
        }

        public void UnregisterPlayerVolumeHandler(EventHandler<MediaPlayerVolumeChangedEventArgs> handler)
        {
            if (_playerVolumeListeners.Contains(handler))
            {
                _playerVolumeListeners.Remove(handler);
            }
        }

        public void RegisterCurrentStateHandler(EventHandler<CurrentStateChangedEventArgs> handler)
        {
            _currentStateListeners.Add(handler);
        }

        public void UnregisterCurrentStateHandler(EventHandler<CurrentStateChangedEventArgs> handler)
        {
            if (_currentStateListeners.Contains(handler))
            {
                _currentStateListeners.Remove(handler);
            }
        }

        public void RegisterTimeChangedHandler(EventHandler<MediaPlayerTimeChangedEventArgs> handler)
        {
            _playerTimeListeners.Add(handler);
        }

        public void UnregisterTimeChangedHandler(EventHandler<MediaPlayerTimeChangedEventArgs> handler)
        {
            if (_playerTimeListeners.Contains(handler))
            {
                _playerTimeListeners.Remove(handler);
            }
        }
    }
}