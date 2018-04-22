using System;
using UnityEngine;

public class OneShotAnimation : MonoBehaviour
{
    public Sprite[] Frames;
    public float FramesPerSec = 5;

    private float _nextFrameTime;
    private SpriteRenderer _spriteRenderer;
    private int _currentFrame = -1;

    public float SecsPerFrame
    {
        get { return 1.0f / FramesPerSec; }
    }

    #region Editor Support

    [ContextMenu("Sort All Frames by Name")]
    private void DoSort()
    {
        Array.Sort(Frames, (a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
        Debug.Log(gameObject.name + " animation frames have been sorted alphabetically.");
    }

    #endregion

    private void Start()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError(gameObject.name + ": Couldn't find SpriteRenderer");
        }
        _nextFrameTime = Time.time;
    }

    private void Update()
    {
        if (Time.time < _nextFrameTime || _spriteRenderer == null) return;
        _currentFrame++;
        if (_currentFrame >= Frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        _spriteRenderer.sprite = Frames[_currentFrame];
        _nextFrameTime += SecsPerFrame;
    }
}
