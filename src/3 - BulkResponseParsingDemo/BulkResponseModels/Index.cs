using Newtonsoft.Json;
using System.Text.Json;

namespace BulkResponseParsingDemo.BulkResponseModels
{
    public class Index
    {
        [JsonProperty(PropertyName = "_id")]
        public string Id { get; set; }
        public int Status { get; set; }
    }
}
