using System;
using System.Diagnostics;
using System.Web;
using System.Collections.Generic;

namespace CacheFactories
{

  /// <summary>
  /// Provides generic methods to get items from a per-request cache and populate the cache
  /// </summary>
  /// 
  /// <remarks>
  /// This cache is dependent upon HttpContext.Current and thus is only significant when 
  /// used in an ASP.NET process. Also, the cache is scoped to each request and only lives
  /// for the duration of the request. 
  /// </remarks>  
  public class RequestCacheFactory
  {

    //NOTE: Items are cached only for the duration of an HttpRequest and are scoped to that request!

    #region Attributes

    private static readonly object nullValue = new object();
    private static readonly object currentlyInProgress = new object();

    #endregion

    #region Constructors

    /// <summary>
    /// Type initializer that enables or disables caching 
    /// </summary>
    static RequestCacheFactory()
    {
      //Disable caching if there's no current HttpContext, e.g., in unit tests
      CacheEnabled = (HttpContext.Current != null);

      if (!CacheEnabled)
      {
        Debug.WriteLine("RequestCacheFactory is not available",
                        "REQUEST_CACHE");
      }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns a value indicating if caching is enabled
    /// </summary>    
    internal static bool CacheEnabled
    {
      get;
      private set;
    }

    #endregion

    /// <summary>
    /// Gets an item from the per-request cache by its key
    /// </summary>
    /// <typeparam name="T">Set to the Type of the cached item</typeparam>    
    /// <param name="cacheKey">Set to a unique CacheKey to identify the item in cache</param>    
    /// <param name="loadItemMethod">Set to a Func that will load the item if it's not cached</param>
    /// 
    /// <remarks>This method will populate the cache if the requested item does not exist.</remarks>
    /// 
    /// <returns>The item from cache</returns>
    public static T GetCachedItem<T>(string cacheKey,
                                     Func<T> loadItemMethod) where T : class
    {
      try
      {
        if (!CacheEnabled)
        {
          return loadItemMethod();
        }

        //Attempt to load the item from cache
        object cacheValue = HttpContext.Current.Items[cacheKey];

        //Insert the data if it doesn't exist in cache
        if (cacheValue == null)
        {
          //Fetch the item by the loadItemMethod Func
          cacheValue = loadItemMethod();

          //Check if the load method returns null
          if (cacheValue == null)
          {
            //Insert the null substitute into cache
            HttpContext.Current.Items.Add(cacheKey,
                                          nullValue);
          }
          else
          {
            //Insert into cache
            HttpContext.Current.Items.Add(cacheKey,
                                          cacheValue);
          }
        }
        else
        {

          //Log a successful hit to cache - only in Debug mode
          Debug.WriteLine("RequestCacheFactory:" + cacheKey,
                      "REQUEST_CACHE");

        }

        //If the cached value represents null, return null
        if (cacheValue == nullValue)
        {
          return null;
        }

        //Otherwise return the cached value, cast to desired type
        return (cacheValue as T);
      }
      catch (Exception exc)
      {
        const string errorMessage = "Error in RequestCacheFactory.GetCachedItem with cacheKey({0}) and type({1})";

        //Log all errors that pass through the cache with the key. 
        // This could create multiple log entries for a single error. 
        // That's an OK tradeoff to always get the cache key logged.
        Trace.WriteLine(string.Format(errorMessage, cacheKey, typeof (T).FullName) + ":" + exc.Message,
                        "REQUEST_CACHE");
        throw;
      }
    }

    /// <summary>
    /// Gets an item from the per-request cache by its key
    /// </summary>
    /// <typeparam name="T">Set to the Type of the cached item</typeparam>    
    /// <param name="cacheKey">Set to a unique CacheKey to identify the item in cache</param>    
    /// <param name="loadItemMethod">Set to a Func that will load the item if it's not cached</param>
    /// 
    /// <remarks>This method will populate the cache if the requested item does not exist.</remarks>
    /// 
    /// <returns>The item from cache</returns>
    public static T GetCachedEvaluator<T>(string cacheKey,
                                          Func<T> loadItemMethod) where T : class
    {
      if (!CacheEnabled)
      {
        return loadItemMethod();
      }

      //Attempt to load the item from cache
      object cacheValue = HttpContext.Current.Items[cacheKey];

      //Insert the data if it doesn't exist in cache
      if (cacheValue == null)
      {
        // Because this factory method is being used to leverage cycle detection in Master Parameter Evaluators, we need to seed the cache
        // with a token indicating that an evaluation is currently in progress for the given key and throw an exception in that case,
        // which we catch in the evaluator

        HttpContext.Current.Items.Add(cacheKey,
                                      currentlyInProgress);

        //Fetch the item by the loadItemMethod Func
        cacheValue = loadItemMethod();

        //Check if the load method returns null, if so return the null substitute
        HttpContext.Current.Items[cacheKey] = cacheValue ?? nullValue;
      }
      //      else
      //      {

      //#if DEBUG
      //        //Log a successful hit to cache - only in Debug mode
      //        LoggerServices.Write("RequestCacheFactory:" + cacheKey,
      //                             LoggerServices.enuLogPriorities.CACHED_QUERY);
      //#endif

      //      }

      //If the cached value represents null, return null
      if (cacheValue == nullValue)
      {
        return null;
      }
      else if (cacheValue == currentlyInProgress)
      {
        throw new InvalidOperationException("Currently in the progress of evaluating " + cacheKey + ". This indicative of a cycle.");
      }

      //Otherwise return the cached value, cast to desired type
      return (cacheValue as T);
    }

    /// <summary>
    /// Clears all items in the cache that start with the given key prefix
    /// </summary>
    /// <param name="cacheKeyPrefix">The prefix of the key to remove</param>
    public static void ClearCachedItem(string cacheKeyPrefix)
    {
      List<string> keysToRemove = new List<string>();

      foreach (string key in HttpContext.Current.Items.Keys)
      {
        if (key.StartsWith(cacheKeyPrefix))
        {
          keysToRemove.Add(key);
        }
      }

      foreach (string key in keysToRemove)
      {
        HttpContext.Current.Items.Remove(key);
      }
    }

    /// <summary>
    /// Gets an item from the per-request cache by either of its two keys
    /// </summary>
    /// <typeparam name="T">Set to the Type of the cached item</typeparam>    
    /// <param name="cacheKey1">Set to a unique CacheKey to identify the item in cache</param>
    /// <param name="cacheKey2">Set to a unique CacheKey to identify the item in cache</param> 
    /// <param name="loadItemMethod">Set to a Func that will load the item if it's not cached</param>
    /// 
    /// <remarks>This method will populate the cache if the requested item does not exist.</remarks>
    /// 
    /// <returns>The item from cache</returns>
    public static T GetCachedItem<T>(string cacheKey1,
                                     string cacheKey2,
                                     Func<T> loadItemMethod) where T : class
    {
      try
      {
        if (!CacheEnabled)
        {
          return loadItemMethod();
        }

        //Attempt to load the item from cache
        T result1 = (HttpContext.Current.Items[cacheKey1] as T);
        T result2 = (HttpContext.Current.Items[cacheKey2] as T);

        T result;

        if (result1 == null && result2 != null)
        { // item was found for key #2 and not #1; return result for key #2 and cache it under key #1 as well
          result = result2;
          HttpContext.Current.Items.Add(cacheKey1,
                                        result);
        }
        else if (result1 != null && result2 == null)
        { // item was found for key #1 and not #2; return result for key #1 and cache it under key #2 as well
          result = result1;
          HttpContext.Current.Items.Add(cacheKey2,
                                        result);
        }
        else if (result1 == null && result2 == null)
        {
          //Fetch the item by the loadItemMethod Func
          result = loadItemMethod();

          //Insert into cache
          HttpContext.Current.Items.Add(cacheKey1,
                                        result);
          HttpContext.Current.Items.Add(cacheKey2,
                                        result);
        }
        else
        { // otherwise both exist in the cache and should - in theory - be the same object. select either one and return

          //Log a successful hit to cache - only in Debug mode
          Debug.WriteLine("RequestCacheFactory:" + cacheKey1 + " | " + cacheKey2,"REQUEST_CACHE");

          result = result1; // arbitrary - could be either result1 or result2
        }

        return result;
      }
      catch (Exception exc)
      {
        const string errorMessage = "Error in RequestCacheFactory.GetCachedItem with cacheKeys({0} | {1}) and type({2})";

        //Log all errors that pass through the cache with the key. 
        // This could create multiple log entries for a single error. 
        // That's an OK tradeoff to always get the cache key logged.
        Trace.WriteLine(string.Format(errorMessage, cacheKey1, cacheKey2, typeof (T).FullName) + ":" + exc.Message,
                        "REQUEST_CACHE");
        throw;
      }
    }

    /// <summary>
    /// Removes the specified item from the cache
    /// </summary>
    /// <param name="cacheKey">The key of the item to remove from the cache</param>
    public static void RemoveCachedItem(string cacheKey)
    {
      try
      {
        if (!CacheEnabled)
        {
          return; // do nothing as there should be no cache at this point
        }

        // Attempt to remove item from cache
        HttpContext.Current.Items.Remove(cacheKey);
      }
      catch (Exception exc)
      {
        const string errorMessage = "Error in RequestCacheFactory.RemoveCachedItem with cacheKey({0}))";

        //Log all errors that pass through the cache with the key. 
        // This could create multiple log entries for a single error. 
        // That's an OK tradeoff to always get the cache key logged.
        Trace.Write(string.Format(errorMessage, cacheKey) + ":" + exc.Message,
                    "REQUEST_CACHE");
        throw;
      }
    }
  }
}
