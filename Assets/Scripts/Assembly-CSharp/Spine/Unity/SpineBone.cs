namespace Spine.Unity
{
    public class SpineBone : SpineAttributeBase
    {
        public SpineBone(string startsWith = "", string dataField = "")
        {
            base.startsWith = startsWith;
            base.dataField = dataField;
        }

        public static Bone GetBone(string boneName, SkeletonRenderer renderer)
        {
            return renderer.skeleton?.FindBone(boneName);
        }

        public static BoneData GetBoneData(string boneName, SkeletonDataAsset skeletonDataAsset)
        {
            return skeletonDataAsset.GetSkeletonData(quiet: true).FindBone(boneName);
        }
    }
}