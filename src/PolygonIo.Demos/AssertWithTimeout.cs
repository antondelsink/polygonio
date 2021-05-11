using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace PolygonIo.Demos
{
    internal static class AssertWithTimeout
    {
        internal static void IsTrue(Func<bool> conditional, string msgTimedOut)
        {
            IsTrue(conditional, msgTimedOut, TimeSpan.FromMilliseconds(10));
        }

        internal static void IsTrue(Func<bool> conditional, string msgTimedOut, TimeSpan timeout)
        {
            if (!SpinWait.SpinUntil(conditional, timeout))
            {
                Assert.Fail(msgTimedOut);
            }
        }
    }
}
