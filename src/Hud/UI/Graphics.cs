using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.UI.Renderers;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Windows;
using System;
using System.Threading;
using System.Windows.Forms;
using ImGuiNET;
using System.Runtime.InteropServices;
using PoeHUD.Controllers;
using PoeHUD.Framework.InputHooks;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = SharpDX.Vector2;
using ImVec2 = System.Numerics.Vector2;
using ImVec4 = System.Numerics.Vector4;
namespace PoeHUD.Hud.UI
{
    public sealed class Graphics : IDisposable
    {
        private const CreateFlags CREATE_FLAGS = CreateFlags.Multithreaded | CreateFlags.HardwareVertexProcessing;
        private readonly DeviceEx device;
        private readonly Direct3DEx direct3D;
        private readonly FontRenderer fontRenderer;
        private readonly TextureRenderer textureRenderer;
        private readonly Action reset;
        private PresentParameters presentParameters;
        private bool resized;
        private bool running = true;
        private readonly ManualResetEventSlim renderLocker = new ManualResetEventSlim(false);
        private RenderForm _form;
        public Graphics(RenderForm form, int width, int height)
        {
            _form = form;
            reset = () => form.Invoke(new Action(() =>
            {
                device.Reset(presentParameters);
                fontRenderer.Flush();
                textureRenderer.Flush();
                resized = false;
            }));
            form.UserResized += (sender, args) => Resize(form.ClientSize.Width, form.ClientSize.Height);
            presentParameters = new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                BackBufferFormat = Format.A8R8G8B8,
                BackBufferCount = 1,
                BackBufferWidth = width,
                BackBufferHeight = height,
                PresentationInterval = PresentInterval.One,
                MultiSampleType = MultisampleType.None,
                MultiSampleQuality = 0,
                PresentFlags = PresentFlags.LockableBackBuffer
            };
            direct3D = new Direct3DEx();
            device = new DeviceEx(direct3D, 0, DeviceType.Hardware, form.Handle, CREATE_FLAGS, presentParameters);
            fontRenderer = new FontRenderer(device);
            textureRenderer = new TextureRenderer(device);



            var io = ImGui.GetIO();
            io.FontAtlas.AddDefaultFont();
            SetStyle();
            PrepareTextureImGui();
            SetOpenTKKeyMappings(io);

            //TODO: Need fix input

            KeyboardHook.KeyDown += OnKeyboardHookOnKeyDown;
            KeyboardHook.KeyUp += OnKeyboardHookOnKeyUp;

            MouseHook.MouseWheel += OnMouseHookOnMouseWheel;
            io.DisplaySize = new ImVec2(form.ClientSize.Width, form.ClientSize.Height);
            io.DisplayFramebufferScale = new ImVec2(form.ClientSize.Width * 1.0f / form.ClientSize.Height);


            io.DeltaTime = 1f / 60f;

            renderLocker.Reset();
        }

        void SetStyle()
        {
            var style = ImGui.GetStyle();
            //style.ChildWindowRounding = 3f;
            style.GrabRounding = 0f;
            style.WindowRounding = 0f;

            style.FrameRounding = 3f;
            style.WindowTitleAlign = Align.Center;

            ImGui.PushStyleColor(ColorTarget.Text, new ImVec4(0.82f, 0.81f, 0.81f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.TextDisabled, new ImVec4(0.50f, 0.50f, 0.50f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.WindowBg, new ImVec4(0.26f, 0.26f, 0.26f, 0.78f));
            ImGui.PushStyleColor(ColorTarget.ChildWindowBg, new ImVec4(0.28f, 0.28f, 0.28f, 0.8f));
            ImGui.PushStyleColor(ColorTarget.PopupBg, new ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.Border, new ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.BorderShadow, new ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.FrameBg, new ImVec4(0.31f, 0.56f, 0.64f, 0.36f));
            ImGui.PushStyleColor(ColorTarget.FrameBgHovered, new ImVec4(0.20f, 0.22f, 0.34f, 0.67f));
            ImGui.PushStyleColor(ColorTarget.FrameBgActive, new ImVec4(0.16f, 0.16f, 0.16f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.TitleBg, new ImVec4(0.36f, 0.36f, 0.36f, 0.61f));
            ImGui.PushStyleColor(ColorTarget.TitleBgActive, new ImVec4(0.36f, 0.36f, 0.36f, 0.80f));
            ImGui.PushStyleColor(ColorTarget.TitleBgCollapsed, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.MenuBarBg, new ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ScrollbarBg, new ImVec4(0.21f, 0.21f, 0.21f, 0.58f));
            ImGui.PushStyleColor(ColorTarget.ScrollbarGrab, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ScrollbarGrabHovered, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ScrollbarGrabActive, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ComboBg, new ImVec4(0.32f, 0.32f, 0.32f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.CheckMark, new ImVec4(0.78f, 0.78f, 0.78f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.SliderGrab, new ImVec4(0.74f, 0.74f, 0.74f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.SliderGrabActive, new ImVec4(0.74f, 0.74f, 0.74f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.Button, new ImVec4(0.64f, 0.23f, 0.23f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ButtonHovered, new ImVec4(0.90f, 0.31f, 0.31f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ButtonActive, new ImVec4(0.40f, 0.22f, 0.22f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.Header, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.HeaderHovered, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.HeaderActive, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ResizeGrip, new ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ResizeGripHovered, new ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ResizeGripActive, new ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.CloseButton, new ImVec4(0.59f, 0.59f, 0.59f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.CloseButtonHovered, new ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.CloseButtonActive, new ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.PlotLines, new ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.PlotLinesHovered, new ImVec4(1.00f, 0.43f, 0.35f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.PlotHistogram, new ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.PlotHistogramHovered, new ImVec4(1.00f, 0.60f, 0.00f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.TextSelectedBg, new ImVec4(0.32f, 0.52f, 0.65f, 1.00f));
            ImGui.PushStyleColor(ColorTarget.ModalWindowDarkening, new ImVec4(0.20f, 0.20f, 0.20f, 0.50f));
        }
        void OnMouseHookOnMouseWheel(MouseInfo info)
        {
            var upOrDown = 0f;
            if (info.WheelDelta == 120)
            {
                upOrDown = 1;
            }
            else if (info.WheelDelta == 65416)
            {
                upOrDown = -1;
            }
            var delta = upOrDown;
            ImGui.GetIO().MouseWheel = delta;
        }
        void OnKeyboardHookOnKeyDown(KeyInfo info)
        {
            var io = ImGui.GetIO();
            unsafe
            {
                if (io.GetNativePointer()->WantTextInput == 1) //|| io.GetNativePointer()->WantCaptureKeyboard == 1 )
                {
                    KeyboardHook.Block = true;
                    io.KeysDown[(int)info.Keys] = true;
                }
                else
                    KeyboardHook.Block = false;
            }
        }
        void OnKeyboardHookOnKeyUp(KeyInfo info)
        {
            var io = ImGui.GetIO();
            unsafe
            {
                if (io.GetNativePointer()->WantTextInput == 1) //|| io.GetNativePointer()->WantCaptureKeyboard == 1   )
                {
                    KeyboardHook.Block = true;
                    io.KeysDown[(int)info.Keys] = false;
                    ImGui.AddInputCharacter((char)info.Keys);
                }
                else
                    KeyboardHook.Block = false;
            }
        }
        private void UpdateModifiers()
        {
            var io = ImGui.GetIO();
            io.AltPressed = Control.ModifierKeys == Keys.Alt;
            io.CtrlPressed = Control.ModifierKeys == Keys.Control;
            io.ShiftPressed = Control.ModifierKeys == Keys.Shift;
        }
        private void SetOpenTKKeyMappings(IO io)
        {

            io.KeyMap[GuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Keys.Home;
            io.KeyMap[GuiKey.End] = (int)Keys.End;
            io.KeyMap[GuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Keys.Back;
            io.KeyMap[GuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[GuiKey.A] = (int)Keys.A;
            io.KeyMap[GuiKey.C] = (int)Keys.C;
            io.KeyMap[GuiKey.V] = (int)Keys.V;
            io.KeyMap[GuiKey.X] = (int)Keys.X;
            io.KeyMap[GuiKey.Y] = (int)Keys.Y;
            io.KeyMap[GuiKey.Z] = (int)Keys.Z;
        }


        public event Action Render;

        public void Clear()
        {
            device.Clear(ClearFlags.Target, Color.Transparent, 0, 0);
            device.Present();
        }
        public void TryRender()
        {
            try
            {
                if (resized)
                {
                    reset();
                }
                if (KeyboardHook.Block)
                {
                    UpdateModifiers();
                }
                UpdateImGuiInput();

                device.Clear(ClearFlags.Target, Color.Transparent, 0, 0);
                device.SetRenderState(RenderState.AlphaBlendEnable, true);
                device.SetRenderState(RenderState.CullMode, Cull.Clockwise);


                device.BeginScene();
                fontRenderer.Begin();
                textureRenderer.Begin();
                try
                {
                    ImGui.NewFrame();
                    Render.SafeInvoke();
                    ImGui.Render();
                    DrawImGui();
                }
                finally
                {
                    textureRenderer.End();
                    fontRenderer.End();
                    device.EndScene();
                    device.Present();

                }
                renderLocker.Set();
            }
            catch (SharpDXException)
            {
            }
        }

        private void UpdateImGuiInput()
        {
            var io = ImGui.GetIO();

            if (_form.Visible)
            {
                var point = Control.MousePosition;
                var windowPoint = GameController.Instance.Window.ScreenToClient(point.X, point.Y);
                io.MousePosition = new System.Numerics.Vector2(windowPoint.X,
                    windowPoint.Y);
            }
            else
            {
                io.MousePosition = new System.Numerics.Vector2(-1f, -1f);
            }



            UpdateModifiers();
            //Mouse button for work with HUD.
            io.MouseDown[0] = Form.MouseButtons == MouseButtons.Middle;
            io.MouseDown[1] = Form.MouseButtons == MouseButtons.Right;
            // io.MouseDown[2] = Form.MouseButtons == MouseButtons.Middle;


        }




        public void Dispose()
        {

            if (!device.IsDisposed)
            {
                running = false;
                renderLocker.Wait();
                renderLocker.Dispose();
                device.Dispose();
                direct3D.Dispose();
                fontRenderer.Dispose();
                textureRenderer.Dispose();
            }
            MouseHook.MouseWheel -= OnMouseHookOnMouseWheel;
            KeyboardHook.KeyDown -= OnKeyboardHookOnKeyDown;
            KeyboardHook.KeyUp -= OnKeyboardHookOnKeyUp;

        }

        private void Resize(int width, int height)
        {
            if (width > 0 && height > 0)
            {
                presentParameters.BackBufferWidth = width;
                presentParameters.BackBufferHeight = height;
                var io = ImGui.GetIO();
                io.DisplaySize = new System.Numerics.Vector2(width, height);
                io.DisplayFramebufferScale = new System.Numerics.Vector2(width * 1.0f / height);
                resized = true;
            }
        }

        public Size2 DrawText(string text, int height, Vector2 position, Color color, FontDrawFlags align = FontDrawFlags.Left)
        {
            return fontRenderer.DrawText(text, "Verdana", height, position, color, align);
        }

        public Size2 DrawText(string text, int height, Vector2 position, FontDrawFlags align = FontDrawFlags.Left)
        {
            return fontRenderer.DrawText(text, "Verdana", height, position, Color.White, align);
        }

        public Size2 MeasureText(string text, int height, FontDrawFlags align = FontDrawFlags.Left)
        {
            return fontRenderer.MeasureText(text, "Verdana", height, align);
        }

        public void DrawLine(Vector2 p1, Vector2 p2, float borderWidth, Color color)
        {
            textureRenderer.DrawLine(p1, p2, borderWidth, color);
        }

        public void DrawBox(RectangleF rectangle, Color color)
        {
            textureRenderer.DrawBox(rectangle, color);
        }

        public void DrawFrame(RectangleF rectangle, float borderWidth, Color color)
        {
            textureRenderer.DrawFrame(rectangle, borderWidth, color);
        }

        public void DrawImage(string fileName, RectangleF rectangle, float repeatX = 1f)
        {
            DrawImage(fileName, rectangle, Color.White, repeatX);
        }

        public void DrawPluginImage(string fileName, RectangleF rectangle, float repeatX = 1f)
        {
            try
            {
                textureRenderer.DrawImage(fileName, rectangle, Color.White, repeatX);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load texture {fileName}: {e.Message}");
                Environment.Exit(0);
            }
        }

        public void DrawPluginImage(string fileName, RectangleF rectangle, Color color, float repeatX = 1f)
        {
            try
            {
                textureRenderer.DrawImage(fileName, rectangle, color, repeatX);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load texture {fileName}: {e.Message}");
                Environment.Exit(0);
            }
        }

        public void DrawImage(string fileName, RectangleF rectangle, RectangleF uvCoords)
        {
            DrawImage(fileName, rectangle, uvCoords, Color.White);
        }

        public void DrawImage(string fileName, RectangleF rectangle, RectangleF uvCoords, Color color)
        {
            try
            {
                textureRenderer.DrawImage("textures/" + fileName, rectangle, uvCoords, color);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load texture {fileName}: {e.Message}");
                Environment.Exit(0);
            }
        }

        public void DrawImage(string fileName, Vertexes.TexturedVertex[] data, Color color, float repeatX = 1f)
        {
            try
            {
                textureRenderer.DrawImage("textures/" + fileName, data, color, repeatX);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load texture {fileName}: {e.Message}");
                Environment.Exit(0);
            }
        }

        public void DrawImage(string fileName, RectangleF rectangle, Color color, float repeatX = 1f)
        {
            try
            {
                textureRenderer.DrawImage("textures/" + fileName, rectangle, color, repeatX);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load texture {fileName}: {e.Message}");
                Environment.Exit(0);
            }
        }

        public void DrawImGui()
        {

            textureRenderer.DrawImGui();
        }
        public unsafe static void memcpy(void* dst, void* src, int count)
        {
            const int blockSize = 4096;
            byte[] block = new byte[blockSize];
            byte* d = (byte*)dst, s = (byte*)src;
            for (int i = 0, step; i < count; i += step, d += step, s += step)
            {
                step = count - i;
                if (step > blockSize)
                {
                    step = blockSize;
                }
                Marshal.Copy(new IntPtr(s), block, 0, step);
                Marshal.Copy(block, 0, new IntPtr(d), step);
            }
        }
        private unsafe void PrepareTextureImGui()
        {
            var io = ImGui.GetIO();

            var texDataAsRgba32 = io.FontAtlas.GetTexDataAsRGBA32();
            io.DisplaySize = new ImVec2(_form.ClientSize.Width, _form.ClientSize.Height);
            var t = new Texture(device, texDataAsRgba32.Width, texDataAsRgba32.Height, 1, Usage.Dynamic,
                Format.A8R8G8B8, Pool.Default);
            var rect = t.LockRectangle(0, LockFlags.None);
            for (int y = 0; y < texDataAsRgba32.Height; y++)
            {
                memcpy((byte*)(rect.DataPointer + rect.Pitch * y), texDataAsRgba32.Pixels + (texDataAsRgba32.Width * texDataAsRgba32.BytesPerPixel) * y, (texDataAsRgba32.Width * texDataAsRgba32.BytesPerPixel));
            }

            t.UnlockRectangle(0);
            io.FontAtlas.SetTexID(t.NativePointer);
        }

        public void DrawImage(byte[] bytes, RectangleF rectangle, Color color, string name)
        {

            try
            {
                textureRenderer.DrawImage(bytes, rectangle, color, name);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load texture from memory: {e.Message}");
                Environment.Exit(0);
            }
        }
    }
}