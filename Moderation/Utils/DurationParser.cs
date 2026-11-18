using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Moderation.Utils
{
    public static class DurationParser
    {
        private static readonly Regex DurationRegex = new Regex(@"(\d+)\s*([a-zA-Z]+)", RegexOptions.Compiled);

        public static bool TryParse(string input, out uint seconds, out string formatted)
        {
            seconds = 0;
            formatted = "permanent";

            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim().ToLowerInvariant();

            if (input == "0" || input == "perm" || input == "permanent" || input == "inf" || input == "infinity")
            {
                seconds = 0;
                formatted = "permanent";
                return true;
            }

            if (uint.TryParse(input, out uint numericSec))
            {
                seconds = numericSec;
                formatted = FormatDuration(seconds);
                return true;
            }

            MatchCollection matches = DurationRegex.Matches(input);
            if (matches.Count == 0)
                return false;

            ulong totalSeconds = 0;
            foreach (Match match in matches)
            {
                if (!ulong.TryParse(match.Groups[1].Value, out ulong amount))
                    continue;

                string unit = match.Groups[2].Value.ToLowerInvariant();
                switch (unit)
                {
                    case "s":
                    case "sec":
                    case "second":
                    case "seconds":
                        totalSeconds += amount;
                        break;
                    case "m":
                    case "min":
                    case "minute":
                    case "minutes":
                        totalSeconds += amount * 60;
                        break;
                    case "h":
                    case "hr":
                    case "hour":
                    case "hours":
                        totalSeconds += amount * 3600;
                        break;
                    case "d":
                    case "day":
                    case "days":
                        totalSeconds += amount * 86400;
                        break;
                    case "w":
                    case "wk":
                    case "week":
                    case "weeks":
                        totalSeconds += amount * 604800;
                        break;
                    case "mo":
                    case "month":
                    case "months":
                        totalSeconds += amount * 2592000;
                        break;
                    case "y":
                    case "yr":
                    case "year":
                    case "years":
                        totalSeconds += amount * 31536000;
                        break;
                    default:
                        return false;
                }
            }

            if (totalSeconds > uint.MaxValue)
                totalSeconds = uint.MaxValue;

            seconds = (uint)totalSeconds;
            formatted = FormatDuration(seconds);
            return true;
        }

        public static string FormatDuration(uint seconds)
        {
            if (seconds == 0)
                return "permanent";

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            StringBuilder sb = new StringBuilder();

            if (span.Days > 365)
            {
                int years = span.Days / 365;
                sb.Append($"{years} year{(years > 1 ? "s" : "")} ");
                int remainingDays = span.Days % 365;
                if (remainingDays > 0)
                    sb.Append($"{remainingDays} day{(remainingDays > 1 ? "s" : "")} ");
            }
            else if (span.Days > 0)
            {
                sb.Append($"{span.Days} day{(span.Days > 1 ? "s" : "")} ");
            }

            if (span.Hours > 0)
                sb.Append($"{span.Hours} hour{(span.Hours > 1 ? "s" : "")} ");

            if (span.Minutes > 0)
                sb.Append($"{span.Minutes} minute{(span.Minutes > 1 ? "s" : "")} ");

            if (span.Seconds > 0 && sb.Length == 0)
                sb.Append($"{span.Seconds} second{(span.Seconds > 1 ? "s" : "")} ");

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "0 seconds" : result;
        }
    }
}
