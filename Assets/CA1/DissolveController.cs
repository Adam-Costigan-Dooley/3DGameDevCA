using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Renderer))]
public class DissolveController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    public float dissolveDuration = 2f;

    [Header("Textures")]
    public Texture2D noiseTexture;

    private Renderer _renderer;
    private Material _material;
    private float _dissolveAmount;
    private bool _dissolving;

    private static readonly int DissolveID =
        Shader.PropertyToID("_Dissolution_Amount");
    private static readonly int NoiseTexID =
        Shader.PropertyToID("_NoiseTex");

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _material = _renderer.material;
        _dissolveAmount = 0f;
        _material.SetFloat(DissolveID, _dissolveAmount);

        if (noiseTexture != null)
            _material.SetTexture(NoiseTexID, noiseTexture);
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !_dissolving)
            _dissolving = true;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            _dissolving = false;
            _dissolveAmount = 0f;
        }

        if (_dissolving)
        {
            _dissolveAmount += Time.deltaTime / dissolveDuration;
            if (_dissolveAmount >= 1f)
            {
                _dissolveAmount = 1f;
                _dissolving = false;
            }
        }

        _material.SetFloat(DissolveID, _dissolveAmount);
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}