using System.Runtime.Serialization;

namespace TraktPluginMP2.Structures
{
  [DataContract]
  public class EpisodeWatched : Episode
  {
    [DataMember]
    public int Plays { get; set; }

    [DataMember]
    public string WatchedAt { get; set; }
  }
}