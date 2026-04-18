using System.IO;

namespace GooglePhotoManager.Models;

public class MyUser
{
    public const string USERS_FOLDER_NAME = "Utenti";

    public string Name { get; }
    public string Id { get; }
    public string BasePath { get; }

    public MyUser(string name, string id)
    {
        Name = name.ToUpper();
        Id = id.ToUpper();
        BasePath = Path.Combine(Directory.GetCurrentDirectory(), USERS_FOLDER_NAME, Name);

        if (!Directory.Exists(BasePath))
        {
            Directory.CreateDirectory(BasePath);
        }
    }
}
