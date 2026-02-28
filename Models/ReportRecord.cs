using System;

namespace InventoryManagementPro.Models
{
    public class ReportRecord
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string Type { get; set; } = "Sales";   
        public string Format { get; set; } = "PDF";   
        public string Status { get; set; } = "Ready";

        public int RangeDays { get; set; } = 30;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
    }
}