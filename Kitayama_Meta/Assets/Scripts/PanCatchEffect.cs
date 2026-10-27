using UnityEngine;

namespace Kitayama.Cooking
{
    public sealed class PanCatchEffect : MonoBehaviour
    {
        public MeatCooking cooking;
        public Material effectMaterial;

        [Min(0.01f)]
        public float ringRadius = 0.19f;

        [Range(8, 80)]
        public int particleCount = 36;

        [Min(0.1f)]
        public float duration = 0.8f;

        public Color effectColor =
            new Color(1f, 0.78f, 0.12f, 1f);

        ParticleSystem particles;
        MeatCooking subscribedCooking;

        void Awake()
        {
            if (cooking == null || effectMaterial == null)
            {
                Debug.LogError(
                    "Run Kitayama > Setup Catch Effect first.",
                    this
                );

                enabled = false;
                return;
            }

            GameObject obj = new GameObject("CatchParticles");
            obj.transform.SetParent(transform, false);

            particles = obj.AddComponent<ParticleSystem>();

            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;

            // フライパンを動かしてもエフェクトがついてくる。
            main.simulationSpace =
                ParticleSystemSimulationSpace.Local;

            main.maxParticles = 80;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;

            // 成功した瞬間だけ、コードから粒を出す。
            var emission = particles.emission;
            emission.enabled = false;

            var shape = particles.shape;
            shape.enabled = false;

            // 時間とともに透明にする。
            var color = particles.colorOverLifetime;
            color.enabled = true;

            Gradient fade = new Gradient();

            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            color.color = fade;

            var renderer =
                particles.GetComponent<ParticleSystemRenderer>();

            renderer.sharedMaterial = effectMaterial;

            // 粒の見た目はカメラの方向を向く。
            renderer.renderMode =
                ParticleSystemRenderMode.Billboard;

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            renderer.receiveShadows = false;
        }

        void OnEnable()
        {
            if (cooking == null || particles == null)
            {
                return;
            }

            subscribedCooking = cooking;

            // 既存のキャッチ成功通知を受け取る。
            subscribedCooking.CookingCompleted += OnCatch;
        }

        void OnDisable()
        {
            if (subscribedCooking != null)
            {
                subscribedCooking.CookingCompleted -= OnCatch;
            }

            subscribedCooking = null;

            if (particles != null)
            {
                particles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        void OnCatch(CookingSession.Doneness grade, int meter)
        {
            PlayEffect();
        }

        [ContextMenu("Preview Catch Effect (Play Mode)")]
        public void PlayEffect()
        {
            if (!Application.isPlaying ||
                particles == null ||
                cooking == null)
            {
                return;
            }

            // フライパンの底の少し上を発生位置にする。
            if (cooking.panBase != null)
            {
                particles.transform.position =
                    cooking.panBase.transform.position
                    + transform.up * 0.06f;
            }

            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particles.Play();

            int count = Mathf.Clamp(particleCount, 8, 80);

            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;

                Vector3 outward = new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)
                );

                var particle = new ParticleSystem.EmitParams();

                // フライパンを囲む円形に配置する。
                particle.position = outward * ringRadius;

                // 外側と上方向へ広がる。
                particle.velocity =
                    outward * 0.24f
                    + Vector3.up * Random.Range(0.1f, 0.3f);

                particle.startLifetime =
                    Mathf.Max(0.1f, duration);

                particle.startSize =
                    Random.Range(0.018f, 0.032f);

                particle.startColor = effectColor;

                particles.Emit(particle, 1);
            }
        }
    }
}