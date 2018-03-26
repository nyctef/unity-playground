using UnityEngine;

public class TextureCreator : MonoBehaviour
{
    private Texture2D _texture;

    [Range(2,512)]
    public int resolution = 256;

    [Range(1, 50)]
    public float frequency = 15f;

    public enum NoiseMethodType
    {
        Value, Perlin
    }

    public NoiseMethodType type = NoiseMethodType.Perlin;

    [Range(1, 3)]
    public int dimensions = 1;

    // called before Start
    private void OnEnable()
    {
        _texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true)
        {
            name = "Procedural Texture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        GetComponent<MeshRenderer>().material.mainTexture = _texture;

        FillTexture();
    }

    public void FillTexture()
    {
        if (_texture.width != resolution)
        {
            _texture.Resize(resolution, resolution);
        }

        Vector3 point00 = transform.TransformPoint(new Vector3(-0.5f, -0.5f));
        Vector3 point10 = transform.TransformPoint(new Vector3(0.5f, -0.5f));
        Vector3 point01 = transform.TransformPoint(new Vector3(-0.5f, 0.5f));
        Vector3 point11 = transform.TransformPoint(new Vector3(0.5f, 0.5f));

        NoiseMethod method = Noise.noiseMethods[(int)type][dimensions - 1];
        float stepSize = 1f / resolution;
        for (int y = 0; y < resolution; y++)
        {
            Vector3 point0 = Vector3.Lerp(point00, point01, (y + 0.5f) * stepSize);
            Vector3 point1 = Vector3.Lerp(point10, point11, (y + 0.5f) * stepSize);
            for (int x = 0; x < resolution; x++)
            {
                Vector3 point = Vector3.Lerp(point0, point1, (x + 0.5f) * stepSize);
                float sample = method(point, frequency);
                if (type != NoiseMethodType.Value)
                {
                    sample = sample * 0.5f + 0.5f;
                }
                _texture.SetPixel(x, y, Color.white * sample);
            }
        }
        _texture.Apply();
    }

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            FillTexture();
        }
    }
}