using System.Numerics;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System;
using Avalonia.Media;
using System.Diagnostics;
using System.Collections.Generic;
using OurGameAvaloniaApp.Views;
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
    public float RotationAngle { get; set; }

}
public class Platform : ReactiveObject 
{
   Vector2 position;
   public Vector2 Position { get => position; set => this.RaiseAndSetIfChanged(ref position, value); }

   Vector2 velocity;
   public Vector2 Velocity { get => velocity; set => this.RaiseAndSetIfChanged(ref velocity, value); }

    public required float Height { get; init; }
    public required float Width { get; init; }
}
public class Level : ReactiveObject
{
    public List<Platform> Platforms { get; set; } = new();
    public Coin Coin { get; set; }
    public Portal Portal { get; set; }
    public int CollectedCoinsCount { get; set; } = 0; // Счетчик собранных монет
    public bool IsCoinCollected { get; set; }

}


public class LevelManager : ReactiveObject
{
    public List<Level> levels = new();
    public int currentLevelIndex = 0;
    public int TotalCollectedCoins = 0;
    public Level CurrentLevel => levels[currentLevelIndex];
    public void AddLevel(Level level)
    {
        levels.Add(level);
    }

    public bool IsLevelComplete(Ball ball)
    {
        // Проверяем, собрана ли монетка
        if (CurrentLevel.Coin != null &&
            CurrentLevel.CollectedCoinsCount >= 2)
        {
            CurrentLevel.Coin = null; // Монетка собрана
        }

        // Проверяем, находится ли шарик в портале
        if (CurrentLevel.Coin == null &&
            ball.Position.X >= CurrentLevel.Portal.Position.X &&
            ball.Position.X <= CurrentLevel.Portal.Position.X + CurrentLevel.Portal.Width &&
            ball.Position.Y >= CurrentLevel.Portal.Position.Y &&
            ball.Position.Y <= CurrentLevel.Portal.Position.Y + CurrentLevel.Portal.Heigth)
        {
            return true;
        }

        return false;
    }

    public bool LoadNextLevel()
    {
        if (currentLevelIndex + 1 < levels.Count)
        {
            currentLevelIndex++;
            return true;
        }
        return false; // Нет больше уровней
    }
}

public class MainViewModel : ViewModelBase
{
    private MainWindow _mainWindow;   

    Ball ball;
    public Ball Ball { get => ball; set => this.RaiseAndSetIfChanged(ref ball, value); }

    LevelManager levelManager;
    public LevelManager LevelManager { get => levelManager; set => this.RaiseAndSetIfChanged(ref levelManager, value); }

    Level level;
    public Level Level { get => level; set => this.RaiseAndSetIfChanged(ref level, value); }
    public DrawingImage Screen { get; } = new DrawingImage();
    public ReactiveCommand<Unit, Unit> Start { get; }

   public ReactiveCommand<Unit, Unit> End { get; }

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

    public void UpdateMovement(long tick, bool isPaused)
    {
        if (isPaused)
        {
           return; // Останавливаем выполнение функции, если игра на паузе
        }
        float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000.0);
        if (dt > 0.1f) dt = 0.1f;
        starttime = DateTime.Now;
        const float frictionCoefficient = 130f;
        float deceleration = 0;

        // Проверяем нажатие клавиш
        if (IsMoveL)
        {
             if(Math.Abs(Ball.Velocity.X) <= 500)
             {
                Ball.Velocity = new Vector2(Ball.Velocity.X - 3, Ball.Velocity.Y); // Двигаем влево
             }
        }
        else if (IsMoveR)
        {
            if (Math.Abs(Ball.Velocity.X) <= 500)
            {
                Ball.Velocity = new Vector2(Ball.Velocity.X + 3, Ball.Velocity.Y); // Двигаем вправо
            }
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

    }
    public void ApplyPhysics(long tick, bool isPaused)
    {
        if (isPaused)
        {
           return; // Останавливаем выполнение функции, если игра на паузе
        }

        float dt = (float)((DateTime.Now - starttime).TotalMilliseconds / 1000.0);
        if (dt > 0.1f) dt = 0.1f;

        const float gravity = -9.8f * 300;
        const float jumpForce = 500f;    // Сила прыжка

        // Применяем гравитацию к скорости
        var newVelocity = Ball.Velocity + new Vector2(0, gravity * dt);
        var newPosition = Ball.Position + newVelocity * dt;

        float groundLevel = 0;
        bool isOnPlatform = false;

        HandleCollisions(ref newPosition, ref newVelocity, ref isOnPlatform, ref isOnGround, dt);

        // Проверка столкновения с землёй
        if (newPosition.Y - Ball.Rad <= groundLevel)
        {
            // Шарик достигает земли
            newPosition = new Vector2(newPosition.X, groundLevel + Ball.Rad);

            if (Math.Abs(newVelocity.Y) > 350) // Если скорость выше порога
            {
                const float bounceFactor = 0.5f; // Коэффициент упругости 
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
        if (IsJumping && isOnGround || isOnPlatform && IsJumping)
        {
            newVelocity = new Vector2(newVelocity.X, jumpForce);
            isOnGround = false;
            IsJumping = false;
            isOnPlatform = false;
        }

        if (newPosition.X - Ball.Rad <= 0)
        {
            const float bounceFactor = 0.6f;
            newPosition = new Vector2(Ball.Rad, newPosition.Y);  // Обновляем позицию, чтобы шарик не выходил за пределы
            newVelocity = new Vector2(-newVelocity.X * bounceFactor, newVelocity.Y);  // Меняем направление
        }

        if (newPosition.X + Ball.Rad >= WindowWidth)
        {
            const float bounceFactor = 0.6f;
            newPosition = new Vector2(WindowWidth - Ball.Rad, newPosition.Y);  // Обновляем позицию
            newVelocity = new Vector2(-newVelocity.X * bounceFactor, newVelocity.Y);  // Меняем направление
        }

        if (newPosition.Y + Ball.Rad >= WindowHeight)
        {
            const float bounceFactor = 0.6f;
            newPosition = new Vector2(newPosition.X, WindowHeight - Ball.Rad);  // Обновляем позицию, чтобы шарик не выходил за нижнюю границу
            newVelocity = new Vector2(newVelocity.X, -newVelocity.Y * bounceFactor);  // Меняем направление
        }

        if (newPosition.Y + Ball.Rad >= WindowHeight)
        {
            const float bounceFactor = 0.6f;
            newPosition = new Vector2(newPosition.X, WindowHeight - Ball.Rad);  // Обновляем позицию, чтобы шарик не выходил за нижнюю границу
            newVelocity = new Vector2(newVelocity.X, -newVelocity.Y * bounceFactor);  // Меняем направление
        }

        // Обновление состояния шарика
        Ball.Velocity = newVelocity;
        Ball.Position = newPosition;

        starttime = DateTime.Now;

        //Debug.WriteLine($"Position: {Ball.Position}, Velocity: {Ball.Velocity}, isOnGround: {isOnGround}, IsJumping: {IsJumping}");
    }
    private bool HandleCollisions(ref Vector2 newPosition, ref Vector2 newVelocity, ref bool isOnPlatform, ref bool isOnGround, float dt)
    {
        // Столкновение с платформами
        foreach (var platform in Level.Platforms)
        {
            var LeftUppPoint = new Vector2(platform.Position.X, platform.Position.Y + platform.Height);
            var RightUppPoint = new Vector2(platform.Position.X + platform.Width, platform.Position.Y + platform.Height);
            var LeftDownPoint = new Vector2(platform.Position.X, platform.Position.Y);
            var RightDownPoint = new Vector2(platform.Position.X + platform.Width, platform.Position.Y);
            // Проверка столкновения с платформой верхней границей
            if (newPosition.X + Ball.Rad > LeftUppPoint.X && newPosition.X - Ball.Rad < RightUppPoint.X && newPosition.Y - Ball.Rad <= platform.Position.Y + platform.Height && newPosition.Y - Ball.Rad > platform.Position.Y - platform.Height )                 
            {
                // Шарик касается платформы
                newPosition = new Vector2(newPosition.X, platform.Position.Y + Ball.Rad + platform.Height); // Устанавливаем мяч на платформу
                newVelocity = new Vector2(newVelocity.X, 0); // Останавливаем вертикальную скорость
                isOnPlatform = true; // Шарик стоит на платформе
            }
            //Столкновение с нижней границей
            if (newPosition.X + Ball.Rad > LeftUppPoint.X && newPosition.X - Ball.Rad < RightUppPoint.X && newPosition.Y + Ball.Rad < platform.Position.Y && newPosition.Y + Ball.Rad >= platform.Position.Y - 7)
            {
                newVelocity = new Vector2(newVelocity.X, -100);  // Отскок (умножаем вертикальную скорость на коэффициент отскока)
            }

        }
        // Столкновение с монеткой 
        if (Level.Coin != null)
        {
            float distance = Vector2.Distance(newPosition, Level.Coin.Position);

            if (distance <= Ball.Rad + Level.Coin.Rad) // Если мяч касается монетки
            {
                Level.Coin = null; // Монетка исчезает
                Level.IsCoinCollected = true; // Флаг собранной монеты
                Level.CollectedCoinsCount += 1;
            }
        }
        return isOnPlatform;
    }
    public MainViewModel()
        {

        LevelManager = new LevelManager();
        // Создаем уровни
        var level1 = new Level
        {
            Platforms = new List<Platform>
        {
            new Platform { Position = new Vector2(0, 300), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(100, 500), Velocity = Vector2.Zero , Height = 20 , Width = 160 },
            new Platform { Position = new Vector2(300, 150), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(750, 850), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(1300, 200), Velocity = Vector2.Zero , Height = 20 , Width = 200 },
            new Platform { Position = new Vector2(800, 500), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(1100, 650), Velocity = Vector2.Zero , Height = 20 , Width = 300 }

        },
           Coin = new Coin { Position = new Vector2(40, 540), Rad = 20 },
           Portal = new Portal { Position = new Vector2(1300, 0), Width = 50, Heigth = 50 }
        };
      var level2 = new Level
      {
          Platforms = new List<Platform>
        {
            new Platform { Position = new Vector2(0, 300), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(300, 150), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(600, 450), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(900, 600), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(1200, 750), Velocity = Vector2.Zero , Height = 20 , Width = 300 },
            new Platform { Position = new Vector2(1350, 900), Velocity = Vector2.Zero , Height = 20 , Width = 800 },
            new Platform { Position = new Vector2(1350, 500), Velocity = Vector2.Zero , Height = 20 , Width = 800 },
            new Platform { Position = new Vector2(1350, 350), Velocity = Vector2.Zero , Height = 20 , Width = 800 },
        },
            Coin = new Coin { Position = new Vector2(150, 350), Rad = 20 },
            Portal = new Portal { Position = new Vector2(1800, 400), Width = 50, Heigth = 50 }
        };
        var level3 = new Level
        {
            Platforms = new List<Platform>
        {
            new Platform { Position = new Vector2(412, 70), Velocity = Vector2.Zero , Height = 20 , Width = 300},
            new Platform { Position = new Vector2(0, 140), Velocity = Vector2.Zero , Height = 20 , Width = 300},
            new Platform { Position = new Vector2(412, 210), Velocity = Vector2.Zero , Height = 20 , Width = 1000},
            new Platform { Position = new Vector2(1370, 350), Velocity = Vector2.Zero , Height = 20 , Width = 1000},
            new Platform { Position = new Vector2(1410, 480), Velocity = Vector2.Zero , Height = 20 , Width = 1000},
            new Platform { Position = new Vector2(300, 650), Velocity = Vector2.Zero , Height = 20 , Width = 500},
            new Platform { Position = new Vector2(0, 850), Velocity = Vector2.Zero , Height = 20 , Width = 270},
        },
            Coin = new Coin { Position = new Vector2(20, 890), Rad = 20 },
            Portal = new Portal { Position = new Vector2(1800, 375), Width = 50, Heigth = 50 }
        };

        LevelManager.AddLevel(level1);
        LevelManager.AddLevel(level2);
        LevelManager.AddLevel(level3);

        Level = LevelManager.CurrentLevel;

        Ball = new Ball() { Mass = BallMass, Rad = BallRad, Position = new Vector2(712, 50) };

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
                        UpdateMovement(0, MainWindow.isPaused);
                        ApplyPhysics(0, MainWindow.isPaused);
                        if (levelManager.IsLevelComplete(Ball))
                        {
                            if (levelManager.LoadNextLevel())
                            {
                                Level = levelManager.CurrentLevel; // Обновляем текущий уровень
                                Ball.Position = new Vector2(712, 20); // Сбрасываем позицию шарика
                                Ball.Velocity = Vector2.Zero; // Сбрасываем скорость шарика
                            }
                            else
                            {
                                GameActive = false; // Конец игры
                            }
                        }
                        GenerateScene.OnNext(0);

                    });
                Debug.WriteLine($"IsMoveL: {IsMoveL}, IsMoveR: {IsMoveR}");
                return Unit.Default;
            });
        }, this.WhenAnyValue(t => t.GameActive).ObserveOn(RxApp.MainThreadScheduler).Select(active => !active));

    }
}
