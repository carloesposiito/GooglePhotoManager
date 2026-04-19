using System.IO;

namespace GooglePhotoManager.Models;

public class MyUser
{
    #region Campi privati

    public const string USERS_FOLDER_NAME = "Utenti";

    #endregion

    #region Proprietà

    // Nome dell'utente (maiuscolo)
    public string Name { get; }

    // ID dell'utente nel sistema Android
    public string Id { get; }

    // Percorso locale della cartella dedicata all'utente
    public string BasePath { get; }

    #endregion

    #region Metodi

    // Crea un nuovo utente e la relativa cartella locale
    public MyUser(string name, string id)
    {
        Name = name.ToUpper();
        Id = id.ToUpper();
        BasePath = Path.Combine(Directory.GetCurrentDirectory(), USERS_FOLDER_NAME, Name);

        if (!Directory.Exists(BasePath))
            Directory.CreateDirectory(BasePath);
    }

    #endregion
}
