
namespace CacheFactories
{

  /// <summary>
  /// Encapsulates components of a key used to uniquely identify an item being stored in cache
  /// </summary>
  public class CacheKey
  {
    #region Attributes

    #endregion

    #region Properties

    /// <summary>
    /// Used to uniquely identify the source of the cache call
    /// </summary>
    private string CacheManager { get; set; }

    /// <summary>
    /// Used to uniquely idenfity the method accessing the cache
    /// </summary>
    private string MethodKey { get; set; }

    /// <summary>
    /// Used to uniquely identify an item when a method can retrieve varying items, 
    /// for example the Id of the item being retrieved
    /// </summary>
    private string ItemKey { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor only accessible insde the class
    /// </summary>
    private CacheKey()
    {
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a CacheKey with the specified CacheManager and MethodKey
    /// </summary>
    /// <param name="cacheManager">Set to the originator of the cache operation</param>
    /// <param name="methodKey">Set to the method accessing the cache</param>
    /// <returns>A CacheKey with the argument values set</returns>
    public static CacheKey Create(string cacheManager, string methodKey)
    {
      return new CacheKey { CacheManager = cacheManager, MethodKey = methodKey };
    }

    /// <summary>
    /// Creates a CacheKey with the specified CacheManager, MethodKey, and ItemKey
    /// </summary>
    /// <param name="cacheManager">Set to the originator of the cache operation</param>
    /// <param name="methodKey">Set to the method accessing the cache</param>
    /// <param name="itemKey">Set to a unique identifier for the item being retrieved from cache</param>
    /// <returns>A CacheKey with the argument values set</returns>
    public static CacheKey Create<T>(string cacheManager, string methodKey, T itemKey)
    {
      return new CacheKey { CacheManager = cacheManager, MethodKey = methodKey, ItemKey = itemKey.ToString() };
    }

    /// <summary>
    /// Creates a string representation of the CacheKey
    /// </summary>
    /// <returns>A string representation of the CacheKey</returns>
    public override string ToString()
    {
      //Do not return the ItemKey if it is null or empty
      if (string.IsNullOrEmpty(ItemKey))
      {
        return CacheManager + "." + MethodKey;
      }

      //Otherwise return the full cache key
      return CacheManager + "." + MethodKey + ":" + ItemKey;
    }

    #endregion
  }
}