using CollaborativeEditing;
using Microsoft.Extensions.Caching.Memory;

namespace dg_text_editor_poc_backend
{
    public interface IDocumentProvider
    {
        public Document Get();
    }

    public class MemoryCacheDocumentProvider: IDocumentProvider
    {
        private const string CacheKey = "documentContext";
        private const string SampleJson = @"[{""type"":""paragraph"",""children"":[{""text"":""1. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.""}]},{""type"":""paragraph"",""children"":[{""text"":""2. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.""}]},{""type"":""paragraph"",""children"":[{""text"":""3. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.""}]}]";
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheDocumentProvider(IMemoryCache memoryCache) =>
            _memoryCache = memoryCache;
        
        public Document Get()
        {
            if (_memoryCache.TryGetValue(CacheKey, out Document? documentContext) && documentContext != null)
            {
                return documentContext;
            }

            var newDocumentContext = new Document(0, SampleJson);
            _memoryCache.Set(CacheKey, newDocumentContext);

            return newDocumentContext;
        }
    }
}


