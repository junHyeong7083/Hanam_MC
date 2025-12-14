using System;
using System.Runtime.InteropServices;

namespace STT
{
    /// <summary>
    /// Vosk C API P/Invoke 래퍼
    /// </summary>
    public static class VoskWrapper
    {
        private const string VOSK_LIB = "libvosk";

        // Model
        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vosk_model_new(string model_path);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vosk_model_free(IntPtr model);

        // Recognizer
        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vosk_recognizer_new(IntPtr model, float sample_rate);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vosk_recognizer_new_grm(IntPtr model, float sample_rate, string grammar);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vosk_recognizer_free(IntPtr recognizer);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vosk_recognizer_accept_waveform(IntPtr recognizer, byte[] data, int length);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vosk_recognizer_accept_waveform_s(IntPtr recognizer, short[] data, int length);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vosk_recognizer_result(IntPtr recognizer);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vosk_recognizer_partial_result(IntPtr recognizer);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vosk_recognizer_final_result(IntPtr recognizer);

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vosk_recognizer_reset(IntPtr recognizer);

        // GPU (선택적)
        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vosk_gpu_init();

        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vosk_gpu_thread_init();

        // 로그 레벨 설정
        [DllImport(VOSK_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vosk_set_log_level(int log_level);
    }
}
