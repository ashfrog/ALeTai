using System;
using UnityEngine;

/// <summary>只负责从 MarkerDetect 字典读取当前物体状态并发布事件。</summary>
public sealed class MarKActions : MonoBehaviour
{
    public int mObjectID = 1;

    public event Action<DetectObjectDetails> Started;
    public event Action<DetectObjectDetails> Moved;
    public event Action<DetectObjectDetails> Ended;
    public event Action Undetected;

    private ObjectState? lastState;

    private void Update()
    {
        if (ObjectDetect.mObjectDic == null ||
            !ObjectDetect.mObjectDic.TryGetValue(mObjectID, out DetectObjectDetails details))
        {
            PublishUndetected();
            return;
        }

        switch (details.objectstate)
        {
            case ObjectState.Start:
                if (lastState != ObjectState.Start) Started?.Invoke(details);
                break;
            case ObjectState.Move:
                Moved?.Invoke(details);
                break;
            case ObjectState.End:
                if (lastState != ObjectState.End) Ended?.Invoke(details);
                break;
            case ObjectState.Undetect:
                PublishUndetected();
                return;
        }

        lastState = details.objectstate;
    }

    private void PublishUndetected()
    {
        if (lastState.HasValue) Undetected?.Invoke();
        lastState = null;
    }
}
