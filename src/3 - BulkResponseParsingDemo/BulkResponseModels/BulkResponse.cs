using System.Collections.Generic;

namespace BulkResponseParsingDemo.BulkResponseModels
{
    public class BulkResponse
    {
        public int Took { get; set; }
        public bool Errors { get; set; }
        public IReadOnlyCollection<Item> Items { get; set; }
    }
}
