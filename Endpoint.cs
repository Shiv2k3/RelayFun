using System.Net;
using Newtonsoft.Json;

namespace RelayFun
{
    public partial class Function
    {
        public class Endpoint
        {
            [JsonProperty("id")]
            public string id;
            [JsonConstructor]
            public Endpoint() { }
            public Endpoint(IPAddress ip, string name)
            {
                id = ip.ToString() + name;
            }
        }

    }
}