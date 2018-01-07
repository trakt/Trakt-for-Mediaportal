using System.Runtime.Serialization;

namespace TraktPluginMP2.Structures
{
  [DataContract]
  public class ListActivity
  {
    [DataMember]
    public int? Id { get; set; }

    [DataMember]
    public string UpdatedAt { get; set; }
  }
}