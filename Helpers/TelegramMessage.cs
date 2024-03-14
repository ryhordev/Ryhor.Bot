using System.Text.RegularExpressions;

namespace Ryhor.Bot.Helpers
{
    public static partial class TelegramMessage
    {
        public static string ReplaceTgCharacters(this string message)
        {
            return TgRegex().Replace(message, @"\")
                            .Replace(@"*", @"\*")
                            .Replace(@"[", @"\[")
                            .Replace(@"]", @"\]")
                            .Replace(@"(", @"\(")
                            .Replace(@")", @"\)")
                            .Replace(@"~", @"\~")
                            .Replace(@"`", @"\`")
                            .Replace(@">", @"\>")
                            .Replace(@"#", @"\#")
                            .Replace(@"+", @"\+")
                            .Replace(@"-", @"\-")
                            .Replace(@"=", @"\=")
                            .Replace(@"|", @"\|")
                            .Replace(@"{", @"\{")
                            .Replace(@"}", @"\}")
                            .Replace(@".", @"\.")
                            .Replace(@"!", @"\!")
                            .Trim();
        }

        [GeneratedRegex(@"\\")]
        private static partial Regex TgRegex();
    }
}
