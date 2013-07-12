using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPerson
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "images")]
        public PersonImages Images { get; set; }

        [DataContract]
        public class PersonImages
        {
            [DataMember(Name = "headshot")]
            public string Headshot { get; set; }
        }
    }
}
