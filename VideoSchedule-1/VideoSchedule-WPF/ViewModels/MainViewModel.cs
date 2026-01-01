using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VideoSchedule_WPF.Models;

namespace VideoSchedule_WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;
        private DispatcherTimer _scheduleTimer;
        private DispatcherTimer _clockTimer;
        private Process _externalPlayerProcess;
        private string _currentDateTime;
        private string _nextScheduleInfo;
        private string _status;
        private string _playerStatus;
        private string _timeInput;
        private VideoItemViewModel _selectedVideo;
        private ScheduleItemViewModel _selectedSchedule;

        public ObservableCollection<VideoItemViewModel> Videos { get; } = new ObservableCollection<VideoItemViewModel>();
        public ObservableCollection<ScheduleItemViewModel> ScheduleItems { get; } = new ObservableCollection<ScheduleItemViewModel>();

        public ICommand AddVideoCommand { get; }
        public ICommand RemoveSelectedVideoCommand { get; }
        public ICommand ClearAllVideosCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand OpenExternalPlayerCommand { get; }
        public ICommand AddScheduleCommand { get; }
        public ICommand RemoveFromScheduleCommand { get; }
        public ICommand RemoveScheduleCommand { get; }

        public VideoItemViewModel SelectedVideo
        {
            get => _selectedVideo;
            set => SetProperty(ref _selectedVideo, value);
        }

        public ScheduleItemViewModel SelectedSchedule
        {
            get => _selectedSchedule;
            set => SetProperty(ref _selectedSchedule, value);
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        public string NextScheduleInfo
        {
            get => _nextScheduleInfo;
            set => SetProperty(ref _nextScheduleInfo, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string PlayerStatus
        {
            get => _playerStatus;
            set => SetProperty(ref _playerStatus, value);
        }

        public string TimeInput
        {
            get => _timeInput;
            set => SetProperty(ref _timeInput, value);
        }

        public string VideoCountText => $"Files: {Videos.Count} | Schedules: {ScheduleItems.Count}";

        public string ExternalPlayerButtonText =>
            !string.IsNullOrEmpty(_settings?.ExternalPlayerPath)
                ? $"📂 {Path.GetFileName(_settings.ExternalPlayerPath)}"
                : "📂 Select External Player";

        public MainViewModel()
        {
            _settings = SettingsManager.LoadSettings();

            // Initialize commands
            AddVideoCommand = new RelayCommand(_ => AddVideo());
            RemoveSelectedVideoCommand = new RelayCommand(_ => RemoveSelectedVideo());
            ClearAllVideosCommand = new RelayCommand(_ => ClearAllVideos());
            PlayCommand = new RelayCommand(_ => PlayVideo());
            OpenExternalPlayerCommand = new RelayCommand(_ => OpenExternalPlayer());
            AddScheduleCommand = new RelayCommand(_ => AddSchedule());
            RemoveFromScheduleCommand = new RelayCommand(_ => RemoveFromSchedule());
            RemoveScheduleCommand = new RelayCommand(param =>
            {
                if (param is ScheduleItemViewModel schedule)
                    RemoveSchedule(schedule);
            });

            InitializeTimers();
            LoadRecentVideos();

            TimeInput = DateTime.Now.ToString("HH:mm");
            CurrentDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            NextScheduleInfo = "No active schedules";
            Status = "Ready";
            PlayerStatus = "Player: not selected";
        }

        private void InitializeTimers()
        {
            // Schedule checking timer
            _scheduleTimer = new DispatcherTimer();
            _scheduleTimer.Interval = TimeSpan.FromSeconds(10);
            _scheduleTimer.Tick += ScheduleTimer_Tick;
            _scheduleTimer.Start();

            // Clock timer
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        private void LoadRecentVideos()
        {
            if (_settings?.RecentVideoPaths == null) return;

            foreach (var path in _settings.RecentVideoPaths)
            {
                if (File.Exists(path))
                {
                    AddVideoItem(path);
                }
            }
        }

        private void AddVideo()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.avi;*.wmv;*.mov;*.mkv;*.flv;*.webm;*.m4v|All files|*.*",
                Multiselect = true,
                Title = "Select video files"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    AddVideoItem(filePath);
                }
            }
        }

        private void AddVideoItem(string filePath)
        {
            try
            {
                var videoItem = VideoItem.CreateFromPath(filePath);
                var videoViewModel = new VideoItemViewModel(videoItem);
                Videos.Add(videoViewModel);

                // Add to recent files
                if (_settings != null && !_settings.RecentVideoPaths.Contains(filePath))
                {
                    _settings.RecentVideoPaths.Add(filePath);
                    SettingsManager.SaveSettings(_settings);
                }

                OnPropertyChanged(nameof(VideoCountText));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding video: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveSelectedVideo()
        {
            if (SelectedVideo == null)
            {
                MessageBox.Show("Select video to remove",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Remove video '{SelectedVideo.FileName}' from list?",
                "Confirm deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Remove from schedule
                var schedulesToRemove = ScheduleItems
                    .Where(s => s.Video == SelectedVideo)
                    .ToList();

                foreach (var schedule in schedulesToRemove)
                {
                    ScheduleItems.Remove(schedule);
                }

                Videos.Remove(SelectedVideo);
                SelectedVideo = null;

                OnPropertyChanged(nameof(VideoCountText));
                UpdateNextScheduleInfo();
            }
        }

        private void ClearAllVideos()
        {
            if (Videos.Count == 0)
            {
                MessageBox.Show("Video list is already empty",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Clear all videos and schedule?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Videos.Clear();
                ScheduleItems.Clear();
                SelectedVideo = null;
                SelectedSchedule = null;

                OnPropertyChanged(nameof(VideoCountText));
                UpdateNextScheduleInfo();
            }
        }

        private void AddSchedule()
        {
            if (SelectedVideo == null)
            {
                MessageBox.Show("Select video from list", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(TimeInput, out TimeSpan startTime))
            {
                MessageBox.Show("Enter correct time in HH:mm format (24-hour)",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if schedule already exists for this time
            var existingSchedule = ScheduleItems.FirstOrDefault(s =>
                s.StartTime == startTime &&
                s.Video == SelectedVideo);

            if (existingSchedule != null)
            {
                MessageBox.Show("This schedule already exists",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var scheduleItem = new ScheduleItem
            {
                StartTime = startTime,
                Video = SelectedVideo.GetModel(),
                IsActive = true
            };

            var scheduleViewModel = new ScheduleItemViewModel(scheduleItem, SelectedVideo);
            ScheduleItems.Add(scheduleViewModel);

            Status = $"Added to schedule at {startTime:hh\\:mm}: {SelectedVideo.FileName}";
            UpdateNextScheduleInfo();
        }

        private void RemoveFromSchedule()
        {
            if (SelectedSchedule == null)
            {
                MessageBox.Show("Select schedule to remove",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ScheduleItems.Remove(SelectedSchedule);
            SelectedSchedule = null;

            Status = "Removed from schedule";
            UpdateNextScheduleInfo();
        }

        private void RemoveSchedule(ScheduleItemViewModel schedule)
        {
            ScheduleItems.Remove(schedule);
            if (SelectedSchedule == schedule)
                SelectedSchedule = null;

            UpdateNextScheduleInfo();
        }

        private void PlayVideo()
        {
            VideoItemViewModel videoToPlay = SelectedVideo ?? Videos.FirstOrDefault();

            if (videoToPlay == null)
            {
                MessageBox.Show("Add video for playback",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PlayVideoFile(videoToPlay.FilePath);
        }

        private void PlayVideoFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.ExternalPlayerPath))
                {
                    PlayWithSystemPlayer(filePath);
                }
                else
                {
                    PlayWithExternalPlayer(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing video: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayWithSystemPlayer(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "open"
                });

                PlayerStatus = "Player: system player";
                Status = $"Playing: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting system player: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayWithExternalPlayer(string filePath)
        {
            if (!File.Exists(_settings.ExternalPlayerPath))
            {
                MessageBox.Show("Specified external player not found",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StopExternalPlayer();

            try
            {
                _externalPlayerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _settings.ExternalPlayerPath,
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                _externalPlayerProcess.Exited += (s, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PlayerStatus = "Player: stopped";
                    });
                };

                _externalPlayerProcess.Start();

                PlayerStatus = $"Player: {Path.GetFileName(_settings.ExternalPlayerPath)}";
                Status = $"Playing: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting external player: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopExternalPlayer()
        {
            if (_externalPlayerProcess != null && !_externalPlayerProcess.HasExited)
            {
                try
                {
                    _externalPlayerProcess.Kill();
                    _externalPlayerProcess.WaitForExit(3000);
                }
                catch { }
                finally
                {
                    _externalPlayerProcess?.Dispose();
                    _externalPlayerProcess = null;
                }
            }
        }

        private void OpenExternalPlayer()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable files|*.exe|All files|*.*",
                Title = "Select external player",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                _settings.ExternalPlayerPath = dialog.FileName;
                SettingsManager.SaveSettings(_settings);

                OnPropertyChanged(nameof(ExternalPlayerButtonText));

                MessageBox.Show($"External player selected: {Path.GetFileName(dialog.FileName)}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ScheduleTimer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;

            foreach (var schedule in ScheduleItems.Where(s => s.IsActive))
            {
                var timeDiff = (currentTime - schedule.StartTime).TotalSeconds;

                if (timeDiff >= 0 && timeDiff < 10)
                {
                    PlayVideoFile(schedule.Video.FilePath);
                    Status = $"Started by schedule: {schedule.Video.FileName} - {DateTime.Now:HH:mm:ss}";
                    break;
                }
            }

            UpdateNextScheduleInfo();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            CurrentDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        private void UpdateNextScheduleInfo()
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;

            var upcomingSchedules = ScheduleItems
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    Schedule = s,
                    TimeUntil = s.StartTime - currentTime
                })
                .Where(x => x.TimeUntil.TotalSeconds > 0)
                .OrderBy(x => x.TimeUntil)
                .ToList();

            if (upcomingSchedules.Any())
            {
                var next = upcomingSchedules.First();
                NextScheduleInfo = $"{next.Schedule.StartTimeFormatted} - {next.Schedule.Video.FileName}";
            }
            else
            {
                NextScheduleInfo = "No active schedules";
            }
        }

        public void SaveSettingsAndCleanup()
        {
            _scheduleTimer?.Stop();
            _clockTimer?.Stop();
            StopExternalPlayer();

            SettingsManager.SaveSettings(_settings);
        }
    }
}