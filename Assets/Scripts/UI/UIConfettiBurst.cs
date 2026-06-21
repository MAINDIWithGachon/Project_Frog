using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIConfettiBurst : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform boundsTarget;
    [SerializeField] private Sprite confettiSprite;

    [Header("Burst")]
    [SerializeField] private int pieceCount = 36;
    [SerializeField] private float duration = 1.25f;
    [SerializeField] private Vector2 startArea = new Vector2(360f, 40f);
    [SerializeField] private Vector2 fallDistanceRange = new Vector2(420f, 760f);
    [SerializeField] private Vector2 driftRange = new Vector2(-180f, 180f);
    [SerializeField] private Vector2 sizeRange = new Vector2(8f, 18f);
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(220f, 620f);

    [Header("Color")]
    [SerializeField] private Color[] colors =
    {
        new Color32(246, 214, 74, 255),
        new Color32(126, 242, 142, 255),
        new Color32(143, 136, 255, 255),
        new Color32(227, 138, 207, 255),
        new Color32(111, 220, 255, 255)
    };

    private readonly List<Image> pieces = new();
    private RectTransform rectTransform;
    private Coroutine burstRoutine;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (boundsTarget == null)
            boundsTarget = rectTransform;

        SetPiecesActive(false);
    }

    private void OnDisable()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        SetPiecesActive(false);
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        EnsurePieces();
        burstRoutine = StartCoroutine(PlayBurst());
    }

    public void Stop()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        SetPiecesActive(false);
    }

    private IEnumerator PlayBurst()
    {
        Rect bounds = boundsTarget != null ? boundsTarget.rect : new Rect(Vector2.zero, startArea);
        float startY = bounds.height * 0.35f;

        for (int i = 0; i < pieces.Count; i++)
        {
            Image piece = pieces[i];
            RectTransform pieceRect = piece.rectTransform;
            float size = Random.Range(sizeRange.x, sizeRange.y);

            piece.sprite = confettiSprite;
            piece.color = colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
            pieceRect.sizeDelta = new Vector2(size * Random.Range(0.45f, 1.4f), size);
            pieceRect.anchoredPosition = new Vector2(Random.Range(-startArea.x * 0.5f, startArea.x * 0.5f), startY + Random.Range(-startArea.y, startArea.y));
            pieceRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            piece.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        Vector2[] startPositions = new Vector2[pieces.Count];
        Vector2[] endPositions = new Vector2[pieces.Count];
        float[] rotationSpeeds = new float[pieces.Count];

        for (int i = 0; i < pieces.Count; i++)
        {
            RectTransform pieceRect = pieces[i].rectTransform;
            startPositions[i] = pieceRect.anchoredPosition;
            endPositions[i] = startPositions[i] + new Vector2(Random.Range(driftRange.x, driftRange.y), -Random.Range(fallDistanceRange.x, fallDistanceRange.y));
            rotationSpeeds[i] = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y) * (Random.value < 0.5f ? -1f : 1f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            for (int i = 0; i < pieces.Count; i++)
            {
                Image piece = pieces[i];
                RectTransform pieceRect = piece.rectTransform;
                pieceRect.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], endPositions[i], eased);
                pieceRect.Rotate(0f, 0f, rotationSpeeds[i] * Time.unscaledDeltaTime);

                Color color = piece.color;
                color.a = 1f - Mathf.Clamp01((t - 0.75f) / 0.25f);
                piece.color = color;
            }

            yield return null;
        }

        SetPiecesActive(false);
        burstRoutine = null;
    }

    private void EnsurePieces()
    {
        while (pieces.Count < pieceCount)
        {
            GameObject pieceObject = new GameObject($"Confetti_{pieces.Count:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pieceObject.transform.SetParent(transform, false);

            Image image = pieceObject.GetComponent<Image>();
            image.raycastTarget = false;
            pieces.Add(image);
        }
    }

    private void SetPiecesActive(bool isActive)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
                pieces[i].gameObject.SetActive(isActive);
        }
    }
}
