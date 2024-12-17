using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System;
using Avalonia.Media;
using System.Diagnostics;
using Avalonia.Input;
using System.Net.Sockets;
using LibVLCSharp.Shared;
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
        //public ReactiveCommand<Unit, Unit> Stop { get; }

        //public ReactiveCommand<Unit, Unit> Jump { get; }
        //public ReactiveCommand<Unit, Unit> MoveLeft { get; }
        //public ReactiveCommand<Unit, Unit> MoveRight { get; }
        // public ReactiveCommand<Unit, Unit> StopMoving { get; }

   
        float ballMass = 1;
        public float BallMass { get => ballMass; set => this.RaiseAndSetIfChanged(ref ballMass, value); }
        float ballRad = 20;
        public float BallRad { get => ballRad; set => this.RaiseAndSetIfChanged(ref ballRad, value); }
   
        public ReactiveCommand<Unit, Unit> CreateGame { get; }
        bool gameActive;
        public bool GameActive { get => gameActive; set => this.RaiseAndSetIfChanged(ref gameActive, value); }

        int windowHeight;
        public int WindowHeight { get => windowHeight; set => this.RaiseAndSetIfChanged(ref windowHeight, value); }

        int windowWidth;
        public int WindowWidth { get => windowWidth; set => this.RaiseAndSetIfChanged(ref windowWidth, value); }
        public Subject<long> GenerateScene { get; } = new();

        private bool isOnGround = true;

        bool isJumping;
        public bool IsJumping
        { get => isJumping;
        set =>  this.RaiseAndSetIfChanged(ref isJumping, value);
        }

        bool _isMoveR;
        public bool IsMoveR { get => _isMoveR; set => this.RaiseAndSetIfChanged(ref _isMoveR, value); }

        bool _isMoveL;
        public bool IsMoveL 
        { 
            get => _isMoveL; 
            set => this.RaiseAndSetIfChanged(ref _isMoveL, value); 
        }

        DateTime starttime;

    public void UpdateMovement(long tick)
    {
        float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000.0);
        if (dt > 0.1f) dt = 0.1f;
        starttime = DateTime.Now;
        const float frictionCoefficient = 130f;
        float deceleration = 0;

        // Проверяем нажатие клавиш
        if (IsMoveL)
        {
            Ball.Velocity = new Vector2(Ball.Velocity.X - 5, Ball.Velocity.Y); // Двигаем влево
        }
        else if (IsMoveR)
        {
            Ball.Velocity = new Vector2(Ball.Velocity.X + 5, Ball.Velocity.Y); // Двигаем вправо
        }
        else
        {
            if (Math.Abs(Ball.Velocity.X) > 0)
            {
                deceleration = frictionCoefficient * Ball.Mass * dt;
                Ball.Velocity = new Vector2(Ball.Velocity.X - Math.Sign(Ball.Velocity.X) * deceleration, Ball.Velocity.Y);

            }
            // Проверяем, если скорость очень мала, устанавливаем её в 0
            if (Math.Abs(Ball.Velocity.X) <= deceleration)
            {
                Ball.Velocity = new Vector2(0, Ball.Velocity.Y);
            }

        }

        Debug.WriteLine($"UpdateMovement: isMoveL={IsMoveL}, isMoveR={IsMoveR}, Velocity={Ball.Velocity}");

        // Обновляем позицию
        Ball.Position += Ball.Velocity * dt;

        // Ограничиваем позицию в пределах экрана
        Ball.Position = new Vector2(
        Math.Clamp(Ball.Position.X, Ball.Rad * 0.5f, WindowWidth - Ball.Rad * 0.5f),
        Math.Max(0, Ball.Position.Y)
        );

        //// Обработка столкновения с левой границей
        //if (Ball.Position.X - Ball.Rad <= 0)
        //{
        //    Ball.Position = new Vector2(Ball.Rad, Ball.Position.Y); // Корректируем позицию
        //    Ball.Velocity = new Vector2(0, Ball.Velocity.Y); // Сбрасываем горизонтальную скорость
        //}

        //// Обработка столкновения с правой границей
        //if (Ball.Position.X + Ball.Rad >= WindowWidth)
        //{
        //    Ball.Position = new Vector2(WindowWidth - Ball.Rad, Ball.Position.Y); // Корректируем позицию
        //    Ball.Velocity = new Vector2(0, Ball.Velocity.Y); // Сбрасываем горизонтальную скорость
        //}
    }

    public void ApplyPhysics(long tick)
    {
        float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000.0);
        if (dt > 0.1f) dt = 0.1f; 

        const float gravity = -9.8f * 300; 
        const float jumpForce = 400f;    // Сила прыжка

        // Применяем гравитацию к скорости
        var newVelocity = Ball.Velocity + new Vector2(0, gravity * dt);
        var newPosition = Ball.Position + newVelocity * dt;

        float groundLevel = 0;

        // Проверка столкновения с землёй
        if (newPosition.Y - Ball.Rad <= groundLevel)
        {
            // Шарик достигает земли
            newPosition = new Vector2(newPosition.X, groundLevel + Ball.Rad);

            if (Math.Abs(newVelocity.Y) > 300) // Если скорость выше порога
            {
                const float bounceFactor = 0.6f; // Коэффициент упругости 
                newVelocity = new Vector2(newVelocity.X, -newVelocity.Y * bounceFactor); // Создаём отскок
                isOnGround = false;
            }
            else
            {
                newVelocity = new Vector2(newVelocity.X, 0);
                isOnGround = true;
            }
        }
        else
        {
            isOnGround = false;
        }

        // Обработка прыжка
        if (IsJumping && isOnGround)
        {
            newVelocity = new Vector2(newVelocity.X, jumpForce); 
            isOnGround = false; 
            IsJumping = false; 
            Debug.WriteLine($"Jump initiated: IsJumping={IsJumping}, isOnGround={isOnGround}, Velocity={newVelocity}, Position={newPosition}");
        }

        // Проверка столкновения с левой и правой границей
        if (newPosition.X - Ball.Rad <= 0) 
        {
            const float bounceFactor = 0.6f; 
            newPosition = new Vector2(Ball.Rad, newPosition.Y); 
            newVelocity = new Vector2(-newVelocity.X * bounceFactor, newVelocity.Y); 
        }

        if (newPosition.X + Ball.Rad >= WindowWidth) 
        {
            const float bounceFactor = 0.6f; 
            newPosition = new Vector2(WindowWidth - Ball.Rad, newPosition.Y); 
            newVelocity = new Vector2(-newVelocity.X * bounceFactor, newVelocity.Y); 
        }
        // Обновление состояния шарика
        Ball.Velocity = newVelocity;
        Ball.Position = newPosition;

        starttime = DateTime.Now;

        Debug.WriteLine($"Position: {Ball.Position}, Velocity: {Ball.Velocity}, isOnGround: {isOnGround}, IsJumping: {IsJumping}");
    }

    public MainViewModel()
        {

        Ball = new Ball() { Mass = BallMass, Rad = BallRad, Position = new Vector2(712, 20) };

        CreateGame = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
            {
            return Task.Run(() =>
            {

                Ball = new Ball() { Mass = BallMass, Rad = BallRad };
                return Unit.Default;
            });
            }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => !active));

        //MoveLeft = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        //{
        //    IsMoveL = true;
        //    IsMoveR = false;
        //    return Task.Run(() =>
        //    {
        //        UpdateMovement(0);
        //        return Unit.Default;
        //    });
        //});
        /*MoveLeft = ReactiveCommand.Create(() =>
        {
            IsMoveL = true;
            IsMoveR = false;
            UpdateMovement(0);
        });*/

        //MoveRight = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        //{
        //    IsMoveL = false;
        //    IsMoveR = true;
        //    return Task.Run(() =>
        //    {
        //        //UpdateMovement(0);
        //        return Unit.Default;
        //    });
        //});

        //StopMoving = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        //{
        //    IsMoveL = false;
        //    IsMoveR = false;
        //    return Task.Run(() =>
        //    {
        //        //UpdateMovement(0);

        //        return Unit.Default;
        //    });
        //});
        this.WhenAnyValue(x => x.IsMoveL, x => x.IsMoveR)
            .Subscribe(_ =>
            {
                //UpdateMovement(0);
                // Здесь вы можете выполнять действия, когда флаги изменяются
              Debug.WriteLine($"IsMoveL: {IsMoveL}, IsMoveR: {IsMoveR}");
            });

        Start = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        {
            return Task.Run(() =>
            {
                GameActive = true;
                Ball.Velocity = new Vector2(0, 0);
                starttime = DateTime.Now;

                Observable
                    .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(16))
                    .TakeUntil(this.WhenAnyValue(t => t.GameActive).Where(act => !act))
                    .Subscribe(_ =>
                    {
                        UpdateMovement(0); 
                        ApplyPhysics(0);  
                        GenerateScene.OnNext(0);

                    });
                Debug.WriteLine($"IsMoveL: {IsMoveL}, IsMoveR: {IsMoveR}");
                return Unit.Default;
            });
        }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => !active));

        //Stop = ReactiveCommand.Create<Unit, Unit>(_ =>
        //{
        //        GameActive = false;
        //        return Unit.Default;
        //}, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => active));

    }
}
