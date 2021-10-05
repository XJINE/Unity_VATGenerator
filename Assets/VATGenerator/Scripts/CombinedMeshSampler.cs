using UnityEngine;

namespace VATGenerator
{
    public class CombinedMeshSampler : IMeshSampler
    {
        #region Field

        private Mesh[]                meshes;
        private SkinnedMeshRenderer[] skinnedMeshRenderers;
        private Animation[]           animations;
        private AnimationState[]      animationStates;

        #endregion Field

        #region Property

        public Mesh  Output { get; private set; }
        public float Length { get; private set; }

        #endregion Property

        #region Constructor

        public CombinedMeshSampler(GameObject target)
        {
            Output = new Mesh();

            this.skinnedMeshRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer> ();

            this.meshes = new Mesh[this.skinnedMeshRenderers.Length];

            for (var i = 0; i < this.skinnedMeshRenderers.Length; i++)
            {
               this.meshes[i] = new Mesh();
            }

            this.animations      = target.GetComponentsInChildren<Animation>();
            this.animationStates = new AnimationState[this.animations.Length];

            for (var i = 0; i < this.animations.Length; i++)
            {
                var animation = this.animations[i];
                var state     = this.animationStates[i]
                              = animation[animation.clip.name];

                state.speed = 0f;

                this.Length = Mathf.Max(this.Length, state.length);

                animation.Play(state.name);
            }
        }

        #endregion Constructor

        #region Method

        public Mesh Sample(float time, out Matrix4x4 meshPosition, out Matrix4x4 meshNormal)
        {
            time = Mathf.Clamp (time, 0f, Length);

            for (var i = 0; i < this.animations.Length; i++)
            {
                this.animationStates[i].time = time;
                this.animations[i].Sample();
            }

            var combines = new CombineInstance[this.meshes.Length];

            for (var i = 0; i < this.skinnedMeshRenderers.Length; i++)
            {
                var skin    = this.skinnedMeshRenderers[i];
                var mesh    = this.meshes[i];
                var combine = combines[i];

                skin.BakeMesh(mesh);

                combine.mesh      = mesh;
                combine.transform = skin.transform.localToWorldMatrix;
                combines [i]      = combine;
            }

            Output.CombineMeshes(combines);
            meshPosition = meshNormal = Matrix4x4.identity;

            return Output;
        }

        public void Dispose()
        {
            if (this.meshes != null)
            {
                for (var i = 0; i < this.meshes.Length; i++)
                {
                    Object.Destroy(this.meshes[i]);
                }

                this.meshes = null;
            }

            Object.Destroy(Output);
        }

        #endregion Method
    }
}