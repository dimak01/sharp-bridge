using System.ComponentModel;

namespace SharpBridge.Models
{
    /// <summary>
    /// Enumeration of available columns for the PC parameter table display
    /// </summary>
    public enum ParameterTableColumn
    {
        /// <summary>
        /// Parameter name with color coding
        /// </summary>
        [Description("Parameter Name")]
        ParameterName,

        /// <summary>
        /// Visual progress bar representation
        /// </summary>
        [Description("Progress Bar")]
        ProgressBar,

        /// <summary>
        /// Raw numeric value
        /// </summary>
        [Description("Value")]
        Value,

        /// <summary>
        /// Weight and min/default/max information
        /// </summary>
        [Description("Range")]
        Range,

        /// <summary>
        /// Transformation expression
        /// </summary>
        [Description("Expression")]
        Expression,

        /// <summary>
        /// Both minimum and maximum values observed during runtime
        /// </summary>
        [Description("Min/Max")]
        MinMax,

        /// <summary>
        /// Interpolation method information
        /// </summary>
        [Description("Interpolation")]
        Interpolation
    }
}