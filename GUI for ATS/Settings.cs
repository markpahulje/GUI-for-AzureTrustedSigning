using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace GUI_for_ATS
{
    [Serializable]
    public class Settings
    {
        public string AzureClientId { get; set; }
        public string AzureClientSecret { get; set; }
        public string AzureTenantId { get; set; }
        public string SigntoolPath { get; set; }
        public string AzureCodeSigningDlibDllPath { get; set; }
        public string TimeStampServer { get; set; }
        public string MetadataJsonPath { get; set; }
        public string LastFilePath { get; set; }
        public string LastPath { get; set; }

        public Settings()
        {
            AzureClientId = string.Empty;
            AzureClientSecret = string.Empty;
            AzureTenantId = string.Empty;
            SigntoolPath = string.Empty;
            AzureCodeSigningDlibDllPath = string.Empty;
            TimeStampServer = "http://timestamp.acs.microsoft.com";
            MetadataJsonPath = string.Empty;
            LastFilePath = string.Empty;
            LastPath = string.Empty;
        }

        public void Save()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                string tempFilePath = filePath + ".tmp";

                // First serialize to temp file
                SerializeToXml(this, tempFilePath);

                // Then replace the original file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempFilePath, filePath);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error saving settings: {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }

        public static Settings LoadSettings()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                if (File.Exists(filePath))
                {
                    return DeserializeFromXml<Settings>(filePath) ?? CreateDefaultSettings();
                }
                return CreateDefaultSettings();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error loading settings: {ex.Message}");
                return CreateDefaultSettings();
            }
        }

        public static Settings CreateDefaultSettings()
        {
            var settings = new Settings();
            try
            {
                settings.Save();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error creating default settings: {ex.Message}");
            }
            return settings;
        }

        private static string GetApplicationDataPath()
        {
            try
            {
                string appName = Assembly.GetExecutingAssembly().GetName().Name;
                if (!string.IsNullOrEmpty(appName))
                {
                    appName = appName.Replace("-", "_");
                    string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        appName);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error getting application data path: {ex.Message}");
            }
            return string.Empty;
        }
        /// <summary>
        /// Gets AppData\Local path for this app
        /// </summary>
        /// <returnsC:\Users\{username}\AppData\Local\GUIforAzureTrustedSigning/Setting.xml</returns>
        public static string GetSettingsFilePath()
        {
            string appDataPath = GetApplicationDataPath();
            if (string.IsNullOrEmpty(appDataPath))
            {
                // Fallback to executable directory if appdata fails
                appDataPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            return Path.Combine(appDataPath, "Settings.xml");
        }

        private static void SerializeToXml<T>(T obj, string fileName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (FileStream stream = new FileStream(fileName, FileMode.Create))
                {
                    serializer.Serialize(stream, obj);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error serializing to XML: {ex.Message}");
                throw;
            }
        }

        private static T DeserializeFromXml<T>(string fileName) where T : class
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (FileStream stream = new FileStream(fileName, FileMode.Open))
                {
                    return (T)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error deserializing from XML: {ex.Message}");
                throw;
            }
        }
    }
}