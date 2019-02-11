namespace MemoryCache.XUnitTests
{
  using System;
  using System.Collections;
  using System.Threading;
  using System.Threading.Tasks;
  using MemoryCache;
  using Xunit;

  public class UnitTestMemoryCache
  {
    // see https://github.com/dotnet/roslyn-analyzers/issues/1399
#pragma warning disable CA1822 // Mark members as static

    private static int lazyCounter = 0;

    [Fact]
    public void Count()
    {
      int sizeLimit = 10000;
      var cache = new MemoryCache<int, string>((uint)sizeLimit);
      Assert.Equal(0, cache.Count);
      for (int i = 1; i <= sizeLimit; i++)
      {
        // make sure count returns correct size of cache
        cache.Set(i, () => Task.FromResult($"{i}"));
        Assert.Equal(i, cache.Count);
      }
    }

    [Fact]
    public void SetSizeLimit()
    {
      // test SetSizeLimit method and Sizelimit property
      var cache = SetupMemoryCache(100);
      Assert.Equal(100U, cache.SizeLimit);
      cache.SetSizeLimit(10);
      Assert.Equal(10, cache.Count);
    }

    [Fact]
    public void SizeLimitSet()
    {
      // make sure size limit is enforced using Set
      int sizeLimit = 100;
      var cache = SetupMemoryCache(sizeLimit);

      Assert.Equal((int)sizeLimit, cache.Count);
      for (int i = sizeLimit; i < sizeLimit + 10; i++)
      {
        cache.Set(i, () => Task.FromResult($"{i}"));
      }

      Assert.Equal((int)sizeLimit, cache.Count);
    }

    [Fact]
    public async void SizeLimitGetOrAdd()
    {
      // make sure sizeLimit is encforced using GetOrAdd
      int sizeLimit = 100;
      var cache = new MemoryCache<int, string>((uint)sizeLimit);
      for (int i = 0; i < sizeLimit; i++)
      {
        await cache.GetOrAddAsync(i, () => Task.FromResult($"{i}")).ConfigureAwait(false);
        await cache.GetOrAddAsync(i, () => Task.FromResult($"{i}")).ConfigureAwait(false);
        await cache.GetOrAddAsync(i, () => Task.FromResult($"{i}")).ConfigureAwait(false);
      }

      Assert.Equal((int)sizeLimit, cache.Count);
      for (int i = sizeLimit; i < sizeLimit + 10; i++)
      {
        await cache.GetOrAddAsync(i, () => Task.FromResult($"{i}")).ConfigureAwait(false);
      }

      Assert.Equal((int)sizeLimit, cache.Count);
    }

    [Fact]
    public async void SetAndGet()
    {
      // store items using Set and check Get returns correct items
      var items = 100;
      var cache = new MemoryCache<int, string>();
      for (int i = 0; i < items; i++)
      {
        var value = $"{i}";
        cache.Set(i, () => Task.FromResult(value));
      }

      for (int i = 0; i < items; i++)
      {
        Assert.Equal($"{i}", await cache.GetAsync(i).ConfigureAwait(false));
      }
    }

    [Fact]
    public async void GetOrAddAndGet()
    {
      // store items using GetOrAdd and check get returns correct items
      var items = 100;
      var cache = new MemoryCache<int, string>();
      for (int i = 0; i < items; i++)
      {
        var value = $"{i}";
        await cache.GetOrAddAsync(i, () => Task.FromResult(value)).ConfigureAwait(false);
      }

      for (int i = 0; i < items; i++)
      {
        Assert.Equal($"{i}", await cache.GetAsync(i).ConfigureAwait(false));
      }
    }

    [Fact]
    public async void SetEvictLRU()
    {
      // make sure evic using corret LRU using SET
      int sizeLimit = 100;
      var cache = SetupMemoryCache(sizeLimit);

      Assert.Equal((int)sizeLimit, cache.Count);
      var last = sizeLimit + 1;
      cache.Set(last, () => Task.FromResult($"{last}"));
      Assert.Equal((int)sizeLimit, cache.Count);

      // 0 should have been evicted since it was the oldest
      Assert.Null(await cache.GetAsync(0).ConfigureAwait(false));

      // check that Get will "protect" from eviction
      await cache.GetAsync(1).ConfigureAwait(false);
      cache.Set(0, () => Task.FromResult("0"));
      Assert.Null(await cache.GetAsync(2).ConfigureAwait(false));
      Assert.Equal("1", await cache.GetAsync(1).ConfigureAwait(false));
    }

    [Fact]
    public async void GetOrAddEvictLRU()
    {
      // make sure evic using corret LRU using GetOrAdd
      int sizeLimit = 100;
      var cache = new MemoryCache<int, string>((uint)sizeLimit);
      for (int i = 0; i < sizeLimit; i++)
      {
        await cache.GetOrAddAsync(i, () => Task.FromResult($"{i}")).ConfigureAwait(false);
      }

      Assert.Equal((int)sizeLimit, cache.Count);
      var last = sizeLimit + 1;
      await cache.GetOrAddAsync(last, () => Task.FromResult($"{last}")).ConfigureAwait(false);
      Assert.Equal((int)sizeLimit, cache.Count);

      // 0 should have been evicted since it was the oldest
      Assert.Null(await cache.GetAsync(0).ConfigureAwait(false));

      // check that Get will "protect" from eviction
      await cache.GetOrAddAsync(1, () => Task.FromResult("1")).ConfigureAwait(false);
      await cache.GetOrAddAsync(0, () => Task.FromResult("0")).ConfigureAwait(false);
      Assert.Null(await cache.GetAsync(2).ConfigureAwait(false));
      Assert.Equal("1", await cache.GetAsync(1).ConfigureAwait(false));
    }

    [Fact]
    public void GetOrAddParallel()
    {
      // make sure GetOrAdd returns the same value even when used from parallel multi threads
      int sizeLimit = 100;
      var cache = new MemoryCache<int, string>((uint)sizeLimit);
      var nThreads = 20;
      var threads = new ArrayList(nThreads);
      ArrayList results = new ArrayList(nThreads);
      for (int j = 1; j <= nThreads; j++)
      {
        var t = new Thread(() => Worker(cache, results));
        t.Start();
        threads.Add(t);
      }

      foreach (Thread thread in threads)
      {
        thread.Join();
      }

      // make sure all the threads got the same result from the GetOrAdd
      Assert.All(results.ToArray(), result => Assert.Equal(results[0], result));

      // make sure factory was only used once
      Assert.Equal(1, lazyCounter);
    }

    [Fact]
    public async void SetLazyEvaluation()
    {
      // make sure Lazy evaluation works with Set
      var cache = new MemoryCache<int, string>();
      var called = false;
      Func<Task<string>> factory = async () =>
      {
        called = true;
        return await Task.FromResult("value").ConfigureAwait(false);
      };

      cache.Set(0, factory);
      Assert.False(called);
      await cache.GetAsync(0).ConfigureAwait(false);
      Assert.True(called);
    }

    [Fact]
    public async void Remove()
    {
      // make sure remove works
      int sizeLimit = 100;
      var cache = SetupMemoryCache(sizeLimit);

      cache.Remove(50);
      Assert.Null(await cache.GetAsync(50).ConfigureAwait(false));
      Assert.Equal(sizeLimit - 1, cache.Count);
    }

    private static async void Worker(ICache<int, string> cache, ArrayList results)
    {
      var rand = new Random();

      Func<Task<string>> valueFactory = async () =>
      {
        Interlocked.Increment(ref lazyCounter);
        return await Task.FromResult($"{rand.Next()}").ConfigureAwait(false);
      };

      var ret = await cache.GetOrAddAsync(42, valueFactory).ConfigureAwait(false);
      results.Add(ret);
      Thread.Sleep(5);
    }

    private static MemoryCache<int, string> SetupMemoryCache(int sizeLimit = 100)
    {
      var cache = new MemoryCache<int, string>((uint)sizeLimit);
      for (int i = 0; i < sizeLimit; i++)
      {
        var value = $"{i}";
        cache.Set(i, () => Task.FromResult(value));
      }

      return cache;
    }

#pragma warning restore CA1822 // Mark members as static
  }
}
