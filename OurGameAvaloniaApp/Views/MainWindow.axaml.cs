using Avalonia.Controls;
using Avalonia.Interactivity;
using Graphic;
using LibVLCSharp.Shared;
using System;
using System.Diagnostics;

namespace OurGameAvaloniaApp.Views
{
    public partial class MainWindow : Window
    {
        private MediaPlayer _mediaPlayer;
        private LibVLC _libVLC;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsVisible = false; // Скрыть меню

            // Запуск музыки
            try
            {
                var filePath = @"C:\Users\Ася\Desktop\Наша игра\Our-game\OurGameAvaloniaApp\Assets\music.mp3";
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.WriteLine("Файл не найден!");
                    return;
                }

                var media = new Media(_libVLC, filePath, FromType.FromPath);
                _mediaPlayer.Play(media);
                _mediaPlayer.Volume = 0; // Установите громкость на 0%
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика для открытия настроек
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрытие приложения
        }
    }
}