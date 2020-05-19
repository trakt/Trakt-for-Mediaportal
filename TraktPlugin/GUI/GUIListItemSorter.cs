using System.Collections.Generic;
using System.Runtime.Serialization;
using TraktAPI.DataStructures;
using TraktAPI.Extensions;

namespace TraktPlugin.GUI
{
    #region Settings Data Structure
    [DataContract]
    public class SortBy
    {
        [DataMember]
        public SortingFields Field { get; set; }

        [DataMember]
        public SortingDirections Direction { get; set; }
    }
    #endregion

    #region Movie Sorter
    public class GUIListItemMovieSorter : IComparer<TraktMovieTrending>, IComparer<TraktMovieSummary>, IComparer<TraktMovieWatchList>, IComparer<TraktPersonMovieCast>, IComparer<TraktPersonMovieJob>, IComparer<TraktMovieAnticipated>
    {
        private readonly SortingFields mSortField;
        private readonly SortingDirections mSortDirection;

        public GUIListItemMovieSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            mSortField = sortField;
            mSortDirection = sortDirection;
        }

        public int Compare(TraktMovieSummary movieX, TraktMovieSummary movieY)
        {
            try
            {
                int rtn;

                switch (mSortField)
                {
                    case SortingFields.ReleaseDate:
                        rtn = movieX.Released.FromISO8601().CompareTo(movieY.Released.FromISO8601());
                        break;

                    case SortingFields.Score:
                        rtn = movieX.Rating.GetValueOrDefault(0).CompareTo(movieY.Rating.GetValueOrDefault(0));
                        if (rtn == 0)
                        {
                            // if same score compare votes
                            rtn = movieX.Votes.CompareTo(movieY.Votes);
                        }
                        break;

                    case SortingFields.Votes:
                        rtn = 0;
                        rtn = movieX.Votes.CompareTo(movieY.Votes);
                        break;

                    case SortingFields.Popularity:
                        double popX = movieX.Votes * movieX.Rating.GetValueOrDefault(0);
                        double popY = movieY.Votes * movieY.Rating.GetValueOrDefault(0);
                        rtn = popX.CompareTo(popY);
                        break;

                    case SortingFields.Runtime:
                        rtn = movieX.Runtime.GetValueOrDefault(0).CompareTo(movieY.Runtime.GetValueOrDefault(0));
                        break;

                    // default to the title field
                    case SortingFields.Title:
                    default:
                        rtn = movieX.Title.CompareTo(movieY.Title);
                        break;
                }

                // if both items are identical, fallback to using the Title
                if (rtn == 0)
                    rtn = movieX.Title.CompareTo(movieY.Title);

                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }
            catch
            {
                return 0;
            }
        }

        public int Compare(TraktMovieTrending movieX, TraktMovieTrending movieY)
        {
            if (mSortField == SortingFields.PeopleWatching)
            {
                int rtn = movieX.Watchers.CompareTo(movieY.Watchers);
                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX.Movie, movieY.Movie);
        }

        public int Compare(TraktMovieWatchList movieX, TraktMovieWatchList movieY)
        {
            if (mSortField == SortingFields.WatchListInserted)
            {
                int rtn = movieX.ListedAt.FromISO8601().CompareTo(movieY.ListedAt.FromISO8601());
                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX.Movie as TraktMovieSummary, movieY.Movie as TraktMovieSummary);
        }

        public int Compare(TraktMovieAnticipated movieX, TraktMovieAnticipated movieY)
        {
            if (mSortField == SortingFields.Anticipated)
            {
                int rtn = movieX.ListCount.CompareTo(movieY.ListCount);
                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX.Movie, movieY.Movie);
        }

        public int Compare(TraktPersonMovieJob movieX, TraktPersonMovieJob movieY)
        {
            return Compare(movieX.Movie as TraktMovieSummary, movieY.Movie as TraktMovieSummary);
        }

        public int Compare(TraktPersonMovieCast movieX, TraktPersonMovieCast movieY)
        {
            return Compare(movieX.Movie as TraktMovieSummary, movieY.Movie as TraktMovieSummary);
        }
    }
    #endregion

    #region Show Sorter
    public class GUIListItemShowSorter : IComparer<TraktShowTrending>, IComparer<TraktShowSummary>, IComparer<TraktShowWatchList>, IComparer<TraktPersonShowCast>, IComparer<TraktPersonShowJob>, IComparer<TraktShowAnticipated>
    {
        private readonly SortingFields mSortField;
        private readonly SortingDirections mSortDirection;

        public GUIListItemShowSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            mSortField = sortField;
            mSortDirection = sortDirection;
        }

        public int Compare(TraktShowSummary showX, TraktShowSummary showY)
        {
            try
            {
                int rtn;

                switch (mSortField)
                {
                    case SortingFields.ReleaseDate:
                        rtn = showX.FirstAired.FromISO8601().CompareTo(showY.FirstAired.FromISO8601());
                        break;

                    case SortingFields.Score:
                        rtn = showX.Rating.GetValueOrDefault(0).CompareTo(showY.Rating.GetValueOrDefault(0));
                        if (rtn == 0)
                        {
                            // if same score compare votes
                            rtn = showX.Votes.CompareTo(showY.Votes);
                        }
                        break;

                    case SortingFields.Votes:
                        rtn = 0;
                        rtn = showX.Votes.CompareTo(showY.Votes);
                        break;

                    case SortingFields.Popularity:
                        double popX = showX.Votes * showX.Rating.GetValueOrDefault(0);
                        double popY = showY.Votes * showY.Rating.GetValueOrDefault(0);
                        rtn = popX.CompareTo(popY);
                        break;

                    case SortingFields.Runtime:
                        rtn = showX.Runtime.GetValueOrDefault(0).CompareTo(showY.Runtime.GetValueOrDefault(0));
                        break;

                    // default to the title field
                    case SortingFields.Title:
                    default:
                        rtn = showX.Title.CompareTo(showY.Title);
                        break;
                }

                // if both items are identical, fallback to using the Title
                if (rtn == 0)
                    rtn = showX.Title.CompareTo(showY.Title);

                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }
            catch
            {
                return 0;
            }
        }

        public int Compare(TraktShowTrending showX, TraktShowTrending showY)
        {
            if (mSortField == SortingFields.PeopleWatching)
            {
                int rtn = showX.Watchers.CompareTo(showY.Watchers);
                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }

        public int Compare(TraktShowWatchList showX, TraktShowWatchList showY)
        {
            if (mSortField == SortingFields.WatchListInserted)
            {
                int rtn = showX.ListedAt.FromISO8601().CompareTo(showY.ListedAt.FromISO8601());
                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }

        public int Compare(TraktShowAnticipated showX, TraktShowAnticipated showY)
        {
            if (mSortField == SortingFields.Anticipated)
            {
                int rtn = showX.ListCount.CompareTo(showY.ListCount);
                if (mSortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }

        public int Compare(TraktPersonShowCast showX, TraktPersonShowCast showY)
        {
            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }

        public int Compare(TraktPersonShowJob showX, TraktPersonShowJob showY)
        {
            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }
    }
    #endregion

    #region List Item Sorter
    public class GUIListItemSorter : IComparer<TraktListItem>
    {
        private readonly SortingFields mSortField;
        private readonly SortingDirections mSortDirection;

        public GUIListItemSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            mSortField = sortField;
            mSortDirection = sortDirection;
        }

        public int Compare(TraktListItem aItemX, TraktListItem aItemY)
        {
            try
            {
                int lReturn;

                switch (mSortField)
                {
                    case SortingFields.ReleaseDate:
                        lReturn = aItemX.Released().FromISO8601().CompareTo(aItemY.Released().FromISO8601());
                        break;

                    case SortingFields.Score:
                        lReturn = aItemX.Score().GetValueOrDefault(0).CompareTo(aItemY.Score().GetValueOrDefault(0));
                        if (lReturn == 0)
                        {
                            // if same score compare votes
                            lReturn = aItemX.Votes().CompareTo(aItemY.Votes());
                        }
                        break;

                    case SortingFields.Votes:
                        lReturn = 0;
                        lReturn = aItemX.Votes().CompareTo(aItemY.Votes());
                        break;

                    case SortingFields.Popularity:
                        double lPopX = aItemX.Votes() * aItemX.Score().GetValueOrDefault(0);
                        double lPopY = aItemY.Votes() * aItemY.Score().GetValueOrDefault(0);
                        lReturn = lPopX.CompareTo(lPopY);
                        break;

                    case SortingFields.Runtime:
                        lReturn = aItemX.Runtime().GetValueOrDefault(0).CompareTo(aItemY.Runtime().GetValueOrDefault(0));
                        break;

                    case SortingFields.Rank:
                        lReturn = aItemX.Rank.CompareTo(aItemY.Rank);
                        break;

                    case SortingFields.Added:
                        lReturn = aItemX.Rank.CompareTo(aItemY.ListedAt);
                        break;

                    // default to the title field
                    case SortingFields.Title:
                    default:
                        lReturn = aItemX.Title().CompareTo(aItemY.Title());
                        break;
                }

                // if both items are identical, fallback to using the Title
                if (lReturn == 0)
                    lReturn = aItemX.Title().CompareTo(aItemY.Title());

                if (mSortDirection == SortingDirections.Descending)
                    lReturn = -lReturn;

                return lReturn;
            }
            catch
            {
                return 0;
            }
        }
    }

    static class ListItemSortExtensions
    {
        internal static double? Score(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Rating;
            if (item.Type == "show" && item.Show != null)
                return item.Show.Rating;
            if (item.Type == "season" && item.Season != null)
                return item.Season.Rating;
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.Rating;

            return 0;
        }

        internal static int Votes(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Votes;
            if (item.Type == "show" && item.Show != null)
                return item.Show.Votes;
            if (item.Type == "season" && item.Season != null)
                return item.Season.Votes;
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.Votes;

            return 0;
        }

        internal static string Released(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Released;
            if (item.Type == "show" && item.Show != null)
                return item.Show.FirstAired;
            if (item.Type == "season" && item.Season != null)
                return item.Season.FirstAired;
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.FirstAired;

            return string.Empty;
        }

        internal static string Title(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Title;
            if (item.Type == "show" && item.Show != null)
                return item.Show.Title;
            if (item.Type == "season" && item.Season != null)
                return item.Season.Title;
            else if (item.Type == "episode" && item.Episode != null)
                return item.Episode.Title;

            return string.Empty;
        }

        internal static int? Runtime(this TraktListItem item)
        {
            if (item.Type == "movie" && item.Movie != null)
                return item.Movie.Runtime;
            if ((item.Type == "show" || item.Type == "season" || item.Type == "episode" ) && item.Show != null)
                return item.Show.Runtime;            

            return 0;
        }
    }

    #endregion
}
