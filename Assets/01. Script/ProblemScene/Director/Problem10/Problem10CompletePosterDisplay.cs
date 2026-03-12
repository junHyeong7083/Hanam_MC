using UnityEngine;

/// <summary>
/// Problem10CompletePosterDisplay - 문제10 스텝3 완료 화면의 포스터 표시 스크립트
///
/// 【역할】 StepCompletionGate의 completeRoot에 부착되어, OnEnable 시
///          Step2에서 선택한 장르에 해당하는 포스터 오브젝트를 spawnPoint로 이동시킨다.
///          포스터 안에는 이미 STT로 채워진 다짐 텍스트가 포함되어 있다.
///          OnDisable 시 포스터를 원래 위치로 복원하여 재사용 가능하게 한다.
/// 【패턴】 독립 MonoBehaviour (Binder/Logic 패턴 외)
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝3 완료 화면
/// 【참조하는 곳】 씬의 Problem10 CompleteRoot에 부착
/// 【참조되는 곳】 Problem10SharedData (선택된 장르 인덱스)
/// 【흐름】 CompleteRoot 활성화(OnEnable) → 선택된 포스터를 spawnPoint로 이동
///         → CompleteRoot 비활성화(OnDisable) → 포스터를 원래 위치로 복원
/// </summary>
public class Problem10CompletePosterDisplay : MonoBehaviour
{
    [Header("===== 공유 데이터 =====")]
    [Tooltip("Step2, Step3과 같은 SharedData 에셋 연결")]
    [SerializeField] private Problem10SharedData sharedData;       // 스텝 간 공유 데이터

    [Header("===== 장르별 포스터 (씬에 있는 오브젝트) =====")]
    [Tooltip("녹음 화면의 장르별 포스터들 (이미 title/commitment 텍스트 포함)")]
    [SerializeField] private RectTransform[] genrePosters;         // 장르별 포스터 RectTransform 배열

    [Header("===== 포스터 표시 위치 =====")]
    [Tooltip("완료 화면에서 포스터가 표시될 빈 RectTransform")]
    [SerializeField] private RectTransform posterSpawnPoint;        // 포스터가 이동될 목적지

    // ===== 이동된 포스터 정보 (복원용) =====
    private RectTransform _movedPoster;            // 이동된 포스터 참조
    private Transform _originalParent;             // 원래 부모 Transform
    private int _originalSiblingIndex;             // 원래 형제 순서 인덱스
    private Vector2 _originalAnchoredPosition;     // 원래 앵커 위치
    private Vector3 _originalScale;                // 원래 스케일

    /// <summary>CompleteRoot가 활성화되면 선택된 포스터를 spawnPoint로 이동시킨다.</summary>
    private void OnEnable()
    {
        MoveSelectedPosterToSpawnPoint();
    }

    /// <summary>CompleteRoot가 비활성화되면 포스터를 원래 위치로 복원한다.</summary>
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
