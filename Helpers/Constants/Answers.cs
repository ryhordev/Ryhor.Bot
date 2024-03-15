namespace Ryhor.Bot.Helpers.Constants
{
    public static class Answers
    {
        public static class Common
        {
            public const string FORBIDDEN = "403 Forbidden 😱";
            public const string ANSWER_GENERETING = "An answer is generating. \nPlease be patient 😇 It can take a while.";
            public const string COMMAND_IS_NOT_RECOGNIZED = "Apologies, I don't recognize this command. 😔 \nPlease refer to the menu to view available commands.";
        }

        public static class Greeting
        {
            public const string HI = "Hi, {0}!\nDelighted to have you here! 🎉";
            public const string WIFE = "I cherish you dearly, my guiding light and better half! ❤";
        }

        public static class Benchmark
        {
            public const string ENTER_CODE = "Please send a method body to start benchmark 🧑‍💻";
            public const string STOPPED_PROCESS = "The process has been stopped after {0} minutes due to excessive body size, or the method may be infinite. 😔 \nPlease try again with another method.";
        }
    }
}
