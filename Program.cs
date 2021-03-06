﻿using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace AzureFileUploader
{
    class Program
    {
        static async Task Main()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var path = configuration["UploadAbsolutePath"];
            var deleteCompressedFile = bool.Parse(configuration["DeleteCompressedFile"] ?? true.ToString());
            var connectionString = Environment.GetEnvironmentVariable(configuration["Azure:ConnectionStringEnvironmentVariableName"]);
            var azureBlobContainer = configuration["Azure:BlobContainer"];
            var azureBlobNamePrefix = string.IsNullOrWhiteSpace(configuration["Azure:BlobNamePrefix"]) ? string.Empty : configuration["Azure:BlobNamePrefix"] + "__";
            var azureBlobName = azureBlobNamePrefix + DateTime.Now.ToString(@"yyyy-MM-dd__HH\h-mm\m") + ".zip";
            var tempPath = Path.Combine(path, @$"..\{azureBlobName}");

            var beginTime = DateTime.Now;

            Console.WriteLine("Compressing files...");

            ZipFile.CreateFromDirectory(path, tempPath, CompressionLevel.Fastest, includeBaseDirectory: false);
            Console.WriteLine("Successfully compressed files.");
            var compressTime = DateTime.Now - beginTime;
            Console.WriteLine($"Compress time: {compressTime.ToString(@"hh\:mm\:ss")}");
            Console.WriteLine();

            var blobContainerClient = new BlobContainerClient(connectionString, azureBlobContainer);
            var blobClient = blobContainerClient.GetBlobClient(azureBlobName);

            var fileInfo = new FileInfo(tempPath);
            var fileSizeInMegabytes = fileInfo.Length / (double)(1024 * 1024);
            Console.WriteLine($@"File name: ""{azureBlobName}""");
            Console.WriteLine($"File size: {fileSizeInMegabytes.ToString("##.##")} MB");

            var beginUpload = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine($"Uploading ...");
            var progress = new Progress(fileInfo.Length, pace: 1);
            using (var uploadFileStream = fileInfo.OpenRead())
            {
                await blobClient.UploadAsync(uploadFileStream, progressHandler: progress);
            }
            if (deleteCompressedFile)
                fileInfo.Delete();

            Console.WriteLine();
            Console.WriteLine("Successfully uploaded files.");

            var endTime = DateTime.Now;
            var uploadTime = endTime - beginUpload;
            var totalTime = endTime - beginTime;
            Console.WriteLine($"Upload time: {uploadTime.ToString(@"hh\:mm\:ss")}");
            Console.WriteLine($"Total time:  {totalTime.ToString(@"hh\:mm\:ss")}");
        }
    }
}