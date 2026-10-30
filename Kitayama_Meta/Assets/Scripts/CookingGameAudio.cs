using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kitayama.Cooking
{
    [DefaultExecutionOrder(200)]
    public sealed class CookingGameAudio : MonoBehaviour
    {
        public CookingStart startScreen;
        public MeatCooking cooking;
        public CookingResult result;

        // ダウンロードしたMP3を設定する。
        public AudioClip startBgm;

        [Range(0f, 1f)]
        public float bgmVolume = 0.25f;

        [Range(0f, 1f)]
        public float effectVolume = 0.6f;

        [Range(0f, 1f)]
        public float heatingVolume = 0.28f;

        AudioSource bgm;
        AudioSource ui;
        AudioSource heat;
        AudioSource catchSource;

        AudioClip startSound;
        AudioClip catchSound;
        AudioClip resultSound;
        AudioClip silentCatch;
        AudioClip originalCatch;

        Button button;

        bool ready;
        bool started;
        bool caught;
        bool resultPlayed;
        bool replacedCatch;

        readonly List<AudioClip> generated =
            new List<AudioClip>();

        void Awake()
        {
            if (startScreen == null ||
                startScreen.startButton == null ||
                startScreen.startPanel == null ||
                startScreen.kitchen == null ||
                cooking == null ||
                result == null ||
                result.resultPanel == null)
            {
                Debug.LogError(
                    "Kitayama → Setup Game Audioを実行してください。",
                    this
                );

                enabled = false;
                return;
            }

            bgm = Source("StartBGM", false, true);
            ui = Source("UIEffects", false, false);
            heat = Source("HeatingSound", true, true);
            catchSource = Source("CatchSound", true, false);

            startSound = Clip(
                "START",
                CookingSoundSamples.Notes(
                    new[] { 659.25f, 987.77f },
                    .085f,
                    .13f
                )
            );

            catchSound = Clip(
                "CATCH",
                CookingSoundSamples.Notes(
                    new[] { 783.99f, 1046.5f, 1567.98f },
                    .075f,
                    .18f
                )
            );

            resultSound = Clip(
                "RESULT",
                CookingSoundSamples.Notes(
                    new[] { 523.25f, 659.25f, 783.99f, 1046.5f },
                    .16f,
                    .4f
                )
            );

            heat.clip = Clip(
                "SIZZLE",
                CookingSoundSamples.Sizzle()
            );

            silentCatch = Clip(
                "SilentOriginalCatch",
                new float[240]
            );

            heat.volume = 0f;
            ready = true;

            if (startBgm == null)
            {
                Debug.LogWarning(
                    "BGMが未設定です。"
                    + "MP3をStart Bgmに設定してください。",
                    this
                );
            }
        }

        void OnEnable()
        {
            if (!ready)
                return;

            button = startScreen.startButton;
            button.onClick.AddListener(OnStartPressed);

            cooking.CookingCompleted += OnCaught;

            // 既存の成功音との二重再生を防ぐ。
            // 失敗音はそのまま維持する。
            originalCatch = cooking.catchClip;
            cooking.catchClip = silentCatch;
            replacedCatch = true;
        }

        void OnStartPressed()
        {
            if (started ||
                result.IsFinished ||
                !startScreen.kitchen.activeInHierarchy)
            {
                return;
            }

            started = true;

            // BGMは止めず、操作音だけ重ねる。
            ui.PlayOneShot(
                startSound,
                effectVolume
            );
        }

        void OnCaught(
            CookingSession.Doneness grade,
            int meter)
        {
            if (!started || caught || resultPlayed)
                return;

            caught = true;

            heat.Stop();
            heat.volume = 0f;

            PositionSounds();

            // BGMはキャッチ演出中も流し続ける。
            catchSource.PlayOneShot(
                catchSound,
                effectVolume
            );
        }

        void Update()
        {
            if (!ready)
                return;

            PositionSounds();

            // キャッチ判定が成功した瞬間ではなく、
            // 実際にリザルト画面が表示されたかで判断する。
            bool resultVisible =
                result.resultPanel.activeInHierarchy;

            bgm.volume = bgmVolume;

            // スタート画面からリザルト表示直前まで継続再生。
            if (!resultVisible &&
                !resultPlayed &&
                startBgm != null)
            {
                if (bgm.clip != startBgm)
                    bgm.clip = startBgm;

                if (!bgm.isPlaying)
                    bgm.Play();
            }
            else if (bgm.isPlaying)
            {
                bgm.Stop();
            }

            // 実際に加熱されている間だけ焼く音を流す。
            bool heating =
                started &&
                !caught &&
                !result.IsFinished &&
                cooking.isActiveAndEnabled &&
                cooking.Session != null &&
                cooking.Session.CurrentPhase ==
                    CookingSession.Phase.Heating;

            if (heating && !heat.isPlaying)
                heat.Play();

            // 短くフェードさせ、急な音の切り替わりを抑える。
            heat.volume = Mathf.MoveTowards(
                heat.volume,
                heating ? heatingVolume : 0f,
                Time.unscaledDeltaTime * 3f
            );

            if (!heating && heat.volume <= 0f)
                heat.Stop();

            // リザルト表示時に一度だけファンファーレ。
            // 時間切れによる終了にも対応する。
            if (!resultPlayed &&
                result.IsFinished &&
                resultVisible)
            {
                resultPlayed = true;

                bgm.Stop();
                heat.Stop();
                heat.volume = 0f;

                ui.PlayOneShot(
                    resultSound,
                    effectVolume
                );
            }
        }

        void PositionSounds()
        {
            if (cooking.panBase == null)
                return;

            Vector3 position =
                cooking.panBase.bounds.center;

            // 焼く音はフライパンに追従する。
            heat.transform.position = position;

            // 成功音はキャッチした場所から鳴らす。
            if (!caught || !catchSource.isPlaying)
                catchSource.transform.position = position;
        }

        AudioSource Source(
            string name,
            bool spatial,
            bool loop)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);

            AudioSource source =
                child.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = loop;
            source.volume = 1f;

            // BGM・画面の操作音は2D。
            // 焼く音・キャッチ音は位置が分かる3D。
            source.spatialBlend = spatial ? 1f : 0f;

            // フライパンを振っても音程を変えない。
            source.dopplerLevel = 0f;

            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 8f;

            return source;
        }

        AudioClip Clip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(
                name,
                samples.Length,
                1,
                CookingSoundSamples.SampleRate,
                false
            );

            clip.SetData(samples, 0);
            generated.Add(clip);

            return clip;
        }

        void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(
                    OnStartPressed
                );
            }

            if (cooking != null)
            {
                cooking.CookingCompleted -= OnCaught;

                if (replacedCatch &&
                    cooking.catchClip == silentCatch)
                {
                    cooking.catchClip = originalCatch;
                }
            }

            replacedCatch = false;

            if (bgm != null) bgm.Stop();
            if (ui != null) ui.Stop();
            if (heat != null) heat.Stop();
            if (catchSource != null) catchSource.Stop();
        }

        void OnDestroy()
        {
            // 自動生成した効果音だけを片付ける。
            // 設定したMP3は削除しない。
            foreach (AudioClip clip in generated)
            {
                if (clip != null)
                    Destroy(clip);
            }
        }
    }

    // ここから下も同じファイルに入れてください。
    // 効果音の波形を生成する処理です。
    public static class CookingSoundSamples
    {
        public const int SampleRate = 24000;

        public static float[] Notes(
            float[] frequencies,
            float gap,
            float duration)
        {
            int count = (int)Math.Ceiling(
                (
                    (frequencies.Length - 1) * gap
                    + duration
                ) * SampleRate
            );

            var samples = new float[count];

            for (int note = 0;
                note < frequencies.Length;
                note++)
            {
                int offset =
                    (int)(note * gap * SampleRate);

                int length =
                    (int)(duration * SampleRate);

                for (int i = 0;
                    i < length && offset + i < count;
                    i++)
                {
                    double t = i / (double)SampleRate;

                    double envelope =
                        Math.Min(1d, t / .008d)
                        * Math.Pow(
                            1d - i / (double)length,
                            2d
                        );

                    double phase =
                        2d * Math.PI * frequencies[note] * t;

                    samples[offset + i] +=
                        (float)(
                            .26d * envelope
                            * (
                                Math.Sin(phase)
                                + .2d * Math.Sin(phase * 2d)
                            )
                        );
                }
            }

            return samples;
        }

        public static float[] Sizzle()
        {
            var random = new System.Random(321);

            var samples = new float[SampleRate * 2];

            double low = 0d;

            for (int i = 0; i < samples.Length; i++)
            {
                double noise =
                    random.NextDouble() * 2d - 1d;

                low += .12d * (noise - low);

                double t = i / (double)SampleRate;

                double pulse =
                    .75d
                    + .15d * Math.Sin(
                        2d * Math.PI * 7d * t
                    )
                    + .1d * Math.Sin(
                        2d * Math.PI * 19d * t
                    );

                // ループの継ぎ目の急な変化を抑える。
                double edge = Math.Min(
                    1d,
                    Math.Min(
                        i,
                        samples.Length - 1 - i
                    ) / 120d
                );

                samples[i] = (float)(
                    (noise - low) * pulse * .5d * edge
                );
            }

            return samples;
        }
    }
}