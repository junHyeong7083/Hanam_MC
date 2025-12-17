using UnityEngine;

/// <summary>
/// Problem10 Step3 완료 화면의 포스터 표시
/// - Gate의 completeRoot에 붙이는 스크립트
/// - OnEnable() 시 선택된 포스터를 spawnPoint로 reparent
/// - 포스터 안에 이미 STT로 채워진 title/commitment 텍스트가 있음
/// </summary>
public class Problem10CompletePosterDisplay : MonoBehaviour
{
    [Header("===== 공유 데이터 =====")]
    [Tooltip("Step2, Step3과 같은 SharedData 에셋 연결")]
    [SerializeField] private Problem10SharedData sharedData;

    [Header("===== 장르별 포스터 (씬에 있는 오브젝트) =====")]
    [Tooltip("녹음 화면의 장르별 포스터들 (이미 title/commitment 텍스트 포함)")]
    [SerializeField] private RectTransform[] genrePosters;

    [Header("===== 포스터 표시 위치 =====")]
    [Tooltip("완료 화면에서 포스터가 표시될 빈 RectTransform")]
    [SerializeField] private RectTransform posterSpawnPoint;

    // 이동된 포스터 정보 (복원용)
    private RectTransform _movedPoster;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Vector2 _originalAnchoredPosition;
    private Vector3 _originalScale;

    private void OnEnable()
    {
        MoveSelectedPosterToSpawnPoint();
    }

    private void OnDisable()
    {
        RestorePosterToOriginalPosition();
    }

    /// <summary>선택된 포스터를 spawnPoint로 이동</summary>
    private void MoveSelectedPosterToSpawnPoint()
    {
        // 유효성 검사
        if (sharedData == null || genrePosters == null || posterSpawnPoint == null) return;

        int index = sharedData.selectedGenreIndex;
        if (index < 0 || index >= genrePosters.Length) return;

        var poster = genrePosters[index];
        if (poster == null) return;

        // 원래 위치 정보 저장
        _movedPoster = poster;
        _originalParent = poster.parent;
        _originalSiblingIndex = poster.GetSiblingIndex();
        _originalAnchoredPosition = poster.anchoredPosition;
        _originalScale = poster.localScale;

        // spawnPoint로 이동
        poster.SetParent(posterSpawnPoint, false);
        poster.anchoredPosition = Vector2.zero;
        poster.localScale = Vector3.one * 0.95f;
        poster.gameObject.SetActive(true);
    }

    /// <summary>포스터를 원래 위치로 복원</summary>
    private void RestorePosterToOriginalPosition()
    {
        if (_movedPoster == null || _originalParent == null) return;

        // 원래 부모로 복원
        _movedPoster.SetParent(_originalParent, false);
        _movedPoster.SetSiblingIndex(_originalSiblingIndex);
        _movedPoster.anchoredPosition = _originalAnchoredPosition;
        _movedPoster.localScale = _originalScale;

        _movedPoster = null;
        _originalParent = null;
    }
}
