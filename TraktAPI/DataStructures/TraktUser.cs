using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUser
    {
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "name")]
        public string FullName { get; set; }

        [DataMember(Name = "vip")]
        public bool IsVip { get; set; }

        [DataMember(Name = "vip_ep")]
        public bool IsVipEP { get; set; }

        [DataMember(Name = "vip_og")]
        public bool IsVipOG { get; set; }

        [DataMember(Name = "vip_years")]
        public int VipYears { get; set; }

        [DataMember(Name = "private")]
        public bool IsPrivate { get; set; }

        [DataMember(Name = "Ids")]
        public TraktUserId Ids { get; set; }
    }
}
