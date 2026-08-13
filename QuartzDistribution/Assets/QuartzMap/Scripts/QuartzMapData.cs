using System;

namespace QuartzDistribution
{
    [Serializable] public sealed class ResourceTypeData
    {
        public string id;
        public string name;
        public string colorHex;
        public string iconThumb;
        public string description;
    }

    [Serializable] public sealed class ResourceTypeCollection
    {
        public ResourceTypeData[] resourceTypes;
    }

    [Serializable] public sealed class PixelPoint
    {
        public float x;
        public float y;
    }

    [Serializable] public sealed class ImageSizeData
    {
        public float width;
        public float height;
    }

    [Serializable] public sealed class MapMarkerData
    {
        public string province;
        public string resourceTypeId;
        public PixelPoint anchorPx;
        public string note;
    }

    [Serializable] public sealed class MapConfigData
    {
        public string mapId;
        public ImageSizeData mapImageSize;
        public MapMarkerData[] markers;
    }
}
