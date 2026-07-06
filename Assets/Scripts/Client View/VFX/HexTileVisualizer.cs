using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public class HexTileVisualizer : MonoBehaviour
{
    [SerializeField] private VisualEffect _EffectRef;
    [SerializeField, ColorUsage(true, true)] private Color _TrueColor;
    [SerializeField, ColorUsage(true, true)] private Color _TrueEffectColor;
    [SerializeField, ColorUsage(true, true)] private Color _TrueFresnelColor;
    [SerializeField, ColorUsage(true, true)] private Color _FalseColor;
    [SerializeField, ColorUsage(true, true)] private Color _FalseEffectColor;
    [SerializeField, ColorUsage(true, true)] private Color _FalseFresnelColor;

    private static readonly int ColorId = Shader.PropertyToID("Color");

    private void Awake()
    {
        int count = transform.childCount;
        
        for (int i = 1; i < count; i++)
        {
            Renderer renderer = transform.GetChild(i).GetComponent<Renderer>();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            block.SetFloat("_VertexSpeedRandom", Random.Range(5, 7.5f));
            block.SetFloat("_ColorSpeedRandom", Random.Range(1.5f, 3.5f));

            renderer.SetPropertyBlock(block);
        }
    }

    public void Show(bool value)
    {
        SetColor(value);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetColor(bool value)
    {
        if (value)
        {
            int count = transform.childCount;

            for (int i = 1; i < count; i++)
            {
                Renderer renderer = transform.GetChild(i).GetComponent<Renderer>();

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                block.SetColor("_Color", _TrueColor);
                block.SetColor("_FresnelColor", _TrueFresnelColor);

                renderer.SetPropertyBlock(block);
            }

            _EffectRef.SetVector4(ColorId, _TrueEffectColor);
            Debug.Log("TrueColor");
        }
        else
        {
            int count = transform.childCount;

            for (int i = 1; i < count; i++)
            {
                Renderer renderer = transform.GetChild(i).GetComponent<Renderer>();

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                block.SetColor("_Color", _FalseColor);
                block.SetColor("_FresnelColor", _FalseFresnelColor);

                renderer.SetPropertyBlock(block);
            }

            _EffectRef.SetVector4(ColorId, _FalseEffectColor);
            Debug.Log("FalseColor");
        }
    }
}
