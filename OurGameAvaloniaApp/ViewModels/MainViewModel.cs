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
        public ReactiveCommand<Unit, Unit> Stop { get; }

        public ReactiveCommand<Unit, Unit> Jump { get; }
        public ReactiveCommand<Unit, Unit> MoveLeft { get; }
        public ReactiveCommand<Unit, Unit> MoveRight { get; }
         public ReactiveCommand<Unit, Unit> StopMoving { get; }

    

    private const float BounceFactor = 0.8f;
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

        private bool isJumping = false;  
        private bool isOnGround = true;  


        bool _isMoveR;
        public bool IsMoveR { get => _isMoveR; set => this.RaiseAndSetIfChanged(ref _isMoveR, value); }

        bool _isMoveL;
        public bool IsMoveL 
        { 
            get => _isMoveL; 
            set => this.RaiseAndSetIfChanged(ref _isMoveL, value); 
        }

        DateTime starttime;



    //public void Recalculate(long tick)
    //{


    //    // Применяем гравитацию (ускорение вниз)
    //    if (!isOnGround)
    //    {
    //        Ball.Velocity += new Vector2(9.8f * dt, 0); // Ускорение вниз (гравитация)
    //                                                    // Обновление позиции
    //        Ball.Position += Ball.Velocity * dt;

    //    }

    //    // Замедление по оси X (горизонтальное замедление)
    //    const float MinVelocityX = 0.05f;
    //    if (Math.Abs(Ball.Velocity.X) > MinVelocityX)
    //    {
    //        Ball.Velocity = new Vector2(Ball.Velocity.X * 0.98f, Ball.Velocity.Y); // Замедление по оси X
    //    }
    //    else
    //    {
    //        Ball.Velocity = new Vector2(0, Ball.Velocity.Y); // Сброс скорости по оси X, если она мала
    //    }

    //    // Проверка на приземление
    //    if (Ball.Position.Y >= 500) // Предположим, что 500 - это уровень земли (высота экрана)
    //    {
    //        Ball.Position = new Vector2(Ball.Position.X, 500); // Ставим шарик на землю
    //        Ball.Velocity = new Vector2(Ball.Velocity.X, 0);  // Останавливаем вертикальную скорость при приземлении
    //        isJumping = false;  // Сброс флага прыжка
    //        isOnGround = true;  // Шарик на земле
    //    }
    //    else
    //    {
    //        isOnGround = false;  // Шарик в воздухе
    //    }

    //    // Округление для точности
    //    Ball.Position = new Vector2(
    //        (float)Math.Round(Ball.Position.X, 2),
    //        (float)Math.Round(Ball.Position.Y, 2)
    //    );
    //    Ball.Velocity = new Vector2(
    //        (float)Math.Round(Ball.Velocity.X, 2),
    //        (float)Math.Round(Ball.Velocity.Y, 2)
    //    );
    //}

    public void UpdateMovement(long tick)
    {
        float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000.0);
        if (dt > 0.1f) dt = 0.1f;
        starttime = DateTime.Now;
        const float frictionCoefficient = 8f;
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
                // Уменьшаем скорость на основе массы и коэффициента трения
                float deceleration = frictionCoefficient * Ball.Mass; // Вычисляем замедление
                Ball.Velocity = new Vector2(Ball.Velocity.X - Math.Sign(Ball.Velocity.X) * deceleration, Ball.Velocity.Y);
            }
        }

        // Проверяем, если скорость очень мала, устанавливаем её в 0
        if (Math.Abs(Ball.Velocity.X) < 0.1f)
        {
            Ball.Velocity = new Vector2(0, Ball.Velocity.Y); // Останавливаем шарик
        }

        Debug.WriteLine($"UpdateMovement: isMoveL={IsMoveL}, isMoveR={IsMoveR}, Velocity={Ball.Velocity}");

        // Обновляем позицию
        Ball.Position += Ball.Velocity * dt;

        // Ограничиваем позицию в пределах экрана
        Ball.Position = new Vector2(
            Math.Max(0, Math.Min(Ball.Position.X, WindowWidth)), // Ограничение по X
            Math.Max(0, Ball.Position.Y) // Ограничение по Y
        );
        if (Ball.Position.X <= Ball.Rad * 0.5 || Ball.Position.X >= windowWidth - Ball.Rad * 0.5) // Столкновение с левой границей
        {
            Ball.Velocity = new Vector2(0, Ball.Velocity.Y); // Останавливаем шарик
        }
       
    }

    public void ApplyGravity(long tick)
    {
        if (!isOnGround)
        {
            float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000.0);
            Ball.Velocity += new Vector2(0, 9.8f * dt); 

            if (Ball.Position.Y >= 500) 
            {
                Ball.Position = new Vector2(Ball.Position.X, 500); 
                Ball.Velocity = new Vector2(Ball.Velocity.X, 0); 
                isJumping = false;  
                isOnGround = true;  
            }
        }
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
        //void StartGameLoop()
        //{
        //    Observable
        //        .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)) // 60 FPS
        //        .TakeUntil(this.WhenAnyValue(t => t.GameActive).Where(act => !act))
        //        .Subscribe(_ =>
        //        {
        //            UpdateMovement(0); // Обновляем движение
        //        });
        //}

        MoveLeft = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        {
            IsMoveL = true;
            IsMoveR = false;
            return Task.Run(() =>
            {
                UpdateMovement(0);
                return Unit.Default;
            });
        });
        /*MoveLeft = ReactiveCommand.Create(() =>
        {
            IsMoveL = true;
            IsMoveR = false;
            UpdateMovement(0);
        });*/

        MoveRight = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        {
            IsMoveL = false;
            IsMoveR = true;
            return Task.Run(() =>
            {
                //UpdateMovement(0);
                return Unit.Default;
            });
        });

        StopMoving = ReactiveCommand.CreateFromTask<Unit, Unit>(_ =>
        {
            IsMoveL = false;
            IsMoveR = false;
            return Task.Run(() =>
            {
                //UpdateMovement(0);

                return Unit.Default;
            });
        });
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
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)) // 60 FPS
                .TakeUntil(this.WhenAnyValue(t => t.GameActive).Where(act => !act))
                .Subscribe(UpdateMovement);

                Observable
                    .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(16))
                    .TakeUntil(this.WhenAnyValue(t => t.GameActive).Where(act => !act))
                    .Subscribe(GenerateScene);

                Debug.WriteLine($"IsMoveL: {IsMoveL}, IsMoveR: {IsMoveR}");
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
                if (isOnGround) 
                {
                    Ball.Velocity = new Vector2(Ball.Velocity.X, 190); 
                    isJumping = true; 
                    isOnGround = false; 
                }
                return Unit.Default;
            });
        }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => active));


    }
}
