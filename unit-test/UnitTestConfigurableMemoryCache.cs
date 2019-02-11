namespace MemoryCache.XUnitTests
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using MemoryCache
  using Moq;
  using Xunit;

  public class UnitTestConfigurableMemoryCache
  {
    // see https://github.com/dotnet/roslyn-analyzers/issues/1399
#pragma warning disable CA1822 // Mark members as static

    private const string ConfigSectionName = "MemoryCache";

    [Fact]
    public void NullConfigProvider()
    {
      Assert.Throws<ArgumentNullException>(() => new ConfigurableMemoryCache<int, string>(null, ConfigSectionName));
    }

    [Fact]
    public void NoConfigurationSectionDefined()
    {
      var configMock = new Mock<IConfigurationProvider>();
      configMock.Setup(config => config.Get(ConfigSectionName)).Throws(new KeyNotFoundException());
      Assert.Throws<KeyNotFoundException>(() => new ConfigurableMemoryCache<int, string>(configMock.Object, ConfigSectionName));
    }

    [Fact]
    public void SetSizeLimit()
    {
      var configMock = new Mock<IConfigurationProvider>();
      var configurationDic = new Dictionary<string, string>();
      configurationDic["SizeLimit"] = "20";
      configMock.Setup(config => config.Get(ConfigSectionName)).Returns(configurationDic);
      var cache = new ConfigurableMemoryCache<int, string>(configMock.Object, ConfigSectionName);
      Assert.Equal(20U, cache.SizeLimit);
    }

    [Fact]
    public async void UnitTestUpdateSizeLimitDynamicallyWithGetOrAdd()
    {
      // setup
      var configurationProviderMock = new Mock<IConfigurationProvider>();
      configurationProviderMock.Setup(x => x.Get(ConfigSectionName)).Returns(new Dictionary<string, string>() { { "SizeLimit", "2" } });

      // run
      var configurableMemoryCache = new ConfigurableMemoryCache<int, string>(configurationProviderMock.Object, ConfigSectionName);
      await configurableMemoryCache.GetOrAddAsync(1, () => Task.FromResult("test1")).ConfigureAwait(false);
      await configurableMemoryCache.GetOrAddAsync(2, () => Task.FromResult("test2")).ConfigureAwait(false);
      await configurableMemoryCache.GetOrAddAsync(3, () => Task.FromResult("test3")).ConfigureAwait(false);

      // change configuration dynamically
      configurationProviderMock.Setup(x => x.Get(ConfigSectionName)).Returns(new Dictionary<string, string>() { { "SizeLimit", "3" } });
      await configurableMemoryCache.GetOrAddAsync(4, () => Task.FromResult("test4")).ConfigureAwait(false);

      // assert
      Assert.Equal(3, configurableMemoryCache.Count);
      Assert.Null(await configurableMemoryCache.GetAsync(1).ConfigureAwait(false));
      Assert.Equal("test2", await configurableMemoryCache.GetAsync(2).ConfigureAwait(false));
      Assert.Equal("test3", await configurableMemoryCache.GetAsync(3).ConfigureAwait(false));
      Assert.Equal("test4", await configurableMemoryCache.GetAsync(4).ConfigureAwait(false));
    }

#pragma warning restore CA1822 // Mark members as static
  }
}
