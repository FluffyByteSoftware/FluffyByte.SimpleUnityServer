namespace FluffyByte.SimpleUnityserver.Utilities
{
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public enum MessageType
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public static class Scribe
    {
        public readonly static bool EnableDebugFlag = true;

        private readonly static ConsoleColor WarningColor = ConsoleColor.Yellow;
        private readonly static ConsoleColor ErrorColor = ConsoleColor.Red;
        private readonly static ConsoleColor InfoColor = ConsoleColor.Green;
        private readonly static ConsoleColor WriteColor = ConsoleColor.Gray;
        private readonly static Lock _lockObj = new();
        private readonly static Channel<(string message, string file, int line, MessageType type)> _logChannel = Channel.CreateUnbounded<(string, string, int, MessageType)>();

        static Scribe()
        {
            Task.Run(ProcessLogQueueAsync);
        }

        public static void Write(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            Log(message, file, line, MessageType.Info);

        public static void Warn(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            Log(message, file, line, MessageType.Warning);

        public static void Error(Exception ex,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            lock (_lockObj)
            {
                Console.ForegroundColor = ErrorColor;
                Console.WriteLine($"[ {TimeStamp} EXCEPTION in {Path.GetFileName(file)}, line {line} ]");
                Console.WriteLine($"[ Message: {ex.Message} ]");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.ResetColor();
            }
        }

        public static void Error(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            lock (_lockObj)
            {
                Log(message, file, line, MessageType.Error);
            }
        }

        public static void Debug(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (EnableDebugFlag)
                Log(message, file, line, MessageType.Debug);
        }

        public static async Task WriteAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            await LogAsync(message, file, line, MessageType.Info);

        public static async Task WarnAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            await LogAsync(message, file, line, MessageType.Warning);

        public static async Task ErrorAsync(Exception ex,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            string formatted =
                $"[ {TimeStamp} EXCEPTION in {Path.GetFileName(file)}, line {line} ]\n" +
                $"[ Message: {ex.Message} ]\n" +
                $"StackTrace: {ex.StackTrace}";
            await LogAsync(formatted, file, line, MessageType.Error);
        }


        public static async Task DebugAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (EnableDebugFlag)
                await LogAsync(message, file, line, MessageType.Debug);
        }

        public static string TimeStamp => DateTime.Now.ToString("yyyy-dd-MM hh:mm:ss.fff");

        private static void Log(string message, string file, int line, MessageType messageType)
        {
            lock (_lockObj)
            {
                SetColor(messageType);
                Console.WriteLine($"[ Object: {Path.GetFileName(file)} at line: {line}]");
                Console.WriteLine($"[{TimeStamp} - {messageType.ToString().ToUpper()} ]: {message}");
                Console.ResetColor();
            }
        }

        private static async Task LogAsync(string message, string file, int line, MessageType type)
        {
            await _logChannel.Writer.WriteAsync((message, file, line, type));
        }

        private static async Task ProcessLogQueueAsync()
        {
            await foreach (var (message, file, line, type) in _logChannel.Reader.ReadAllAsync())
            {
                Log(message, file, line, type);
            }
        }

        private static void SetColor(MessageType type)
        {
            Console.ForegroundColor = type switch
            {
                MessageType.Info => InfoColor,
                MessageType.Warning => WarningColor,
                MessageType.Error => ErrorColor,
                MessageType.Debug => WriteColor,
                _ => WriteColor,
            };
        }
    }
}
