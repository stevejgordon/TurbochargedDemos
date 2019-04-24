using System;

namespace ObjectKeyBuilderDemo
{
    public struct EventContext
    {
        public DateTime EventDateUtc { get; set; }
        public string Product { get; set; }
        public string SiteKey { get; set; }
        public string EventName { get; set; }
        public string MessageId { get; set; }
    }
}
