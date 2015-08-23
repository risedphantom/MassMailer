using System.Threading;

namespace MailService
{
    class SendCounter
    {
        private static long _counter;

        public static void Increment()
        {
            Interlocked.Increment(ref _counter);
        }

        public static long Read()
        {
            return _counter;
        }

        public static long Refresh()
        {
            return Interlocked.Exchange(ref _counter, 0);
        }

        public static void Add(long value)
        {
            Interlocked.Add(ref _counter, value);
        }

        public static void Set(long value)
        {
            Interlocked.Exchange(ref _counter, value);
        }
    }

    class ErrorCounter
    {
        private static long _counter;

        public static void Increment()
        {
            Interlocked.Increment(ref _counter);
        }

        public static long Read()
        {
            return _counter;
        }

        public static long Refresh()
        {
            return Interlocked.Exchange(ref _counter, 0);
        }

        public static void Add(long value)
        {
            Interlocked.Add(ref _counter, value);
        }

        public static void Set(long value)
        {
            Interlocked.Exchange(ref _counter, value);
        }
    }
}
