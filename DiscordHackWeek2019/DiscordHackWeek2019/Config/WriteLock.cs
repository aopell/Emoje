using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscordHackWeek2019.Config
{
    public struct WriteLock : IDisposable
    {
        private bool lockHeld;
        private ReaderWriterLockSlim rwLock;

        public WriteLock(ReaderWriterLockSlim rwLock)
        {
            this.rwLock = rwLock;
            rwLock.EnterWriteLock();
            lockHeld = true;
        }

        public void Dispose()
        {
            if (lockHeld)
            {
                rwLock.ExitWriteLock();
            }
        }
    }
}
