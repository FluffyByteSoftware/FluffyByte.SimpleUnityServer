// Polished Scribe.cs: Robust, async-friendly, thread-safe logger for console applications
namespace FluffyByte.SimpleUnityServer.Utilities
{
    using System;
    using System.IO;
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
        public static readonly bool EnableDebugFlag = true;
        private static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
        private static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
        private static readonly ConsoleColor InfoColor = ConsoleColor.Green;
        private static readonly ConsoleColor DebugColor = ConsoleColor.Cyan;
        private static readonly ConsoleColor WriteColor = ConsoleColor.Gray;
        private static readonly Lock _lockObj = new();
        private static readonly Channel<(string message, string file, int line, MessageType type)> _logChannel = Channel.CreateUnbounded<(string, string, int, MessageType)>();

        static Scribe()
        {
            // Fire-and-forget background logging processor
            _ = Task.Run(ProcessLogQueueAsync);
        }

        // === SYNC LOGGING API ===
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
                SetColor(MessageType.Error);
                string formatted =
                    $"[ {TimeStamp} EXCEPTION in {Path.GetFileName(file)}, line {line} ]\n" +
                    $"[ Message: {ex.Message} ]\n" +
                    $"StackTrace: {ex.StackTrace}";
                Console.WriteLine(formatted);
                Console.ResetColor();
            }
        }

        public static void Error(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            Log(message, file, line, MessageType.Error);

        public static void Debug(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (EnableDebugFlag)
                Log(message, file, line, MessageType.Debug);
        }

        // === ASYNC LOGGING API ===
        public static Task WriteAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            LogAsync(message, file, line, MessageType.Info);

        public static Task WarnAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) =>
            LogAsync(message, file, line, MessageType.Warning);

        public static async Task ErrorAsync(Exception ex,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            var formatted =
                $"[ {TimeStamp} EXCEPTION in {Path.GetFileName(file)}, line {line} ]\n" +
                $"[ Message: {ex.Message} ]\n" +
                $"StackTrace: {ex.StackTrace}";
            await LogAsync(formatted, file, line, MessageType.Error);
        }

        public static Task ErrorAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            var formatted =
                $"[ {TimeStamp} ERROR in {Path.GetFileName(file)}, line {line} ] [ Message: {message} ]";
            return LogAsync(formatted, file, line, MessageType.Error);
        }

        public static Task DebugAsync(string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (EnableDebugFlag)
                return LogAsync(message, file, line, MessageType.Debug);
            return Task.CompletedTask;
        }

        // === SUPPORT ===
        /// <summary>
        /// Writes a plain, unformatted message directly to the console. No colors, no prefixes, not queued.
        /// </summary>
        public static Task WriteCleanAsync(string message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        public static string TimeStamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        private static void Log(string message, string file, int line, MessageType messageType)
        {
            lock (_lockObj)
            {
                SetColor(messageType);
                if (messageType == MessageType.Error && message.Contains("EXCEPTION"))
                {
                    // Multi-line for full exceptions
                    Console.WriteLine(message);
                }
                else
                {
                    // Single line log: [Timestamp][TYPE][File:Line] Message
                    Console.WriteLine($"[{TimeStamp}] [{messageType.ToString().ToUpper()}] [{Path.GetFileName(file)}:{line}] {message}");
                }
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
                MessageType.Debug => DebugColor,
                _ => WriteColor,
            };
        }

        public static Task ClearConsole()
        {
            lock (_lockObj)
            {
                Console.Clear();
            }
            return Task.CompletedTask;
        }
    }
}
