using Azure;
using Azure.Data.Tables;
using System;

namespace ABC_Retailer.Models
{
    public class FileUpload : ITableEntity
    {
        // Azure Table Storage keys
        public string PartitionKey { get; set; } = "UPLOADS";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Properties
        public string CustomerName { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
    }
}
