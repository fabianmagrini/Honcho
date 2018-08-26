using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Honcho.Core.Events;
using Honcho.Core.Interfaces;

namespace Honcho.Core.Services
{
    public class LeaderElectionService
    {
        public LeaderElectionService(IDistributedLock distributedLock, ILogger<LeaderElectionService> logger)
        {
            if (distributedLock == null) throw new ArgumentNullException("lock is null");
            _distributedLock = distributedLock;

            string leadershipLockKey = _distributedLock.LockName;
            if (string.IsNullOrEmpty(leadershipLockKey))
                throw new ArgumentNullException(leadershipLockKey);


            this.key = leadershipLockKey;

             _logger = logger;
        }

        private readonly ILogger<LeaderElectionService> _logger;

        public event EventHandler<LeaderChangedEventArgs> LeaderChanged;

        string key;
        CancellationTokenSource cts = new CancellationTokenSource();
        Timer timer;

        bool lastIsHeld = false;
        IDistributedLock _distributedLock;      

        //private static readonly TimeSpan LockTTL = new TimeSpan(0, 0, 0, 10);
        private static readonly TimeSpan RefreshTime = new TimeSpan(0, 0, 0, 5);
        
        public void Start()
        {
            timer = new Timer(async (object state) => await TryAcquireLock((CancellationToken)state), cts.Token, 0, Timeout.Infinite);
        }

        private async Task TryAcquireLock(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            try
            {
                //distributedLock = Lock.Create(key, LockTTL);
                await _distributedLock.Acquire();
            }
            catch (Exception)
            {
                //Log
            }
            finally
            {
                bool lockHeld = _distributedLock?.IsHeld == true;
                HandleLockStatusChange(lockHeld);
                timer.Change(RefreshTime, Timeout.InfiniteTimeSpan);
            }
        }

        protected virtual void HandleLockStatusChange(bool isHeldNew)
        {
#if DEBUG
            var status = isHeldNew ? "Active" : "Standby";
            _logger.LogDebug($"[Debug] status is ({status}) at {DateTime.Now.ToString("hh:mm:ss")}"); 
#endif

            if (lastIsHeld == isHeldNew)
                return;
            else
            {
                lastIsHeld = isHeldNew;
            }


            if (LeaderChanged != null)
            {
                LeaderChangedEventArgs args = new LeaderChangedEventArgs(lastIsHeld);
                foreach (EventHandler<LeaderChangedEventArgs> handler in LeaderChanged.GetInvocationList())
                {
                    try
                    {
                        handler(this, args);
                    }
                    catch (Exception)
                    {
                        //Log
                    }
                }
            }
        }
    }
}