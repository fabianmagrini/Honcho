using System;
using System.Threading;
using System.Threading.Tasks;

namespace Honcho.Core.Interfaces
{
    public interface IDistributedLock
    {
        string LockName { get; }
        bool IsHeld { get; }

        Task<bool> Acquire(CancellationToken ct = default(CancellationToken));
        Task Destroy(CancellationToken ct = default(CancellationToken));
        Task Release(CancellationToken ct = default(CancellationToken));
    }
}