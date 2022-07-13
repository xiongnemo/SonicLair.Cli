﻿using LibVLCSharp.Shared;

using Newtonsoft.Json;

using SonicLair.Services;
using SonicLair.Types.SonicLair;

using SonicLairXbox.Infrastructure;
using SonicLairXbox.Types.SonicLair;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SonicLairXbox.Services
{
    public class CurrentState
    {
        public Song CurrentTrack { get; set; }
        public decimal Position { get; set; }
        public bool IsPlaying { get; set; }
        public Playlist CurrentPlaylist { get; set; }
        public bool IsShuffled { get; set; }
    }

    public class MusicPlayerService : IMusicPlayerService
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly ISubsonicService _client;
        private Playlist _playlist;
        private List<Song> _originalPlaylist;
        private readonly List<EventHandler<CurrentStateChangedEventArgs>> _currentStateListeners;
        private readonly List<EventHandler<MediaPlayerTimeChangedEventArgs>> _playerTimeListeners;
        private bool _isShuffling;
        public Song _currentTrack { get; private set; }

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
            _currentStateListeners = new List<EventHandler<CurrentStateChangedEventArgs>>();
            _playerTimeListeners = new List<EventHandler<MediaPlayerTimeChangedEventArgs>>();
            LibVLCSharp.Shared.Core.Initialize();
            _libVlc = new LibVLC();

            _mediaPlayer = new MediaPlayer(_libVlc);
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
                ((App)App.Current).NotifyObservers("MSplay");
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

                    ((App)App.Current).NotifyObservers("MSpaused");
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
                ((App)App.Current).NotifyObservers("MSprogress", $"{{\"time\": {t}}}");
            };
            _mediaPlayer.EndReached += (sender, args) =>
            {
                ThreadPool.QueueUserWorkItem(_ => Next());
            };
        }

        public CurrentState GetCurrentState()
        {
            return new CurrentState()
            {
                CurrentTrack = _currentTrack,
                Position = (decimal)_mediaPlayer.Position,
                IsPlaying = _mediaPlayer.IsPlaying,
                CurrentPlaylist = _playlist,
                IsShuffled = _isShuffling
            };
        }

        public void Shuffle()
        {
            if (_isShuffling) {
                _playlist.Entry = _originalPlaylist;
            }
            else
            {
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

        public void Play()
        {
            _mediaPlayer.Play();
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
            _mediaPlayer.Position = Math.Clamp(position + time, 0, 1);
        }

        private void LoadMedia()
        {
            var uri = _client.GetSongUri(_currentTrack.Id);
            var media = new Media(_libVlc, uri);
            _mediaPlayer.Media = media;
            ((App)App.Current).NotifyObservers("MScurrentTrack", $"{{\"currentTrack\": {JsonConvert.SerializeObject(_currentTrack, StaticHelpers.GetJsonSerializerSettings())}}}");
        }

        public void Next()
        {
            if (_playlist.Entry.IndexOf(_currentTrack) == _playlist.Entry.Count - 1)
            {
                return;
            }
            _currentTrack = _playlist.Entry[_playlist.Entry.IndexOf(_currentTrack) + 1];
            LoadMedia();
            Play();
        }

        public void Prev()
        {
            if (_playlist.Entry.IndexOf(_currentTrack) == 0)
            {
                return;
            }
            _currentTrack = _playlist.Entry[_playlist.Entry.IndexOf(_currentTrack) - 1];
            LoadMedia();
            Play();
        }

        private int GetPlaylistDuration(List<Song> songs)
        {
            return songs.Sum(s => s.Duration);
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
                StaticHelpers.ShowError(ex.Message);
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
                _ = songs.Prepend(await _client.GetSong(id));
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
                StaticHelpers.ShowError(ex.Message);
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
                StaticHelpers.ShowError(ex.Message);
                return;
            }

            LoadImagesAndPlay(track);
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