namespace ST10445050_CLDV6212_POE_Part1.Models
{
    public class FileModel
    {
        public string fileName { get; set; } = string.Empty; // initialized to avoid CS8618
        public long fileSize { get; set; }
        public DateTimeOffset? LastModified { get; set; }

        public string DisplaySize
        {
            get
            {
                if (fileSize >= 1024 * 1024)
                    return $"{fileSize / 1024 / 1024} MB";
                if (fileSize >= 1024)
                    return $"{fileSize / 1024} KB";
                return $"{fileSize} Bytes";
            }
        }
    }
}
