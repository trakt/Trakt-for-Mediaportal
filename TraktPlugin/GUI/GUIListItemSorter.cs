using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;

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
    public class GUIListItemMovieSorter : IComparer<TraktMovieTrending>, IComparer<TraktMovieSummary>, IComparer<TraktMovieWatchList>
    {
        private SortingFields _sortField;
        private SortingDirections _sortDirection;

        public GUIListItemMovieSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            _sortField = sortField;
            _sortDirection = sortDirection;
        }

        public int Compare(TraktMovieSummary movieX, TraktMovieSummary movieY)
        {
            try
            {
                int rtn;

                switch (_sortField)
                {
                    case SortingFields.ReleaseDate:
                        rtn = movieX.Released.FromISO8601().CompareTo(movieY.Released.FromISO8601());
                        break;

                    case SortingFields.Score:
                        rtn = movieX.Rating.GetValueOrDefault(0).CompareTo(movieY.Rating.GetValueOrDefault(0));
                        if (rtn == 0)
                        {
                            // if same score compare votes
                            //TODOrtn = movieX.Ratings.Votes.CompareTo(movieY.Ratings.Votes);
                        }
                        break;

                    case SortingFields.Votes:
                        rtn = 0;
                        //TODOrtn = movieX.Ratings.Votes.CompareTo(movieY.Ratings.Votes);
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

                if (_sortDirection == SortingDirections.Descending)
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
            if (_sortField == SortingFields.PeopleWatching)
            {
                int rtn = movieX.Watchers.CompareTo(movieY.Watchers);
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX as TraktMovieSummary, movieY as TraktMovieSummary);
        }

        public int Compare(TraktMovieWatchList movieX, TraktMovieWatchList movieY)
        {
            if (_sortField == SortingFields.WatchListInserted)
            {
                int rtn = movieX.ListedAt.FromISO8601().CompareTo(movieY.ListedAt.FromISO8601());
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX.Movie as TraktMovieSummary, movieY.Movie as TraktMovieSummary);
        }

        #region IComparer<TraktWatchListMovie> Members

        int IComparer<TraktMovieWatchList>.Compare(TraktMovieWatchList x, TraktMovieWatchList y)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    #endregion

    #region Show Sorter
    public class GUIListItemShowSorter : IComparer<TraktShowTrending>, IComparer<TraktShowSummary>, IComparer<TraktShowWatchList>
    {
        private SortingFields _sortField;
        private SortingDirections _sortDirection;

        public GUIListItemShowSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            _sortField = sortField;
            _sortDirection = sortDirection;
        }

        public int Compare(TraktShowSummary showX, TraktShowSummary showY)
        {
            try
            {
                int rtn;

                switch (_sortField)
                {
                    case SortingFields.ReleaseDate:
                        rtn = showX.FirstAired.FromISO8601().CompareTo(showY.FirstAired.FromISO8601());
                        break;

                    case SortingFields.Score:
                        rtn = showX.Rating.GetValueOrDefault(0).CompareTo(showY.Rating.GetValueOrDefault(0));
                        if (rtn == 0)
                        {
                            // if same score compare votes
                            //TODOrtn = showX.Ratings.Votes.CompareTo(showY.Ratings.Votes);
                        }
                        break;

                    case SortingFields.Votes:
                        rtn = 0;
                        //TODOrtn = showX.Ratings.Votes.CompareTo(showY.Ratings.Votes);
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

                if (_sortDirection == SortingDirections.Descending)
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
            if (_sortField == SortingFields.PeopleWatching)
            {
                int rtn = showX.Watchers.CompareTo(showY.Watchers);
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }

        public int Compare(TraktShowWatchList showX, TraktShowWatchList showY)
        {
            if (_sortField == SortingFields.WatchListInserted)
            {
                int rtn = showX.ListedAt.FromISO8601().CompareTo(showY.ListedAt.FromISO8601());
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX.Show as TraktShowSummary, showY.Show as TraktShowSummary);
        }
    }
    #endregion
}
