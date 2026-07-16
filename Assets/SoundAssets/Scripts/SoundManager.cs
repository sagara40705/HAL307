using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CriWare;

namespace Sound.Manager
{
    public class SoundManager : MonoBehaviour
    {
        private static CriAtomEx.CueInfo[] cueInfoList;
        private static CriAtomExPlayer atomExPlayer;
        private static CriAtomExAcb atomExAcb;

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
            atomExAcb = CriAtom.GetAcb("InGameCueSheet");
            cueInfoList = atomExAcb.GetCueInfoList();
        }

        // Update is called once per frame
        private void OnDestroy()
        {
            atomExPlayer.Dispose();
        }

        public
         static void Play(int SoundTag, float Soundvolume)
        {
            atomExPlayer.SetCue(atomExAcb, cueInfoList[SoundTag].name);
            atomExPlayer.SetVolume(Soundvolume);
            atomExPlayer.Start();
        }

        public static void Stop(int SoundTag)
        {
            atomExPlayer.SetCue(atomExAcb, cueInfoList[SoundTag].name);
            atomExPlayer.Stop();
        }
    }
}
