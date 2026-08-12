using System.Collections.Generic;
using UnityEngine;

/// <summary>开发环境向 ObjectDetect.mObjectDic 注入 Start/Move/End/Undetect 演示数据。</summary>
public sealed class MarkerDetectSimulationDriver : MonoBehaviour
{
    [SerializeField] private int objectID = 1;
    [SerializeField] private Vector2 startPosition = new Vector2(400f, 300f);
    [SerializeField] private Vector2 moveOffset = new Vector2(120f, 55f);
    [SerializeField, Min(0f)] private float initialDelay;
    [SerializeField, Min(0.1f)] private float startSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float moveSeconds = 4f;
    [SerializeField, Min(0.1f)] private float endSeconds = 1f;
    [SerializeField, Min(0.1f)] private float undetectSeconds = 1f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private float timer;
    private bool playing;

    private void Start()
    {
        if (playOnStart) PlaySequence();
    }

    private void Update()
    {
        if (!playing) return;
        timer += Time.unscaledDeltaTime;
        if (timer < 0f)
        {
            WriteSample(ObjectState.Undetect, startPosition, 0f);
            return;
        }
        float startEnd = startSeconds;
        float moveEnd = startEnd + moveSeconds;
        float endEnd = moveEnd + endSeconds;
        float sequenceEnd = endEnd + undetectSeconds;

        if (timer < startEnd)
        {
            WriteSample(ObjectState.Start, startPosition, 0f);
        }
        else if (timer < moveEnd)
        {
            float t = (timer - startEnd) / moveSeconds;
            Vector2 offset = new Vector2(Mathf.Sin(t * Mathf.PI * 2f) * moveOffset.x,
                Mathf.Sin(t * Mathf.PI) * moveOffset.y);
            WriteSample(ObjectState.Move, startPosition + offset, t * 30f);
        }
        else if (timer < endEnd)
        {
            WriteSample(ObjectState.End, startPosition, 0f);
        }
        else if (timer < sequenceEnd)
        {
            WriteSample(ObjectState.Undetect, startPosition, 0f);
        }
        else if (loop)
        {
            timer = 0f;
        }
        else
        {
            playing = false;
        }
    }

    [ContextMenu("播放 Start → Move → End → Undetect")]
    public void PlaySequence()
    {
        timer = -initialDelay;
        playing = true;
    }

    [ContextMenu("模拟 Start")]
    public void SimulateStart() => WriteSample(ObjectState.Start, startPosition, 0f);

    [ContextMenu("模拟 Move")]
    public void SimulateMove() => WriteSample(ObjectState.Move, startPosition + moveOffset, 20f);

    [ContextMenu("模拟 End")]
    public void SimulateEnd() => WriteSample(ObjectState.End, startPosition, 0f);

    [ContextMenu("模拟 Undetect")]
    public void SimulateUndetect() => WriteSample(ObjectState.Undetect, startPosition, 0f);

    public void WriteSample(ObjectState state, Vector2 position, float angle)
    {
        if (ObjectDetect.mObjectDic == null)
            ObjectDetect.mObjectDic = new Dictionary<int, DetectObjectDetails>();
        PointInfos point = new PointInfos(position, 0);
        ObjectDetect.mObjectDic[objectID] = new DetectObjectDetails(point, point, point, angle, position,
            objectID, state, 0f, 0f, 0, 0L);
    }
}
