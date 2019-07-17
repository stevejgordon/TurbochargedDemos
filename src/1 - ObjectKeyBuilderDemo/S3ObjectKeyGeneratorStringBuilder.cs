using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectKeyBuilderDemo
{
    public class S3ObjectKeyGeneratorStringBuilder
    {
        private const string ValidKeyPartPattern = "^[a-zA-Z0-9_]+$";
        private const string UnknownPart = "unknown";
        private const string InvalidPart = "invalid";
        private const char JoinChar = '/';

        public static string GenerateSafeObjectKey(EventContext eventContext)
        {
            var sb = new StringBuilder();

            sb.Append(GetPart(eventContext.Product)).Append(JoinChar);
            sb.Append(GetPart(eventContext.SiteKey)).Append(JoinChar);
            sb.Append(GetPart(eventContext.EventName)).Append(JoinChar);

            if (eventContext.EventDateUtc != default)
            {
                sb.Append(eventContext.EventDateUtc.Year.ToString()).Append(JoinChar);
                sb.Append(eventContext.EventDateUtc.Month.ToString("D2")).Append(JoinChar);
                sb.Append(eventContext.EventDateUtc.Day.ToString("D2")).Append(JoinChar);
                sb.Append(eventContext.EventDateUtc.Hour.ToString("D2")).Append(JoinChar);
            }

            sb.Append(eventContext.MessageId + ".json");

            var key = sb.ToString();
            return key.ToLower(CultureInfo.InvariantCulture);
        }

        private static string GetPart(string input)
        {
            var part = string.IsNullOrEmpty(input) ? UnknownPart : RemoveSpaces(input);
            return IsPartValid(part) ? part : InvalidPart;
        }

        private static string RemoveSpaces(string part) => part.IndexOf(' ') == -1
            ? part
            : part.Replace(' ', '_');

        private static bool IsPartValid(string input) => Regex.IsMatch(input, ValidKeyPartPattern);
    }
}
