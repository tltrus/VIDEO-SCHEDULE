using VideoSchedule_WPF.Models;
using System;

namespace VideoSchedule_WPF.ViewModels
{
    public class VideoItemViewModel : ViewModelBase
    {
        private readonly VideoItem _videoItem;

        public string FilePath => _videoItem.FilePath;

        public string FileName => _videoItem.FileName;

        public long FileSize => _videoItem.FileSize;

        public string FileSizeFormatted => $"{_videoItem.FileSize} MB";

        public DateTime DateAdded => _videoItem.DateAdded;

        public string DateAddedFormatted => _videoItem.DateAdded.ToString("dd.MM.yyyy HH:mm");

        public VideoItemViewModel(VideoItem videoItem)
        {
            _videoItem = videoItem ?? throw new ArgumentNullException(nameof(videoItem));
        }

        public VideoItem GetModel() => _videoItem;
    }
}