using System;
using System.Collections.Generic;
using System.Text.Json;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities.Migrations;

/// <summary>
/// Migrates configuration files that have no version property to version 1.
/// This handles the current scenario where existing JSON files don't have version information.
/// </summary>
public class NoVersionToV1Migration : IJsonConfigMigration
{
    /// <summary>
    /// The version this migration migrates from (0 represents no version)
    /// </summary>
    public int FromVersion => 0;

    /// <summary>
    /// The version this migration migrates to
    /// </summary>
    public int ToVersion => 1;

    /// <summary>
    /// Migrates configuration JSON from no version to version 1.
    /// This simply adds the Version property to the existing JSON.
    /// </summary>
    /// <param name="sourceJson">The source JSON document without version</param>
    /// <returns>The migrated JSON with Version property added</returns>
    public string Migrate(JsonDocument sourceJson)
    {
        if (sourceJson == null)
            throw new ArgumentNullException(nameof(sourceJson));

        // Parse the existing JSON into a dictionary-like structure
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // Convert to Dictionary to manipulate
        var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(sourceJson.RootElement.GetRawText(), jsonOptions);

        if (jsonDict == null)
            throw new InvalidOperationException("Failed to parse source JSON");

        // Add the Version property if it doesn't exist
        if (!jsonDict.ContainsKey("Version"))
        {
            jsonDict["Version"] = 1;
        }

        // Serialize back to JSON
        return JsonSerializer.Serialize(jsonDict, jsonOptions);
    }
}
