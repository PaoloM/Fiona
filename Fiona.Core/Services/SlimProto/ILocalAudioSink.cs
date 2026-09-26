using System;

namespace Fiona.Core.Services.SlimProto
{
    /// <summary>
    /// Whatever actually makes noise, behind an interface so <see cref="SlimProtoClient"/> can
    /// stay free of any platform audio API and be tested without one.
    ///
    /// The server drives all of this: it decides what to load, when to start, and when to stop.
    /// The events going the other way are what it needs told about - notably <see cref="Ended"/>,
    /// which is how the server learns to send the next track.
    /// </summary>
    public interface ILocalAudioSink
    {
        /// <summary>Opens a stream and buffers it, without starting playback.</summary>
        void Load(Uri stream);

        void Play();

        void Pause();

        /// <summary>Stops and discards the current stream.</summary>
        void Stop();

        /// <summary>Linear amplitude, 0.0 to 1.0.</summary>
        void SetVolume(double level);

        /// <summary>The server's aude command: false silences output entirely.</summary>
        void SetOutputEnabled(bool enabled);

        /// <summary>How far into the current track we are, for the server's progress bar.</summary>
        TimeSpan Position { get; }

        /// <summary>0.0 to 1.0. Reported to the server as buffer fullness.</summary>
        double BufferFill { get; }

        bool IsPlaying { get; }

        /// <summary>Actually producing sound. Becomes STMs, which is what moves the server's
        /// "now playing" on to this track.</summary>
        event EventHandler Started;

        /// <summary>The stream ran out. Becomes STMd then STMu, which is what asks the server
        /// for the next track.</summary>
        event EventHandler Ended;

        /// <summary>Could not be opened or decoded. Becomes STMn, so the server gives up on
        /// this track rather than waiting on us.</summary>
        event EventHandler Failed;
    }
}
