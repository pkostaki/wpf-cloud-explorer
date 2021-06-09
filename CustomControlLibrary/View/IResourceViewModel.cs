using StorageLib.CloudStorage.Api;

namespace CustomControlLibrary
{
    /// <summary>
    /// Represent view of resource
    /// </summary>
    public interface IResourceViewModel : IResource
    {       
        /// <summary>
        /// True, if resource was selected inside container view (i.g. DataGrid).
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// True, if resource was cut inside container view. After Paste or clear buffer this property set to false.
        /// </summary>
        bool IsCutted { get; set; }
    }
}
