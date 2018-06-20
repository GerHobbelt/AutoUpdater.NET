using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;

namespace AutoUpdaterDotNET.BasicImpls
{
#pragma warning disable 1591
    public class BasicUpdateDownloader: UpdateDownloader
    {
        private readonly UpdateDownloadPresenter _presenter;
        private bool _allowCancellation;
        private IWebProxy _proxy;

        private string _downloadPath;
        private DownloadFinishHandler _finishHandler;
        private string _tempFile;
        private readonly ManualResetEvent _waitModal = new ManualResetEvent(false);

        public BasicUpdateDownloader(UpdateDownloadPresenter presenter, bool allowCancellation, IWebProxy proxy)
        {
            _presenter = presenter;
            _allowCancellation = allowCancellation;
            _proxy = proxy;
        }

        public virtual void Download(string fromUrl, string downloadPath, DownloadFinishHandler finishHandler)
        {
            var uri = new Uri(fromUrl);
            _downloadPath = downloadPath;
            _finishHandler = finishHandler;
            _waitModal.Reset();

            if (string.IsNullOrEmpty(_downloadPath))
                _tempFile = Path.GetTempFileName();
            else
            {
                _tempFile = Path.Combine(_downloadPath, $"{Guid.NewGuid().ToString()}.tmp");
                Directory.CreateDirectory(_downloadPath);
            }

            var webClient = SetupWebClient();
            webClient.DownloadFileAsync(uri, _tempFile);

            try
            {
                _presenter?.ShowModal();
            }
            catch {/*ignored*/}

            if (webClient.IsBusy) // presenter wasn't a good Modal :/
                ModallyWaitForDownloadCompleted();
        }

        private MyWebClient SetupWebClient()
        {
            var webClient = new MyWebClient { CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore) };

            if (_proxy != null)
                webClient.Proxy = _proxy;

            webClient.DownloadProgressChanged += (s, a) =>
            {
                try
                {
                    _presenter?.DownloadProgressChanged(a.BytesReceived, a.TotalBytesToReceive);
                }
                catch {/*ignored*/}
            };
            webClient.DownloadFileCompleted += WebClientOnDownloadFileCompleted;

            if (_presenter != null)
                _presenter.AllowCancellationDelegate = () =>
                {
                    if (!_allowCancellation) return false;
                    try
                    {
                        if (webClient.IsBusy)
                            webClient.CancelAsync();
                    }
                    catch {/*ignored*/}
                    return true;
                };
            return webClient;
        }

        private void ModallyWaitForDownloadCompleted()
        {
            var thread = new Thread(() => _waitModal.WaitOne());
            //thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private void WebClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _waitModal.Set();
            _allowCancellation = true;
            _presenter?.Close();

            if (args.Cancelled || args.Error != null)
            {
                _finishHandler(string.Empty, args.Error);
                return;
            }

            try
            {
                var webClient = (MyWebClient)sender;
                var updateFile = Path.Combine(string.IsNullOrEmpty(_downloadPath)
                                                    ? Path.GetTempPath()
                                                    : _downloadPath,
                                                GetUpdateFileName(webClient));
                if (File.Exists(updateFile))
                    File.Delete(updateFile);
                File.Move(_tempFile, updateFile);

                _finishHandler(updateFile, null);
            }
            catch (Exception e)
            {
                _finishHandler(string.Empty, e);
            }
        }

        private static string GetUpdateFileName(MyWebClient webClient)
        {
            var contentDisposition = webClient.ResponseHeaders["Content-Disposition"] ?? string.Empty;

            if (string.IsNullOrEmpty(contentDisposition))
                return Path.GetFileName(webClient.ResponseUri.LocalPath);

            var fileName = TryToFindFileName(contentDisposition, "filename=");
            if (string.IsNullOrEmpty(fileName))
                fileName = TryToFindFileName(contentDisposition, "filename*=UTF-8''");
            return fileName;
        }

        private static string TryToFindFileName(string contentDisposition, string lookForFileName)
        {
            var fileName = string.Empty;
            if (string.IsNullOrEmpty(contentDisposition)) return fileName;
            var index = contentDisposition.IndexOf(lookForFileName, StringComparison.CurrentCultureIgnoreCase);
            if (index >= 0)
                fileName = contentDisposition.Substring(index + lookForFileName.Length);
            if (fileName.StartsWith("\""))
            {
                var file = fileName.Substring(1, fileName.Length - 1);
                var i = file.IndexOf("\"", StringComparison.CurrentCultureIgnoreCase);
                if (i != -1)
                    fileName = file.Substring(0, i);
            }
            return fileName;
        }


        private class MyWebClient : WebClient
        {
            /// <summary>
            ///     Response Uri after any redirects.
            /// </summary>
            public Uri ResponseUri;

            /// <inheritdoc />
            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                var webResponse = base.GetWebResponse(request, result);
                ResponseUri = webResponse.ResponseUri;
                return webResponse;
            }
        }
    }
#pragma warning restore 1591
}