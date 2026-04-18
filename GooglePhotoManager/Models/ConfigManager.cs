using System;
using System.IO;
using System.Xml.Linq;

namespace GooglePhotoManager.Models;

public class ConfigManager
{
    private const string CONFIG_FILENAME = "config.xml";
    private const string DEFAULT_BACKUP_DEVICE_MODEL = "Pixel_5";
    private const string DEFAULT_BACKUP_DEVICE_PRODUCT = "redfin";

    private readonly string _configFilePath;
    private string _backupDeviceModel = DEFAULT_BACKUP_DEVICE_MODEL;
    private string _backupDeviceProduct = DEFAULT_BACKUP_DEVICE_PRODUCT;

    public string BackupDeviceModel
    {
        get => _backupDeviceModel;
        set => _backupDeviceModel = value;
    }

    public string BackupDeviceProduct
    {
        get => _backupDeviceProduct;
        set => _backupDeviceProduct = value;
    }

    public string BackupDeviceName => $"{BackupDeviceModel} ({BackupDeviceProduct})";

    public ConfigManager()
    {
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILENAME);
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                XDocument doc = XDocument.Load(_configFilePath);
                XElement? root = doc.Root;

                if (root != null)
                {
                    XElement? backupDevice = root.Element("BackupDevice");
                    if (backupDevice != null)
                    {
                        string? model = backupDevice.Element("Model")?.Value;
                        string? product = backupDevice.Element("Product")?.Value;

                        if (!string.IsNullOrWhiteSpace(model))
                            _backupDeviceModel = model;

                        if (!string.IsNullOrWhiteSpace(product))
                            _backupDeviceProduct = product;
                    }
                }
            }
            else
            {
                Save();
            }
        }
        catch
        {
            _backupDeviceModel = DEFAULT_BACKUP_DEVICE_MODEL;
            _backupDeviceProduct = DEFAULT_BACKUP_DEVICE_PRODUCT;
        }
    }

    public void Save()
    {
        try
        {
            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Configuration",
                    new XElement("BackupDevice",
                        new XElement("Model", _backupDeviceModel),
                        new XElement("Product", _backupDeviceProduct)
                    )
                )
            );

            doc.Save(_configFilePath);
        }
        catch
        {
            // Ignora errori di salvataggio
        }
    }

    public void SetBackupDevice(string model, string product)
    {
        _backupDeviceModel = model;
        _backupDeviceProduct = product;
        Save();
    }

    public bool IsBackupDevice(string model, string product)
    {
        return model.Equals(_backupDeviceModel, StringComparison.OrdinalIgnoreCase) &&
               product.Equals(_backupDeviceProduct, StringComparison.OrdinalIgnoreCase);
    }
}
