using Brutal.GlfwApi;
using Brutal.ImGuiApi;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Core;
using KSA;
using RenderCore;

namespace StarMap.Core.UI
{
    internal class ConfirmRestart : SetupTaskBase
    {
        private readonly Renderer _renderer;
        private readonly ConfirmRestartPopup _popup;

        public bool Show { get; set; }
        public bool Restart { get; set; }

        public static ConfirmRestart? Current { get; private set; }

        public ConfirmRestart()
        {
            this._renderer = Program.GetRenderer();
            Show = true;
            OnFrame();
            Current = this;

            _popup = ConfirmRestartPopup.Create(this);
        }

        public void DrawUi()
        {
            if (!Show)
                return;
            ImGuiHelper.BlankBackground();
            Popup.DrawAll();
        }

        public unsafe void OnFrame()
        {
            if (!Program.IsMainThread())
                return;
            Glfw.PollEvents();
            if (Program.GetWindow().ShouldClose)
            {
                Environment.Exit(0);
            }
            else
            {
                ImGuiBackend.NewFrame();
                ImGui.NewFrame();
                ImGuiHelper.StartFrame();
                DrawUi();
                ImGui.Render();
                if (EnumEx.IsSet((int)ImGui.GetIO().ConfigFlags, 1024 /*0x0400*/))
                {
                    ImGui.UpdatePlatformWindows();
                    ImGui.RenderPlatformWindowsDefault();
                }
                (FrameResult result, AcquiredFrame acquiredFrame1) = _renderer.TryAcquireNextFrame();
                AcquiredFrame acquiredFrame2 = acquiredFrame1;
                if (result != 0)
                {
                    PartialRebuild();
                }
                else
                {
                    acquiredFrame1 = acquiredFrame2;
                    (FrameResources resources, CommandBuffer commandBuffer) = acquiredFrame1;
                    VkSubpassContents contents = VkSubpassContents.Inline;
                    VkRenderPassBeginInfo pRenderPassBegin = new VkRenderPassBeginInfo()
                    {
                        RenderPass = Program.MainPass.Pass,
                        Framebuffer = resources.Framebuffer,
                        RenderArea = new VkRect2D(this._renderer.Extent),
                        ClearValues = (VkClearValue*)Program.MainPass.ClearValues.Ptr,
                        ClearValueCount = 2
                    };
                    commandBuffer.Reset();
                    commandBuffer.Begin(VkCommandBufferUsageFlags.OneTimeSubmitBit);
                    commandBuffer.BeginRenderPass<CommandBuffer>(in pRenderPassBegin, contents);
                    ImGuiBackend.Vulkan.RenderDrawData(commandBuffer);
                    commandBuffer.EndRenderPass<CommandBuffer>();
                    commandBuffer.End<CommandBuffer>();
                    if (_renderer.TrySubmitFrame() == 0)
                        return;
                    PartialRebuild();
                }
            }
        }

        public void PartialRebuild()
        {
            _renderer.Rebuild(GameSettings.GetPresentMode());
            _renderer.Device.WaitIdle();
            Program.MainPass.Pass = _renderer.MainRenderPass;
            Program.ScheduleRendererRebuild();
        }
    }
}
