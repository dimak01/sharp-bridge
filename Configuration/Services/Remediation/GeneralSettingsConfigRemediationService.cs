using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration;
using SharpBridge.Interfaces.Configuration.Services.Remediation;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.UI.Providers;
using SharpBridge.Utilities;

namespace SharpBridge.Configuration.Services.Remediation
{
    /// <summary>
    /// Remediation service for GeneralSettingsConfig configuration sections.
    /// </summary>
    public class GeneralSettingsConfigRemediationService : IConfigSectionRemediationService
    {
        private readonly IShortcutParser _shortcutParser;

        /// <summary>
        /// Initializes a new instance of the GeneralSettingsConfigRemediationService class.
        /// </summary>
        /// <param name="shortcutParser">Shortcut parser for formatting shortcuts</param>
        public GeneralSettingsConfigRemediationService(IShortcutParser shortcutParser)
        {
            _shortcutParser = shortcutParser ?? throw new ArgumentNullException(nameof(shortcutParser));
        }

        /// <summary>
        /// Remediates configuration issues for a GeneralSettingsConfig section by fixing missing or invalid fields.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>A tuple indicating the remediation result and the updated configuration section (if changes were made)</returns>
        public async Task<(RemediationResult Result, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState)
        {
            var workingFields = new List<ConfigFieldState>(fieldsState);

            // Check if ANY field is missing - if so, silently fill with defaults
            if (IsAnyFieldMissing(workingFields))
            {
                var defaultConfig = CreateConfigFromFieldStates(ApplyDefaultsToMissingFields(workingFields));
                return (RemediationResult.Succeeded, defaultConfig);
            }

            // If all fields are present, no remediation needed
            return (RemediationResult.NoRemediationNeeded, null);
        }

        /// <summary>
        /// Checks if any required fields are missing.
        /// </summary>
        /// <param name="fields">The field states to check</param>
        /// <returns>True if any field is missing</returns>
        private static bool IsAnyFieldMissing(List<ConfigFieldState> fields)
        {
            // Check if any required fields are missing (not present or null)
            bool Missing(string name)
            {
                var f = fields.FirstOrDefault(x => x.FieldName == name);
                return f == null || !f.IsPresent || f.Value == null;
            }

            var missingEditorCommand = Missing("EditorCommand");
            var missingShortcuts = Missing("Shortcuts");

            return missingEditorCommand || missingShortcuts;
        }

        /// <summary>
        /// Applies default values to missing fields.
        /// </summary>
        /// <param name="fields">The field states to update</param>
        /// <returns>Updated field states with defaults applied</returns>
        private List<ConfigFieldState> ApplyDefaultsToMissingFields(List<ConfigFieldState> fields)
        {
            var result = new List<ConfigFieldState>(fields);

            // Apply default for EditorCommand if missing
            var editorCommandField = result.FirstOrDefault(f => f.FieldName == "EditorCommand");
            if (editorCommandField == null || !editorCommandField.IsPresent || editorCommandField.Value == null)
            {
                var defaultEditorCommand = new ConfigFieldState(
                    "EditorCommand",
                    "notepad.exe \"%f\"",
                    true,
                    typeof(string),
                    "External Editor Command");

                if (editorCommandField != null)
                {
                    var idx = result.IndexOf(editorCommandField);
                    result[idx] = defaultEditorCommand;
                }
                else
                {
                    result.Add(defaultEditorCommand);
                }
            }

            // Apply default for Shortcuts if missing
            var shortcutsField = result.FirstOrDefault(f => f.FieldName == "Shortcuts");
            if (shortcutsField == null || !shortcutsField.IsPresent || shortcutsField.Value == null)
            {
                var defaultShortcuts = CreateDefaultShortcutsDictionary();
                var defaultShortcutsField = new ConfigFieldState(
                    "Shortcuts",
                    defaultShortcuts,
                    true,
                    typeof(Dictionary<string, string>),
                    "Keyboard Shortcuts");

                if (shortcutsField != null)
                {
                    var idx = result.IndexOf(shortcutsField);
                    result[idx] = defaultShortcutsField;
                }
                else
                {
                    result.Add(defaultShortcutsField);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates the default shortcuts dictionary using the static utility and shortcut parser.
        /// </summary>
        /// <returns>Dictionary with default shortcuts as strings</returns>
        private Dictionary<string, string> CreateDefaultShortcutsDictionary()
        {
            var defaultShortcuts = DefaultShortcutsProvider.GetDefaultShortcuts();
            var shortcutsDict = new Dictionary<string, string>();

            foreach (var (action, shortcut) in defaultShortcuts)
            {
                var actionName = action.ToString();
                var shortcutString = _shortcutParser.FormatShortcut(shortcut);
                shortcutsDict[actionName] = shortcutString;
            }

            return shortcutsDict;
        }

        /// <summary>
        /// Creates a GeneralSettingsConfig from the field states.
        /// </summary>
        /// <param name="fieldsState">The field states to convert</param>
        /// <returns>GeneralSettingsConfig object</returns>
        private static GeneralSettingsConfig CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
        {
            var config = new GeneralSettingsConfig();

            foreach (var field in fieldsState)
            {
                if (field.IsPresent && field.Value != null)
                {
                    switch (field.FieldName)
                    {
                        case "EditorCommand" when field.Value is string editorCommand:
                            config.EditorCommand = editorCommand;
                            break;
                        case "Shortcuts" when field.Value is Dictionary<string, string> shortcuts:
                            config.Shortcuts = shortcuts;
                            break;
                    }
                }
            }

            return config;
        }
    }
}
