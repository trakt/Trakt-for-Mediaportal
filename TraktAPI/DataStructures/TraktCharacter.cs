using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktCharacter
    {
        [DataMember(Name = "character")]
        public string Character { get; set; }

        [DataMember(Name = "person")]
        public TraktPersonSummary Person { get; set; }
    }
}
