namespace CloudfrontLogParserDemo
{
    public struct CloudFrontRecordStruct
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string UserAgent { get; set; }
    }

    public class CloudFrontRecord
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string UserAgent { get; set; }
    }
}
