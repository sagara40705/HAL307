using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CriWare;

namespace Sound.Manager
{
    public class SoundManager : MonoBehaviour
    {
        private static CriAtomEx.CueInfo[][] cueInfoList;
        private static CriAtomExPlayer atomExPlayer;
        private static CriAtomExAcb[] atomExAcb;
        private static bool isSceneTransitioning;
        private static CriAtomExPlayback currentBgmPlayback;
        private static bool hasCurrentBgmPlayback;
        private static int currentBgmSceneTag = -1;
        private static int currentBgmSoundTag = -1;

        private void Awake()
        {
            atomExAcb = new CriAtomExAcb[4];
            cueInfoList = new CriAtomEx.CueInfo[4][];
        }

        public enum SceneSoundTag
        {
            TitleSound,
            IntroSound,
            InGameSound,
            ResultSound,
        }

        public enum TitleSoundTag
        {
            TitleBGM,
        }

        public enum IntroSoundTag
        {
            Handclap,
            CountDown,
        }

        public enum InGameSoundTag
        {
            CheersSE,
            PoseSE,
            ParrySE,
            InGameBGM,
            FeverBGM,
            DecideSE,
            MoveCursorSE,
        }

        public enum ResultSoundTag
        {
            ResultSE,
        }

        IEnumerator Start()
        {
            /* キューシートファイルのロード待ち */
            while (CriAtom.CueSheetsAreLoading)
            {
                yield return null;
            }

            /* AtomExPlayerの生成 */
            atomExPlayer = new CriAtomExPlayer();

            /* Cue情報の取得 */
            atomExAcb[(int)SceneSoundTag.TitleSound] = CriAtom.GetAcb("TitleCueSheet");
            cueInfoList[(int)SceneSoundTag.TitleSound] = atomExAcb[(int)SceneSoundTag.TitleSound].GetCueInfoList();
            atomExAcb[(int)SceneSoundTag.IntroSound] = CriAtom.GetAcb("IntroCueSheet");
            cueInfoList[(int)SceneSoundTag.IntroSound] = atomExAcb[(int)SceneSoundTag.IntroSound].GetCueInfoList();
            atomExAcb[(int)SceneSoundTag.InGameSound] = CriAtom.GetAcb("InGameCueSheet");
            cueInfoList[(int)SceneSoundTag.InGameSound] = atomExAcb[(int)SceneSoundTag.InGameSound].GetCueInfoList();
            atomExAcb[(int)SceneSoundTag.ResultSound] = CriAtom.GetAcb("ResultCueSheet");
            cueInfoList[(int)SceneSoundTag.ResultSound] = atomExAcb[(int)SceneSoundTag.ResultSound].GetCueInfoList();
        }

        // Update is called once per frame
        private void OnDestroy()
        {
            atomExPlayer?.Dispose();
            atomExPlayer = null;
            ClearCurrentBgm();
        }

        // 全Cueの停止。CRI未初期化時も呼び出し可能
        public static void StopAll()
        {
            atomExPlayer?.Stop(true);
            ClearCurrentBgm();
        }

        public static void BeginSceneTransition()
        {
            isSceneTransitioning = true;
            StopAll();
        }

        public static void EndSceneTransition()
        {
            isSceneTransitioning = false;
        }

        public static void Play(int scenesoundtag, int soundtag)
        {
            if (isSceneTransitioning || atomExPlayer == null)
            {
                return;
            }

            bool isBgm = IsBgmCue(scenesoundtag, soundtag);
            if (isBgm && hasCurrentBgmPlayback)
            {
                CriAtomExPlayback.Status status = currentBgmPlayback.GetStatus();
                bool isPlaying =
                    status == CriAtomExPlayback.Status.Prep ||
                    status == CriAtomExPlayback.Status.Playing;

                if (isPlaying &&
                    currentBgmSceneTag == scenesoundtag &&
                    currentBgmSoundTag == soundtag)
                {
                    return;
                }

                if (isPlaying)
                {
                    currentBgmPlayback.Stop(true);
                }

                ClearCurrentBgm();
            }

            atomExPlayer.SetCue(atomExAcb[scenesoundtag], cueInfoList[scenesoundtag][soundtag].name);
            CriAtomExPlayback playback = atomExPlayer.Start();

            if (isBgm)
            {
                currentBgmPlayback = playback;
                currentBgmSceneTag = scenesoundtag;
                currentBgmSoundTag = soundtag;
                hasCurrentBgmPlayback = true;
            }
        }

        public static void Stop(int scenesoundtag, int soundtag)
        {
            if (atomExPlayer == null)
            {
                return;
            }

            atomExPlayer.SetCue(atomExAcb[scenesoundtag], cueInfoList[scenesoundtag][soundtag].name);
            atomExPlayer.Stop();
            ClearCurrentBgm();
        }

        private static bool IsBgmCue(int sceneSoundTag, int soundTag)
        {
            return
                (sceneSoundTag == (int)SceneSoundTag.TitleSound &&
                 soundTag == (int)TitleSoundTag.TitleBGM) ||
                (sceneSoundTag == (int)SceneSoundTag.InGameSound &&
                 (soundTag == (int)InGameSoundTag.InGameBGM ||
                  soundTag == (int)InGameSoundTag.FeverBGM));
        }

        private static void ClearCurrentBgm()
        {
            hasCurrentBgmPlayback = false;
            currentBgmSceneTag = -1;
            currentBgmSoundTag = -1;
        }
    }
}
