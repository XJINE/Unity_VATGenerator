using UnityEngine;

namespace VATGenerator
{
    public class SingleMeshSampler : IMeshSampler
    {
        #region Field

        private readonly SkinnedMeshRenderer skinnedMeshRenderer;
        private readonly Animation           animation;
        private readonly AnimationState      animationState;

        #endregion Field

        #region Property

        public Mesh  Output { get; private set; }
        public float Length { get; private set; }

        #endregion Property

        #region Constructor

        public SingleMeshSampler(GameObject target)
        {
            this.skinnedMeshRenderer  = target.GetComponentInChildren<SkinnedMeshRenderer>();
            this.animation            = target.GetComponentInChildren<Animation>();
            this.animationState       = this.animation[this.animation.clip.name];
            this.animationState.speed = 0f;

            this.Output = new Mesh();
            this.Length = this.animationState.length;

            this.animation.Play(this.animationState.name);
        }

        #endregion Constructor

        #region Method

        public Mesh Sample(float time, out Matrix4x4 meshPosition, out Matrix4x4 meshNormal)
        {
            this.animationState.time = Mathf.Clamp(time, 0f, this.Length);
            this.animation.Sample();
            this.skinnedMeshRenderer.BakeMesh(this.Output);

            meshPosition = this.skinnedMeshRenderer.localToWorldMatrix;
            meshNormal   = this.skinnedMeshRenderer.worldToLocalMatrix.transpose;

            return this.Output;
        }

        public void Dispose()
        {
            GameObject.Destroy(this.Output);
        }

        #endregion Method
    }
}