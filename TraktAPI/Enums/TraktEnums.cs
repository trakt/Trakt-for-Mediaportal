﻿using System;

namespace TraktAPI.Enums
{
    /// <summary>
    /// Trakt Connection States
    /// </summary>
    public enum ConnectionState
    {
        Connected,
        Connecting,
        Disconnected,
        Invalid,
        UnAuthorised,
        Pending
    }

    /// <summary>
    /// Media Types for syncing
    /// </summary>
    public enum TraktMediaType
    {
        digital,
        bluray,
        hddvd,
        dvd,
        vcd,
        vhs,
        betamax,
        laserdisc
    }

    /// <summary>
    /// Video resolution for syncing
    /// </summary>
    public enum TraktResolution
    {
        uhd_4k,
        hd_1080p,
        hd_1080i,
        hd_720p,
        sd_576p,
        sd_576i,
        sd_480p,
        sd_480i
    }

    /// <summary>
    /// Audio types for syncing
    /// </summary>
    public enum TraktAudio
    {
        lpcm,
        mp3,
        aac,
        dts,
        dts_ma,
        dts_x,
        flac,
        ogg,
        wma,
        dolby_prologic,
        dolby_digital,
        dolby_digital_plus,
        dolby_truehd,
        dolby_atmos
    }

    /// <summary>
    /// List of Rate Values
    /// </summary>
    public enum TraktRateValue
    {
        unrate,
        one,
        two,
        three,
        four,
        five,
        six,
        seven,
        eight,
        nine,
        ten
    }

    /// <summary>
    /// List of Item Types
    /// </summary>
    public enum TraktItemType
    {
        episode,
        season,
        show,
        movie,
        person
    }
    
    /// <summary>
    /// All possible search types
    /// </summary>
    [Flags]
    public enum SearchType
    {
        none = 0,
        movie = 1,
        show = 2,
        episode = 4,
        person = 8,
        user = 16,
        list = 32
    }

    /// <summary>
    /// Extended info parameter used on GET requests
    /// </summary>
    public enum ExtendedInfo
    {
        min,
        images,
        full
    }
}
