using VideoSchedule_WPF.Models;
using System;

namespace VideoSchedule_WPF.ViewModels
{
    public class ScheduleItemViewModel : ViewModelBase
    {
        private readonly ScheduleItem _scheduleItem;
        private bool _isActive;

        public TimeSpan StartTime => _scheduleItem.StartTime;
        public string StartTimeFormatted => _scheduleItem.StartTime.ToString(@"hh\:mm");

        public VideoItemViewModel Video { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    _scheduleItem.IsActive = value;
                }
            }
        }

        public ScheduleItemViewModel(ScheduleItem scheduleItem, VideoItemViewModel videoViewModel)
        {
            _scheduleItem = scheduleItem ?? throw new ArgumentNullException(nameof(scheduleItem));
            Video = videoViewModel ?? throw new ArgumentNullException(nameof(videoViewModel));
            _isActive = scheduleItem.IsActive;
        }

        public ScheduleItem GetModel() => _scheduleItem;
    }
}