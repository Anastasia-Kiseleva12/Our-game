using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System;
using Avalonia.Media;
namespace OurGameAvaloniaApp.ViewModels;

public class Ball : ReactiveObject
{
    Vector2 position;
    public Vector2 Position { get => position; set => this.RaiseAndSetIfChanged(ref position, value); }

    Vector2 velocity;
    public Vector2 Velocity { get => velocity; set => this.RaiseAndSetIfChanged(ref velocity, value); }
    public required float Rad { get; init; }
    public required float Mass { get; init; }
}
public class Portal : ReactiveObject 
{
   Vector2 position;
   public Vector2 Position { get => position; set => this.RaiseAndSetIfChanged(ref position, value); }

   public required float Heigth { get; init; }
   public required float Width { get; init; }
}
public class Coin : ReactiveObject 
{
   Vector2 position;
   public Vector2 Position { get => position; set => this.RaiseAndSetIfChanged(ref position, value); }

   public required float Rad { get; init; }
}
public class Platform : ReactiveObject 
{
   Vector2 position;
   public Vector2 Position { get => position; set => this.RaiseAndSetIfChanged(ref position, value); }

   Vector2 velocity;
   public Vector2 Velocity { get => velocity; set => this.RaiseAndSetIfChanged(ref velocity, value); }
}
public class MainViewModel : ViewModelBase
    {
        Ball ball;
        public Ball Ball { get => ball; set => this.RaiseAndSetIfChanged(ref ball, value); }

        public DrawingImage Screen { get; } = new DrawingImage();

        public ReactiveCommand<Unit, Unit> Start { get; }
        public ReactiveCommand<Unit, Unit> Stop { get; }

        public ReactiveCommand<Unit, Unit> Jump { get; }
        public ReactiveCommand<Unit, Unit> MoveLeft { get; }
        public ReactiveCommand<Unit, Unit> MoveRight { get; }

        private const float BounceFactor = 0.8f;
        float ballMass = 1;
        public float BallMass { get => ballMass; set => this.RaiseAndSetIfChanged(ref ballMass, value); }
        float ballRad = 20;
        public float BallRad { get => ballRad; set => this.RaiseAndSetIfChanged(ref ballRad, value); }
        float roomWidth = 10f;
        public float RoomWidth { get => roomWidth; set => this.RaiseAndSetIfChanged(ref roomWidth, value); }
        float roomHeight = 10f;
        public float RoomHeight { get => roomHeight; set => this.RaiseAndSetIfChanged(ref roomHeight, value); }
        public ReactiveCommand<Unit, Unit> CreateGame { get; }
        bool gameActive;
        public bool GameActive { get => gameActive; set => this.RaiseAndSetIfChanged(ref gameActive, value); }

        public Subject<long> GenerateScene { get; } = new();

        DateTime starttime;
    public void Recalculate(long tick)
    {
        float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000);

        // Обновляем скорость с учетом гравитации
        Ball.Velocity += new Vector2(0, -9.8f * dt); // Обратите внимание на знак

        // Обновляем позицию
        Ball.Position += Ball.Velocity * dt;

        // Проверка на столкновение с нижней границей
        if (Ball.Position.Y < Ball.Rad)
        {
            Ball.Position = new Vector2(Ball.Position.X, Ball.Rad); // Устанавливаем на границу
            Ball.Velocity = new Vector2(Ball.Velocity.X, -Ball.Velocity.Y * BounceFactor); // Инвертируем скорость (отскок)
        }

        // Проверка на столкновение с верхней границей
        if (Ball.Position.Y > RoomHeight - Ball.Rad)
        {
            Ball.Position = new Vector2(Ball.Position.X, RoomHeight - Ball.Rad); // Устанавливаем на границу
            Ball.Velocity = new Vector2(Ball.Velocity.X, -Ball.Velocity.Y * BounceFactor); // Инвертируем скорость (отскок)
        }

        // Обновляем время для следующего вызова
        starttime = DateTime.Now;
    }
    public MainViewModel()
        {

        // Инициализация шарика с начальной позицией и скоростью
        Ball = new Ball() { Mass = BallMass, Rad = BallRad };

        // Создание команды для создания игры
        CreateGame = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
            {
            return Task.Run(() =>
            {
                // Создание нового шарика с теми же параметрами
                Ball = new Ball() { Mass = BallMass, Rad = BallRad };
                return Unit.Default;
            });
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => !active));
  

            Start = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
            {
                return Task.Run(() =>
                {
                    Ball.Velocity = new Vector2(0, 10);
                    GameActive = true;
                    starttime = DateTime.Now;
                    Observable
                    .Timer(new TimeSpan(0), new System.TimeSpan(0, 0, 0, 0, 100))
                    .TakeUntil(this.WhenAnyValue(t => t.GameActive).Where(act => !act))
                    .Subscribe(GenerateScene);
                    GenerateScene.Subscribe(Recalculate);
                    return Unit.Default;
                });
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => !active));

            Stop = ReactiveCommand.Create<Unit, Unit>(_ =>
            {
                GameActive = false;
                return Unit.Default;
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => active));


            Jump = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
            {
                return Task.Run(() =>
                {
                    if (Ball.Position.Y == Ball.Rad)
                    {
                        Ball.Velocity = new Vector2(Ball.Velocity.X, 10);
                    }
                    return Unit.Default;
                });
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => active));

            MoveLeft = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
            {
                return Task.Run(() =>
                {
                    Ball.Velocity = new Vector2(Ball.Velocity.X - 0.1f, Ball.Velocity.Y);
                    return Unit.Default;
                });
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => active));

            MoveRight = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
            {
                return Task.Run(() =>
                {
                    Ball.Velocity = new Vector2(Ball.Velocity.X + 0.1f, Ball.Velocity.Y);
                    return Unit.Default;
                });
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => active));

        }
    
}
