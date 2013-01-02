// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Threading;

namespace fCraft {
    static class RWLSExtension {
        public static ReadLockHelper ReadLock ( this ReaderWriterLockSlim readerWriterLock ) {
            return new ReadLockHelper( readerWriterLock );
        }

        public static UpgradeableReadLockHelper UpgradableReadLock ( this ReaderWriterLockSlim readerWriterLock ) {
            return new UpgradeableReadLockHelper( readerWriterLock );
        }

        public static WriteLockHelper WriteLock ( this ReaderWriterLockSlim readerWriterLock ) {
            return new WriteLockHelper( readerWriterLock );
        }

        public struct ReadLockHelper : IDisposable {
            private readonly ReaderWriterLockSlim readerWriterLock;

            public ReadLockHelper ( ReaderWriterLockSlim readerWriterLock ) {
                readerWriterLock.EnterReadLock();
                this.readerWriterLock = readerWriterLock;
            }

            public void Dispose () {
                readerWriterLock.ExitReadLock();
            }
        }

        public struct UpgradeableReadLockHelper : IDisposable {
            private readonly ReaderWriterLockSlim readerWriterLock;

            public UpgradeableReadLockHelper ( ReaderWriterLockSlim readerWriterLock ) {
                readerWriterLock.EnterUpgradeableReadLock();
                this.readerWriterLock = readerWriterLock;
            }

            public void Dispose () {
                readerWriterLock.ExitUpgradeableReadLock();
            }
        }

        public struct WriteLockHelper : IDisposable {
            private readonly ReaderWriterLockSlim readerWriterLock;

            public WriteLockHelper ( ReaderWriterLockSlim readerWriterLock ) {
                readerWriterLock.EnterWriteLock();
                this.readerWriterLock = readerWriterLock;
            }

            public void Dispose () {
                readerWriterLock.ExitWriteLock();
            }
        }
    }
}