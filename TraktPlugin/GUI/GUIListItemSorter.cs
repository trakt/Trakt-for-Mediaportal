using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using MediaPortal.GUI.Library;
using TraktPlugin.TraktAPI.DataStructures;

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
    public class GUIListItemMovieSorter : IComparer<TraktTrendingMovie>, IComparer<TraktMovie>, IComparer<TraktWatchListMovie>
    {
        private SortingFields _sortField;
        private SortingDirections _sortDirection;

        public GUIListItemMovieSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            _sortField = sortField;
            _sortDirection = sortDirection;
        }

        public int Compare(TraktMovie movieX, TraktMovie movieY)
        {
            try
            {
                int rtn;

                switch (_sortField)
                {
                    case SortingFields.ReleaseDate:
                        rtn = movieX.Released.CompareTo(movieY.Released);
                        break;

                    case SortingFields.Score:
                        rtn = movieX.Ratings.Percentage.CompareTo(movieY.Ratings.Percentage);
                        if (rtn == 0)
                        {
                            // if same score compare votes
                            rtn = movieX.Ratings.Votes.CompareTo(movieY.Ratings.Votes);
                        }
                        break;

                    case SortingFields.Votes:
                        rtn = movieX.Ratings.Votes.CompareTo(movieY.Ratings.Votes);
                        break;

                    case SortingFields.Runtime:
                        rtn = movieX.Runtime.CompareTo(movieY.Runtime);
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

        public int Compare(TraktTrendingMovie movieX, TraktTrendingMovie movieY)
        {
            if (_sortField == SortingFields.PeopleWatching)
            {
                int rtn = movieX.Watchers.CompareTo(movieY.Watchers);
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX as TraktMovie, movieY as TraktMovie);
        }

        public int Compare(TraktWatchListMovie movieX, TraktWatchListMovie movieY)
        {
            if (_sortField == SortingFields.WatchListInserted)
            {
                int rtn = movieX.Inserted.CompareTo(movieY.Inserted);
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(movieX as TraktMovie, movieY as TraktMovie);
        }
    }
    #endregion

    #region Show Sorter
    public class GUIListItemShowSorter : IComparer<TraktTrendingShow>, IComparer<TraktShow>, IComparer<TraktWatchListShow>
    {
        private SortingFields _sortField;
        private SortingDirections _sortDirection;

        public GUIListItemShowSorter(SortingFields sortField, SortingDirections sortDirection)
        {
            _sortField = sortField;
            _sortDirection = sortDirection;
        }

        public int Compare(TraktShow showX, TraktShow showY)
        {
            try
            {
                int rtn;

                switch (_sortField)
                {
                    case SortingFields.ReleaseDate:
                        rtn = showX.FirstAired.CompareTo(showY.FirstAired);
                        break;

                    case SortingFields.Score:
                        rtn = showX.Ratings.Percentage.CompareTo(showY.Ratings.Percentage);
                        if (rtn == 0)
                        {
                            // if same score compare votes
                            rtn = showX.Ratings.Votes.CompareTo(showY.Ratings.Votes);
                        }
                        break;

                    case SortingFields.Votes:
                        rtn = showX.Ratings.Votes.CompareTo(showY.Ratings.Votes);
                        break;

                    case SortingFields.Runtime:
                        rtn = showX.Runtime.CompareTo(showY.Runtime);
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

        public int Compare(TraktTrendingShow showX, TraktTrendingShow showY)
        {
            if (_sortField == SortingFields.PeopleWatching)
            {
                int rtn = showX.Watchers.CompareTo(showY.Watchers);
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX as TraktShow, showY as TraktShow);
        }

        public int Compare(TraktWatchListShow showX, TraktWatchListShow showY)
        {
            if (_sortField == SortingFields.WatchListInserted)
            {
                int rtn = showX.Inserted.CompareTo(showY.Inserted);
                if (_sortDirection == SortingDirections.Descending)
                    rtn = -rtn;

                return rtn;
            }

            return Compare(showX as TraktShow, showY as TraktShow);
        }
    }
    #endregion
}
