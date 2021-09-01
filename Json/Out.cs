using Newtonsoft.Json;
using System.Collections.Generic;

namespace InferenceModelMetadata.Json
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Decision
    {
        [JsonProperty(PropertyName = "area")]
        public string area { get; set; }

        [JsonProperty(PropertyName = "porc_conf")]
        public double porc_conf { get; set; }

        [JsonProperty(PropertyName = "fiable")]
        public bool fiable { get; set; }

    }
    
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class  Others
    {
        [JsonProperty(PropertyName = "area")]
        public string area { get; set; }

        [JsonProperty(PropertyName = "porc_conf")]
        public double porc_conf { get; set; }

        [JsonProperty(PropertyName = "fiable")]
        public bool fiable { get; set; }

    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Application
    {
        [JsonProperty(PropertyName = "decision")]
        public Decision decision { get; set; }

        [JsonProperty(PropertyName = "others")]
        public IList<Others> others { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string message { get; set; }

        [JsonProperty(PropertyName = "logid")]
        public string logid { get; set; }

        [JsonProperty(PropertyName = "datetime")]
        public string datetime { get; set; }

    }
}
