using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace QuartzDistribution.HotspotAnnotations.Tests
{
    public sealed class HotspotRuntimeTests
    {
        private readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            ObjectDetect.mObjectDic?.Clear();
            for (int i = cleanup.Count - 1; i >= 0; i--)
                if (cleanup[i] != null) Object.DestroyImmediate(cleanup[i]);
            cleanup.Clear();
        }

        [UnityTest]
        public IEnumerator DllScreenPositionMovesFlatTrackingAnchor()
        {
            MarKActions actions = CreateFlatController(11, out RectTransform anchor);
            actions.PushDllSimulationSample(new Vector2(Screen.width * 0.25f, Screen.height * 0.5f), 20f, ObjectState.Move);
            yield return null;
            Vector2 first = anchor.anchoredPosition;

            actions.PushDllSimulationSample(new Vector2(Screen.width * 0.75f, Screen.height * 0.5f), 40f, ObjectState.Move);
            yield return null;
            Assert.Greater(anchor.anchoredPosition.x, first.x);
            Assert.AreEqual(40f, actions.LastTrackedAngle, 0.01f);
        }

        [UnityTest]
        public IEnumerator DetectionDictionaryShowsAndHidesFlatGroup()
        {
            MarKActions actions = CreateFlatController(7, out _);
            PointInfos point = new PointInfos(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), 0);
            ObjectDetect.mObjectDic = new Dictionary<int, DetectObjectDetails>
            {
                [7] = new DetectObjectDetails(point, point, point, 0f, point.pos, 7,
                    ObjectState.Start, 0f, 0f, 0, 0L)
            };

            yield return null;
            Assert.IsTrue(actions.IsVisible);
            ObjectDetect.mObjectDic.Remove(7);
            yield return null;
            Assert.IsFalse(actions.IsVisible);
        }

        [UnityTest]
        public IEnumerator PublicSimulationEntryWritesDllDictionary()
        {
            MarKActions actions = CreateFlatController(5, out _);
            actions.SetSimulatedTrackingData(new Vector2(960f, 540f), 32f, true);
            yield return null;

            Assert.IsTrue(ObjectDetect.mObjectDic.ContainsKey(5));
            DetectObjectDetails details = ObjectDetect.mObjectDic[5];
            Assert.AreEqual(ObjectState.Move, details.objectstate);
            Assert.AreEqual(32f, details.objectRotationAngle, 0.01f);
            Assert.IsTrue(actions.IsVisible);
        }

        private MarKActions CreateFlatController(int id, out RectTransform anchor)
        {
            GameObject canvasObject = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject controller = Track(new GameObject("Controller", typeof(RectTransform)));
            controller.transform.SetParent(canvas.transform, false);
            GameObject visual = Track(new GameObject("Visual", typeof(RectTransform), typeof(CanvasGroup)));
            visual.transform.SetParent(controller.transform, false);
            GameObject anchorObject = Track(new GameObject("TrackingAnchor", typeof(RectTransform)));
            anchorObject.transform.SetParent(visual.transform, false);
            anchor = anchorObject.GetComponent<RectTransform>();

            MarKActions actions = controller.AddComponent<MarKActions>();
            actions.Configure(id, anchor, canvas, visual, visual.GetComponent<CanvasGroup>(), null);
            return actions;
        }

        private GameObject Track(GameObject go)
        {
            cleanup.Add(go);
            return go;
        }
    }
}
