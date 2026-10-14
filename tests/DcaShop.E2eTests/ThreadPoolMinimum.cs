using System.Runtime.CompilerServices;

namespace DcaShop.E2eTests;

/// <summary>
/// Raises the ThreadPool's minimum before the first test runs. The suite shares one pool between xUnit's parallel
/// test classes, the shops it hosts in this process, their event dispatch and the stubs. Above its minimum (the
/// processor count) the pool adds threads only slowly, so a burst of blocked or busy work — many test classes
/// starting at once — leaves queued work waiting for seconds, and a test's wait runs out against the queue, not
/// against the shop. Measured with every suite running side by side: the pool needed up to about 40 threads, and work
/// waited up to 3 s for one at the default minimum and under 0.7 s at 64. The pool still creates threads only on
/// demand; the minimum only lets it do so without the delay.
/// </summary>
internal static class ThreadPoolMinimum
{
    private const int Threads = 64;

#pragma warning disable CA2255 // a test assembly, not a library: the setting must hold before the runner starts the first test
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Raise()
    {
        ThreadPool.GetMinThreads(out var worker, out var completionPort);
        ThreadPool.SetMinThreads(Math.Max(worker, Threads), Math.Max(completionPort, Threads));
    }
}