using System;

namespace VideoSchedule_WPF.Models
{
    public class ScheduleItem
    {
        public TimeSpan StartTime { get; set; }
        public VideoItem Video { get; set; }
        public bool IsActive { get; set; } = true;

        public override string ToString()
        {
            return $"{StartTime:hh\\:mm} - {Video?.FileName}";
        }
    }
}