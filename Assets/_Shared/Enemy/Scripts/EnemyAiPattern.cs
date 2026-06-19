using Pose;

namespace Enemy
{

    // 敵のAIパターンを定義する列挙型
    public enum AiPattern
    {
        NoFakeRandom,     // フェイクなしのランダム
        FakeRandom,       // フェイクありのランダム
        AllRandom,        // 全てランダム
        AdvancedSettings, // 高度な設定
    }

    // 詳細設定のカスタム構造体
    public struct DetailCustomDesc
    {
        public PoseID _prePoseId;   // 予備動作中のポーズID
        public PoseID _postPoseId;  // 後続動作中のポーズID

        // リセット
        public void Reset()
        {
            _prePoseId = PoseID.None;
            _postPoseId = PoseID.None;
        }

        // 有効かどうかを判定
        public bool IsValid()
        {
            return _prePoseId != PoseID.None && _postPoseId != PoseID.None;
        }

        // フェイクかどうかを判定
        public bool IsFake()
        {
            return _prePoseId != PoseID.None && _postPoseId == PoseID.None;
        }
    }

    public class PoseDesc
    {
        public AiPattern _pattern;           // AIパターン
        public DetailCustomDesc _customDesc; // 詳細設定

        // コンストラクタ
        public PoseDesc()
        {
            Reset();
        }

        // リセット
        public void Reset()
        {
            _pattern = AiPattern.NoFakeRandom;
            _customDesc.Reset();
        }
    }

}
