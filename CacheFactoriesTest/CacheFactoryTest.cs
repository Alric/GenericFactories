using CacheFactories;
using NUnit.Framework;
using System;

namespace CacheFactoriesTest
{

  /// <summary>
  ///This is a test class for CacheFactoryTest and is intended
  ///to contain all CacheFactoryTest Unit Tests
  ///</summary>
  [TestFixture()]
  public class CacheFactoryTest
  {
    private const string CACHE_MANAGER = "TEST";

    [Test]
    public void GetCachedItem_String_Test()
    {
      const string TEST_STRING = "Frodo Lives!";

      string target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "STRING_KEY"),
                                                 () => TEST_STRING);
      Assert.AreEqual(TEST_STRING, target);
    }

    [Test]
    public void GetCachedItem_Object_Test()
    {
      object expected = new object();

      object target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "OBJECT_KEY"),
                                                 () => expected);
      Assert.AreEqual(expected, target);
    }

    [Test]
    public void GetCachedItem_Null_Test()
    {
      object target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "NULL_KEY"),
                                     () => (null as object));
      Assert.IsNull(target);
    }

    [Test]
    public void GetCachedItem_MultipleReads_Test()
    {
      const string TEST_STRING = "Frodo Lives!";
      int numberOfMethodExecutions = 0;

      Func<string> getMethod = () =>
                                 {
                                   numberOfMethodExecutions++;
                                   return TEST_STRING;
                                 };
      
      //First read
      string target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "MULTI_KEY"),
                                                 getMethod);
      Assert.AreEqual(TEST_STRING, target);
      Assert.AreEqual(1, numberOfMethodExecutions);

      //Second read
      target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "MULTI_KEY"),
                                          getMethod);
      Assert.AreEqual(TEST_STRING, target);
      Assert.AreEqual(1, numberOfMethodExecutions, "getMethod should still ony be executed once.");
    }

    [Test]
    public void GetCachedItem_MultipleReadsNull_Test()
    {      
      int numberOfMethodExecutions = 0;

      Func<object> getMethod = () =>
      {
        numberOfMethodExecutions++;
        return null;
      };

      //First read
      object target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "MULTI_NULL_KEY"),
                                                 getMethod);
      Assert.IsNull(target);
      Assert.AreEqual(1, numberOfMethodExecutions);

      //Second read
      target = CacheFactory.GetCachedItem(CacheFactory.GetCacheKey(CACHE_MANAGER, "MULTI_NULL_KEY"),
                                          getMethod);
      Assert.IsNull(target);
      Assert.AreEqual(1, numberOfMethodExecutions, "getMethod should still ony be executed once.");
    }


  }
}
