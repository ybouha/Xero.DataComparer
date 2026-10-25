using Xero.DataComparer.Core;

namespace Xero.DataComparer.Tests;

public class PooledDictionaryTests
{
    [Fact]
    public void Grows_from_any_capacity()
    {
        var dictionary = new PooledDictionary<string, int>(0);
        for (int i = 0; i < 10; i++) dictionary.Add($"k{i}", i);

        Assert.Equal(10, dictionary.Count);
        Assert.Equal(7, dictionary["k7"]);
    }

    [Fact]
    public void Rejects_a_key_already_present()
    {
        var dictionary = new PooledDictionary<string, int>();
        dictionary.Add("a", 1);

        Assert.Throws<ArgumentException>(() => dictionary.Add("a", 2));
        Assert.Equal(1, dictionary["a"]);
    }

    [Fact]
    public void Missing_keys()
    {
        var dictionary = new PooledDictionary<string, int>();

        Assert.False(dictionary.TryGetValue("a", out _));
        Assert.Throws<KeyNotFoundException>(() => dictionary["a"]);
    }
}
