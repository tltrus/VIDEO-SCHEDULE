using System;
using System.IO;

namespace VideoSchedule_WPF.Models
{
    public class VideoItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime DateAdded { get; set; }

        public static VideoItem CreateFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var fileInfo = new FileInfo(filePath);
            return new VideoItem
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length / (1024 * 1024), // MB
                DateAdded = DateTime.Now
            };
        }
    }
}