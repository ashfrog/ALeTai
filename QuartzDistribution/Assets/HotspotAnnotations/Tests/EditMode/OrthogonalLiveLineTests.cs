using NUnit.Framework;
using UnityEngine;

namespace QuartzDistribution.HotspotAnnotations.Tests
{
    public sealed class OrthogonalLiveLineTests
    {
        [Test]
        public void ChoosesNearestFacingHorizontalEdge()
        {
            Rect card = new Rect(100f, -50f, 200f, 100f);
            Assert.AreEqual(OrthogonalLiveLine.AttachmentSide.Left,
                OrthogonalLiveLine.ChooseAttachmentSide(Vector2.zero, card));
            Assert.AreEqual(OrthogonalLiveLine.AttachmentSide.Right,
                OrthogonalLiveLine.ChooseAttachmentSide(new Vector2(500f, 0f), card));
        }

        [Test]
        public void ChoosesNearestFacingVerticalEdge()
        {
            Rect card = new Rect(-100f, 100f, 200f, 100f);
            Assert.AreEqual(OrthogonalLiveLine.AttachmentSide.Bottom,
                OrthogonalLiveLine.ChooseAttachmentSide(Vector2.zero, card));
            Assert.AreEqual(OrthogonalLiveLine.AttachmentSide.Top,
                OrthogonalLiveLine.ChooseAttachmentSide(new Vector2(0f, 400f), card));
        }

        [Test]
        public void HorizontalAttachmentBuildsOnlyOrthogonalSegments()
        {
            var path = OrthogonalLiveLine.BuildOrthogonalPath(Vector2.zero,
                new Rect(300f, 100f, 200f, 120f), OrthogonalLiveLine.AttachmentSide.Left, 4f);
            Assert.GreaterOrEqual(path.Count, 2);
            for (int i = 0; i < path.Count - 1; i++)
                Assert.IsTrue(Mathf.Approximately(path[i].x, path[i + 1].x) || Mathf.Approximately(path[i].y, path[i + 1].y));
        }

        [Test]
        public void DegenerateSegmentsAreRemoved()
        {
            var path = OrthogonalLiveLine.BuildOrthogonalPath(new Vector2(96f, 0f),
                new Rect(100f, -50f, 200f, 100f), OrthogonalLiveLine.AttachmentSide.Left, 4f);
            for (int i = 0; i < path.Count - 1; i++)
                Assert.Greater(Vector2.Distance(path[i], path[i + 1]), 0.49f);
        }
    }
}
