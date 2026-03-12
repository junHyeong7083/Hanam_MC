using System;
using System.Runtime.InteropServices;

namespace STT
{
    /// <summary>
    /// WhisperWrapper - Whisper.cpp 네이티브 C API의 P/Invoke(DllImport) 래퍼 클래스
    ///
    /// 【역할】
    ///   whisper.dll (네이티브 C/C++ 라이브러리)의 함수들을 C#에서 호출할 수 있도록
    ///   DllImport 어트리뷰트를 통한 P/Invoke 바인딩을 제공한다.
    ///   이 클래스는 STTManager에서만 직접 호출되며, 나머지 시스템은 STTManager를 통해 간접 사용한다.
    ///
    /// 【참조하는 곳】
    ///   - STTManager : InitializeWhisper(), RunWhisper(), Cleanup()에서 직접 호출
    ///
    /// 【참조되는 곳】
    ///   - whisper.dll : Plugins 폴더의 네이티브 DLL (whisper.cpp 빌드)
    ///
    /// 【흐름】
    ///   whisper_init_from_file() → whisper_full_default_params() → whisper_full()
    ///   → whisper_full_n_segments() / whisper_full_get_segment_text() → whisper_free()
    ///
    /// ※ 이 파일의 구조체 레이아웃은 whisper.cpp의 C 구조체와 정확히 일치해야 한다.
    ///   필드 순서, 크기, 정렬이 다르면 메모리 레이아웃 불일치로 크래시가 발생한다.
    /// </summary>
    public static class WhisperWrapper
    {
        /// <summary>네이티브 DLL 이름 (Plugins 폴더의 whisper.dll 또는 libwhisper.so)</summary>
        private const string WHISPER_LIB = "whisper";

        // ===== Context (모델 로드/해제) =====

        /// <summary>
        /// 모델 파일(.bin)에서 Whisper 컨텍스트를 생성한다.
        /// 반환된 IntPtr은 이후 모든 Whisper API 호출에 사용되며,
        /// 사용 완료 후 반드시 whisper_free()로 해제해야 한다.
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr whisper_init_from_file(string path_model);

        /// <summary>
        /// Whisper 컨텍스트를 해제하고 관련 메모리를 반환한다.
        /// STTManager.Cleanup()에서 호출된다.
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void whisper_free(IntPtr ctx);

        // ===== Full Parameters (파라미터 생성) =====

        /// <summary>
        /// 지정된 샘플링 전략(Greedy/BeamSearch)에 대한 기본 파라미터 구조체를 반환한다.
        /// 반환된 구조체의 필드를 수정하여 언어, 스레드 수 등을 설정한 후 whisper_full()에 전달한다.
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern WhisperFullParams whisper_full_default_params(WhisperSamplingStrategy strategy);

        // ===== Processing (음성 인식 실행) =====

        /// <summary>
        /// 단일 스레드로 음성 인식을 수행한다 (메인 추론 함수).
        /// samples는 16kHz mono float PCM 데이터의 네이티브 포인터.
        /// 반환값: 0=성공, 그 외=실패
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_full(IntPtr ctx, WhisperFullParams param, IntPtr samples, int n_samples);

        /// <summary>
        /// 멀티 프로세서로 병렬 음성 인식을 수행한다 (현재 사용하지 않음).
        /// n_processors만큼 오디오를 분할하여 병렬 처리한다.
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_full_parallel(IntPtr ctx, WhisperFullParams param, IntPtr samples, int n_samples, int n_processors);

        // ===== Results (인식 결과 조회) =====

        /// <summary>
        /// 마지막 whisper_full() 호출의 인식 결과 세그먼트 수를 반환한다.
        /// 각 세그먼트는 하나의 문장/구절에 해당한다.
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_full_n_segments(IntPtr ctx);

        /// <summary>
        /// 지정된 세그먼트의 인식 텍스트를 네이티브 문자열 포인터로 반환한다.
        /// Marshal.PtrToStringAnsi()로 C# 문자열로 변환하여 사용한다.
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr whisper_full_get_segment_text(IntPtr ctx, int i_segment);

        /// <summary>
        /// 지정된 세그먼트의 시작 타임스탬프를 반환한다 (현재 사용하지 않음).
        /// 단위: 10ms (예: 100 = 1.0초)
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern long whisper_full_get_segment_t0(IntPtr ctx, int i_segment);

        /// <summary>
        /// 지정된 세그먼트의 종료 타임스탬프를 반환한다 (현재 사용하지 않음).
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern long whisper_full_get_segment_t1(IntPtr ctx, int i_segment);

        // ===== Language (언어 관련) =====

        /// <summary>
        /// 언어 코드(예: "ko", "en")에 해당하는 내부 언어 ID를 반환한다 (현재 사용하지 않음).
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_lang_id(string lang);

        // ===== System Info =====

        /// <summary>
        /// Whisper 시스템 정보 문자열을 반환한다 (SIMD 지원 여부 등, 디버그용).
        /// </summary>
        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr whisper_print_system_info();
    }

    /// <summary>
    /// Whisper 음성 인식 샘플링 전략
    /// GREEDY: 각 단계에서 가장 확률 높은 토큰 선택 (빠르지만 정확도 약간 낮음) — 현재 사용 중
    /// BEAM_SEARCH: 여러 후보를 동시에 탐색하여 최적 시퀀스 선택 (느리지만 정확도 높음)
    /// </summary>
    public enum WhisperSamplingStrategy
    {
        /// <summary>탐욕적 샘플링 - 매 스텝 최고 확률 토큰 선택 (기본값, 속도 우선)</summary>
        WHISPER_SAMPLING_GREEDY = 0,
        /// <summary>빔 서치 - 다수 후보 동시 탐색 (정확도 우선, 현재 미사용)</summary>
        WHISPER_SAMPLING_BEAM_SEARCH = 1
    }

    /// <summary>
    /// Whisper Full Parameters 구조체 - whisper.cpp의 whisper_full_params와 1:1 매핑
    ///
    /// ※ 중요: 이 구조체의 필드 순서와 크기는 whisper.cpp의 C 구조체와 정확히 일치해야 한다.
    ///   LayoutKind.Sequential로 메모리 레이아웃이 선언 순서대로 배치된다.
    ///   bool 필드는 MarshalAs(UnmanagedType.I1)로 1바이트로 마샬링한다 (C의 bool과 동일).
    ///
    /// STTManager.RunWhisper()에서 whisper_full_default_params()로 기본값을 받은 후
    /// 필요한 필드만 수정하여 whisper_full()에 전달한다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WhisperFullParams
    {
        /// <summary>샘플링 전략 (GREEDY 또는 BEAM_SEARCH)</summary>
        public WhisperSamplingStrategy strategy;

        /// <summary>추론에 사용할 CPU 스레드 수</summary>
        public int n_threads;
        /// <summary>텍스트 컨텍스트 최대 토큰 수</summary>
        public int n_max_text_ctx;
        /// <summary>오디오 시작 오프셋 (ms) — 0이면 처음부터</summary>
        public int offset_ms;
        /// <summary>처리할 오디오 길이 (ms) — 0이면 전체</summary>
        public int duration_ms;

        /// <summary>true이면 인식 결과를 영어로 번역</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool translate;
        /// <summary>true이면 이전 컨텍스트(initial_prompt 포함) 무시</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool no_context;
        /// <summary>true이면 단일 세그먼트로 처리 (실시간 모드에서 사용)</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool single_segment;
        /// <summary>특수 토큰 출력 여부 (디버그용)</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool print_special;
        /// <summary>처리 진행률 출력 여부</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool print_progress;
        /// <summary>실시간 결과 출력 여부</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool print_realtime;
        /// <summary>타임스탬프 출력 여부</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool print_timestamps;

        /// <summary>토큰별 타임스탬프 생성 여부</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool token_timestamps;
        /// <summary>토큰 타임스탬프 확률 임계값</summary>
        public float thold_pt;
        /// <summary>토큰 타임스탬프 누적 확률 임계값</summary>
        public float thold_ptsum;
        /// <summary>세그먼트 최대 길이 (문자 수)</summary>
        public int max_len;
        /// <summary>단어 경계에서 세그먼트 분리 여부</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool split_on_word;
        /// <summary>생성할 최대 토큰 수 (환각 방지용 — 실시간:4, 최종:16)</summary>
        public int max_tokens;

        /// <summary>속도 향상 모드 (정확도 감소, 현재 미사용)</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool speed_up;
        /// <summary>디버그 모드 활성화 여부</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool debug_mode;
        /// <summary>오디오 컨텍스트 크기 (0이면 자동)</summary>
        public int audio_ctx;

        /// <summary>TDRZ(Tinydiarize) 화자 분리 활성화 (현재 미사용)</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool tdrz_enable;

        /// <summary>초기 프롬프트 문자열 포인터 (키워드 힌트 전달용, Marshal.StringToHGlobalAnsi로 생성)</summary>
        public IntPtr initial_prompt;
        /// <summary>프롬프트 토큰 배열 포인터 (현재 미사용)</summary>
        public IntPtr prompt_tokens;
        /// <summary>프롬프트 토큰 수 (현재 미사용)</summary>
        public int prompt_n_tokens;

        /// <summary>인식 대상 언어 코드 포인터 (예: "ko"의 ANSI 문자열)</summary>
        public IntPtr language;
        /// <summary>자동 언어 감지 여부 (false = language 필드 사용)</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool detect_language;

        /// <summary>빈 세그먼트(무음) 결과 억제</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool suppress_blank;
        /// <summary>비음성 토큰(음악 기호, 잡음 표시 등) 억제</summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool suppress_non_speech_tokens;

        /// <summary>샘플링 온도 (0=결정적, 높을수록 다양한 결과)</summary>
        public float temperature;
        /// <summary>초기 타임스탬프 최대값</summary>
        public float max_initial_ts;
        /// <summary>길이 페널티 (빔 서치용)</summary>
        public float length_penalty;

        /// <summary>실패 시 온도 증가량 (0이면 재시도 안함 → 속도 향상)</summary>
        public float temperature_inc;
        /// <summary>엔트로피 임계값 (이 값 초과 시 해당 세그먼트 폐기)</summary>
        public float entropy_thold;
        /// <summary>로그 확률 임계값</summary>
        public float logprob_thold;
        /// <summary>무음 판정 임계값 (이 값 이상이면 "음성 없음")</summary>
        public float no_speech_thold;

        // ===== Greedy 샘플링 파라미터 =====
        /// <summary>Greedy 전략에서 고려할 후보 수 (1이면 최고 확률만)</summary>
        public int greedy_best_of;

        // ===== Beam Search 파라미터 =====
        /// <summary>빔 서치 빔 크기 (동시 탐색 후보 수)</summary>
        public int beam_search_beam_size;
        /// <summary>빔 서치 patience (조기 종료 기준)</summary>
        public float beam_search_patience;

        // ===== 콜백 함수 포인터 (현재 프로젝트에서 사용하지 않음) =====
        /// <summary>새 세그먼트 생성 시 콜백 (미사용)</summary>
        public IntPtr new_segment_callback;
        public IntPtr new_segment_callback_user_data;
        /// <summary>진행률 콜백 (미사용)</summary>
        public IntPtr progress_callback;
        public IntPtr progress_callback_user_data;
        /// <summary>인코더 시작 콜백 (미사용)</summary>
        public IntPtr encoder_begin_callback;
        public IntPtr encoder_begin_callback_user_data;
        /// <summary>중단 콜백 (미사용)</summary>
        public IntPtr abort_callback;
        public IntPtr abort_callback_user_data;
        /// <summary>로짓 필터 콜백 (미사용)</summary>
        public IntPtr logits_filter_callback;
        public IntPtr logits_filter_callback_user_data;

        // ===== Grammar (문법 기반 제한, Whisper에서 실험적 기능) =====
        /// <summary>문법 규칙 포인터 (미사용)</summary>
        public IntPtr grammar_rules;
        /// <summary>문법 규칙 수 (미사용)</summary>
        public int n_grammar_rules;
        /// <summary>시작 규칙 인덱스 (미사용)</summary>
        public int i_start_rule;
        /// <summary>문법 페널티 (미사용)</summary>
        public float grammar_penalty;
    }
}
