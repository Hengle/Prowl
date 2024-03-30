﻿using Prowl.Runtime;

namespace Prowl.Editor.Utilities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        public enum Location
        {
            ProjectFolder,
            ProjectSettingsFolder,
            PreferencesFolder
        }

        public string FilePath { get; }
        public Location FileLocation { get; }

        public FilePathAttribute(string filePath, Location fileLocation)
        {
            FilePath = filePath;
            FileLocation = fileLocation;
        }
    }

    public abstract class ScriptableSingleton<T> where T : ScriptableSingleton<T>, new()
    {
        private static T instance;

        public static T Instance {
            get {
                if (instance == null)
                {
                    instance = LoadOrCreateInstance();
                }
                return instance;
            }
        }

        public void Save()
        {
            StringTagConverter.WriteToFile(Serializer.Serialize(this), new(GetFilePath()));
        }

        protected string GetFilePath()
        {
            var attribute = Attribute.GetCustomAttribute(GetType(), typeof(FilePathAttribute)) as FilePathAttribute;
            if (attribute != null)
            {
                string directory = string.Empty;
                switch (attribute.FileLocation)
                {
                    case FilePathAttribute.Location.ProjectFolder:
                        directory = Project.ProjectDirectory;
                        break;
                    case FilePathAttribute.Location.ProjectSettingsFolder:
                        directory = Path.Combine(Project.ProjectDirectory, "ProjectSettings");
                        break;
                    case FilePathAttribute.Location.PreferencesFolder:
                        // Persistent across all projects
                        directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Prowl", "Editor");
                        break;
                }
                // Ensure Directory Exists
                Directory.CreateDirectory(directory);
                return Path.Combine(directory, attribute.FilePath);
            }
            return string.Empty;
        }

        private static T LoadOrCreateInstance()
        {
            string filePath = new T().GetFilePath();

            if (File.Exists(filePath))
            {
                return Serializer.Deserialize<T>(StringTagConverter.ReadFromFile(new FileInfo(filePath)));
            }
            else
            {
                var newInstance = new T();
                newInstance.Save();
                return newInstance;
            }
        }
    }
}