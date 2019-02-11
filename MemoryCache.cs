
namespace MemoryCache
{
  using System;
  using System.Collections.Concurrent;
  using System.Linq;
  using System.Threading.Tasks;

  public class MemoryCache<TKey, TValue> : ICache<TKey, TValue>
    where TValue : class
  {
    private readonly object cacheLock = new object();
    private readonly ConcurrentDictionary<TKey, CacheEntry<Lazy<Task<TValue>>>> cache = new ConcurrentDictionary<TKey, CacheEntry<Lazy<Task<TValue>>>>();
    private uint sizeLimit = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCache{TKey, TValue}"/> class.
    /// if the cache is created with a size limit, uses LRU to decide which item to remove when the cache is full
    /// </summary>
    /// <param name="sizeLimit"> limit the number of items in the cache. 0 or no size limit means unlimited</param>
    public MemoryCache(uint sizeLimit = 0)
    {
      this.sizeLimit = sizeLimit;
    }

    public uint SizeLimit { get => this.sizeLimit; }

    public int Count { get => this.cache.Count; }

    public void SetSizeLimit(uint sizeLimit)
    {
      this.sizeLimit = sizeLimit;
      this.ManageSizeLimit();
    }

    public async virtual Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> itemFactory)
    {
      var lazyAsyncItemFactory = new Lazy<Task<TValue>>(itemFactory);
      var cacheEntry = new CacheEntry<Lazy<Task<TValue>>>(lazyAsyncItemFactory);
      var item = await this.cache.GetOrAdd(key, cacheEntry).Item.Value.ConfigureAwait(false);

      this.ManageSizeLimit();

      return item;
    }

    public void Remove(TKey key)
    {
      this.cache.TryRemove(key, out _);
    }

    public Task<TValue> GetAsync(TKey key)
    {
      CacheEntry<Lazy<Task<TValue>>> entry;
      var status = this.cache.TryGetValue(key, out entry);
      if (status)
      {
        return entry.Item.Value;
      }

      return Task.FromResult<TValue>(null);
    }

    public virtual void Set(TKey key, Func<Task<TValue>> itemFactory)
    {
      this.cache.TryAdd(key, new CacheEntry<Lazy<Task<TValue>>>(new Lazy<Task<TValue>>(itemFactory)));
      this.ManageSizeLimit();
    }

    private void Compact()
    {
      var key = this.GetLastUsedItemFromCache();
      this.Remove(key);
    }

    private void ManageSizeLimit()
    {
      if (this.sizeLimit > 0)
      {
        lock (this.cacheLock)
        {
          while (this.cache.Count > this.sizeLimit)
          {
            this.Compact();
          }
        }
      }
    }

    /// <summary>
    /// return the item with the oldest LastAccess field
    /// </summary>
    /// <returns>key of the oldest LastAccess item</returns>
    private TKey GetLastUsedItemFromCache()
    {
      var oldestUsedItem = this.cache.Aggregate((last, current) => last.Value.LastAccessed < current.Value.LastAccessed ? last : current);
      return oldestUsedItem.Key;
    }

    private class CacheEntry<T>
    {
      private readonly T item;

      public CacheEntry(T item)
      {
        this.item = item;
      }

      public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;

      public T Item
      {
        get
        {
          this.LastAccessed = DateTime.UtcNow;
          return this.item;
        }
      }
    }
  }
}