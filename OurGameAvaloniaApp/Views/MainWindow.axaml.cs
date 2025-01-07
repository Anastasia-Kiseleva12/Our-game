using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LibVLCSharp.Shared;
using OurGameAvaloniaApp.ViewModels;
using ReactiveUI;
using System;
using System.IO;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Numerics;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;




namespace OurGameAvaloniaApp.Views
{
   public class AudioPlayer
   {
      private IWavePlayer waveOut;
      private AudioFileReader audioFileReader;
      private VolumeSampleProvider volumeProvider;

      public float Volume
      {
         get => audioFileReader?.Volume ?? 0f;
         set
         {
            if (audioFileReader != null)
            {
               audioFileReader.Volume = value; // Устанавливаем громкость
            }
         }
      }

      public void Play(string filePath)
      {
         waveOut = new WaveOutEvent();
         audioFileReader = new AudioFileReader(filePath);
         volumeProvider = new VolumeSampleProvider(audioFileReader);
         volumeProvider.Volume = 0.005f;
         waveOut.Init(volumeProvider);
         waveOut.Play();
      }

      public void Stop()
      {
         waveOut?.Stop();
         waveOut?.Dispose();
         audioFileReader?.Dispose();
      }
   }
   public partial class MainWindow : Window
    {

        private MainViewModel _viewModel;
        public static bool isPaused = false;
        private AudioPlayer audioPlayer;
        private readonly List<Control> _platforms = new();
        private Ellipse _ballEllipse;
        private Ellipse _coinEllipse;
        private Rectangle _portalRectangle;
        private Rectangle _groundRectangle;

        public MainWindow()
        {
            InitializeComponent();  // Сначала инициализируем компоненты, чтобы XAML правильно установил DataContext
            _viewModel = (MainViewModel)DataContext;  // Теперь _viewModel не будет null

            // Дальнейшая инициализация
            InitializeDrawingObjects();
            Core.Initialize();
            this.Focus();
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

         audioPlayer = new AudioPlayer();
            PlaySound();
         VolumeSlider.Value = audioPlayer.Volume;
      }
      private void PlaySound()
      {
         string relativePath = "Assets/music.wav";
         string filePath = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
         audioPlayer.Play(filePath);
      }

      private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
      {
         // Устанавливаем громкость в зависимости от значения слайдера
         audioPlayer.Volume = (float)e.NewValue;
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
        private void ReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            ReferenceTextBlock.IsVisible = !ReferenceTextBlock.IsVisible;
        }
        private void InitializeDrawingObjects()
        {
            // Земля
            _groundRectangle = new Rectangle
            {
                Fill = Brushes.Brown,
                Height = 10 // Толщина земли
            };
            DrawingCanvas.Children.Add(_groundRectangle);

            // Платформы
            foreach (var platform in _viewModel.LevelManager.CurrentLevel.Platforms)
            {
                var platformRect = new Rectangle
                {
                    Fill = Brushes.Gray,
                    Width = platform.Width,
                    Height = platform.Height + 7
                };
                _platforms.Add(platformRect);
                DrawingCanvas.Children.Add(platformRect);
            }

            // Монета
            _coinEllipse = new Ellipse
            {
                Fill = Brushes.Gold
            };
            DrawingCanvas.Children.Add(_coinEllipse);

            // Портал
            _portalRectangle = new Rectangle
            {
                Fill = Brushes.Purple
            };
            DrawingCanvas.Children.Add(_portalRectangle);

            // Шарик
            _ballEllipse = new Ellipse
            {
                Fill = Brushes.Cyan
            };
            DrawingCanvas.Children.Add(_ballEllipse);
        }
        private void Redraw(long tick)
        {
            var canvasWidth = (float)DrawingCanvas.Bounds.Width;
            var canvasHeight = (float)DrawingCanvas.Bounds.Height;
            var groundLevel = canvasHeight - 10; // Уровень земли
            var currentLevel = _viewModel.LevelManager.CurrentLevel;

            // Обновляем землю
            _groundRectangle.Width = canvasWidth;
            Canvas.SetLeft(_groundRectangle, 0);
            Canvas.SetTop(_groundRectangle, groundLevel);

            // Обновляем платформы
            for (int i = 0; i < _platforms.Count; i++)
            {
                var platform = currentLevel.Platforms[i];
                var platformRect = _platforms[i];

                platformRect.Width = platform.Width;
                platformRect.Height = platform.Height + 7;

                platformRect.RenderTransform = new TranslateTransform(platform.Position.X, groundLevel - platform.Position.Y - platform.Height);
            }

            if (currentLevel.Coin != null)
            {
                // Настраиваем размеры и положение монеты
                _coinEllipse.Width = _coinEllipse.Height = currentLevel.Coin.Rad * 2;
                _coinEllipse.RenderTransform = new TranslateTransform(
                    currentLevel.Coin.Position.X - currentLevel.Coin.Rad,
                    groundLevel - currentLevel.Coin.Position.Y - currentLevel.Coin.Rad);

                // Если монета ранее скрыта, добавляем её обратно
                if (!DrawingCanvas.Children.Contains(_coinEllipse))
                {
                    DrawingCanvas.Children.Add(_coinEllipse);
                }

                _coinEllipse.Fill = Brushes.Gold; // Устанавливаем нормальный цвет монеты
            }
            else
            {
                // Убираем монету с канваса, если она всё ещё присутствует
                if (DrawingCanvas.Children.Contains(_coinEllipse))
                {
                    DrawingCanvas.Children.Remove(_coinEllipse);
                }
            }


            // Обновляем портал
            var portalBrush = currentLevel.IsCoinCollected ? Brushes.Green : Brushes.Purple;
            _portalRectangle.Fill = portalBrush;

            _portalRectangle.Width = currentLevel.Portal.Width;
            _portalRectangle.Height = currentLevel.Portal.Heigth;
            _portalRectangle.RenderTransform = new TranslateTransform(
                currentLevel.Portal.Position.X,
                groundLevel - currentLevel.Portal.Position.Y - currentLevel.Portal.Heigth);

            // Обновляем шарик
            if (_viewModel.Ball != null)
            {
                float ballCanvasX = Math.Clamp(_viewModel.Ball.Position.X - _viewModel.Ball.Rad, 0, canvasWidth - 2 * _viewModel.Ball.Rad);
                float ballCanvasY = groundLevel - _viewModel.Ball.Position.Y - _viewModel.Ball.Rad;

                _ballEllipse.Width = _ballEllipse.Height = _viewModel.Ball.Rad * 2;
                _ballEllipse.RenderTransform = new TranslateTransform(ballCanvasX, ballCanvasY);
            }
        }


    }
}
