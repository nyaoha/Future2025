using UnityEngine;
using UnityEngine.Video;

public class moviemanagement : MonoBehaviour
{
    [SerializeField]
    VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer.loopPointReached += LoopPointReached;
        videoPlayer.Play();
    }

    public void LoopPointReached(VideoPlayer vp)
    {
        // 動画再生完了時の処理
        Debug.Log("うんち");
    }
}
