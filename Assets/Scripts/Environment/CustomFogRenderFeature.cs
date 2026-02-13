using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Runner.Environment
{
    public class CustomFogRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class FogSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public FogSettings settings = new FogSettings();
        private CustomFogRenderPass fogPass;

        public override void Create()
        {
            fogPass = new CustomFogRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            if (FogController.Instance == null) return;
            if (FogController.Instance.FogMaterial == null) return;
            if (!FogController.Instance.FogEnabled) return;

            fogPass.SetMaterial(FogController.Instance.FogMaterial);
            renderer.EnqueuePass(fogPass);
        }

        protected override void Dispose(bool disposing)
        {
            fogPass?.Dispose();
        }
    }

    public class CustomFogRenderPass : ScriptableRenderPass
    {
        private CustomFogRenderFeature.FogSettings settings;
        private Material fogMaterial;
        private RTHandle tempTexture;
        private const string ProfilerTag = "Custom Fog";

        public CustomFogRenderPass(CustomFogRenderFeature.FogSettings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void SetMaterial(Material material)
        {
            fogMaterial = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempFogTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (fogMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);

            RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            Blitter.BlitCameraTexture(cmd, cameraTarget, tempTexture, fogMaterial, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, cameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }
}