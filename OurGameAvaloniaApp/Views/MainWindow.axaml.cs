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
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace OurGameAvaloniaApp.Views
{
    public partial class MainWindow : Window
    {
        private Ellipse ballEllipse;
        private MediaPlayer _mediaPlayer;
        private LibVLC _libVLC;
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            this.Focus();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _viewModel = (MainViewModel)DataContext;

            _viewModel.GenerateScene
           .ObserveOn(RxApp.MainThreadScheduler)  // Обновление UI в главном потоке
           .Subscribe(Redraw);
            this.KeyDown += Window_KeyDown;
            this.KeyUp += Window_KeyUp;

            this.Opened += OnWindowOpened;
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
                
                VolumeSlider.Value = _mediaPlayer.Volume; // Установите значение слайдера в соответствии с громкостью
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
        private void OnWindowOpened(object sender, EventArgs e)
        {
            // Теперь размеры окна доступны
            _viewModel.WindowHeight = Convert.ToInt32(this.Height);
            _viewModel.WindowWidth = Convert.ToInt32(this.Width);
        }
        private void Window_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Space)
            {
                Debug.WriteLine("Space key pressed");
                ((MainViewModel)this.DataContext).Jump.Execute().Subscribe();
            }
            else if (e.Key == Avalonia.Input.Key.A)
            {
                Debug.WriteLine("A key pressed");
                _viewModel.MoveLeft.Execute(Unit.Default);
            }
            else if (e.Key == Avalonia.Input.Key.D)
            {
                Debug.WriteLine("D key pressed");
                _viewModel.MoveRight.Execute(Unit.Default);
            }
        }

        private void Window_KeyUp(object sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.A)
            {
                Debug.WriteLine("A key pressed");
                _viewModel.StopMoving.Execute(Unit.Default);
            }
            else if (e.Key == Avalonia.Input.Key.D)
            {
                Debug.WriteLine("D key pressed");
                _viewModel.StopMoving.Execute(Unit.Default);
            }
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsVisible = false; // Скрыть меню
            var canvasWidth = (float)DrawingCanvas.Bounds.Width;
            var canvasHeight = (float)DrawingCanvas.Bounds.Height;
            _viewModel.Start.Execute(Unit.Default);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsVisible = false;
            SettingsPanel.IsVisible = true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.IsVisible = false;
            Menu.IsVisible = true;
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                double volume = e.NewValue; // Получить новое значение громкости
                _mediaPlayer.Volume = (int)volume; // Установите громкость в медиаплеере
                Console.WriteLine($"Громкость установлена на: {volume}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрытие приложения
        }

        private void WindowSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WindowSizeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string size = selectedItem.Content.ToString();
                string[] dimensions = size.Split('x');

                if (dimensions.Length == 2 &&
                    int.TryParse(dimensions[0], out int width) &&
                    int.TryParse(dimensions[1], out int height))
                {
                    this.Width = width;
                    this.Height = height;
                }
                
            }
            _viewModel.WindowHeight = Convert.ToInt32(this.Height);
            _viewModel.WindowWidth = Convert.ToInt32(this.Width);
        }

        private void Redraw(long tick)
        {
            if (_viewModel.Ball != null)
            {
                var canvasWidth = (float)DrawingCanvas.Bounds.Width;
                var canvasHeight = (float)DrawingCanvas.Bounds.Height;

                // Если ballEllipse еще не создан, создаем его
                if (ballEllipse == null)
                {
                    ballEllipse = new Ellipse
                    {
                        Fill = Brushes.Cyan,
                        Width = _viewModel.Ball.Rad * 2,
                        Height = _viewModel.Ball.Rad * 2
                    };
                    DrawingCanvas.Children.Add(ballEllipse); // Добавляем его в холст
                }

                // Логируем координаты для отладки
                Debug.WriteLine($"Ball position after move in Redraw: {_viewModel.Ball.Position.X}, {_viewModel.Ball.Position.Y}");

                // Обновляем положение шарика
                Canvas.SetLeft(ballEllipse, _viewModel.Ball.Position.X - _viewModel.Ball.Rad);
                Canvas.SetTop(ballEllipse, canvasHeight - _viewModel.Ball.Position.Y - _viewModel.Ball.Rad);
            }
        }


    }
}
