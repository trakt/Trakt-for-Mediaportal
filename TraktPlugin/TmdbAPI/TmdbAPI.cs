using System.IO;
using System.Net;
using TraktPlugin.TmdbAPI.DataStructures;
using TraktPlugin.TmdbAPI.Extensions;

namespace TraktPlugin.TmdbAPI
{
    public static class TmdbAPI
    {
        #region Web Events

        // these events can be used to log data sent / received from tmdb
        public delegate void OnDataSendDelegate(string url, string postData);
        public delegate void OnDataReceivedDelegate(string response, HttpWebResponse webResponse);
        public delegate void OnDataErrorDelegate(string error);

        public static event OnDataSendDelegate OnDataSend;
        public static event OnDataReceivedDelegate OnDataReceived;
        public static event OnDataErrorDelegate OnDataError;

        #endregion

        #region Settings

        // these settings should be set before sending data to tmdb        
        public static string UserAgent { get; set; }

        #endregion

        public static TmdbConfiguration GetConfiguration()
        {
            string response = GetFromTmdb(TmdbURIs.apiConfig);
            return response.FromJSON<TmdbConfiguration>();
        }

        public static TmdbMovieImages GetMovieImages(string id)
        {
            string response = GetFromTmdb(string.Format(TmdbURIs.apiGetMovieImages, id));
            return response.FromJSON<TmdbMovieImages>();
        }

        public static TmdbShowImages GetShowImages(string id)
        {
            string response = GetFromTmdb(string.Format(TmdbURIs.apiGetShowImages, id));
            return response.FromJSON<TmdbShowImages>();
        }

        public static TmdbSeasonImages GetShowImages(string id, int season)
        {
            string response = GetFromTmdb(string.Format(TmdbURIs.apiGetSeasonImages, id, season));
            return response.FromJSON<TmdbSeasonImages>();
        }

        public static TmdbSeasonImages GetSeasonImages(string id, int season)
        {
            string response = GetFromTmdb(string.Format(TmdbURIs.apiGetSeasonImages, id, season));
            return response.FromJSON<TmdbSeasonImages>();
        }

        public static TmdbEpisodeImages GetEpisodeImages(string id, int season, int episode)
        {
            string response = GetFromTmdb(string.Format(TmdbURIs.apiGetEpisodeImages, id, season, episode));
            return response.FromJSON<TmdbEpisodeImages>();
        }

        public static TmdbPeopleImages GetPeopleImages(string id)
        {
            string response = GetFromTmdb(string.Format(TmdbURIs.apiGetPersonImages, id));
            return response.FromJSON<TmdbPeopleImages>();
        }

        static string GetFromTmdb(string address)
        {
            if (OnDataSend != null)
                OnDataSend(address, null);

            var request = WebRequest.Create(address) as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = "GET";
            request.ContentLength = 0;
            request.Timeout = 120000;
            request.ContentType = "application/json";
            request.UserAgent = UserAgent;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream stream = response.GetResponseStream();

                StreamReader reader = new StreamReader(stream);
                string strResponse = reader.ReadToEnd();

                if (OnDataReceived != null)
                    OnDataReceived(strResponse, response);

                stream.Close();
                reader.Close();
                response.Close();

                return strResponse;
            }
            catch (WebException wex)
            {
                string errorMessage = wex.Message;
                if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = wex.Response as HttpWebResponse;

                    string headers = string.Empty;
                    foreach (string key in response.Headers.AllKeys)
                    {
                        headers += string.Format("{0}: {1}, ", key, response.Headers[key]);
                    }
                    errorMessage = string.Format("Protocol Error, Code = '{0}', Description = '{1}', Url = '{2}', Headers = '{3}'", (int)response.StatusCode, response.StatusDescription, address, headers.TrimEnd(new char[] { ',', ' ' }));
                }

                if (OnDataError != null)
                    OnDataError(errorMessage);

                return null;
            }
            catch (IOException ioe)
            {
                string errorMessage = string.Format("Request failed due to an IO error, Description = '{0}', Url = '{1}', Method = 'GET'", ioe.Message, address);

                if (OnDataError != null)
                    OnDataError(ioe.Message);

                return null;
            }
        }
    }   
}
