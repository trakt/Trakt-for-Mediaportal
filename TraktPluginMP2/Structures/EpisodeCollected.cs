using System;
using System.Runtime.Serialization;

namespace TraktPluginMP2.Structures
{
  [DataContract]
  public class EpisodeCollected : Episode
  {
    [DataMember]
    public DateTime? CollectedAt { get; set; }
  }
}