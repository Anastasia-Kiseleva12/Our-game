﻿using Avalonia.Controls;
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
using System.Collections.Generic;

namespace OurGameAvaloniaApp.Views
{
   public partial class MainWindow : Window
    {
        private Ellipse ballEllipse;
        private Platform platform;
        private Coin coin;
        private Portal portal;
        private MediaPlayer _mediaPlayer;
        private LibVLC _libVLC;
        private MainViewModel _viewModel;
        private Rectangle groundRectangle;
        public static bool isPaused = false;

      public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            this.Focus();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _viewModel = (MainViewModel)DataContext;

            var fullScreenCheckBox = this.FindControl<CheckBox>("FullScreenCheckBox");
            this.FindControl<CheckBox>("FullScreenCheckBox").Checked += OnFullScreenChecked;
            this.FindControl<CheckBox>("FullScreenCheckBox").Unchecked += OnFullScreenUnchecked;
            if (fullScreenCheckBox.IsChecked == true)
            {
               this.WindowState = WindowState.FullScreen;
            }
            DataContext = this; // Устанавливаем DataContext для привязки


         _viewModel.GenerateScene
           .ObserveOn(RxApp.MainThreadScheduler)  // Обновление UI в главном потоке
           .Subscribe(Redraw);
            this.KeyDown += Window_KeyDown;
            this.KeyUp += Window_KeyUp;
            this.Opened += OnWindowOpened;
            Debug.WriteLine("SizeChanged handler is attached.");

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
                
                VolumeSlider.Value = _mediaPlayer.Volume;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

      private void OnFullScreenChecked(object sender, RoutedEventArgs e)
      {
         this.WindowState = WindowState.FullScreen; // Устанавливаем полноэкранный режим
      }

      private void OnFullScreenUnchecked(object sender, RoutedEventArgs e)
      {
         this.WindowState = WindowState.Normal; // Возвращаемся в обычный режим
      }
      private void OnWindowOpened(object sender, EventArgs e)
        {
            _viewModel.WindowHeight = Convert.ToInt32(this.Height);
            _viewModel.WindowWidth = Convert.ToInt32(this.Width);
        }
        private void Window_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.W)
            {
                Debug.WriteLine("W key pressed");
                _viewModel.IsJumping = true;
            }
            else if (e.Key == Avalonia.Input.Key.A)
            {
                Debug.WriteLine("A key pressed");
                //_viewModel.MoveLeft.Execute(Unit.Default);
                _viewModel.IsMoveL = true;
                _viewModel.IsMoveR = false;
            }
            else if (e.Key == Avalonia.Input.Key.D)
            {
                Debug.WriteLine("D key pressed");
                //_viewModel.MoveRight.Execute(Unit.Default);
                _viewModel.IsMoveL = false;
                _viewModel.IsMoveR = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (isPaused && _viewModel.GameActive)
                {

                  ResumeGame();
                }
                else if(_viewModel.GameActive)
                {  

                  PauseGame();
                }
            }
        }

        private void Window_KeyUp(object sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.A || e.Key == Avalonia.Input.Key.D)
            {
                _viewModel.IsMoveL = false;
                _viewModel.IsMoveR = false;
            }
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsVisible = false;
            var canvasWidth = (float)DrawingCanvas.Bounds.Width;
            var canvasHeight = (float)DrawingCanvas.Bounds.Height;
            _viewModel.Ball.Position = new Vector2(712, 20);
            _viewModel.LevelManager.currentLevelIndex = 0;
            var currentLevel = _viewModel.LevelManager.currentLevelIndex;
            _viewModel.Start.Execute(Unit.Default);
            DrawingCanvas.IsVisible = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsVisible = false;
            PauseMenu.IsVisible = false;
            SettingsPanel.IsVisible = true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.IsVisible = false;
            if (isPaused)
            {
               PauseMenu.IsVisible = true;
               Menu.IsVisible = false;
            }
            else
            {
               PauseMenu.IsVisible = false;
               Menu.IsVisible = true;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                double volume = e.NewValue; 
                _mediaPlayer.Volume = (int)volume;
                Debug.WriteLine($"Громкость установлена на: {volume}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
         this.Close(); 
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
                    // Обновление размера окна
                    this.Width = width;
                    this.Height = height;

                    // Обновление размеров в ViewModel
                    _viewModel.WindowWidth = width;
                    _viewModel.WindowHeight = height;

                    // Перерисовка после изменения размера
                    Redraw(0);
                }
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Debug.WriteLine("Window_SizeChanged triggered");

            // Обновляем размеры окна при изменении через событие
            _viewModel.WindowWidth = (int)e.NewSize.Width;
            _viewModel.WindowHeight = (int)e.NewSize.Height;

            Debug.WriteLine($"New Window Size: {_viewModel.WindowWidth}x{_viewModel.WindowHeight}");

        }

        private void PauseGame()
        {
            isPaused = true;
            Menu.IsVisible = false;
            DrawingCanvas.IsVisible = false;
            PauseMenu.IsVisible = true;
        }

        private void ResumeGame()
        {
            isPaused = false;
            Menu.IsVisible = false; 
            DrawingCanvas.IsVisible = true;
            PauseMenu.IsVisible = false;
      }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            ResumeGame();
        }

        private void ExitToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика для выхода в главное меню
            isPaused = false;
            _viewModel.GameActive = false;
            Menu.IsVisible = true; // Показываем главное меню
            PauseMenu.IsVisible = false;
         }


        private void Redraw(long tick)
        {
            // Очистим Canvas перед отрисовкой
            DrawingCanvas.InvalidateVisual();

            // Очистим Canvas перед отрисовкой
            DrawingCanvas.Children.Clear();

            var canvasWidth = (float)DrawingCanvas.Bounds.Width;
            var canvasHeight = (float)DrawingCanvas.Bounds.Height;

            var currentLevel = _viewModel.LevelManager.CurrentLevel;

            // Уровень земли и её толщина
            const float groundThickness = 10; // Толщина земли
            float groundLevel = canvasHeight - groundThickness; // Уровень земли (нижняя граница)

            // Отрисовка платформ
            foreach (var platform in currentLevel.Platforms)
            {
                var platformRectangle = new Rectangle
                {
                    Fill = Brushes.Gray,
                    Width = platform.Width,
                    Height = platform.Height + 7
                };

                DrawingCanvas.Children.Add(platformRectangle);

                // Отображаем платформу с учетом земли
                Canvas.SetLeft(platformRectangle, platform.Position.X);
                Canvas.SetTop(platformRectangle, groundLevel - platform.Position.Y - platform.Height);
            }

            // Отрисовка монеты
            if (currentLevel.Coin != null)
            {
                var coinEllipse = new Ellipse
                {
                    Fill = Brushes.Gold,
                    Width = currentLevel.Coin.Rad * 2,
                    Height = currentLevel.Coin.Rad * 2
                };
                DrawingCanvas.Children.Add(coinEllipse);
                Canvas.SetLeft(coinEllipse, currentLevel.Coin.Position.X - currentLevel.Coin.Rad);
                Canvas.SetTop(coinEllipse, groundLevel - currentLevel.Coin.Position.Y - currentLevel.Coin.Rad);
            }

            // Отрисовка портала
            var portalRectangle = new Rectangle
            {
                Fill = Brushes.Purple,
                Width = currentLevel.Portal.Width,
                Height = currentLevel.Portal.Heigth
            };
            DrawingCanvas.Children.Add(portalRectangle);
            Canvas.SetLeft(portalRectangle, currentLevel.Portal.Position.X);
            Canvas.SetTop(portalRectangle, groundLevel - currentLevel.Portal.Position.Y - currentLevel.Portal.Heigth);

            // Отрисовка земли
            var groundRectangle = new Rectangle
            {
                Fill = Brushes.Brown,
                Width = canvasWidth,
                Height = groundThickness
            };
            DrawingCanvas.Children.Add(groundRectangle);
            Canvas.SetLeft(groundRectangle, 0);
            Canvas.SetTop(groundRectangle, groundLevel);

            // Отрисовка шарика
            if (_viewModel.Ball != null)
            {
                // Переворачиваем ось Y для шарика
                float ballCanvasY = groundLevel - _viewModel.Ball.Position.Y - _viewModel.Ball.Rad;

                // Ограничиваем положение шарика
                // Пересчитываем ограничения с учетом новых размеров Canvas
                ballCanvasY = Math.Clamp(ballCanvasY, 0, canvasHeight - 2 * _viewModel.Ball.Rad);
                float ballCanvasX = Math.Clamp(_viewModel.Ball.Position.X - _viewModel.Ball.Rad, 0, canvasWidth - 2 * _viewModel.Ball.Rad);

                var ballEllipse = new Ellipse
                {
                    Fill = Brushes.Cyan,
                    Width = _viewModel.Ball.Rad * 2,
                    Height = _viewModel.Ball.Rad * 2
                };
                DrawingCanvas.Children.Add(ballEllipse);
                Canvas.SetLeft(ballEllipse, ballCanvasX);
                Canvas.SetTop(ballEllipse, ballCanvasY);

                // Логирование для отладки
                Debug.WriteLine($"Canvas Dimensions: {canvasWidth}x{canvasHeight}");
                Debug.WriteLine($"Ground: {groundLevel}");
                Debug.WriteLine($"Ball position: X={_viewModel.Ball.Position.X}, Y={_viewModel.Ball.Position.Y}");
            }
        }

    }
}
