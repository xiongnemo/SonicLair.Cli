using LibVLCSharp.Shared;
using NStack;

using SonicLair.Cli;
using SonicLair.Cli.Tools;
using SonicLair.Lib.Infrastructure;
using SonicLair.Lib.Services;
using SonicLair.Lib.Types.SonicLair;

using Terminal.Gui;

namespace SonicLairCli
{
    public class MainWindow : IWindowFrame
    {
        private readonly Toplevel _top;
        private FrameView? audioControlView;
        private ISubsonicService? _subsonicService;
        private IMusicPlayerService? _musicPlayerService;
        private FrameView? mainView;
        private TextView? _nowPlaying;
        private ProgressBar? _playingTime;
        private ProgressBar? _volumeSlider;
        private TextView? _volumeText;
        private TextView? _RepeatText;
        private TextView? _shuffleText;
        private TextView? _timeElapsed;
        private TextView? _songDuration;
        private CurrentState? _state;
        private SonicLairListView<Song>? _nowPlayingList;
        private readonly History _history;

        public MainWindow(Toplevel top)
        {
            _top = top;
            _history = new History();
            _history.Push(() =>
            {
                _ = ArtistsView();
            });

        }

        private FrameView GetCurrentPlaylist(View anchorLeft)
        {
            var ret = new FrameView()
            {
                X = Pos.Right(anchorLeft),
                Y = 0,
                Height = anchorLeft.Height,
                Width = 35,
                Title = "Current Playlist",
            };
            if (_nowPlayingList == null)
            {
                _nowPlayingList = new SonicLairListView<Song>()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                };
                _nowPlayingList.OpenSelectedItem += (ListViewItemEventArgs e) =>
                {
                    var currentState = _musicPlayerService!.GetCurrentState();
                    var index = currentState.CurrentPlaylist.Entry.IndexOf((Song)e.Value);
                    if (index != -1)
                    {
                        _musicPlayerService.SkipTo(index);
                    }
                };
                _nowPlayingList.SetOnLeave((lv) =>
                {
                    if (lv?.Source != null && lv.Source.Count > 0)
                    {
                        var currentState = _musicPlayerService!.GetCurrentState();
                        if (currentState?.CurrentPlaylist != null && currentState.CurrentPlaylist.Entry.Any())
                        {
                            var index = currentState.CurrentPlaylist.Entry.IndexOf(currentState.CurrentTrack);
                            lv.SelectedItem = index;
                        }
                    }
                });
            }

            ret.Add(_nowPlayingList);
            return ret;
        }

        public FrameView GetMainView()
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = 0,
                Height = Dim.Fill() - 9,
                Width = Dim.Fill() - 35
            };
            return ret;
        }

        public FrameView GetBaseBar(View controlView)
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = Pos.Bottom(controlView),
                Height = 4,
                Width = Dim.Fill(),
                Title = "Controls"
            };
            _RepeatText = new TextView()
            {
                X = Pos.Right(ret) - (12 + 1),
                Y = 0,
                Height = 1,
                Width = 12,
                CanFocus = false,
                Text = "Repeat: Off",
            };
            _shuffleText = new TextView()
            {
                X = Pos.Right(ret) - (13 + 1),
                Y = 1,
                Height = 1,
                Width = 13,
                CanFocus = false,
                Text = "Shuffle: Off",
            };
            var helperText = new TextView()
            {
                X = 0,
                Y = 0,
                Height = 2,
                Width = Dim.Fill() - 15,
                CanFocus = false,
                Text =
@"C-a Artists | C-l Album | C-p Playlists | C-r Search | C-Right Fw(10s) | C-Left Bw(10s)
C-q Quit | Space Play/Pause | C-b Prev | C-n Next | C-t Repeat | C-h Shuffle | C-m Add | BackSpace Back",
            };
            ret.Add(_RepeatText, _shuffleText, helperText);
            return ret;
        }

        public FrameView GetAudioControlView(View anchorTop)
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = Pos.Bottom(anchorTop),
                Height = 5,
                Width = Dim.Fill(),
                Title = "Idling"
            };
            _nowPlaying = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(60),
                Height = 1,
                CanFocus = false,
            };
            var volumeLabel = new TextView()
            {
                X = Pos.Right(_nowPlaying),
                Y = 0,
                Width = 10,
                Height = 1,
                CanFocus = false,
                Text = "Vol[C-k/i]"
            };
            _volumeSlider = new ProgressBar()
            {
                X = Pos.Right(volumeLabel) + 1,
                Y = 0,
                // minus x: x = length of _volumeText + extra space
                Width = Dim.Fill() - (4 + 1),
                Height = 1,
                ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage,
                ProgressBarStyle = ProgressBarStyle.Blocks,
                CanFocus = false,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black),
                    Focus = Application.Driver.MakeAttribute(Color.White, Color.Black)
                },
                Fraction = 1
            };
            _volumeText = new TextView()
            {
                X = Pos.Right(ret) - (4 + 2),
                Y = 0,
                Width = 4,
                Height = 1,
                CanFocus = false,
                Text = "100%"
            };
            _timeElapsed = new TextView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = 1,
                CanFocus = false,
            };
            _playingTime = new ProgressBar()
            {
                X = 0,
                Y = 2,
                // minus x: x = length of _volumeText - 1
                Width = Dim.Fill() - (4 + 1),
                Height = 1,
                ProgressBarFormat = ProgressBarFormat.Simple,
                ProgressBarStyle = ProgressBarStyle.Blocks,
                CanFocus = false,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black),
                    Focus = Application.Driver.MakeAttribute(Color.White, Color.Black)
                }
            };
            _songDuration = new TextView()
            {
                X = Pos.Right(ret) - (5 + 2),
                Y = 1,
                Width = 5, // MM:SS
                Height = 1,
                CanFocus = false,
            };
            ret.Add(_nowPlaying);
            ret.Add(volumeLabel);
            ret.Add(_volumeSlider);
            ret.Add(_volumeText);
            ret.Add(_playingTime);
            ret.Add(_timeElapsed);
            ret.Add(_songDuration);
            return ret;
        }

        private void SearchView()
        {
            mainView!.RemoveAll();
            mainView.Title = "Search";
            TextView searchLabel = new TextView()
            {
                X = 0,
                Y = 0,
                Width = 11,
                Height = 1,
                Text = "[Search: ?]",
                CanFocus = false,
            };
            TextField searchField = SonicLairControls.GetTextField("");
            searchField.X = Pos.Right(searchLabel) + 1;
            searchField.Y = 0;
            searchField.Height = 1;
            searchField.Width = Dim.Fill();

            FrameView artistsContainer = new FrameView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(50),
                Height = Dim.Percent(50),
                Title = "Artists"
            };
            SonicLairListView<Artist> artistsList = new SonicLairListView<Artist>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            artistsList.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var artist = (Artist)e.Value;
                _ = ArtistView(artist.Id);
                _history.Push(() => { _ = ArtistView(artist.Id); });
            };
            artistsContainer.Add(artistsList);

            FrameView albumsContainer = new FrameView()
            {
                X = Pos.Right(artistsContainer),
                Y = 1,
                Width = Dim.Percent(50),
                Height = Dim.Percent(50),
                Title = "Albums",
            };
            SonicLairListView<Album> albumsList = new SonicLairListView<Album>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            albumsList.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var album = (Album)e.Value;
                _musicPlayerService!.PlayAlbum(album.Id, 0);
            };
            albumsContainer.Add(albumsList);

            FrameView songsContainer = new FrameView()
            {
                X = 0,
                Y = Pos.Bottom(artistsContainer),
                Width = Dim.Fill(),
                Height = Dim.Percent(50),
                Title = "Songs"
            };
            SonicLairListView<Song> songsList = new SonicLairListView<Song>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            songsList.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var song = (Song)e.Value;
                _musicPlayerService!.PlayRadio(song.Id);
            };
            songsContainer.Add(songsList);
            searchField.TextChanged += async (ustring value) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return;
                }
                var cancellationTokenSource = new CancellationTokenSource();
                SonicLairControls.AnimateTextView(searchLabel, new[]{
                    "[Search: /]",
                    "[Search: -]",
                    "[Search: \\]",
                    "[Search: |]",
                }, 800, cancellationTokenSource.Token);
                var ret = await _subsonicService!.Search(value.ToString(), 100);
                cancellationTokenSource.Cancel();
                searchLabel.Text = "[Search: ?]";
                Application.MainLoop.Invoke((Action)(() =>
                {
                    if (ret.Artists != null && ret.Artists.Any<Artist>())
                    {
                        artistsList.Source = new SonicLairDataSource<Artist>(ret.Artists, (s) =>
                        {
                            return s.Name;
                        });
                    }
                    if (ret.Albums != null && ret.Albums.Any<Album>())
                    {
                        var max = ret.Albums.Max(s => s.Name.Length);
                        albumsList.Source = new SonicLairDataSource<Album>(ret.Albums, (s) =>
                        {
                            return $"{s.Artist} :: {s.Name.RunePadRight(max, ' ')}";
                        });
                    }
                    if (ret.Songs != null && ret.Songs.Any<Song>())
                    {
                        var max = ret.Songs.Max(s => s.Title.Length);
                        songsList.Source = new SonicLairDataSource<Song>(ret.Songs, (s) =>
                        {
                            return $"{s.Artist} :: {s.Album} :: {s.Title.RunePadRight(max, ' ')}";
                        });
                    }
                    Application.Refresh();
                }));
            };
            mainView.Add(searchLabel,
            searchField,
            artistsContainer,
            albumsContainer,
            songsContainer);
            searchField.FocusFirst();
        }

        private async Task AlbumsView()
        {
            var albums = await _subsonicService!.GetAllAlbums();
            if (albums == null)
            {
                return;
            }
            mainView!.RemoveAll();
            mainView.Title = "Albums";
            SonicLairListView<Album> listView = new SonicLairListView<Album>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            listView.SetSource(albums);
            listView.OpenSelectedItem += AlbumView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task ArtistsView()
        {
            var artists = await _subsonicService!.GetArtists();
            if (artists == null)
            {
                return;
            }
            mainView!.RemoveAll();
            mainView.Title = "Artists";
            SonicLairListView<Artist> listView = new SonicLairListView<Artist>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var max = artists.Max(s => s.Name.Length);
            var maxAlbums = artists.Max(s => s.AlbumCount.ToString().Length);
            listView.Source = new SonicLairDataSource<Artist>(artists, (a) =>
            {
                var tag = a.AlbumCount > 1 ? "Albums" : "Album";
                return $"{a.ToString().RunePadRight(max, ' ')} {a.AlbumCount.ToString().RunePadLeft(maxAlbums, ' ')} {tag}";
            });
            listView.OpenSelectedItem += ArtistsView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task ArtistView(string id)
        {
            var artist = await _subsonicService!.GetArtist(id);
            mainView!.RemoveAll();
            mainView.Title = artist.Name;
            SonicLairListView<Album> listView = new SonicLairListView<Album>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            listView.Source = new SonicLairDataSource<Album>(artist.Album, (a) =>
            {
                return $"({a.Year:0000}) {a.Name}";
            });
            listView.OpenSelectedItem += AlbumView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task AlbumView(string id)
        {
            var album = await _subsonicService!.GetAlbum(id);
            mainView!.RemoveAll();
            mainView.Title = $"{album.Name} :: {album.Artist}";
            SonicLairListView<Song> listView = new SonicLairListView<Song>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var source = new SonicLairDataSource<Song>(album.Song, (s) =>
            {
                return $"{s.Track:00} - {s.Title} [{s.Duration.GetAsMMSS()}]";
            });
            listView.Source = source;
            listView.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var song = (Song)e.Value;
                _musicPlayerService!.PlayAlbum(album.Id, album.Song.IndexOf(song));
            };
            listView.RegisterHotKey(Key.M | Key.CtrlMask, () =>
            {
                _musicPlayerService!.AddToCurrentPlaylist(source.Items[listView.SelectedItem]);
            });
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task PlaylistView(string id)
        {
            var playlist = await _subsonicService!.GetPlaylist(id);
            mainView!.RemoveAll();
            mainView.Title = $"Playlist [{playlist.Name} :: {playlist.Owner}] -- Lasts {playlist.Duration.GetAsMMSS()}";
            SonicLairListView<Song> listView = new SonicLairListView<Song>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var maxTitle = playlist.Entry.Max(s => s.Title.StandardizedStringLength());
            var maxArtist = playlist.Entry.Max(s => s.Artist.StandardizedStringLength());
            var source = new SonicLairDataSource<Song>(playlist.Entry, (s) =>
            {
                return $"{s.Title.RunePadRight(maxTitle, ' ')} :: {s.Artist.RunePadRight(maxArtist, ' ')} [{s.Duration.GetAsMMSS()}]";
            });
            listView.Source = source;
            listView.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var song = (Song)e.Value;
                _musicPlayerService!.PlayPlaylist(playlist.Id, playlist.Entry.IndexOf(song));
            };
            listView.RegisterHotKey(Key.M | Key.CtrlMask, () =>
             {
                 _musicPlayerService!.AddToCurrentPlaylist(source.Items[listView.SelectedItem]);
             });
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task PlaylistsView()
        {
            var playlists = await _subsonicService!.GetPlaylists();
            mainView!.RemoveAll();
            mainView.Title = $"Playlists";
            SonicLairListView<Playlist> listView = new SonicLairListView<Playlist>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var maxName = playlists.Max(s => s.Name.Length);
            var maxOwner = playlists.Max(s => s.Owner.Length);
            listView.Source = new SonicLairDataSource<Playlist>(playlists, (p) =>
            {
                return $"{p.Name.RunePadRight(maxName + 1, ' ')} :: {p.Owner.RunePadRight(maxOwner + 1, ' ')} [lasts {p.Duration.GetAsMMSS()}]";
            });
            listView.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                _history.Push(() => { _ = PlaylistView(((Playlist)e.Value).Id); });

                _ = PlaylistView(((Playlist)e.Value).Id);
            };
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private void ArtistsView_Selected(ListViewItemEventArgs obj)
        {
            _history.Push(() => { _ = ArtistView(((Artist)obj.Value).Id); });

            _ = ArtistView(((Artist)obj.Value).Id);
        }

        private void AlbumView_Selected(ListViewItemEventArgs obj)
        {
            _history.Push(() => { _ = AlbumView(((Album)obj.Value).Id); });

            _ = AlbumView(((Album)obj.Value).Id);
        }

        private void CurrentStateHandler(object? sender, CurrentStateChangedEventArgs e)
        {
            _state = e.CurrentState;
            Application.MainLoop.Invoke(() =>
            {
                if (e.CurrentState.IsPlaying)
                {
                    audioControlView!.Title = "Now Playing";
                }
                else if (e.CurrentState.Stopped)
                {
                    audioControlView!.Title = "Stopped";
                }
                else
                {
                    audioControlView!.Title = "Paused";
                }
                if (e.CurrentState?.CurrentTrack != null)
                {
                    _nowPlaying!.Text = $"{e.CurrentState.CurrentTrack.Artist} :: {e.CurrentState.CurrentTrack.Album} :: {e.CurrentState.CurrentTrack.Title} ";
                    _songDuration!.Text = e.CurrentState.CurrentTrack.Duration.GetAsMMSS();
                }
                if (_nowPlayingList != null && _state?.CurrentPlaylist?.Entry != null && _state.CurrentPlaylist.Entry.Any())
                {
                    _nowPlayingList.Source = new SonicLairDataSource<Song>(_state.CurrentPlaylist.Entry, (s) =>
                    {
                        // Max 35
                        var currentId = _state.CurrentTrack?.Id ?? "-";
                        var max = s.Id == currentId ? 24 : 25;
                        string title;
                        if (s.Title.Length > max)
                        {
                            title = s.Title.Substring(0, max);
                        }
                        else
                        {
                            title = s.Title.RunePadRight(max, ' ');
                        }
                        return $"{(s.Id == currentId ? "*" : "")}{title}[{s.Duration.GetAsMMSS()}]";
                    });
                    _nowPlayingList.SelectedItem = _state.CurrentPlaylist.Entry.IndexOf(_state.CurrentTrack);
                    _nowPlayingList.ScrollTo(_state.CurrentPlaylist.Entry.IndexOf(_state.CurrentTrack));
                }
                // RepeatStatus
                switch (_state!.RepeatStatus)
                {
                    case RepeatStatus.RepeatAll:
                        _RepeatText!.Text = "Repeat: All";
                        break;
                    case RepeatStatus.RepeatOne:
                        _RepeatText!.Text = "Repeat: One";
                        break;
                    case RepeatStatus.None:
                        _RepeatText!.Text = "Repeat: Off";
                        break;
                    default:
                        break;
                }
                // Shuffle
                if (_state.IsShuffled)
                {
                    _shuffleText!.Text = " Shuffle: On";
                }
                else
                {
                    _shuffleText!.Text = "Shuffle: Off";
                }
                Application.Refresh();
            });
        }

        private void PlayingTimeHandler(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (_state != null && _state.CurrentTrack != null)
            {
                Application.MainLoop.Invoke(() =>
                {
                    _timeElapsed!.Text = ((int)e.Time / 1000).GetAsMMSS();
                    // if (_state.IsPlaying)
                    // {
                    //     _playingTime!.Fraction = (e.Time / 1000f) / (_state.CurrentTrack.Duration);
                    // }
                    // else
                    // {
                    // _playingTime!.Fraction = (float)Math.Ceiling((double)(e.Time / 10) / _state.CurrentTrack.Duration) / 100;
                    _playingTime!.Fraction = (float)Math.Round(((double)e.Time / 10) / _state.CurrentTrack.Duration) / 100;
                    // }
                    Application.Refresh();
                });
            }
        }

        private void PlayerVolumeHandler(object? sender, MediaPlayerVolumeChangedEventArgs e)
        {
            if (_volumeSlider != null)
            {
                _volumeSlider.Fraction = e.Volume;
            }
            if (_volumeText != null)
            {
                _volumeText.Text = $"{(int)(e.Volume * 100)}%";
            }
        }

        public void Load()
        {
            var account = Statics.GetActiveAccount();
            if (_subsonicService == null)
            {
                _subsonicService = new SubsonicService();
                _subsonicService.Configure(account);
            }
            if (_musicPlayerService == null)
            {
                _musicPlayerService = new MusicPlayerService(_subsonicService);
                _musicPlayerService.RegisterCurrentStateHandler(CurrentStateHandler);
                _musicPlayerService.RegisterTimeChangedHandler(PlayingTimeHandler);
                _musicPlayerService.RegisterPlayerVolumeHandler(PlayerVolumeHandler);
            }
            _top.RemoveAll();
            var win = new SonicLairWindow($"SonicLair | {account.Username} on {account.Url}")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            RegisterHotKeys(win);

            mainView = GetMainView();
            win.Add(mainView);
            audioControlView = GetAudioControlView(mainView);
            win.Add(audioControlView);
            var baseBar = GetBaseBar(audioControlView);
            win.Add(baseBar);
            var currentPlayingList = GetCurrentPlaylist(mainView);
            win.Add(currentPlayingList);
            _ = ArtistsView();
            _top.Add(win);
        }

        public void RegisterHotKeys(SonicLairWindow window)
        {
            window.RegisterHotKey(Key.Q | Key.CtrlMask, () =>
            {
                Application.RequestStop();
            });
            window.RegisterHotKey(Key.N | Key.CtrlMask, () =>
            {
                _musicPlayerService!.ToggleNext();
            });
            window.RegisterHotKey(Key.B | Key.CtrlMask, () =>
            {
                _musicPlayerService!.Prev();
            });
            window.RegisterKey(Key.Space, () =>
            {
                _musicPlayerService!.PlayPause();
            });
            window.RegisterHotKey(Key.A | Key.CtrlMask, () =>
            {
                _history.Push(() => { _ = ArtistsView(); });
                _ = ArtistsView();
            });
            window.RegisterHotKey(Key.L | Key.CtrlMask, () =>
            {
                _history.Push(() => { _ = AlbumsView(); });
                _ = AlbumsView();
            });
            window.RegisterHotKey(Key.P | Key.CtrlMask, () =>
            {
                _history.Push(() => { _ = PlaylistsView(); });
                _ = PlaylistsView();
            });
            window.RegisterHotKey(Key.R | Key.CtrlMask, () =>
            {
                _history.Push(() => { SearchView(); });
                SearchView();
            });
            window.RegisterHotKey(Key.T | Key.CtrlMask, () =>
            {
                _musicPlayerService!.ToggleRepeat();
            });
            window.RegisterHotKey(Key.H | Key.CtrlMask, () =>
            {
                _musicPlayerService!.Shuffle();
            });
            window.RegisterHotKey(Key.I | Key.CtrlMask, () =>
            {
                _musicPlayerService!.SetVolume(5, true);
            });
            window.RegisterHotKey(Key.K | Key.CtrlMask, () =>
            {
                _musicPlayerService!.SetVolume(-5, true);
            });
            window.RegisterKey(Key.Backspace, () =>
            {
                _history.GoBack();
            });
            window.RegisterHotKey(Key.CursorRight | Key.CtrlMask, () =>
            {
                if (_musicPlayerService!.GetCurrentState().IsPlaying)
                {
                    var newPosition = 10f / _musicPlayerService!.GetCurrentState().CurrentTrack.Duration;
                    _musicPlayerService.Seek(newPosition, true);
                }
            });
            window.RegisterHotKey(Key.CursorLeft | Key.CtrlMask, () =>
            {
                if (_musicPlayerService!.GetCurrentState().IsPlaying)
                {
                    var newPosition = 10f / _musicPlayerService!.GetCurrentState().CurrentTrack.Duration;
                    _musicPlayerService.Seek(-newPosition, true);

                }
            });
        }
    }
}