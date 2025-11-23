using DA_Assets.DAI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    internal class ScriptGeneratorTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            var root = new VisualElement
            {
                style =
        {
          paddingTop = 10,
          paddingBottom = 10,
          paddingLeft = 10,
          paddingRight = 10
        }
            };

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            parent.Add(uitk.CreateTitle(FcuLocKey.label_script_generator.Localize()));
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            var settings = monoBeh.Settings.ScriptGeneratorSettings;

            var serializationModeField = uitk.EnumField(FcuLocKey.label_serialization_mode.Localize(), settings.SerializationMode);
            serializationModeField.tooltip = FcuLocKey.tooltip_serialization_mode.Localize();
            serializationModeField.RegisterValueChangedCallback(evt => settings.SerializationMode = (FieldSerializationMode)evt.newValue);
            formContainer.Add(serializationModeField);
            formContainer.Add(uitk.ItemSeparator());

            var namespaceField = uitk.TextField(FcuLocKey.label_namespace.Localize());
            namespaceField.tooltip = FcuLocKey.tooltip_namespace.Localize();
            namespaceField.value = settings.Namespace;
            namespaceField.RegisterValueChangedCallback(evt => settings.Namespace = evt.newValue);
            formContainer.Add(namespaceField);
            formContainer.Add(uitk.ItemSeparator());

            var baseClassField = uitk.TextField(FcuLocKey.label_base_class.Localize());
            baseClassField.tooltip = FcuLocKey.tooltip_base_class.Localize();
            baseClassField.value = settings.BaseClass;
            baseClassField.RegisterValueChangedCallback(evt => settings.BaseClass = evt.newValue);
            formContainer.Add(baseClassField);
            formContainer.Add(uitk.ItemSeparator());

            var folderPathContainer = uitk.CreateFolderInput(
                label: FcuLocKey.label_scripts_output_path.Localize(),
                tooltip: FcuLocKey.tooltip_scripts_output_path.Localize(),
                initialValue: settings.OutputPath,
                onPathChanged: (newValue) => settings.OutputPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    FcuLocKey.label_select_folder.Localize(),
                    settings.OutputPath, 
                    ""),
                buttonTooltip: FcuLocKey.tooltip_select_folder.Localize());
            formContainer.Add(folderPathContainer);
            formContainer.Add(uitk.ItemSeparator());

            var fieldNameLengthField = uitk.IntegerField(FcuLocKey.label_field_name_max_length.Localize());
            fieldNameLengthField.tooltip = FcuLocKey.tooltip_field_name_max_length.Localize();
            fieldNameLengthField.value = settings.FieldNameMaxLenght;
            fieldNameLengthField.RegisterValueChangedCallback(evt => settings.FieldNameMaxLenght = evt.newValue);
            formContainer.Add(fieldNameLengthField);
            formContainer.Add(uitk.ItemSeparator());

            var methodNameLengthField = uitk.IntegerField(FcuLocKey.label_method_name_max_length.Localize());
            methodNameLengthField.tooltip = FcuLocKey.tooltip_method_name_max_length.Localize();
            methodNameLengthField.value = settings.MethodNameMaxLenght;
            methodNameLengthField.RegisterValueChangedCallback(evt => settings.MethodNameMaxLenght = evt.newValue);
            formContainer.Add(methodNameLengthField);
            formContainer.Add(uitk.ItemSeparator());

            var classNameLengthField = uitk.IntegerField(FcuLocKey.label_class_name_max_length.Localize());
            classNameLengthField.tooltip = FcuLocKey.tooltip_class_name_max_length.Localize();
            classNameLengthField.value = settings.ClassNameMaxLenght;
            classNameLengthField.RegisterValueChangedCallback(evt => settings.ClassNameMaxLenght = evt.newValue);
            formContainer.Add(classNameLengthField);
            formContainer.Add(uitk.ItemSeparator());

            var buttonsContainer = new VisualElement
            {
                style =
                {
                  flexDirection = FlexDirection.Row,
                  flexWrap = Wrap.Wrap,
                  alignItems = Align.FlexStart
                }
            };

            var generateButton = uitk.Button(FcuLocKey.scriptgen_button_generate.Localize(), () =>
            {
                monoBeh.EditorEventHandlers.GenerateScripts_OnClick();
            });
            generateButton.style.flexShrink = 0;
            buttonsContainer.Add(generateButton);
            buttonsContainer.Add(uitk.Space10());

            var serializeButton = uitk.Button(FcuLocKey.scriptgen_button_serialize.Localize(), () =>
            {
                monoBeh.EditorEventHandlers.SerializeObjects_OnClick();
            });
            serializeButton.style.flexShrink = 0;
            buttonsContainer.Add(serializeButton);

            formContainer.Add(uitk.Space10());
            formContainer.Add(buttonsContainer);
        }
    }
}
