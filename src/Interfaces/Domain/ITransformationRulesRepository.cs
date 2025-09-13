// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using SharpBridge.Models;
using SharpBridge.Models.Events;

namespace SharpBridge.Interfaces.Domain
{
    /// <summary>
    /// Interface for loading and managing transformation rules from various sources
    /// </summary>
    public interface ITransformationRulesRepository : IDisposable
    {
        /// <summary>
        /// Event raised when the rules file changes on disk
        /// </summary>
        event EventHandler<RulesChangedEventArgs> RulesChanged;

        /// <summary>
        /// Loads transformation rules from the configured path in application config
        /// </summary>
        /// <returns>Result containing loaded rules, validation errors, and cache information</returns>
        Task<RulesLoadResult> LoadRulesAsync();

        /// <summary>
        /// Loads transformation rules from the specified file path
        /// </summary>
        /// <param name="filePath">Path to the transformation rules file</param>
        /// <returns>Result containing loaded rules, validation errors, and cache information</returns>
        Task<RulesLoadResult> LoadRulesAsync(string filePath);

        /// <summary>
        /// Gets whether the currently loaded rules are up to date with the source
        /// </summary>
        bool IsUpToDate { get; }

        /// <summary>
        /// Gets the path to the currently loaded rules file
        /// </summary>
        string TransformationRulesPath { get; }

        /// <summary>
        /// Gets the timestamp when rules were last successfully loaded
        /// </summary>
        DateTime LastLoadTime { get; }
    }
}