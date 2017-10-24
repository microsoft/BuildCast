// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using BuildCast.DataModel;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.Background;
using Windows.Networking.BackgroundTransfer;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;

namespace BuildCast.Helpers
{
    public enum DownloadStartResult
    {
        Started,
        Error,
        AllreadyDownloaded,
    }

    public static class BackgroundDownloadHelper
    {
        private static ToastNotifier _notifier = ToastNotificationManager.CreateToastNotifier();

        public static async Task<DownloadStartResult> Download(Uri sourceUri)
        {
            var hash = SafeHashUri(sourceUri);
            var file = await CheckLocalFileExistsFromUriHash(sourceUri);

            var downloadingAlready = await IsDownloading(sourceUri);

            if (file == null && !downloadingAlready)
            {
                await Task.Run(() =>
                {
                    var task = StartDownload(sourceUri, BackgroundTransferPriority.High, hash);
                    task.ContinueWith((state) =>
                    {
                        if (state.Exception != null)
                        {
                            DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                            {
                                await UIHelpers.ShowContentAsync($"An error occured with this download {state.Exception}");
                            });
                        }
                        else
                        {
                            Debug.WriteLine("Download Completed");
                        }
                    });
                });
                return DownloadStartResult.Started;
            }
            else if (file != null)
            {
                await SetItemDownloaded(hash);
                await UIHelpers.ShowContentAsync($"Already downloaded.");
                return DownloadStartResult.AllreadyDownloaded;
            }
            else
            {
                return DownloadStartResult.Error;
            }
        }

        public static async Task<bool> IsDownloading(Uri sourceUri)
        {
            var downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            if (downloads.Where(dl => dl.RequestedUri == sourceUri).FirstOrDefault() != null)
            {
                return true;
            }

            return false;
        }

        public static async Task<StorageFile> CheckLocalFileExistsFromUriHash(Uri sourceUri)
        {
            string hash = SafeHashUri(sourceUri);
            return await CheckLocalFileExists(hash);
        }

        public static async Task<IReadOnlyList<IStorageItem>> GetAllFiles()
        {
            var files = await ApplicationData.Current.LocalCacheFolder.GetItemsAsync();
            return files;
        }

        public static string SafeHashUri(Uri sourceUri)
        {
            string safeUri = sourceUri.ToString().ToLower();
            var hash = Hash(safeUri);
            return hash;
        }

        public static async Task UpdateDownloadedFromCache()
        {
            using (LocalStorageContext c = new LocalStorageContext())
            {
                var foundInDb = c.EpisodeCache.FirstOrDefault();
                foundInDb.IsDownloaded = true;
                await c.SaveChangesAsync();
            }
        }

        public static void RegisterBackgroundTask(IBackgroundTrigger trigger)
        {
            var builder = new BackgroundTaskBuilder();
            builder.Name = "DownloadCompleteTrigger";
            builder.SetTrigger(trigger);

            BackgroundTaskRegistration task = builder.Register();
        }

        public static async Task AttachToDownloads()
        {
            var downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            foreach (var download in downloads)
            {
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                await download.AttachAsync().AsTask(progressCallback);
            }
        }

        /// <summary>
        /// Deletes the downloaded file.
        /// </summary>
        /// <param name="fileName">filename</param>
        /// <returns>Task</returns>
        public static async Task DeleteDownload(string fileName)
        {
            var fileItem = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(fileName);
            if (fileItem != null)
            {
                await fileItem.DeleteAsync();
            }
        }

        /// <summary>
        /// This will be called when the app is background activated as a result of a download trigger
        /// Enables the database to be updated according to result of download
        /// </summary>
        /// <param name="instance">A background task instance</param>
        /// <returns>a task</returns>
        internal static async Task CheckCompletionResult(IBackgroundTaskInstance instance)
        {
            var deferral = instance.GetDeferral();
            var trigger = instance.TriggerDetails as BackgroundTransferCompletionGroupTriggerDetails;
            if (trigger != null)
            {
                foreach (var item in trigger.Downloads)
                {
                    // TODO: handle other bad statuses
                    if (item.Progress.Status == BackgroundTransferStatus.Error)
                    {
                        await item.ResultFile.DeleteAsync();
                    }
                    else if (item.Progress.Status == BackgroundTransferStatus.Completed)
                    {
                        await SetItemDownloaded(item);
                    }
                }
            }

            deferral?.Complete();
        }

        private static async Task SetItemDownloaded(DownloadOperation item)
        {
            await SetItemDownloaded(item.ResultFile.Name);
            DownloadProgress(item);
        }

        private static async Task SetItemDownloaded(string filename)
        {
            using (var db = new LocalStorageContext())
            {
                var dbItems = db.EpisodeCache.Where(ep => ep.LocalFileName == filename);
                foreach (var dbMatch in dbItems)
                {
                    dbMatch.IsDownloaded = true;
                }

                await db.SaveChangesAsync();
            }
        }

        private static async Task<StorageFile> CheckLocalFileExists(string fileName)
        {
            StorageFile file = null;
            try
            {
                file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(fileName);
                var props = await file.GetBasicPropertiesAsync();
                if (props.Size == 0)
                {
                    await file.DeleteAsync();
                    return null;
                }
            }
            catch (FileNotFoundException)
            {
            }

            return file;
        }

        private static string Hash(string input)
        {
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider hashAlgorithm = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
            var hashByte = hashAlgorithm.HashData(buffer).ToArray();
            var sb = new StringBuilder(hashByte.Length * 2);
            foreach (byte b in hashByte)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private static async Task<bool> IsFileEmpty(StorageFile file)
        {
            if (file != null)
            {
                var props = await file.GetBasicPropertiesAsync();
                return props.Size == 0;
            }

            return true;
        }

        private static async Task<StorageFile> GetLocalFileFromUri(Uri sourceUri)
        {
            string filename = GetFileNameFromUri(sourceUri);
            StorageFile file = null;
            try
            {
                file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(filename);
            }
            catch (FileNotFoundException)
            {
            }

            return file;
        }

        private static async Task<StorageFile> GetLocalFileFromName(string filename)
        {
            StorageFile file = null;
            try
            {
                file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            }
            catch (FileNotFoundException)
            {
            }

            return file;
        }

        private static string GetFileNameFromUri(Uri sourceUri)
        {
            return System.IO.Path.GetFileName(sourceUri.PathAndQuery);
        }

        private static async Task StartDownload(Uri target, BackgroundTransferPriority priority, string localFilename)
        {
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            StorageFile destinationFile;
            destinationFile = await GetLocalFileFromName(localFilename);

            var group = BackgroundTransferGroup.CreateGroup(Guid.NewGuid().ToString());
            group.TransferBehavior = BackgroundTransferBehavior.Serialized;

            BackgroundTransferCompletionGroup completionGroup = new BackgroundTransferCompletionGroup();

            // this will cause the app to be activated when the download completes and
            // CheckCompletionResult will be called for the final download state
            RegisterBackgroundTask(completionGroup.Trigger);

            BackgroundDownloader downloader = new BackgroundDownloader(completionGroup);
            downloader.TransferGroup = group;
            group.TransferBehavior = BackgroundTransferBehavior.Serialized;
            CreateNotifications(downloader);
            DownloadOperation download = downloader.CreateDownload(target, destinationFile);
            download.Priority = priority;

            completionGroup.Enable();

            Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
            var downloadTask = download.StartAsync().AsTask(progressCallback);

            string tag = GetFileNameFromUri(target);

            CreateToast(tag, localFilename);

            try
            {
                await downloadTask;

                // Will occur after download completes
                ResponseInformation response = download.GetResponseInformation();
            }
            catch (Exception)
            {
                Debug.WriteLine("Download exception");
            }
        }

        private static void DownloadProgress(DownloadOperation obj)
        {
            Debug.WriteLine(obj.Progress.ToString());

            var progress = (double)obj.Progress.BytesReceived / (double)obj.Progress.TotalBytesToReceive;

            string tag = GetFileNameFromUri(obj.RequestedUri);

            UpdateToast(obj.ResultFile.Name, progress);
        }

        private static void CreateNotifications(BackgroundDownloader downloader)
        {
            var successToastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            successToastXml.GetElementsByTagName("text").Item(0).InnerText =
                "Downloads completed successfully.";
            ToastNotification successToast = new ToastNotification(successToastXml);
            downloader.SuccessToastNotification = successToast;

            var failureToastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            failureToastXml.GetElementsByTagName("text").Item(0).InnerText =
                "At least one download completed with failure.";
            ToastNotification failureToast = new ToastNotification(failureToastXml);
            downloader.FailureToastNotification = failureToast;
        }

        private static void CreateToast(string title, string tag)
        {
            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "File downloading...",
                            },

                            new AdaptiveProgressBar()
                            {
                                Title = title,
                                Value = new BindableProgressBarValue("progressValue"),
                                ValueStringOverride = new BindableString("p"),
                                Status = "Downloading...",
                            },
                        },
                    },
                },
            };

            var data = new Dictionary<string, string>
            {
                { "progressValue", "0" },
                { "p", $"cool" }, // TODO: better than cool
            };

            // And create the toast notification
            ToastNotification notification = new ToastNotification(toastContent.GetXml())
            {
                Tag = tag,
                Data = new NotificationData(data),
            };

            // And then send the toast
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        private static void UpdateToast(string toastTag, double progressValue)
        {
            var data = new Dictionary<string, string>
            {
                { "progressValue", progressValue.ToString() },
                { "p", $"cool" }, // TODO: better than cool
            };

            try
            {
                _notifier.Update(new NotificationData(data), toastTag);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
