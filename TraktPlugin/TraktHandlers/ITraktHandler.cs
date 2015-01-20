using System;

namespace TraktPlugin.TraktHandlers
{
    /// <summary>
    /// Handles Trakt Functions for a particular library
    /// </summary>
    interface ITraktHandler
    {
        /// <summary>
        /// The Name of the Library it supports
        /// </summary>
        string Name { get;}

        /// <summary>
        /// The Priority of this Library being scrobbled
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// Syncs the Plugins library to Trakt
        /// </summary>
        void SyncLibrary();

        /// <summary>
        /// Syncs playback progress of partially watched items
        /// </summary>
        void SyncProgress();

        /// <summary>
        /// Scrobbles to Trakt the given filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        bool Scrobble(String filename);

        /// <summary>
        /// Stops any existing scrobbling
        /// </summary>
        void StopScrobble();
    }
}
