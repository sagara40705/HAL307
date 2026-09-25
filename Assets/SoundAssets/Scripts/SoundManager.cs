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
        }

        // 全Cueの停止。CRI未初期化時も呼び出し可能
        public static void StopAll()
        {
            atomExPlayer?.Stop(true);
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

        public
         static void Play(int scenesoundtag, int soundtag)
        {
            if (isSceneTransitioning || atomExPlayer == null)
            {
                return;
            }

            atomExPlayer.SetCue(atomExAcb[scenesoundtag], cueInfoList[scenesoundtag][soundtag].name);
            atomExPlayer.Start();
        }

        public static void Stop(int scenesoundtag, int soundtag)
        {
            if (atomExPlayer == null)
            {
                return;
            }

            atomExPlayer.SetCue(atomExAcb[scenesoundtag], cueInfoList[scenesoundtag][soundtag].name);
            atomExPlayer.Stop();
        }
    }
}
