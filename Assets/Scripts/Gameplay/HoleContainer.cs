using DG.Tweening;
using UnityEngine;

public class HoleContainer : MonoBehaviour
{
    [Header("Effect")]
    public ParticleSystem effect;
    [Header("Container Info")]
    public int containerIndex;
    public bool isEmpty = true;
    public Hole storedHole = null;
    public bool isLocked = false; // NEW: Add this boolean
    
    [Header("Visual Feedback")]
    public GameObject emptyIndicator; // Visual element to show container is empty
    public GameObject fullIndicator;  // Visual element to show container has a hole
    
    public void StoreHole(Hole hole)
    {
        if (!isEmpty)
        {
            Debug.LogWarning("Trying to store hole in occupied container!");
            return;
        }
        
        storedHole = hole;
        isEmpty = false;
        hole.transform.SetParent(transform);
        
        // Position the hole at container position
        hole.transform.localPosition = Vector3.zero;
        
        // Disable tap interaction for stored holes
        hole.isFirstRow = false;
        
        // Update visual indicators
        UpdateVisualFeedback();
        
        Debug.Log($"Hole stored in container {containerIndex}");
    }
    
    public Hole RemoveHole()
    {
        if (isEmpty) return null;
        
        Hole hole = storedHole;
        storedHole = null;
        isEmpty = true;
        
        // Update visual indicators
        UpdateVisualFeedback();
        
        Debug.Log($"Hole removed from container {containerIndex}");
        return hole;
    }
    
    public void UpdateVisualFeedback()
    {
        if(effect != null){effect.Play();}

        if (emptyIndicator != null)
        {
            var sr = emptyIndicator.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.DOFade(isEmpty ? 0.4f : 0f, 0.2f).SetEase(Ease.InOutSine);
                // Use different delay times for fade in and fade out
                // float delay = isEmpty ? 0.0f : 0f; // 0.2s for fade in, 0.5s for fade out 
                // DOVirtual.DelayedCall(delay, () => sr.DOFade(isEmpty ? 0.4f : 0f, 0.2f).SetEase(Ease.InOutSine));
            }
        }

        if (fullIndicator != null)
        {
            var sr = fullIndicator.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Fade in if full, fade out otherwise
                float delay = isEmpty ? 0.1f : 0.3f; // 0.2s for fade in, 0.5s for fade out
                DOVirtual.DelayedCall(delay, () => sr.DOFade(isEmpty ? 0f : 1f, 0.5f).SetEase(Ease.InOutSine));
            }
        }
    }
    
    void Start()
    {
        UpdateVisualFeedback();
    }
}