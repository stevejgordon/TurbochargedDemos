using System.Globalization;
using System.Text.RegularExpressions;

namespace ObjectKeyBuilderDemo
{
    public class S3ObjectKeyGenerator
    {
        private const string ValidKeyPartPattern = "^[a-zA-Z0-9_]+$";
        private const string UnknownPart = "unknown";
        private const string InvalidPart = "invalid";
        private const char JoinChar = '/';

        public static string GenerateSafeObjectKey(EventContext eventContext)
        {
            var elements = eventContext.EventDateUtc == default ? 4 : 5;

            var parts = new string[elements];

            parts[0] = GetPart(eventContext.Product);
            parts[1] = GetPart(eventContext.SiteKey);
            parts[2] = GetPart(eventContext.EventName);

            if (eventContext.EventDateUtc != default)
            {
                parts[3] = eventContext.EventDateUtc.ToString("yyyy/MM/dd/HH");
                parts[4] = eventContext.MessageId + ".json";
            }
            else
            {
                parts[3] = eventContext.MessageId + ".json";
            }
            
            var key = string.Join(JoinChar, parts); 
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

        private static bool IsPartValid(string input) => 
            Regex.IsMatch(input, ValidKeyPartPattern);
    }
}
