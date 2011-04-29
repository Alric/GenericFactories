using System;
using System.Diagnostics;
using System.Web;
using System.Web.Caching;

namespace CacheFactories
{
  /// <summary>
  /// Provides generic methods to get items from cache and populate the cache
  /// </summary>
  public class CacheFactory
  {
    /// <summary>
    /// Reusable TimeSpan for a two minute absolute cache duration
    /// </summary>
    public static readonly TimeSpan TWO_MINUTE_DURATION = new TimeSpan(0, 2, 0);

    private static readonly TimeSpan DEFAULT_ABSOLUTE_EXPIRATION = new TimeSpan(0, 15, 0);

    /// <summary>
    /// Acts as a placeholder to insert into the cache when a cached operation returns null
    /// </summary>
    private static readonly object NULL_OBJECT_PLACEHOLDER = new object();

    /// <summary>
    /// Lists the types of cache expirations
    /// </summary>
    public enum enuExpirationType
    {
      /// <summary>
      /// The cached item will never expire, used when dependencies are specified or 
      /// if an item should be cached forever until application restart.
      /// </summary>
      NONE = 0,

      /// <summary>
      /// The cached item will expire at a specific time or 
      /// when the dependencies are invalidated.
      /// </summary>
      ABSOLUTE = 1,

      /// <summary>
      /// The cached item will expire after a certain amount of time passes
      /// while the item has not been accessed. Each access of the cached item 
      /// will reset the expiration timeout.
      /// </summary>
      SLIDING = 2
    }

    private static bool mCacheEnabled = true;

    /// <summary>
    /// Returns a value indicating if caching is enabled
    /// </summary>
    public static bool CacheEnabled
    {
      get { return mCacheEnabled; }
      set { mCacheEnabled = value; }
    }

    #region Methods

    /// <summary>
    /// Gets an item from cache by its key
    /// </summary>
    /// <typeparam name="T">Set to the Type of the cached item</typeparam>    
    /// <param name="key">Set to a unique CacheKey to identify the item in cache</param>
    /// <param name="loadItemMethod">Set to a Func that will load the item if it's not cached</param>
    /// 
    /// <remarks>This method will populate the cache if the requested item does not exist.</remarks>
    /// 
    /// <returns>The item from cache</returns>
    public static T GetCachedItem<T>(CacheKey key, Func<T> loadItemMethod) where T : class
    {
      //Default to 15 minute absolute expiration
      return GetCachedItem(key,
                           DEFAULT_ABSOLUTE_EXPIRATION,
                           loadItemMethod);
    }

    /// <summary>
    /// Gets an item from cache by its key
    /// </summary>
    /// <typeparam name="T">Set to the Type of the cached item</typeparam>    
    /// <param name="key">Set to a unique CacheKey to identify the item in cache</param>
    /// <param name="absoluteExpiration">Set to timespan until the cached item should expire</param>
    /// <param name="loadItemMethod">Set to a Func that will load the item if it's not cached</param>
    /// 
    /// <remarks>This method will populate the cache if the requested item does not exist.</remarks>
    /// 
    /// <returns>The item from cache</returns>
    public static T GetCachedItem<T>(CacheKey key, TimeSpan absoluteExpiration, Func<T> loadItemMethod) where T : class
    {
      return GetCachedItem(key,
                           null,
                           enuExpirationType.ABSOLUTE,
                           absoluteExpiration,
                           loadItemMethod);
    }

    /// <summary>
    /// Gets an item from cache by its key
    /// </summary>
    /// <typeparam name="T">Set to the Type of the cached item</typeparam>    
    /// <param name="key">Set to a unique CacheKey to identify the item in cache</param>
    /// <param name="dependencies">Set to the dependencies that will invalidate the cached item</param>    
    /// <param name="expirationType">Set to the expiration type to use</param>
    /// <param name="expirationTime">Set to the expiration timeout, how long until the cached item expires</param>
    /// <param name="loadItemMethod">Set to a Func that will load the item if it's not cached</param>
    /// 
    /// <remarks>This method will populate the cache if the requested item does not exist.</remarks>
    /// 
    /// <returns>The item from cache</returns>
    public static T GetCachedItem<T>(CacheKey key,
                                     CacheDependency dependencies,
                                     enuExpirationType expirationType,
                                     TimeSpan expirationTime,
                                     Func<T> loadItemMethod) where T : class
    {
      try
      {
        //If the cache is not enabled, perform the load method and bypass caching
        if (!CacheEnabled)
        {
          return loadItemMethod();
        }

        //Attempt to load the item from cache
        T result = (HttpRuntime.Cache[key.ToString()] as T);

        //If the cached value is the null placeholder, return null
        if (result == NULL_OBJECT_PLACEHOLDER)
        {
          return null;
        }

        //Insert the data if it doesn't exist in cache
        if (result == null)
        {
          DateTime absoluteExpiration;
          TimeSpan slidingExpiration;

          //Select the appropriate expiration combination
          GetCacheExpiration(expirationType, expirationTime,
                             out absoluteExpiration, out slidingExpiration);

          //Fetch the item by the loadItemMethod Func
          result = loadItemMethod();

          //If the loadItemMethod returns null, use the null placeholder
          if (result == null)
          {
            //Insert null placeholder into cache
            HttpRuntime.Cache.Insert(key.ToString(),
                                     NULL_OBJECT_PLACEHOLDER,
                                     dependencies,
                                     absoluteExpiration,
                                     slidingExpiration);
          }
          else
          {
            //Insert result into cache
            HttpRuntime.Cache.Insert(key.ToString(),
                                     result,
                                     dependencies,
                                     absoluteExpiration,
                                     slidingExpiration);
          }

        }
        else
        {

          //Log a successful hit to cache, only in debug mode
          Debug.WriteLine("Successful cache hit: " + key, "CACHE");
        }

        return result;
      }
      catch (Exception exc)
      {
        const string errorMessage = "Error in CacheFactory.GetCachedItem with cacheKey({0}) and type({1})";

        //Log all errors that pass through the cache with the key. 
        // This could create multiple log entries for a single error. 
        // That's an OK tradeoff to always get the cache key logged.
        Trace.Write(string.Format(errorMessage, key, typeof(T).FullName) + ":" + exc.Message,"CACHE");
        throw;
      }
    }

    /// <summary>
    /// Returns a key uniquely identifying the cached content
    /// </summary>
    /// <typeparam name="T">Set to the type of item key</typeparam>
    /// <param name="cacheManager">Set to the name of the cache manager, used to identify source for logging</param>
    /// <param name="itemKey">Set to a key uniquely identifying a specific cached item for a method</param>
    /// <param name="methodKey">Set to the method key</param>
    /// <returns>A string uniquely identifying the cached content</returns>
    public static CacheKey GetCacheKey<T>(string cacheManager, string methodKey, T itemKey)
    {
      return CacheKey.Create(cacheManager, methodKey, itemKey);
    }

    /// <summary>
    /// Returns a key uniquely identifying the cached content
    /// </summary>     
    /// <param name="cacheManager">Set to the name of the cache manager, used to identify source for logging</param>
    /// <param name="methodKey">Set to the method key</param>
    /// <returns>A string uniquely identifying the cached content</returns>
    public static CacheKey GetCacheKey(string cacheManager, string methodKey)
    {
      return CacheKey.Create(cacheManager, methodKey);
    }

    /// <summary>
    /// Determines the appropriate expiration values for the specified type and time
    /// </summary>
    /// <param name="expirationType">Expiration type to use</param>
    /// <param name="expirationTime">Time to expire</param>
    /// <param name="absoluteExpiration">Contains the value to use for absolute expiration</param>
    /// <param name="slidingExpiration">Contains the value to use for sliding expiration</param>
    private static void GetCacheExpiration(enuExpirationType expirationType,
                                           TimeSpan expirationTime,
                                           out DateTime absoluteExpiration,
                                           out TimeSpan slidingExpiration)
    {
      switch (expirationType)
      {
        case enuExpirationType.ABSOLUTE:
          absoluteExpiration = DateTime.UtcNow.Add(expirationTime);
          slidingExpiration = Cache.NoSlidingExpiration;
          break;

        case enuExpirationType.SLIDING:
          absoluteExpiration = Cache.NoAbsoluteExpiration;
          slidingExpiration = expirationTime;
          break;

        default:
          absoluteExpiration = Cache.NoAbsoluteExpiration;
          slidingExpiration = Cache.NoSlidingExpiration;
          break;
      }
    }

    /// <summary>
    /// Clears a cache item
    /// </summary>
    /// <param name="itemKey">The item key</param>
    public static void ClearCacheKey(CacheKey itemKey)
    {
      //Attempt to load the item from cache
      object item = (HttpRuntime.Cache[itemKey.ToString()]);

      //If the item is not cached, return
      if (item != null)
      {
        HttpRuntime.Cache.Remove(itemKey.ToString());
      }
    }

    #endregion

  }
}
