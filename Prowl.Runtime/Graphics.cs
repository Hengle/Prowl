﻿using Prowl.Runtime.RenderPipelines;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;



namespace Prowl.Runtime
{
    public static class Graphics
    {
        public static GraphicsDevice Device { get; internal set; }
        public static ResourceFactory Factory => Device.ResourceFactory;

        private static RenderTexture _screenTarget;
        public static RenderTexture ScreenTarget 
        {
            get
            {
                if (_screenTarget == null || _screenTarget.Framebuffer != Device.SwapchainFramebuffer)
                    _screenTarget = new RenderTexture(Device.SwapchainFramebuffer);
                
                return _screenTarget;
            }
        }

        public static Vector2Int TargetResolution => new Vector2(ScreenTarget.Width, ScreenTarget.Height);

        public static bool VSync
        {
            get { return Device.SyncToVerticalBlank; }
            set { Device.SyncToVerticalBlank = value; }
        }

        [DllImport("Shcore.dll")]
        internal static extern int SetProcessDpiAwareness(int value);

        public static void Initialize(bool VSync = true, GraphicsBackend preferredBackend = GraphicsBackend.OpenGL)
        {
            GraphicsDeviceOptions deviceOptions = new()
            {
                SyncToVerticalBlank = VSync,
                ResourceBindingModel = ResourceBindingModel.Default,
                HasMainSwapchain = true,
                SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt,
                SwapchainSrgbFormat = false,
            };

            Device = VeldridStartup.CreateGraphicsDevice(Screen.InternalWindow, deviceOptions, preferredBackend);

            if (RuntimeUtils.IsWindows())
            {
                Exception? exception = Marshal.GetExceptionForHR(SetProcessDpiAwareness(1));

                if (exception != null)
                    Debug.LogError("Failed to set DPI awareness", exception);
            }

            Screen.Resize += ResizeGraphicsResources;
        }

        private static void ResizeGraphicsResources(Vector2Int newSize)
        {
            _screenTarget.UpdateFramebufferInfo();
            Device.ResizeMainWindow((uint)newSize.x, (uint)newSize.y);
        }

        public static void EndFrame()
        {   
            Device.SwapBuffers();
            RenderTexture.UpdatePool();
        }

        public static CommandList GetCommandList()
        {
            CommandList list = Factory.CreateCommandList();

            list.Begin();

            return list;
        }


        private static CommandList CreateCommandListForBuffer(CommandBuffer commandBuffer)
        {
            CommandList list = GetCommandList();

            RenderState state = new RenderState();

            foreach (var command in commandBuffer.Buffer)
                command.ExecuteCommand(list, state);

            return list;
        }

        public static void SubmitCommandBuffer(CommandBuffer commandBuffer, bool awaitComplete = false)
        {
            CommandList list = CreateCommandListForBuffer(commandBuffer);
            
            try 
            {
                SubmitCommandList(list, awaitComplete);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to execute command list", ex);
            }
            finally
            {
                list.Dispose();   
            }
        }

        public static Task SubmitCommandBufferAsync(CommandBuffer commandBuffer)
        {
            return new Task(() => SubmitCommandBuffer(commandBuffer, true));
        }

        internal static void SubmitCommandList(CommandList list, bool awaitComplete, ulong timeout = ulong.MaxValue)
        {   
            list.End();

            if (awaitComplete)
            {
                Fence fence = Factory.CreateFence(false);
                Device.SubmitCommands(list, fence);
                Device.WaitForFence(fence, timeout);
                fence.Dispose();

                return;
            }

            Device.SubmitCommands(list);
        }

        internal static Task SubmitCommandListAsync(CommandList list, ulong timeout)
        {
            return new Task(() => SubmitCommandList(list, true, timeout));
        }

        internal static void InternalCopyTexture(Veldrid.Texture source, Veldrid.Texture destination, uint mipLevel, uint arrayLayer, bool awaitComplete = false)
        {
            CommandList commandList = GetCommandList();

            commandList.CopyTexture(source, destination, mipLevel, arrayLayer);
            
            SubmitCommandList(commandList, awaitComplete);

            commandList.Dispose();
        }

        internal static void Dispose()
        {
            ShaderPipelineCache.Dispose();
            GUI.Graphics.UIDrawList.DisposeBuffers();
        
            Device.Dispose();
        }
    }
}
