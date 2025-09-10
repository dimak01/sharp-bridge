namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// Contains information about a transformation rule that encountered an error
    /// </summary>
    public class RuleInfo
    {
        /// <summary>
        /// The name of the transformation rule
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The function/expression string of the rule
        /// </summary>
        public string Func { get; }
        
        /// <summary>
        /// The error message describing what went wrong
        /// </summary>
        public string Error { get; }
        
        /// <summary>
        /// The type of error: "Validation" for syntax/bounds errors during loading, "Evaluation" for dependency/runtime errors
        /// </summary>
        public string Type { get; }
        
        /// <summary>
        /// Creates a new instance of RuleInfo
        /// </summary>
        /// <param name="name">The name of the rule</param>
        /// <param name="func">The function/expression string</param>
        /// <param name="error">The error message</param>
        /// <param name="type">The type of error (Validation or Evaluation)</param>
        public RuleInfo(string name, string func, string error, string type)
        {
            Name = name ?? string.Empty;
            Func = func ?? string.Empty;
            Error = error ?? string.Empty;
            Type = type ?? string.Empty;
        }
    }
} 