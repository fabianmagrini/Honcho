using System;
using Xunit;
using Honcho.Core.FileLock;

namespace Honcho.Tests
{
    public class TestFileLock
    {
        [Fact]
        public void TestFileLockIsCreated()
        {
            TimeSpan LockTimeout = new TimeSpan(0, 0, 0, 10);
            string LockName = "TestLock";
            var fileLock = Lock.Create(LockName, LockTimeout);
            Assert.Equal(LockName, fileLock.LockName);
        }
    }
}
