using System;
using System.Runtime.InteropServices;

namespace STT
{
    /// <summary>
    /// Whisper.cpp C API P/Invoke 래퍼
    /// </summary>
    public static class WhisperWrapper
    {
        private const string WHISPER_LIB = "whisper";

        // ===== Context =====

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr whisper_init_from_file(string path_model);

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void whisper_free(IntPtr ctx);

        // ===== Full Parameters =====

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern WhisperFullParams whisper_full_default_params(WhisperSamplingStrategy strategy);

        // ===== Processing =====

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_full(IntPtr ctx, WhisperFullParams param, IntPtr samples, int n_samples);

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_full_parallel(IntPtr ctx, WhisperFullParams param, IntPtr samples, int n_samples, int n_processors);

        // ===== Results =====

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_full_n_segments(IntPtr ctx);

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr whisper_full_get_segment_text(IntPtr ctx, int i_segment);

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern long whisper_full_get_segment_t0(IntPtr ctx, int i_segment);

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern long whisper_full_get_segment_t1(IntPtr ctx, int i_segment);

        // ===== Language =====

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int whisper_lang_id(string lang);

        // ===== System Info =====

        [DllImport(WHISPER_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr whisper_print_system_info();
    }

    /// <summary>
    /// Whisper 샘플링 전략
    /// </summary>
    public enum WhisperSamplingStrategy
    {
        WHISPER_SAMPLING_GREEDY = 0,
        WHISPER_SAMPLING_BEAM_SEARCH = 1
    }

    /// <summary>
    /// Whisper Full Parameters 구조체
    /// whisper.cpp의 whisper_full_params 구조체와 매핑
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WhisperFullParams
    {
        public WhisperSamplingStrategy strategy;

        public int n_threads;
        public int n_max_text_ctx;
        public int offset_ms;
        public int duration_ms;

        [MarshalAs(UnmanagedType.I1)]
        public bool translate;
        [MarshalAs(UnmanagedType.I1)]
        public bool no_context;
        [MarshalAs(UnmanagedType.I1)]
        public bool single_segment;
        [MarshalAs(UnmanagedType.I1)]
        public bool print_special;
        [MarshalAs(UnmanagedType.I1)]
        public bool print_progress;
        [MarshalAs(UnmanagedType.I1)]
        public bool print_realtime;
        [MarshalAs(UnmanagedType.I1)]
        public bool print_timestamps;

        [MarshalAs(UnmanagedType.I1)]
        public bool token_timestamps;
        public float thold_pt;
        public float thold_ptsum;
        public int max_len;
        [MarshalAs(UnmanagedType.I1)]
        public bool split_on_word;
        public int max_tokens;

        [MarshalAs(UnmanagedType.I1)]
        public bool speed_up;
        [MarshalAs(UnmanagedType.I1)]
        public bool debug_mode;
        public int audio_ctx;

        [MarshalAs(UnmanagedType.I1)]
        public bool tdrz_enable;

        public IntPtr initial_prompt;
        public IntPtr prompt_tokens;
        public int prompt_n_tokens;

        public IntPtr language;
        [MarshalAs(UnmanagedType.I1)]
        public bool detect_language;

        [MarshalAs(UnmanagedType.I1)]
        public bool suppress_blank;
        [MarshalAs(UnmanagedType.I1)]
        public bool suppress_non_speech_tokens;

        public float temperature;
        public float max_initial_ts;
        public float length_penalty;

        public float temperature_inc;
        public float entropy_thold;
        public float logprob_thold;
        public float no_speech_thold;

        // Greedy 파라미터
        public int greedy_best_of;

        // Beam Search 파라미터
        public int beam_search_beam_size;
        public float beam_search_patience;

        // 콜백 (사용하지 않음)
        public IntPtr new_segment_callback;
        public IntPtr new_segment_callback_user_data;
        public IntPtr progress_callback;
        public IntPtr progress_callback_user_data;
        public IntPtr encoder_begin_callback;
        public IntPtr encoder_begin_callback_user_data;
        public IntPtr abort_callback;
        public IntPtr abort_callback_user_data;
        public IntPtr logits_filter_callback;
        public IntPtr logits_filter_callback_user_data;

        public IntPtr grammar_rules;
        public int n_grammar_rules;
        public int i_start_rule;
        public float grammar_penalty;
    }
}
