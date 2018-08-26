using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Honcho.Core.Interfaces;

namespace Honcho.Core.FileLock
{
    public class Lock : IDistributedLock
    {
        protected Lock(string lockName, TimeSpan lockTimeout)
        {
            LockName = lockName;
            LockTimeout = lockTimeout;
        }

        public TimeSpan LockTimeout { get; private set; }

        public string LockName { get; private set; }

        private string LockFilePath { get; set; }
        private readonly AsyncLock _mutex = new AsyncLock();
        private bool _isheld;

        public bool IsHeld
        {
            get
            {
                return _isheld;
            }
            private set
            {
                _isheld = value;
            }
        }

        public Task<bool> Acquire()
        {
            return Acquire(CancellationToken.None);
        }

        public async Task<bool> Acquire(CancellationToken ct)
        {
            try
            {
                using (await _mutex.LockAsync().ConfigureAwait(false))
                {
                    if (LockExists(LockFilePath))
                    {               
                        LockInfo lockInfo = null;
                        var missingLockFile = false;

                        try
                        {
                            using (var stream = File.OpenRead(LockFilePath))
                            {
                                lockInfo = DeserializeFromStream(stream); 
                                if (lockInfo == null) {
                                    missingLockFile = true;
                                }
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            //Console.WriteLine("FileNotFoundException");
                            missingLockFile = true;
                        }
                        catch (IOException)
                        {
                            // Someone else owns the lock
                            return false;
                        }
                        catch (Exception) //We have no idea what went wrong - reacquire this lock
                        {
                            //Console.WriteLine("Exception");
                            missingLockFile = true;
                        }

                        //the file no longer exists
                        if (missingLockFile || lockInfo==null)
                        {
                            //Console.WriteLine("The file no longer exists");
                            return TryAcquireLock();
                        }

                        var lockWriteTime = new DateTime(lockInfo.Timestamp);

                        //This lock belongs to this process - we can reacquire the lock
                        if (lockInfo.PID == Process.GetCurrentProcess().Id)
                        {
                            return TryAcquireLock();
                        }

                        //The lock has not timed out - we can't acquire it
                        if (!(Math.Abs((DateTime.Now - lockWriteTime).TotalSeconds) > LockTimeout.TotalSeconds)) return false;
                    }

                    //Acquire the lock
                    
                    return TryAcquireLock();
                }
            }
            finally
            {
            }
        }

        public async Task Release(CancellationToken ct = default(CancellationToken))
        {
            using (await _mutex.LockAsync().ConfigureAwait(false))
            {
                //Need to own the lock in order to release it (and we can reacquire the lock inside the current process)
                if (LockExists(LockFilePath) && TryAcquireLock())
                    DeleteLock(LockFilePath);
                }
        }

        public async Task Destroy(CancellationToken ct = default(CancellationToken))
        {
            using (await _mutex.LockAsync().ConfigureAwait(false))
            {

            }
        }

        #region Internal methods

        protected LockInfo CreateLockInfo()
        {
            var process = Process.GetCurrentProcess();
            return new LockInfo()
            {
                PID = process.Id,
                Timestamp = DateTime.Now.Ticks,
                ProcessName = process.ProcessName,
                Owner = GetGloballyUniqueServerId()
            };
        }

        private bool TryAcquireLock()
        {
            try
            {
                using (var stream = File.CreateText(LockFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(stream, CreateLockInfo());

                }
                IsHeld = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Create methods

        public static Lock Create(string lockName, TimeSpan lockTimeout)
        {
            if (string.IsNullOrEmpty(lockName))
                throw new ArgumentNullException("lockName", "lockName IsNullOrEmpty");
            return new Lock(lockName, lockTimeout) { LockFilePath = GetFilePath(lockName) };
        }

        #endregion

        #region Helper methods
        public static string GetFilePath(string lockName)
        {
            var filename = MakeValidFileName(lockName);
            return Path.Combine(Path.GetTempPath(), filename);
        }

        public static LockInfo DeserializeFromStream(Stream stream)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<LockInfo>(jsonTextReader);
            }
        }

        public static bool LockExists(string lockFilePath)
        {
            return File.Exists(lockFilePath);
        }

        public static void DeleteLock(string lockFilePath)
        {
            try
            {
                File.Delete(lockFilePath);
            }
            catch (Exception)
            {
                
            }
        }

        static string GetGloballyUniqueServerId()
        {
            var serverName = Environment.MachineName
                ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
                ?? Environment.GetEnvironmentVariable("HOSTNAME");

            var guid = Guid.NewGuid().ToString();


            return !String.IsNullOrWhiteSpace(serverName)
                ? $"{serverName.ToLowerInvariant()}:{guid}"
                : guid;
        } 

        private static string MakeValidFileName(string name)
        {
            string invalidFileNameChars = new string(Path.GetInvalidFileNameChars());
            string invalidChars = Regex.Escape(invalidFileNameChars);
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(name, invalidRegStr, "_");
        }
        #endregion
    }
}