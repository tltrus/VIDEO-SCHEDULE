using System.Collections.Generic;

namespace VideoSchedule_WPF.Models
{
    public class AppSettings
    {
        public string ExternalPlayerPath { get; set; } = "";
        public List<string> RecentVideoPaths { get; set; } = new List<string>();
    }
}