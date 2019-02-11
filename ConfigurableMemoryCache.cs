namespace MemoryCache
{
  using System;
  using System.Threading.Tasks;

  public class ConfigurableMemoryCache<TKey, TValue> : MemoryCache<TKey, TValue>
    where TValue : class
  {
    private readonly string configurationSectionName;

    private readonly IConfigurationProvider configurationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableMemoryCache{TKey, TValue}"/> class.
    /// this class allows to use MemoryCacheLazy with a IConfigurationProvider
    /// </summary>
    /// <param name="configurationProvider"></param>
    /// <param name="configurationSectionName"></param>
    public ConfigurableMemoryCache(IConfigurationProvider configurationProvider, string configurationSectionName)
      : base()
    {
      this.configurationSectionName = configurationSectionName;
      this.configurationProvider = configurationProvider
      this.ReloadConfiguration();
    }

    public override Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> itemFactory)
    {
      this.ReloadConfiguration();
      return base.GetOrAddAsync(key, itemFactory);
    }

    public override void Set(TKey key, Func<Task<TValue>> itemFactory)
    {
      this.ReloadConfiguration();
      base.Set(key, itemFactory);
    }

    private void ReloadConfiguration()
    {
      this.SetSizeLimit(this.configurationProvider.Get(this.configurationSectionName).Convert<MemoryCacheConfiguration>().SizeLimit);
    }

    private class MemoryCacheConfiguration
    {
      public uint SizeLimit { get; set; } = 60;
    }
  }
}
