using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SimpleOutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public Material outlineMaterial = null;
        public int renderPassEvent = (int)RenderPassEvent.AfterRenderingOpaques;
    }

    public OutlineSettings settings = new OutlineSettings();

    class OutlinePass : ScriptableRenderPass
    {
        private Material outlineMaterial;

        public OutlinePass(Material mat, RenderPassEvent passEvent)
        {
            outlineMaterial = mat;
            renderPassEvent = passEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (outlineMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Outline Pass");
            var drawingSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, SortingCriteria.CommonOpaque);
            drawingSettings.overrideMaterial = outlineMaterial;

            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    OutlinePass outlinePass;

    public override void Create()
    {
        if (settings.outlineMaterial != null)
        {
            outlinePass = new OutlinePass(settings.outlineMaterial, (RenderPassEvent)settings.renderPassEvent);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlinePass != null)
        {
            renderer.EnqueuePass(outlinePass);
        }
    }
}
