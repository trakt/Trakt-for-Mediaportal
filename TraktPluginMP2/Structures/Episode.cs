using System.Runtime.Serialization;

namespace TraktPluginMP2.Structures
{
  [DataContract]
  public class Episode
  {
    [DataMember]
    public int? ShowId { get; set; }

    [DataMember]
    public int? ShowTvdbId { get; set; }

    [DataMember]
    public string ShowImdbId { get; set; }

    [DataMember]
    public string ShowTitle { get; set; }

    [DataMember]
    public int? ShowYear { get; set; }

    [DataMember]
    public int Season { get; set; }

    [DataMember]
    public int Number { get; set; }
  }
}