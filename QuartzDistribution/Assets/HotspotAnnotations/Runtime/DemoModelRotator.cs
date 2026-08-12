using UnityEngine;

namespace QuartzDistribution.HotspotAnnotations
{
    public sealed class DemoModelRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 12f, 0f);

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
