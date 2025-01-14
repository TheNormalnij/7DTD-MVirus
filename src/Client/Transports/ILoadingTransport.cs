using System;
using System.Threading;
using System.Threading.Tasks;
using MVirus.ModInfo;

namespace MVirus.Client.Transports
{
    public interface ILoadingTransport
    {
        void OnDownloadCanceled();

        Task DownloadFileAsync(ServerFileInfo fileInfo, string outPath, CancellationToken cancellationToken, Action<int> progressReporter);
    }
}
