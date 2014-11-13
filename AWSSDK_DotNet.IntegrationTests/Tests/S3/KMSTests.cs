﻿using System;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSSDK_DotNet.IntegrationTests.Utils;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.S3.Transfer;
using System.Security.Cryptography;
using System.Net;
using Amazon.Runtime;
using Amazon;
using System.Collections.Generic;
using Amazon.Util;

namespace AWSSDK_DotNet.IntegrationTests.Tests.S3
{
    [TestClass]
    public class KMSTests : TestBase<AmazonS3Client>
    {
        private const string key = "foo.txt";
        private const string testContents = "Test contents";
        private static string largeTestContents = new string('@', (int)(TransferUtilityTests.MEG_SIZE * 19));
        private static string fileContents = "Test file contents";
        private const string profile = "trent";
        private static string keyId;

        [ClassCleanup]
        public static void Cleanup()
        {
            BaseClean();

            if (!string.IsNullOrEmpty(keyId))
                KeyManagementService.Client.DisableKey(keyId);
        }

        [ClassInitialize]
        public static void Initialize(TestContext t)
        {
            keyId = KeyManagementService.Client.CreateKey(new Amazon.KeyManagementService.Model.CreateKeyRequest
            {
                KeyUsage = Amazon.KeyManagementService.KeyUsageType.ENCRYPT_DECRYPT,
                Description = ".NET SDK S3 Test Key"
            }).KeyMetadata.KeyId;
        }
        
        [TestMethod]
        [TestCategory("S3")]
        public void GetObjectFromNonDefaultEndpoint()
        {
            var client = new AmazonS3Client(RegionEndpoint.USWest2);
            var bucketName = S3TestUtils.CreateBucket(client);
            try
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentBody = testContents,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
                };
                client.PutObject(putObjectRequest);

                using (var response = client.GetObject(bucketName, key))
                using (var reader = new StreamReader(response.ResponseStream))
                {
                    var data = reader.ReadToEnd();
                    Assert.AreEqual(testContents, data);
                }
            }
            finally
            {
                AmazonS3Util.DeleteS3BucketWithObjects(client, bucketName);
                client.Dispose();
            }
        }

        [TestMethod]
        [TestCategory("S3")]
        public void GetObjectFromDefaultEndpointBeforeDNSResolution()
        {
            var client = new AmazonS3Client(RegionEndpoint.USWest2);
            var defaultEndpointClient = new AmazonS3Client(RegionEndpoint.USEast1);
            var bucketName = S3TestUtils.CreateBucket(client);
            try
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentBody = testContents,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
                };
                client.PutObject(putObjectRequest);

                using (var response = defaultEndpointClient.GetObject(bucketName, key))
                using (var reader = new StreamReader(response.ResponseStream))
                {
                    var data = reader.ReadToEnd();
                    Assert.AreEqual(testContents, data);
                }
            }
            finally
            {
                AmazonS3Util.DeleteS3BucketWithObjects(client, bucketName);
                client.Dispose();
                defaultEndpointClient.Dispose();
            }
        }

        // To run this test set bucketName to a valid bucket name.
        //[TestMethod]
        //[TestCategory("S3")]
        public void GetObjectFromDefaultEndpointAfterDNSResolution()
        {
            var client = new AmazonS3Client(RegionEndpoint.USWest2);
            var defaultEndpointClient = new AmazonS3Client(RegionEndpoint.USEast1);

            // Set to a valid bucket name.
            string bucketName = null;
            try
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentBody = testContents,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
                };
                client.PutObject(putObjectRequest);

                using (var response = defaultEndpointClient.GetObject(bucketName, key))
                using (var reader = new StreamReader(response.ResponseStream))
                {
                    var data = reader.ReadToEnd();
                    Assert.AreEqual(testContents, data);
                }
            }
            finally
            {
                client.Dispose();
                defaultEndpointClient.Dispose();
            }
        }

        [TestMethod]
        [TestCategory("S3")]
        public void DefaultKeyTests()
        {
            TestSseKms(keyId: null);
            TestPresignedUrls(keyId: null);
        }

        [TestMethod]
        [TestCategory("S3")]
        public void SpecificKeyTests()
        {
            TestSseKms(keyId);
            TestPresignedUrls(keyId);
        }

        public void TestPresignedUrls(string keyId)
        {
            var oldSigV4 = AWSConfigs.S3Config.UseSignatureVersion4;
            AWSConfigs.S3Config.UseSignatureVersion4 = true;

            using (var newClient = new AmazonS3Client())
            {
                var bucketName = S3TestUtils.CreateBucket(newClient);
                try
                {
                    VerifyPresignedPut(bucketName, key, keyId);
                    VerifyObjectWithTransferUtility(bucketName);
                    TestPresignedGet(bucketName, key, keyId);

                    var key2 = key + "Copy2";
                    var copyResponse = newClient.CopyObject(new CopyObjectRequest
                    {
                        SourceBucket = bucketName,
                        SourceKey = key,
                        DestinationBucket = bucketName,
                        DestinationKey = key2,
                        // No KMS values set
                        //ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
                        //ServerSideEncryptionKeyManagementServiceKeyId = keyId
                    });
                    Assert.IsNotNull(copyResponse);
                    var usedKeyId = copyResponse.ServerSideEncryptionKeyManagementServiceKeyId;
                    Assert.IsNull(usedKeyId);
                }
                finally
                {
                    AmazonS3Util.DeleteS3BucketWithObjects(newClient, bucketName);
                    AWSConfigs.S3Config.UseSignatureVersion4 = oldSigV4;
                }
            }
        }

        private void TestSseKms(string keyId)
        {
            var bucketName = S3TestUtils.CreateBucket(Client);
            try
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentBody = testContents,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
                };
                putObjectRequest.ServerSideEncryptionKeyManagementServiceKeyId = keyId;
                var putObjectResponse = Client.PutObject(putObjectRequest);
                Assert.IsNotNull(putObjectResponse.ServerSideEncryptionKeyManagementServiceKeyId);
                var usedKeyId = putObjectResponse.ServerSideEncryptionKeyManagementServiceKeyId;
                VerifyKeyId(keyId, usedKeyId);
                VerifyObject(bucketName, key, usedKeyId);
                VerifyObjectWithTransferUtility(bucketName);

                TestCopyPart(bucketName, key, keyId);

                var key2 = key + "Copy";
                var copyResponse = Client.CopyObject(new CopyObjectRequest
                {
                    SourceBucket = bucketName,
                    SourceKey = key,
                    DestinationBucket = bucketName,
                    DestinationKey = key2,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
                    ServerSideEncryptionKeyManagementServiceKeyId = keyId
                });
                Assert.IsNotNull(copyResponse);
                usedKeyId = copyResponse.ServerSideEncryptionKeyManagementServiceKeyId;
                VerifyKeyId(keyId, usedKeyId);
                VerifyObject(bucketName, key2, usedKeyId);
            }
            finally
            {
                AmazonS3Util.DeleteS3BucketWithObjects(Client, bucketName);
            }
        }

        private void TestPresignedGet(string bucketName, string key, string keyId)
        {
            GetPreSignedUrlRequest getPresignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.Now.AddMinutes(5)
            };
            var url = Client.GetPreSignedURL(getPresignedUrlRequest);
            var webRequest = HttpWebRequest.Create(url);

            using (var response = webRequest.GetResponse())
            {
                var usedKeyId = response.Headers[HeaderKeys.XAmzServerSideEncryptionAwsKmsKeyIdHeader];
                VerifyKeyId(keyId, usedKeyId);

                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var data = reader.ReadToEnd();
                    VerifyContents(data);
                }
            }
        }
        private void VerifyPresignedPut(string bucketName, string key, string keyId)
        {
            GetPreSignedUrlRequest getPresignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
                ServerSideEncryptionKeyManagementServiceKeyId = keyId,
                Expires = DateTime.Now.AddMinutes(5)
            };
            var url = Client.GetPreSignedURL(getPresignedUrlRequest);

            string usedKeyId = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    usedKeyId = VerifyPresignedPut(keyId, url);
                    break;
                }
                catch { }
            }

            Assert.IsNotNull(usedKeyId);
            VerifyKeyId(keyId, usedKeyId);
            VerifyObject(bucketName, key, usedKeyId);
        }
        private void TestCopyPart(string bucketName, string key, string keyId)
        {
            string dstKey = "dstObject";
            string srcKey = key;
            string srcVersionID;
            string srcETag;
            DateTime srcTimeStamp;
            string uploadID = null;

            try
            {
                //Get the srcObjectTimestamp
                GetObjectMetadataResponse gomr = Client.GetObjectMetadata(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = srcKey
                });
                srcTimeStamp = gomr.LastModified;
                srcVersionID = gomr.VersionId;
                srcETag = gomr.ETag;

                //Start the multipart upload
                InitiateMultipartUploadResponse imur = Client.InitiateMultipartUpload(new InitiateMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = dstKey,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
                    ServerSideEncryptionKeyManagementServiceKeyId = keyId
                });
                Assert.AreEqual(ServerSideEncryptionMethod.AWSKMS, imur.ServerSideEncryptionMethod);
                var usedKeyId = imur.ServerSideEncryptionKeyManagementServiceKeyId;
                VerifyKeyId(keyId, usedKeyId);
                uploadID = imur.UploadId;


                CopyPartRequest request = new CopyPartRequest
                {
                    DestinationBucket = bucketName,
                    DestinationKey = dstKey,
                    SourceBucket = bucketName,
                    SourceKey = srcKey,
                    UploadId = uploadID,
                    PartNumber = 1,
                };
                CopyPartResponse response = Client.CopyPart(request);
                Assert.AreEqual(ServerSideEncryptionMethod.AWSKMS, response.ServerSideEncryptionMethod);
                usedKeyId = response.ServerSideEncryptionKeyManagementServiceKeyId;
                VerifyKeyId(keyId, usedKeyId);

                //ETag
                Assert.IsNotNull(response.ETag);
                Assert.IsTrue((response.ETag != null) && (response.ETag.Length > 0));

                //LastModified
                Assert.IsNotNull(response.LastModified);
                Assert.AreNotEqual(DateTime.MinValue, response.LastModified);

                //PartNumber
                Assert.IsTrue(response.PartNumber == 1);

                var completeResponse = Client.CompleteMultipartUpload(new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = dstKey,
                    UploadId = uploadID,
                    PartETags = new List<PartETag>()
                    {
                        new PartETag { ETag = response.ETag, PartNumber = response.PartNumber }
                    }
                });
                Assert.AreEqual(ServerSideEncryptionMethod.AWSKMS, completeResponse.ServerSideEncryptionMethod);
                usedKeyId = completeResponse.ServerSideEncryptionKeyManagementServiceKeyId;
                VerifyKeyId(keyId, usedKeyId);
            }
            finally
            {
                //abort the multipart upload
                if (uploadID != null)
                {
                    Client.AbortMultipartUpload(new AbortMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = dstKey,
                        UploadId = uploadID
                    });
                }
            }
        }

        private string VerifyPresignedPut(string keyId, string url)
        {
            var webRequest = HttpWebRequest.Create(url);
            webRequest.Method = "PUT";

            if (keyId != null)
                webRequest.Headers.Add(HeaderKeys.XAmzServerSideEncryptionAwsKmsKeyIdHeader, keyId);
            webRequest.Headers.Add(HeaderKeys.XAmzServerSideEncryptionHeader, ServerSideEncryptionMethod.AWSKMS.Value);

            using (var stream = webRequest.GetRequestStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(testContents);
            }

            string usedKeyId;
            using (var response = webRequest.GetResponse())
            {
                usedKeyId = response.Headers[HeaderKeys.XAmzServerSideEncryptionAwsKmsKeyIdHeader];
            }
            return usedKeyId;
        }
        private void VerifyObject(string bucketName, string key, string usedKeyId)
        {
            var metadata = Client.GetObjectMetadata(bucketName, key);
            if (usedKeyId != null)
                Assert.IsTrue(metadata.ServerSideEncryptionKeyManagementServiceKeyId.IndexOf(usedKeyId, StringComparison.OrdinalIgnoreCase) >= 0);

            using (var response = Client.GetObject(bucketName, key))
            {
                Assert.AreEqual(ServerSideEncryptionMethod.AWSKMS, response.ServerSideEncryptionMethod);
                Assert.IsNotNull(response.ServerSideEncryptionKeyManagementServiceKeyId);
                if (usedKeyId != null)
                    Assert.IsTrue(response.ServerSideEncryptionKeyManagementServiceKeyId.IndexOf(usedKeyId, StringComparison.OrdinalIgnoreCase) >= 0);

                using (var reader = new StreamReader(response.ResponseStream))
                {
                    var data = reader.ReadToEnd();
                    VerifyContents(data);
                }
            }
        }
        private void VerifyObjectWithTransferUtility(string bucketName)
        {
            var transferUtility = new TransferUtility(Client);
            var filePath = Path.GetFullPath("downloadedFile.txt");
            transferUtility.Download(new TransferUtilityDownloadRequest
            {
                BucketName = bucketName,
                Key = key,
                FilePath = filePath
            });
            var fileContents = File.ReadAllText(filePath);
            VerifyContents(fileContents);
        }

        private static void VerifyContents(string contents)
        {
            if (contents.Length == testContents.Length)
                Assert.IsTrue(string.Equals(testContents, contents, StringComparison.Ordinal));
            else if (contents.Length == largeTestContents.Length)
                Assert.IsTrue(string.Equals(largeTestContents, contents, StringComparison.Ordinal));
            else
                Assert.IsTrue(string.Equals(fileContents, contents, StringComparison.Ordinal));
        }
        private static void VerifyKeyId(string suppliedKeyId, string returnedKeyId)
        {
            if (suppliedKeyId != null)
            {
                var index = returnedKeyId.IndexOf(suppliedKeyId, StringComparison.OrdinalIgnoreCase);
                Assert.IsTrue(index >= 0);
            }
        }
    }
}
