﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HeboTech.Wiper.Dialogs;
using HeboTech.Wiper.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HeboTech.Wiper
{
    public class MainViewModel : ViewModelBase
    {
        private IFolderOperations folderOperations;
        private IDialogService dialogService;
        private IFolderBrowserDialogService folderBrowserService;
        private ISettings settingsProvider;

        public MainViewModel(
            IFolderOperations folderOperations,
            IDialogService dialogService,
            IFolderBrowserDialogService folderBrowserService,
            ISettings settingsProvider)
        {
            if (folderOperations == null)
                throw new ArgumentNullException(nameof(folderOperations));
            if (dialogService == null)
                throw new ArgumentNullException(nameof(dialogService));
            if (folderBrowserService == null)
                throw new ArgumentNullException(nameof(folderBrowserService));
            if (settingsProvider == null)
                throw new ArgumentNullException(nameof(settingsProvider));

            this.folderOperations = folderOperations;
            this.dialogService = dialogService;
            this.folderBrowserService = folderBrowserService;
            this.settingsProvider = settingsProvider;

            LoadSettings();

            PropertyChanged += MainViewModel_PropertyChanged;

            browseCommand = new RelayCommand(Browse);
            findFoldersCommand = new RelayCommand(async () => { await FindFolders(); });
            deleteCommand = new RelayCommand(async () => { await Delete(); }, CanDeleteExecute);
            saveSettingsCommand = new RelayCommand(SaveSettings);
        }

        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderToDelete)
                || e.PropertyName == nameof(RootFolder)
                || e.PropertyName == nameof(IsRecursive))
            {
                CanDelete = false;
            }
        }

        private void Browse()
        {
            if (folderBrowserService.ShowDialog(RootFolder))
            {
                RootFolder = folderBrowserService.SelectedFolder;
            }
        }

        private async Task FindFolders()
        {
            IEnumerable<string> foldersToDelete = Parse(folderToDelete);
            Folders = await EnumerateFolders(rootFolder, foldersToDelete, isRecursive);
            CanDelete = true;
        }

        private async Task Delete()
        {
            IEnumerable<string> foldersToDelete = Parse(folderToDelete);

            if (dialogService.ShowConfirmDialog(
                string.Format("Do you want to delete folder(s) '{0}' in '{1}'?",
                    string.Join(", ", foldersToDelete.Select(x => x)),
                    rootFolder),
                "Delete folder(s)?"))
            {
                IEnumerable<string> folders = await EnumerateFolders(rootFolder, foldersToDelete, isRecursive);
                int numberOfDeletedFolders = await DeleteFolders(folders);
                dialogService.ShowDialog(string.Format("{0} of {1} folder(s) deleted.", numberOfDeletedFolders, folders.Count()), "Folder(s) deleted");
            }
        }

        private bool CanDeleteExecute()
        {
            return canDelete && Folders.Count() > 0;
        }

        private bool canDelete = false;
        public bool CanDelete
        {
            get { return canDelete; }
            set
            {
                if (canDelete != value)
                {
                    canDelete = value;
                    deleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private IEnumerable<string> Parse(string input)
        {
            if (input == null)
                return new List<string>();
            return input.Split('|');
        }

        private Task<IEnumerable<string>> EnumerateFolders(string path, IEnumerable<string> foldersToDelete, bool recursive)
        {
            return Task.Factory.StartNew(() =>
            {
                List<string> allFolders = new List<string>();
                foreach (string folderToDelete in foldersToDelete)
                    allFolders.AddRange(folderOperations.EnumerateFolders(path, folderToDelete, recursive));
                return allFolders as IEnumerable<string>;
            });
        }

        private Task<int> DeleteFolders(IEnumerable<string> folders)
        {
            return Task.Factory.StartNew(() =>
            {
                return folderOperations.DeleteFolders(folders);
            });
        }

        private bool isRecursive = true;
        public bool IsRecursive
        {
            get { return isRecursive; }
            set
            {
                if (isRecursive != value)
                {
                    isRecursive = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string folderToDelete;
        public string FolderToDelete
        {
            get { return folderToDelete; }
            set
            {
                if (folderToDelete != value)
                {
                    folderToDelete = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string rootFolder;
        public string RootFolder
        {
            get { return rootFolder; }
            set
            {
                if (rootFolder != value)
                {
                    rootFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

        private IEnumerable<string> folders;
        public IEnumerable<string> Folders
        {
            get { return folders; }
            private set
            {
                folders = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand browseCommand;
        public ICommand BrowseCommand { get { return browseCommand; } }

        private RelayCommand findFoldersCommand;
        public ICommand FindFoldersCommand { get { return findFoldersCommand; } }

        private RelayCommand deleteCommand;
        public ICommand DeleteCommand { get { return deleteCommand; } }

        private RelayCommand saveSettingsCommand;
        public ICommand SaveSettingsCommand { get { return saveSettingsCommand; } }

        private void SaveSettings()
        {
            settingsProvider.SetSetting(nameof(FolderToDelete), FolderToDelete);
            settingsProvider.SetSetting(nameof(RootFolder), RootFolder);
            settingsProvider.SetSetting(nameof(IsRecursive), IsRecursive);
            settingsProvider.Save();
        }

        private void LoadSettings()
        {
            FolderToDelete = settingsProvider.GetSetting<string>(nameof(FolderToDelete));
            RootFolder = settingsProvider.GetSetting<string>(nameof(RootFolder));
            IsRecursive = settingsProvider.GetSetting<bool>(nameof(IsRecursive));
        }
    }
}
