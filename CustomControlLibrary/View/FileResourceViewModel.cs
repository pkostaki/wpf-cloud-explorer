using StorageLib.CloudStorage.Implementation;

namespace CustomControlLibrary
{
    /// <summary>
    /// Represent file resource view.
    /// </summary>
    public class FileResourceViewModel : FileResource, IResourceViewModel
    {
        private bool _isSelected;

        ///<inheritdoc/>
        public bool IsSelected
        {
            get => _isSelected; set { Set(ref _isSelected, value); }
        }   
        
        private bool _isCutted;

        ///<inheritdoc/>
        public bool IsCutted
        {
            get => _isCutted; set { Set(ref _isCutted, value); }
        }
    }
}
