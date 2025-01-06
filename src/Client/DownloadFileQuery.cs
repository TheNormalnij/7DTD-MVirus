using MVirus.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client
{
    public class DownloadFileQuery
    {
        public List<ServerFileInfo> list;

        public DownloadFileQuery(List<ServerFileInfo> _list)
        {
            list = _list;
        }

        public long CalculateDownloadSize()
        {
            return list.Aggregate(0L, (size, item) => size + item.Size);
        }

        public bool IsFileListSafe()
        {
            return !list.Exists(item => !PathUtils.IsSafeClientFilePath(item.Path));
        }

        public async Task FilterLocalFiles(string cachePath, CancellationToken cancellationToken, Action<ServerFileInfo> exists)
        {
            list = await CacheScanner.FilterLocalFiles(list, cachePath, cancellationToken, exists);
        }
    }
}
