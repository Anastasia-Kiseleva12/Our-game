using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using LibVLCSharp.Shared;
using OurGameAvaloniaApp.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Numerics;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls.Shapes;

namespace OurGameAvaloniaApp.Views
{
    public partial class MainWindow : Window
    {
        private MediaPlayer _mediaPlayer;
        private LibVLC _libVLC;
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _viewModel = (MainViewModel)DataContext;

            _viewModel.GenerateScene.ObserveOn(RxApp.MainThreadScheduler).Subscribe(Redraw);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsVisible = false; // Скрыть меню

            // Запуск музыки
            try
            {
                var filePath = @"Resources\music.mp3";
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.WriteLine("Файл не найден!");
                    return;
                }

                var media = new Media(_libVLC, filePath, FromType.FromPath);
                _mediaPlayer.Play(media);
                _mediaPlayer.Volume = 40; // Установите громкость на 0%
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            _viewModel.Start.Execute(Unit.Default);
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика для открытия настроек
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрытие приложения
        }


        private void Redraw(long tick)
        {
            Debug.WriteLine("Redraw called");
            if (_viewModel.Ball != null)
            {
                // Получаем размеры окна
                var width = (int)this.ClientSize.Width;
                var height = (int)this.ClientSize.Height;

                // Устанавливаем позицию шарика так, чтобы его нижняя граница была на уровне "пола"
                float ballYPosition = height - _viewModel.Ball.Rad; // Устанавливаем Y так, чтобы нижняя граница шарика была на "полу"
                _viewModel.Ball.Position = new Vector2((float)(width / 2), ballYPosition); // Центрируем по X

                // Проверка значений
                Debug.WriteLine($"Ball Position: ({_viewModel.Ball.Position.X}, {_viewModel.Ball.Position.Y}), Radius: {_viewModel.Ball.Rad}");

                var centerX = _viewModel.Ball.Position.X;
                var centerY = _viewModel.Ball.Position.Y; // Инвертируем Y для отрисовки

                // Очищаем Canvas перед отрисовкой
                DrawingCanvas.Children.Clear();

                // Рисуем шарик
                var ballEllipse = new Ellipse
                {
                    Fill = Brushes.Cyan,
                    Width = _viewModel.Ball.Rad * 2,
                    Height = _viewModel.Ball.Rad * 2
                };

                // Устанавливаем позицию шарика
                Canvas.SetLeft(ballEllipse, centerX - _viewModel.Ball.Rad);
                Canvas.SetTop(ballEllipse, centerY - _viewModel.Ball.Rad);
                DrawingCanvas.Children.Add(ballEllipse);

                // Рисуем нижнюю границу (землю)
                var groundHeight = 10; // Высота земли
                var groundRectangle = new Rectangle
                {
                    Fill = Brushes.Brown,
                    Width = width,
                    Height = groundHeight
                };

                Canvas.SetLeft(groundRectangle, 0);
                Canvas.SetTop(groundRectangle, height - groundHeight);
                DrawingCanvas.Children.Add(groundRectangle);

                // Обновляем модель
                _viewModel.Recalculate(tick);
            }
        }
    }
}
