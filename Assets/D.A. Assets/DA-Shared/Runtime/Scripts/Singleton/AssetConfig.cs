using DA_Assets.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.Singleton
{
    public enum DALanguage
    {
        en,
        ja,
        ko,
        zh,
        es,
        de,
        fr
    }

    public class AssetConfig<T> : SingletonScriptableObject<T> where T : ScriptableObject
    {
        public string ProductVersion => productVersion;
        [SerializeField] string productVersion;

        public InternalLocalizator Localizator => localizator.Init();
        [SerializeField] private InternalLocalizator localizator;

        [SerializeField] private bool isLanguageInitialized = false;

        protected override void OnCreateInstance()
        {
            base.OnCreateInstance();

            if (!isLanguageInitialized)
            {
                DALanguage detectedLanguage = SystemLanguageDetector.GetSystemLanguage();
                localizator.Language = detectedLanguage;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                isLanguageInitialized = true;
                Debug.Log(SharedLocKey.log_initial_language_set.Localize(name, detectedLanguage));
            }
        }
    }

    [Serializable]
    public class InternalLocalizator
    {
        [SerializeField] TextAsset locFile;
        [SerializeField] DALanguage _language = DALanguage.en;

        public event Action OnLanguageChanged;

        private Dictionary<string, Dictionary<DALanguage, string>> _localizations
            = new Dictionary<string, Dictionary<DALanguage, string>>();

        public DALanguage Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        public InternalLocalizator Init()
        {
#if UNITY_EDITOR
            if (_localizations.Count == 0)
            {
                if (locFile == null)
                {
                    //throw new NullReferenceException("Localization file missing.");
                }
                else
                {
                    _localizations = ParseCsv(locFile.text);
                }
            }
#endif
            return this;
        }

        public string GetLocalizedText(Enum key, DALanguage? lang = null, params object[] args) =>
            GetLocalizedText(key.ToString(), lang, args);

        private string GetLocalizedText(string key, DALanguage? lang, params object[] args)
        {
            DALanguage targetLanguage = lang ?? _language;

            if (_localizations.TryGetValue(key, out var langDict))
            {
                bool hasValue = langDict.TryGetValue(targetLanguage, out var txt);

                if (string.IsNullOrEmpty(txt))
                {
                    return key;
                }

                try
                {
                    return string.Format(txt, args);
                }
                catch (FormatException ex)
                {
                    Debug.LogWarning(SharedLocKey.log_localization_format_error.Localize(key, targetLanguage, txt));
                    return txt;
                }
            }

            return key;
        }

        private Dictionary<string, Dictionary<DALanguage, string>> ParseCsv(string csvText)
        {
            var result = new Dictionary<string, Dictionary<DALanguage, string>>();
            var langs = (DALanguage[])Enum.GetValues(typeof(DALanguage));

            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();

                        if (currentRow.Count > 1)
                        {
                            var key = currentRow[0].TrimStart('\uFEFF').Trim();
                            if (!string.IsNullOrEmpty(key))
                            {
                                var langDict = new Dictionary<DALanguage, string>();
                                for (int j = 0; j < langs.Length && j + 1 < currentRow.Count; j++)
                                {
                                    langDict[langs[j]] = currentRow[j + 1];
                                }
                                result[key] = langDict;
                            }
                        }

                        currentRow.Clear();

                        if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                        {
                            i++;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }

            currentRow.Add(currentField.ToString());
            if (currentRow.Count > 1)
            {
                var key = currentRow[0].TrimStart('\uFEFF').Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    var langDict = new Dictionary<DALanguage, string>();
                    for (int j = 0; j < langs.Length && j + 1 < currentRow.Count; j++)
                    {
                        langDict[langs[j]] = currentRow[j + 1];
                    }
                    result[key] = langDict;
                }
            }

            return result;
        }
    }
}
