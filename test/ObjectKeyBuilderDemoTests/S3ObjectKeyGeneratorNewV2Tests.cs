using System;
using FluentAssertions;
using ObjectKeyBuilderDemo;
using Xunit;

namespace ObjectKeyBuilderDemoTests
{
    public class S3ObjectKeyGeneratorNewV2Tests
    {
        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WhenAllPartsArePresentAndValid()
        {
            const string expectedKey = "my_product/sitekey/myevent/2019/04/11/02/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = new DateTime(2019, 04, 11, 02, 00, 00, DateTimeKind.Utc),
                EventName = "MyEvent",
                Product = "My Product",
                SiteKey = "SiteKey"
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }

        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WithoutExtraDatePadding_WhenAllPartsArePresentAndValid()
        {
            const string expectedKey = "my_product/sitekey/myevent/2019/10/11/15/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = new DateTime(2019, 10, 11, 15, 00, 00, DateTimeKind.Utc),
                EventName = "MyEvent",
                Product = "My Product",
                SiteKey = "SiteKey"
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }

        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WhenAllPartsArePresentAndValid_ExceptForDateTimeUtc()
        {
            const string expectedKey = "my_product/sitekey/myevent/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = default,
                EventName = "MyEvent",
                Product = "My Product",
                SiteKey = "SiteKey"
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }

        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WhenAllPartsArePresentAndValid_ExceptForProduct()
        {
            const string expectedKey = "unknown/sitekey/myevent/2019/04/11/02/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = new DateTime(2019, 04, 11, 02, 00, 00, DateTimeKind.Utc),
                EventName = "MyEvent",
                Product = null,
                SiteKey = "SiteKey"
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }

        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WhenAllPartsArePresentAndValid_ExceptForSiteKey()
        {
            const string expectedKey = "my_product/unknown/myevent/2019/04/11/02/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = new DateTime(2019, 04, 11, 02, 00, 00, DateTimeKind.Utc),
                EventName = "MyEvent",
                Product = "My Product",
                SiteKey = null
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }

        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WhenAllPartsArePresentAndValid_ExceptForEventName()
        {
            const string expectedKey = "my_product/sitekey/unknown/2019/04/11/02/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = new DateTime(2019, 04, 11, 02, 00, 00, DateTimeKind.Utc),
                EventName = null,
                Product = "My Product",
                SiteKey = "SiteKey"
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }

        [Fact]
        public void GenerateSafeObjectKey_ReturnsExpectedKey_WhenAllPartsArePresentAndValid_WhereProductContainsInvalidChars()
        {
            const string expectedKey = "invalid/sitekey/myevent/2019/04/11/02/16de1167-9e28-4eb5-95e9-3b23b6191dc7.json";

            var ctx = new EventContext
            {
                MessageId = "16de1167-9e28-4eb5-95e9-3b23b6191dc7",
                EventDateUtc = new DateTime(2019, 04, 11, 02, 00, 00, DateTimeKind.Utc),
                EventName = "MyEvent",
                Product = "My Product %20", //invalid
                SiteKey = "SiteKey"
            };

            var key = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(ctx);

            key.Should().Be(expectedKey);
        }
    }
}