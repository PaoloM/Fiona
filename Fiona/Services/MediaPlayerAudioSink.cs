using System;
using System.Threading;

using Fiona.Core.Services.SlimProto;

using Windows.Media.Core;
using Windows.Media.Playback;

namespace Fiona.Services
{
    /// <summary>
    /// Plays the server's audio with a UWP <see cref="MediaPlayer"/>, which does the HTTP fetch and
    /// the decoding for us.
    ///
    /// The trade that buys: we never see the bytes, so the buffer figures reported back to the
    /// server are estimates and the gap between tracks is however long Windows takes to open the
    /// next stream. Elapsed position, which is the number the server actually shows people, comes
    /// from the playback session and is accurate.
    ///
    /// Everything the protocol asks for is cached from the pipeline's own callbacks rather than read
    /// when asked. That is not an optimisation. The protocol thread asks every second, the pipeline
    /// tears down and rebuilds its session as the source changes, and reading the session in the
    /// middle of that throws E_NOINTERFACE - skipping tracks quickly is enough to hit it. So the
    /// media objects are touched only from their own events, and the protocol thread reads fields.
    ///
    /// Playback continues while the app is in the background because of the backgroundMediaPlayback
    /// capability in the manifest; it stops when the app is closed, which is the shape of the
    /// feature rather than a bug to fix here.
    /// </summary>
    public class MediaPlayerAudioSink : ILocalAudioSink, IDisposable
    {
        private readonly MediaPlayer _player;

        // The server sets volume and output enable independently of each other, and either can
        // arrive while nothing is loaded, so both are held here and applied together.
        private double _volume = 1.0;
        private bool _outputEnabled = true;

        // Playback reaches Playing again after every unpause, but a track only starts once. Without
        // this the server is told the track restarted each time it is resumed, and moves its "now
        // playing" back to the top of the track.
        private bool _reportedStarted;

        // Written by the pipeline's callbacks, read by the protocol thread. Ticks rather than a
        // TimeSpan and whole percent rather than a double, because these are read without a lock
        // and those are the widths that cannot tear on the 32-bit build.
        private long _positionTicks;
        private int _bufferPercent;
        private volatile bool _isPlaying;

        public MediaPlayerAudioSink()
        {
            _player = new MediaPlayer
            {
                AudioCategory = MediaPlayerAudioCategory.Media,

                // The server decides when to start; automatic playback on open would race it.
                AutoPlay = false
            };

            // Windows' own transport controls would let the media keys drive this player directly,
            // behind the server's back, leaving the two disagreeing about what is playing. Until
            // the keys are wired through to the server they stay out of it.
            _player.CommandManager.IsEnabled = false;

            _player.MediaEnded += OnMediaEnded;
            _player.MediaFailed += OnMediaFailed;

            MediaPlaybackSession session = _player.PlaybackSession;
            session.PlaybackStateChanged += OnPlaybackStateChanged;
            session.PositionChanged += OnPositionChanged;
            session.BufferingProgressChanged += OnBufferingProgressChanged;
        }

        public event EventHandler Started;
        public event EventHandler Ended;
        public event EventHandler Failed;

        public TimeSpan Position
        {
            get { return TimeSpan.FromTicks(Interlocked.Read(ref _positionTicks)); }
        }

        public double BufferFill
        {
            get
            {
                // Buffering progress only moves while a buffering pass is in flight; once playing
                // steadily it sits at zero, which would tell the server we had run dry.
                if (_isPlaying) return 1.0;

                return _bufferPercent / 100.0;
            }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        public void Load(Uri stream)
        {
            _reportedStarted = false;
            ResetReportedState();

            _player.Source = MediaSource.CreateFromUri(stream);
        }

        public void Play()
        {
            ApplyVolume();
            _player.Play();
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Stop()
        {
            _player.Pause();

            // Dropping the source releases the HTTP connection. Without it the server sees a
            // player still holding a stream it believes it has stopped.
            _player.Source = null;

            _reportedStarted = false;
            ResetReportedState();
        }

        public void SetVolume(double level)
        {
            _volume = Math.Min(1.0, Math.Max(0.0, level));
            ApplyVolume();
        }

        public void SetOutputEnabled(bool enabled)
        {
            _outputEnabled = enabled;
            ApplyVolume();
        }

        /// <summary>
        /// Nothing is loaded, so nothing is playing and there is no position to report. Left alone,
        /// the previous track's elapsed time would go to the server as though it were this one's.
        /// </summary>
        private void ResetReportedState()
        {
            Interlocked.Exchange(ref _positionTicks, 0);
            _bufferPercent = 0;
            _isPlaying = false;
        }

        private void ApplyVolume()
        {
            _player.Volume = _outputEnabled ? _volume : 0.0;
        }

        private void OnPositionChanged(MediaPlaybackSession sender, object args)
        {
            // Read on the pipeline's own callback, where the session is valid, rather than whenever
            // the protocol thread happens to ask for it.
            Interlocked.Exchange(ref _positionTicks, sender.Position.Ticks);
        }

        private void OnBufferingProgressChanged(MediaPlaybackSession sender, object args)
        {
            double progress = sender.BufferingProgress;
            _bufferPercent = (int)(Math.Min(1.0, Math.Max(0.0, progress)) * 100);
        }

        private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            MediaPlaybackState state = sender.PlaybackState;
            _isPlaying = state == MediaPlaybackState.Playing;

            // Playing is the first moment sound is actually coming out, which is what the server
            // means by a track having started - but only the first time for a given stream.
            if (!_isPlaying || _reportedStarted) return;

            _reportedStarted = true;
            Raise(Started);
        }

        private void OnMediaEnded(MediaPlayer sender, object args)
        {
            Raise(Ended);
        }

        private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Raise(Failed);
        }

        private void Raise(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _player.MediaEnded -= OnMediaEnded;
            _player.MediaFailed -= OnMediaFailed;

            MediaPlaybackSession session = _player.PlaybackSession;
            session.PlaybackStateChanged -= OnPlaybackStateChanged;
            session.PositionChanged -= OnPositionChanged;
            session.BufferingProgressChanged -= OnBufferingProgressChanged;

            _player.Dispose();
        }
    }
}
