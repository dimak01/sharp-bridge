using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service for opening files in external editors
    /// </summary>
    public interface IExternalEditorService
    {
        /// <summary>
        /// Attempts to open the transformation configuration file in the configured external editor
        /// </summary>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        Task<bool> TryOpenTransformationConfigAsync();
    }
}