using UnityEngine;

public class HoleShaderController : MonoBehaviour
{
    public Transform holeTransform;
    public Material groundMaterial;
    public float holeRadius = 2f;
    public float rimWidth = 0.5f;
    public Color outerColor = Color.gray;
    public Color innerColor = Color.black;
    public float gradientStrength = 0.5f;
    public Texture2D noiseTex;
    public float noiseStrength = 0.1f;

    void Update()
    {
        groundMaterial.SetVector("_HolePosition", holeTransform.position);
        groundMaterial.SetFloat("_HoleRadius", holeRadius);
        groundMaterial.SetFloat("_RimWidth", rimWidth);
        groundMaterial.SetColor("_OuterColor", outerColor);
        groundMaterial.SetColor("_InnerColor", innerColor);
        groundMaterial.SetFloat("_GradientStrength", gradientStrength);
        groundMaterial.SetTexture("_NoiseTex", noiseTex);
        groundMaterial.SetFloat("_NoiseStrength", noiseStrength);
    }
}
