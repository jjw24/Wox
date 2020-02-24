using Squirrel;
using System;
using Wox.Infrastructure;
using Wox.Plugin.SharedCommands;

namespace Wox.Core.Configuration
{
    public class Portable : IPortable
    {
        private string applicationName;
        private string exeName;
        private string rootAppDirectory;
        private UpdateManager portabilityUpdater;
        private string roamingDataPath;
        private string portableDataPath;

        public Portable()
        {
            applicationName = Constant.Wox;
            exeName = applicationName + ".exe";
            rootAppDirectory = Constant.RootDirectory;
            portabilityUpdater = new UpdateManager(string.Empty, applicationName, rootAppDirectory);

            roamingDataPath = Constant.RoamingDataPath;
            portableDataPath = Constant.PortableDataPath;
        }

        public void DisablePortableMode()
        {
            try
            {
                MoveUserDataFolder(portableDataPath, roamingDataPath);
                CreateShortcuts();
                CreateUninstallerEntry();

                // always dispose UpdateManager???????????
                // CHANGE TO PRIVATE/INTERNAL METHODS
            }
            catch (Exception e)
            {
                //log and update error message to output above locations where shortcuts may not have been removed
#if DEBUG
                throw;
#else
                throw;// PRODUCTION LOGGING AND CONTINUE
                
#endif
            }
        }

        public void EnablePortableMode()
        {
            try
            {
                MoveUserDataFolder(roamingDataPath, portableDataPath);
                RemoveShortcuts();
                RemoveUninstallerEntry();

                // always dispose UpdateManager???????????
            }
            catch (Exception e)
            {
                //log and update error message to output above locations where shortcuts may not have been removed
#if DEBUG
                throw;
#else
                throw;// PRODUCTION LOGGING AND CONTINUE
                
#endif
            }
        }

        public bool IsPortableModeEnabled()
        {
            throw new NotImplementedException();
        }

        public void RemoveShortcuts()
        {
            portabilityUpdater.RemoveShortcutsForExecutable(exeName, ShortcutLocation.StartMenu);
            portabilityUpdater.RemoveShortcutsForExecutable(exeName, ShortcutLocation.Desktop);
            portabilityUpdater.RemoveShortcutsForExecutable(exeName, ShortcutLocation.Startup);
        }

        public void RemoveUninstallerEntry()
        {
            portabilityUpdater.RemoveUninstallerRegistryEntry();
        }

        public void MoveUserDataFolder(string fromLocation, string toLocation)
        {
            FilesFolders.Copy(fromLocation, toLocation);
            VerifyUserDataAfterMove(fromLocation, toLocation);
            FilesFolders.RemoveFolder(fromLocation);
        }

        public void VerifyUserDataAfterMove(string fromLocation, string toLocation)
        {
            FilesFolders.VerifyBothFolderFilesEqual(fromLocation, toLocation);
        }

        public void CreateShortcuts()
        {
            portabilityUpdater.CreateShortcutsForExecutable(exeName, ShortcutLocation.StartMenu, false);
            portabilityUpdater.CreateShortcutsForExecutable(exeName, ShortcutLocation.Desktop, false);
            portabilityUpdater.CreateShortcutsForExecutable(exeName, ShortcutLocation.Startup, false);
        }

        public void CreateUninstallerEntry()
        {
            portabilityUpdater.CreateUninstallerRegistryEntry();
        }
    }
}