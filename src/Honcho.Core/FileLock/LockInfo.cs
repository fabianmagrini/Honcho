
namespace Honcho.Core.FileLock
{
    public class LockInfo
    {
        public long PID { get; set; }
        public long Timestamp { get; set; }
        public string ProcessName { get; set; }
        public string Owner { get; set; }
    }
}