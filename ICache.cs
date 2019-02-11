namespace MemoryCache
{
  using System;
  using System.Threading.Tasks;

  public interface ICache<TKey, TValue>
    where TValue : class
  {
    /// <summary>
    /// Gets current size limit
    /// </summary>
    uint SizeLimit { get; }

    /// <summary>
    /// gets current number of items in the cache
    /// </summary>
    int Count { get; }

    /// <summary>
    /// get an item from the cache or null
    /// thread safe
    /// </summary>
    /// <param name="key"></param>
    /// <returns>the requested item or null</returns>
    Task<TValue> GetAsync(TKey key);

    /// <summary>
    /// saves an item to the cache. if the cache is full, removes and item first, according to the eviction policy
    /// thread safe
    /// </summary>
    /// <param name="key"></param>
    /// <param name="itemFactory"></param>
    void Set(TKey key, Func<Task<TValue>> itemFactory);

    /// <summary>
    /// remove an item from cache
    /// </summary>
    /// <param name="key"></param>
    void Remove(TKey key);

    /// <summary>
    /// get or add item from the cache.
    /// if the cache is full, removes an item first, according to the eviction policy
    /// thread safe
    /// </summary>
    /// <param name="key"></param>
    /// <param name="itemFactory">the factory to be used to create the item if it's not in the cache</param>
    /// <returns>the item from the cache</returns>
    Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> itemFactory);
  }
}