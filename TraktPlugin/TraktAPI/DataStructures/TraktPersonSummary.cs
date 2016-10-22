 using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPersonSummary : TraktPerson
    {
        [DataMember(Name = "biography")]
        public string Biography { get; set; }

        [DataMember(Name = "birthday")]
        public string Birthday { get; set; }

        [DataMember(Name = "death")]
        public string Death { get; set; }

        [DataMember(Name = "birthplace")]
        public string Birthplace { get; set; }

        [DataMember(Name = "homepage")]
        public string Homepage { get; set; }
    }
}
