using System;
using System.Collections.Generic;
using UnityEngine;

// originally based on https://www.gamasutra.com/blogs/JoeStrout/20150807/250646/2D_Animation_Methods_in_Unity.php
public class SimpleSpriteAnimator : MonoBehaviour
{
    [Serializable]
    public class Anim
    {
        public string Name;
        public Sprite[] Frames;
        public float FramesPerSec = 5;
        public bool Loop = true;

        public float Duration
        {
            get { return Frames.Length * FramesPerSec; }
        }

        public float SecsPerFrame
        {
            get { return 1.0f / FramesPerSec; }
        }
    }

    public List<Anim> Animations = new List<Anim>();

    [HideInInspector] public int CurrentFrame;

    [HideInInspector]
    public bool Done
    {
        get { return CurrentFrame >= _current.Frames.Length; }
    }

    [HideInInspector]
    public bool Playing
    {
        get { return _playing; }
    }

    private SpriteRenderer _spriteRenderer;
    private Anim _current;
    private bool _playing;
    private float _nextFrameTime;

    #region Editor Support

    [ContextMenu("Sort All Frames by Name")]
    private void DoSort()
    {
        foreach (var anim in Animations)
        {
            Array.Sort(anim.Frames, (a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
        }
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

        if (Animations.Count > 0) PlayByIndex(0);
    }

    private void Update()
    {
        if (!_playing || Time.time < _nextFrameTime || _spriteRenderer == null) return;
        CurrentFrame++;
        if (CurrentFrame >= _current.Frames.Length)
        {
            if (!_current.Loop)
            {
                _playing = false;
                return;
            }
            CurrentFrame = 0;
        }
        _spriteRenderer.sprite = _current.Frames[CurrentFrame];
        _nextFrameTime += _current.SecsPerFrame;
    }

    public void Play(string animName)
    {
        var index = Animations.FindIndex(a => a.Name == animName);
        if (index < 0)
        {
            Debug.LogError(gameObject + ": No such animation: " + animName);
        }
        else
        {
            PlayByIndex(index);
        }
    }

    public void PlayByIndex(int index)
    {
        if (index < 0) return;
        var anim = Animations[index];

        _current = anim;

        CurrentFrame = -1;
        _playing = true;
        _nextFrameTime = Time.time;
    }

    public void Stop()
    {
        _playing = false;
    }

    public void Resume()
    {
        _playing = true;
        _nextFrameTime = Time.time + _current.SecsPerFrame;
    }
}
