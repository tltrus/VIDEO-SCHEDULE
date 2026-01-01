using System.IO;
using System.Text.Json;
using System.Windows;
using VideoSchedule_WPF.Models;

namespace VideoSchedule_WPF
{
    public static class SettingsManager
    {
        private static string GetSettingsPath()
        {
            // Получаем путь к исполняемому файлу
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var exeDirectory = Path.GetDirectoryName(exePath);

            // Формируем путь к settings.json в той же папке
            return Path.Combine(exeDirectory, "settings.json");
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                var settingsPath = GetSettingsPath();

                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    return JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем работу
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }

            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var settingsPath = GetSettingsPath();
                var directory = Path.GetDirectoryName(settingsPath);

                // Гарантируем, что папка существует
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");

                // Показываем пользователю сообщение об ошибке
                MessageBox.Show($"Не удалось сохранить настройки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}