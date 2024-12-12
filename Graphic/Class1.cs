using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;

namespace Graphic
{
    public class Class1 : GameWindow
    {
        public Class1() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            // Конструктор
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.5f, 0.5f, 0.5f, 1.0f); // Установите цвет фона
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit); // Очистка буфера цвета

            // Пример отрисовки треугольника
            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(1.0f, 0.0f, 0.0f); // Красный цвет
            GL.Vertex2(-0.5f, -0.5f);
            GL.Color3(0.0f, 1.0f, 0.0f); // Зеленый цвет
            GL.Vertex2(0.5f, -0.5f);
            GL.Color3(0.0f, 0.0f, 1.0f); // Синий цвет
            GL.Vertex2(0.0f, 0.5f);
            GL.End();

            SwapBuffers(); // Обмен буферов
        }
    }
}