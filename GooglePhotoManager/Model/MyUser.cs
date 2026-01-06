using System.Transactions;

namespace GooglePhotoManager.Model
{
    /// <summary>
    /// Class that holds user data.
    /// </summary>
    internal class MyUser
    {
        #region "Constants about backup folder name"

        internal const string USERS_FOLDER_NAME = "Utenti";

        #endregion

        #region "Private fields"

        private string _name = string.Empty;
        private string _id = string.Empty;
        private string _basePath = string.Empty;

        #endregion

        #region "Properties"

        public string Name { get => _name; }
        public string Id { get => _id; }
        public string BasePath { get => _basePath; }

        #endregion

        #region "Constructor"

        internal MyUser(string name, string id)
        {
            _name = name.ToUpper();
            _id = id.ToUpper();
            _basePath =  $"{Directory.GetCurrentDirectory()}\\{USERS_FOLDER_NAME}\\{_name.ToUpper()}";

            // Create user directory if not existing
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }
        
        #endregion
    }
}
