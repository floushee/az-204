using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WiredBrainCoffee.Storage
{
  public class CoffeeVideoStorage : ICoffeeVideoStorage
  {
    private readonly string _containerNameVideos = "coffeevideos";
    private readonly string _containerNameVideosArchive = "coffeevideos-archive";
    private readonly string _connectionString;
    private readonly string _metadataKeyTitle = "title";
    private readonly string _metadataKeyDescription = "description";

    public CoffeeVideoStorage(string connectionString)
    {
      _connectionString = connectionString;
    }

    public async Task<CloudBlockBlob> UploadVideoAsync(
      byte[] videoByteArray, string blobName, string title, string description)
    {
      var cloudBlobContainer = await GetCoffeeVideosContainerAsync();

      var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

      cloudBlockBlob.Properties.ContentType = "video/mp4";

      SetMetadata(cloudBlockBlob, _metadataKeyTitle, title);
      SetMetadata(cloudBlockBlob, _metadataKeyDescription, description);

      await cloudBlockBlob.UploadFromByteArrayAsync(videoByteArray, 0, videoByteArray.Length);

      return cloudBlockBlob;
    }

    public async Task<bool> CheckIfBlobExistsAsync(string blobName)
    {
      var cloudBlobContainer = await GetCoffeeVideosContainerAsync();

      var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

      return await cloudBlockBlob.ExistsAsync();
    }

    public async Task<IEnumerable<CloudBlockBlob>> ListVideoBlobsAsync(string prefix = null, bool includeSnapshots = false)
    {
      var cloudBlockBlobs = new List<CloudBlockBlob>();
      var cloudBlobContainer = await GetCoffeeVideosContainerAsync();

      BlobContinuationToken token = null;
      do
      {
        var blobListingDetails = BlobListingDetails.Metadata;

        if (includeSnapshots)
        {
          blobListingDetails |= BlobListingDetails.Snapshots;
        }

        var blobResultSegment =
          await cloudBlobContainer.ListBlobsSegmentedAsync(prefix, true,
            blobListingDetails, null, token, null, null);
        token = blobResultSegment.ContinuationToken;
        cloudBlockBlobs.AddRange(blobResultSegment.Results.OfType<CloudBlockBlob>());
      }
      while (token != null);

      return cloudBlockBlobs;
    }

    public async Task DownloadVideoAsync(CloudBlockBlob cloudBlockBlob, Stream targetStream)
    {
      await cloudBlockBlob.DownloadToStreamAsync(targetStream);
    }

    public async Task OverwriteVideoAsync(CloudBlockBlob cloudBlockBlob, byte[] videoByteArray, string leaseId)
    {
      var accessCondition = new AccessCondition
      {
        IfMatchETag = cloudBlockBlob.Properties.ETag,
        LeaseId = leaseId
      };

      await cloudBlockBlob.UploadFromByteArrayAsync(videoByteArray, 0, videoByteArray.Length,
        accessCondition, null, null);
    }

    public async Task DeleteVideoAsync(CloudBlockBlob cloudBlockBlob, string leaseId)
    {
      var accessCondition = new AccessCondition
      {
        IfMatchETag = cloudBlockBlob.Properties.ETag,
        LeaseId = leaseId
      };

      var deleteSnapshotsOption = cloudBlockBlob.IsSnapshot
        ? DeleteSnapshotsOption.None
        : DeleteSnapshotsOption.IncludeSnapshots;

      await cloudBlockBlob.DeleteAsync(deleteSnapshotsOption, accessCondition,
        null, null);
    }

    public async Task UpdateMetadataAsync(
      CloudBlockBlob cloudBlockBlob, string title, string description, string leaseId)
    {
      SetMetadata(cloudBlockBlob, _metadataKeyTitle, title);
      SetMetadata(cloudBlockBlob, _metadataKeyDescription, description);

      var accessCondition = new AccessCondition
      {
        IfMatchETag = cloudBlockBlob.Properties.ETag,
        LeaseId = leaseId
      };

      await cloudBlockBlob.SetMetadataAsync(accessCondition, null, null);
    }

    public async Task ReloadMetadataAsync(CloudBlockBlob cloudBlockBlob)
    {
      await cloudBlockBlob.FetchAttributesAsync();
    }

    public (string title, string description) GetBlobMetadata(CloudBlockBlob cloudBlockBlob)
    {
      return (cloudBlockBlob.Metadata.ContainsKey(_metadataKeyTitle)
               ? cloudBlockBlob.Metadata[_metadataKeyTitle]
               : ""
            , cloudBlockBlob.Metadata.ContainsKey(_metadataKeyDescription)
               ? cloudBlockBlob.Metadata[_metadataKeyDescription]
               : "");
    }

    public string GetBlobUriWithSasToken(CloudBlockBlob cloudBlockBlob)
    {
      var sharedAccessBlobPolicy = new SharedAccessBlobPolicy
      {
        Permissions = SharedAccessBlobPermissions.Read,
        SharedAccessExpiryTime = DateTime.Now.AddDays(1)
      };

      var sasToken = cloudBlockBlob.GetSharedAccessSignature(sharedAccessBlobPolicy);

      if (cloudBlockBlob.IsSnapshot)
      {
        sasToken = sasToken.Replace('?', '&');
      }

      return cloudBlockBlob.SnapshotQualifiedUri + sasToken;
    }

    public async Task<string> AcquireOneMinuteLeaseAsync(CloudBlockBlob cloudBlockBlob)
    {
      var accessCondition = new AccessCondition
      {
        IfMatchETag = cloudBlockBlob.Properties.ETag
      };

      return await cloudBlockBlob.AcquireLeaseAsync(TimeSpan.FromMinutes(1), null,
        accessCondition, null, null);
    }

    public async Task RenewLeaseAsync(CloudBlockBlob cloudBlockBlob, string leaseId)
    {
      var accessCondition = new AccessCondition
      {
        LeaseId = leaseId
      };

      await cloudBlockBlob.RenewLeaseAsync(accessCondition);
    }

    public async Task ReleaseLeaseAsync(CloudBlockBlob cloudBlockBlob, string leaseId)
    {
      var accessCondition = new AccessCondition
      {
        LeaseId = leaseId
      };

      await cloudBlockBlob.ReleaseLeaseAsync(accessCondition);
    }

    public async Task<string> LoadLeaseInfoAsync(CloudBlockBlob cloudBlockBlob)
    {
      await cloudBlockBlob.FetchAttributesAsync();

      return $"Lease state: {cloudBlockBlob.Properties.LeaseState}\n" +
        $"Lease status: {cloudBlockBlob.Properties.LeaseStatus}\n" +
        $"Lease duration: {cloudBlockBlob.Properties.LeaseDuration}";
    }

    public async Task CreateSnapshotAsync(CloudBlockBlob cloudBlockBlob)
    {
      await cloudBlockBlob.CreateSnapshotAsync();
    }

    public async Task PromoteSnapshotAsync(CloudBlockBlob snapshotCloudBlockBlob)
    {
      if (!snapshotCloudBlockBlob.IsSnapshot)
      {
        throw new ArgumentException("CloudBlockBlob must be a snapshot",
          nameof(snapshotCloudBlockBlob));
      }

      var cloudBlobContainer = await GetCoffeeVideosContainerAsync();

      var baseCloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(
        snapshotCloudBlockBlob.Name);

      await baseCloudBlockBlob.StartCopyAsync(snapshotCloudBlockBlob);

      await WaitForCopyToCompleteAsync(baseCloudBlockBlob);
    }

    private static async Task WaitForCopyToCompleteAsync(CloudBlockBlob cloudBlockBlob)
    {
      var copyInProgress = true;

      while (copyInProgress)
      {
        await Task.Delay(500);
        await cloudBlockBlob.FetchAttributesAsync();
        copyInProgress = cloudBlockBlob.CopyState.Status == CopyStatus.Pending;
      }
    }

    public async Task ArchiveVideoAsync(CloudBlockBlob cloudBlockBlob)
    {
      var archiveCloubBlobContainer = await GetCoffeeVideosArchiveContainerAsync();

      var archiveCloudBlockBlob = archiveCloubBlobContainer.GetBlockBlobReference(
        $"{DateTime.Now:yyyy/MM/dd}/{cloudBlockBlob.Name}");

      await archiveCloudBlockBlob.StartCopyAsync(cloudBlockBlob);

      await WaitForCopyToCompleteAsync(archiveCloudBlockBlob);

      await archiveCloudBlockBlob.SetStandardBlobTierAsync(StandardBlobTier.Cool);
    }

    private async Task<CloudBlobContainer> GetCoffeeVideosArchiveContainerAsync()
    {
      return await GetContainerAsync(_containerNameVideosArchive);
    }

    private async Task<CloudBlobContainer> GetCoffeeVideosContainerAsync()
    {
      return await GetContainerAsync(_containerNameVideos);
    }

    private async Task<CloudBlobContainer> GetContainerAsync(string containerName)
    {
      var cloudStorageAccount = CloudStorageAccount.Parse(_connectionString);

      var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

      var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
      await cloudBlobContainer.CreateIfNotExistsAsync();
      return cloudBlobContainer;
    }

    private static void SetMetadata(CloudBlockBlob cloudBlockBlob, string key, string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        if (cloudBlockBlob.Metadata.ContainsKey(key))
        {
          cloudBlockBlob.Metadata.Remove(key);
        }
      }
      else
      {
        cloudBlockBlob.Metadata[key] = value;
      }
    }
  }
}
