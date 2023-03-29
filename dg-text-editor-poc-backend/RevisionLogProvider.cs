using CollaborativeEditing;
using Microsoft.Extensions.Caching.Memory;

namespace dg_text_editor_poc_backend
{
    public interface IRevisionLogProvider
    {
        public List<OperationBatch> Get();
    }

    public class RevisionLogProvider : IRevisionLogProvider
    {
        private const string CacheKey = "revisionLog";
        private readonly IMemoryCache _memoryCache;

        public RevisionLogProvider(IMemoryCache memoryCache) =>
            _memoryCache = memoryCache;

        public List<OperationBatch> Get()
        {
            if (_memoryCache.TryGetValue(CacheKey, out List<OperationBatch>? revisionLog) && revisionLog != null)
            {
                return revisionLog;
            }

            var newRevisionLog = new List<OperationBatch>();
            _memoryCache.Set(CacheKey, newRevisionLog);

            return newRevisionLog;
        }
    }
}