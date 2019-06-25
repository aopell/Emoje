using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscordHackWeek2019.Config
{
    public struct ReadLock : IDisposable
    {
        private bool lockHeld;
        private ReaderWriterLockSlim rwLock;

        public ReadLock(ReaderWriterLockSlim rwLock)
        {
            this.rwLock = rwLock;
            rwLock.EnterReadLock();
            lockHeld = true;
        }

        public void Dispose()
        {
            if (lockHeld)
            {
                rwLock.ExitReadLock();
            }
        }
    }
}
