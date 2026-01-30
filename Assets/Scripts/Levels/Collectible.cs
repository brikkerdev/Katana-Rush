using Runner.LevelGeneration;
using UnityEngine;

public class Collectible : MonoBehaviour, IResettable
{
    private bool _collected = false;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _originalPosition = transform.localPosition;
    }

    public void Collect()
    {
        if (_collected) return;

        _collected = true;
        gameObject.SetActive(false);
        // Add score, play effects, etc.
    }

    public void Reset()
    {
        _collected = false;
        transform.localPosition = _originalPosition;
        gameObject.SetActive(true);
    }
}