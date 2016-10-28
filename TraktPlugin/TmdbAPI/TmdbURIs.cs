
namespace TraktPlugin.TmdbAPI
{
    public static class TmdbURIs
    {
        private const string apiKey = "e636af47bb9604b7fe591847a98ca408";
        private const string apiUrl = "http://api.themoviedb.org/3/";

        public static string apiConfig = string.Concat(apiUrl, "configuration?api_key=", apiKey);
        public static string apiGetMovieImages = string.Concat(apiUrl, "movie/{0}/images?api_key=", apiKey);
        public static string apiGetShowImages = string.Concat(apiUrl, "tv/{0}/images?api_key=", apiKey);
        public static string apiGetSeasonImages = string.Concat(apiUrl, "tv/{0}/season/{1}/images?api_key=", apiKey);
        public static string apiGetEpisodeImages = string.Concat(apiUrl, "tv/{0}/season/{1}/episode/{2}/images?api_key=", apiKey);
        public static string apiGetPersonImages = string.Concat(apiUrl, "person/{0}/images?api_key=", apiKey);
    }
}
