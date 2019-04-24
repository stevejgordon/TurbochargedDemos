using System;

namespace ObjectKeyBuilderDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var ctx = new EventContext
            {
                MessageId = "ebc6d78b-8b29-4fc3-a412-bc43be6a9d21",
                EventDateUtc = new DateTime(2019, 04, 01, 10, 00, 00, DateTimeKind.Utc),
                EventName = "My Event",
                Product = "My Product",
                SiteKey = "Site Key"
            };

            var key1 = S3ObjectKeyGenerator.GenerateSafeObjectKey(ctx);
            var key2 = S3ObjectKeyGeneratorNew.GenerateSafeObjectKey(ctx);
            var key3 = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);
        }
    }
}
