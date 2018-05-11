﻿using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Knowte.Data.Entities;
using Knowte.Services.App;
using Knowte.Services.Entities;
using Knowte.PluginBase.Collection.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Knowte.Core.IO;
using Digimezzo.Foundation.Core.Settings;
using Knowte.Core.Utils;

namespace Knowte.Services.Collection
{
    public class CollectionService : ICollectionService
    {
        private IAppService appService;

        // ProviderName doesn't matter here (it won't appear in the list of available services)
        public string ProviderName => string.Empty;

        private CollectionProviderImporter importer;

        private string selectedNotebookId;

        public event CollectionChangedEventHandler CollectionAdded = delegate { };
        public event CollectionChangedEventHandler CollectionEdited = delegate { };
        public event CollectionChangedEventHandler CollectionDeleted = delegate { };
        public event CollectionChangedEventHandler ActiveCollectionChanged = delegate { };

        public event NotebookChangedEventHandler NotebookAdded = delegate { };
        public event NotebookChangedEventHandler NotebookEdited = delegate { };
        public event NotebookChangedEventHandler NotebookDeleted = delegate { };
        public event NotebookSelectionChangedEventHandler NotebookSelectionChanged = delegate { };

        public event NoteChangedEventHandler NoteAdded = delegate { };
        public event NoteChangedEventHandler NoteDeleted = delegate { };
        public event NoteChangedEventHandler NoteMarked = delegate { };
        public event NoteChangedEventHandler NoteUnmarked = delegate { };

        public CollectionService(IAppService appService)
        {
            this.appService = appService;

            string pluginsFolder = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.PluginsDirectory);

            // If the Plugins folder doesn't exist, create it.
            if (!Directory.Exists(pluginsFolder))
            {
                Directory.CreateDirectory(Path.Combine(pluginsFolder));
            }

            this.importer = new CollectionProviderImporter(pluginsFolder);
            this.importer.DoImport();
        }

        public async Task<bool> ActivateCollectionAsync(CollectionViewModel collection)
        {
            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return false;
            }

            if (string.IsNullOrEmpty(collection.Id))
            {
                LogClient.Error($"{nameof(collection.Id)} is null or empty");
                return false;
            }

            this.appService.IsBusy = true;

            bool activateSuccess = false;

            try
            {
                await this.importer.GetProvider().ActivateCollectionAsync(collection.Id);
                activateSuccess = true;
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(ActivateCollectionAsync)} failed. Exception: {ex.Message}");
            }

            if (!activateSuccess)
            {
                LogClient.Error($"{nameof(ActivateCollectionAsync)} failed. {nameof(collection.Id)}={collection.Id}");
                this.appService.IsBusy = false;

                return false;
            }

            this.ActiveCollectionChanged(this, new CollectionChangedEventArgs(collection.Id));
            LogClient.Info($"{nameof(ActivateCollectionAsync)} successful. {nameof(collection.Id)}={collection.Id}");
            this.appService.IsBusy = false;

            return true;
        }

        public async Task<ChangeCollectionResult> AddCollectionAsync(string title, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                LogClient.Error($"{nameof(title)} is empty");
                return ChangeCollectionResult.Invalid;
            }

            this.appService.IsBusy = true;

            string existingCollectionId = string.Empty;

            try
            {
                existingCollectionId = await this.importer.GetProvider().GetCollectionIdAsync(title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(AddCollectionAsync)} failed. Exception: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(existingCollectionId))
            {
                LogClient.Error($"There is already a collection with the title '{title}'");
                this.appService.IsBusy = false;

                return ChangeCollectionResult.Duplicate;
            }

            string collectionId = string.Empty;

            try
            {
                collectionId = await this.importer.GetProvider().AddCollectionAsync(title, isActive);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(AddCollectionAsync)} failed. Exception: {ex.Message}");
            }

            if (string.IsNullOrEmpty(collectionId))
            {
                LogClient.Error($"{nameof(AddCollectionAsync)} failed. {nameof(collectionId)} is empty");
                this.appService.IsBusy = false;

                return ChangeCollectionResult.Error;
            }

            this.CollectionAdded(this, new CollectionChangedEventArgs(collectionId));
            LogClient.Info($"{nameof(AddCollectionAsync)} successful. {nameof(title)}={title}");

            if (isActive)
            {
                this.ActiveCollectionChanged(this, new CollectionChangedEventArgs(collectionId));
                LogClient.Info($"Collection activated: {nameof(title)}={title}");
            }

            this.appService.IsBusy = false;

            return ChangeCollectionResult.Ok;
        }

        public async Task<bool> DeleteCollectionAsync(CollectionViewModel collection)
        {
            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return false;
            }

            if (string.IsNullOrEmpty(collection.Id))
            {
                LogClient.Error($"{nameof(collection.Id)} is null or empty");
                return false;
            }

            this.appService.IsBusy = true;

            bool deleteSuccess = false;

            try
            {
                await this.importer.GetProvider().DeleteCollectionAsync(collection.Id);
                deleteSuccess = true;
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(DeleteCollectionAsync)} failed. Exception: {ex.Message}");
            }

            if (!deleteSuccess)
            {
                LogClient.Error($"{nameof(DeleteCollectionAsync)} failed. {nameof(collection.Id)}={collection.Id}");
                this.appService.IsBusy = false;

                return false;
            }

            this.CollectionDeleted(this, new CollectionChangedEventArgs(collection.Id));
            LogClient.Info($"{nameof(DeleteCollectionAsync)} successful. {nameof(collection.Id)}={collection.Id}");
            this.appService.IsBusy = false;

            return true;
        }

        public async Task<ChangeCollectionResult> EditCollectionAsync(CollectionViewModel collection, string title)
        {
            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return ChangeCollectionResult.Invalid;
            }

            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return ChangeCollectionResult.Invalid;
            }

            if (string.IsNullOrEmpty(collection.Id))
            {
                LogClient.Error($"{nameof(collection.Id)} is null or empty");
                return ChangeCollectionResult.Invalid;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                LogClient.Error($"{nameof(title)} is empty");
                return ChangeCollectionResult.Invalid;
            }

            this.appService.IsBusy = true;

            string existingCollectionId = string.Empty;

            try
            {
                existingCollectionId = await this.importer.GetProvider().GetCollectionIdAsync(title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(EditCollectionAsync)} failed. Exception: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(existingCollectionId))
            {
                LogClient.Error($"Collection with {nameof(title)}={title} already exists");
                this.appService.IsBusy = false;

                return ChangeCollectionResult.Duplicate;
            }

            bool editSuccess = false;

            try
            {
                await this.importer.GetProvider().EditCollectionAsync(collection.Id, title);
                editSuccess = true;
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(EditCollectionAsync)} failed. Exception: {ex.Message}");
            }

            if (!editSuccess)
            {
                LogClient.Error($"{nameof(EditCollectionAsync)} failed. {nameof(collection.Id)}={collection.Id}, {nameof(title)}={title}");
                this.appService.IsBusy = false;

                return ChangeCollectionResult.Error;
            }

            this.CollectionEdited(this, new CollectionChangedEventArgs(collection.Id));
            LogClient.Info($"Edit success. {nameof(collection.Id)}={collection.Id}, {nameof(title)}={title}");
            this.appService.IsBusy = false;

            return ChangeCollectionResult.Ok;
        }

        public async Task<List<CollectionViewModel>> GetCollectionsAsync()
        {
            this.appService.IsBusy = true;

            List<ICollection> collections = null;

            try
            {
                collections = await this.importer.GetProvider().GetCollectionsAsync();
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(GetCollectionsAsync)} failed. Exception: {ex.Message}");
            }

            if (collections == null || collections.Count.Equals(0))
            {
                LogClient.Error($"{nameof(collections)} is null or empty");
                this.appService.IsBusy = false;

                return new List<CollectionViewModel>();
            }

            var collectionViewModels = new List<CollectionViewModel>();

            foreach (Data.Entities.Collection collection in collections)
            {
                collectionViewModels.Add(new CollectionViewModel(collection.Id, collection.Title, collection.IsActive));
            }

            this.appService.IsBusy = false;

            return collectionViewModels.OrderBy(c => c.Title).ToList();
        }

        public async Task<ChangeNotebookResult> AddNotebookAsync(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                LogClient.Error($"{nameof(title)} is empty");
                return ChangeNotebookResult.Invalid;
            }

            this.appService.IsBusy = true;

            string activeCollectionId = string.Empty;

            try
            {
                activeCollectionId = await this.importer.GetProvider().GetActiveCollectionId();
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(AddNotebookAsync)} failed. Exception: {ex.Message}");
            }

            if (string.IsNullOrEmpty(activeCollectionId))
            {
                LogClient.Error($"There is no active collection");
                this.appService.IsBusy = false;

                return ChangeNotebookResult.Error;
            }

            string existingNotebookId = string.Empty;

            try
            {
                existingNotebookId = await this.importer.GetProvider().GetNotebookIdAsync(title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(AddNotebookAsync)} failed. Exception: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(existingNotebookId))
            {
                LogClient.Error($"There is already a notebook with the title '{title}'");
                this.appService.IsBusy = false;

                return ChangeNotebookResult.Duplicate;
            }

            string notebookId = string.Empty;

            try
            {
                notebookId = await this.importer.GetProvider().AddNotebookAsync(activeCollectionId, title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(AddNotebookAsync)} failed. Exception: {ex.Message}");
            }

            if (string.IsNullOrEmpty(notebookId))
            {
                LogClient.Error($"{nameof(AddNotebookAsync)} failed. {nameof(notebookId)} is empty");
                this.appService.IsBusy = false;

                return ChangeNotebookResult.Error;
            }

            this.NotebookAdded(this, new NotebookChangedEventArgs(notebookId));
            LogClient.Info($"{nameof(AddNotebookAsync)} successful. {nameof(title)}={title}");
            this.appService.IsBusy = false;

            return ChangeNotebookResult.Ok;
        }

        public async Task<ChangeNotebookResult> EditNotebookAsync(NotebookViewModel notebook, string title)
        {
            if (notebook == null)
            {
                LogClient.Error($"{nameof(notebook)} is null");
                return ChangeNotebookResult.Invalid;
            }

            if (notebook == null)
            {
                LogClient.Error($"{nameof(notebook)} is null");
                return ChangeNotebookResult.Invalid;
            }

            if (string.IsNullOrEmpty(notebook.Id))
            {
                LogClient.Error($"{nameof(notebook.Id)} is null or empty");
                return ChangeNotebookResult.Invalid;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                LogClient.Error($"{nameof(title)} is empty");
                return ChangeNotebookResult.Invalid;
            }

            this.appService.IsBusy = true;

            string existingNotebookId = string.Empty;

            try
            {
                existingNotebookId = await this.importer.GetProvider().GetNotebookIdAsync(title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(EditNotebookAsync)} failed. Exception: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(existingNotebookId))
            {
                LogClient.Error($"Notebook with {nameof(title)}={title} already exists");
                this.appService.IsBusy = false;

                return ChangeNotebookResult.Duplicate;
            }

            bool editSuccess = false;

            try
            {
                await this.importer.GetProvider().EditNotebookAsync(notebook.Id, title);
                editSuccess = true;
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(EditNotebookAsync)} failed. Exception: {ex.Message}");
            }

            if (!editSuccess)
            {
                LogClient.Error($"{nameof(EditNotebookAsync)} failed. {nameof(notebook.Id)}={notebook.Id}, {nameof(title)}={title}");
                this.appService.IsBusy = false;

                return ChangeNotebookResult.Error;
            }

            this.NotebookEdited(this, new NotebookChangedEventArgs(notebook.Id));
            LogClient.Info($"{nameof(EditNotebookAsync)} success. {nameof(notebook.Id)}={notebook.Id}, {nameof(title)}={title}");
            this.appService.IsBusy = false;

            return ChangeNotebookResult.Ok;
        }

        public async Task<bool> DeleteNotebookAsync(NotebookViewModel notebook)
        {
            if (notebook == null)
            {
                LogClient.Error($"{nameof(notebook)} is null");
                return false;
            }

            if (string.IsNullOrEmpty(notebook.Id))
            {
                LogClient.Error($"{nameof(notebook.Id)} is null or empty");
                return false;
            }

            this.appService.IsBusy = true;

            bool deleteSuccess = false;

            try
            {
                await this.importer.GetProvider().DeleteNotebookAsync(notebook.Id);
                deleteSuccess = true;
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(DeleteNotebookAsync)} failed. Exception: {ex.Message}");
            }

            if (!deleteSuccess)
            {
                LogClient.Error($"{nameof(DeleteNotebookAsync)} failed. {nameof(notebook.Id)}={notebook.Id}");
                this.appService.IsBusy = false;

                return false;
            }

            this.NotebookDeleted(this, new NotebookChangedEventArgs(notebook.Id));
            LogClient.Info($"{nameof(DeleteNotebookAsync)} successful. {nameof(notebook.Id)}={notebook.Id}");
            this.appService.IsBusy = false;

            return true;
        }

        public async Task<List<NotebookViewModel>> GetNotebooksAsync()
        {
            this.appService.IsBusy = true;

            string activeCollectionId = string.Empty;

            try
            {
                activeCollectionId = await this.importer.GetProvider().GetActiveCollectionId();
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(GetNotebooksAsync)} failed. Exception: {ex.Message}");
            }

            if (string.IsNullOrEmpty(activeCollectionId))
            {
                LogClient.Error($"{nameof(activeCollectionId)} is null or empty");
                this.appService.IsBusy = false;

                return new List<NotebookViewModel>();
            }


            List<INotebook> notebooks = null;

            try
            {
                notebooks = await this.importer.GetProvider().GetNotebooksAsync(activeCollectionId);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(GetNotebooksAsync)} failed. Exception: {ex.Message}");
            }

            var notebookViewModels = new List<NotebookViewModel>();

            // Add the default notebooks
            notebookViewModels.Add(new NotebookViewModel(NotebookViewModel.AllNotesNotebookId, ResourceUtils.GetString("Language_All_Notes")));
            notebookViewModels.Add(new NotebookViewModel(NotebookViewModel.UnfiledNotesNotebookId, ResourceUtils.GetString("Language_Unfiled_Notes")));

            // If found, add the user's notebooks
            if (notebooks != null && notebooks.Count > 0)
            {
                var userNotebookViewModels = new List<NotebookViewModel>();

                foreach (Notebook notebook in notebooks)
                {
                    userNotebookViewModels.Add(new NotebookViewModel(notebook.Id, notebook.Title));
                }

                notebookViewModels.AddRange(userNotebookViewModels.OrderBy(n => n.Title).ToList());
            }

            this.appService.IsBusy = false;

            return notebookViewModels;
        }

        public async Task<bool> HasActiveCollection()
        {
            string activeCollectionId = string.Empty;

            try
            {
                activeCollectionId = await this.importer.GetProvider().GetActiveCollectionId();
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(HasActiveCollection)} failed. Exception: {ex.Message}");
            }

            return !string.IsNullOrEmpty(activeCollectionId);
        }

        public async Task<bool> AddNoteAsync(string proposedTitle)
        {
            this.appService.IsBusy = true;

            string noteId = string.Empty;
            string uniqueNoteTitle = string.Empty;

            try
            {
                uniqueNoteTitle = await this.GetUniqueNewNoteTitleAsync(proposedTitle);
                string notebookId = selectedNotebookId;

                if (selectedNotebookId.Equals(NotebookViewModel.AllNotesNotebookId) ||
                    selectedNotebookId.Equals(NotebookViewModel.UnfiledNotesNotebookId))
                {
                    notebookId = string.Empty;
                }

                noteId = await this.importer.GetProvider().AddNoteAsync(notebookId, uniqueNoteTitle);
            }
            catch (Exception ex)
            {
                LogClient.Error($"New note creation failed. Exception: {ex.Message}");
            }

            if (string.IsNullOrEmpty(noteId))
            {
                LogClient.Error($"{nameof(AddNoteAsync)} failed. {nameof(noteId)} is empty");
                this.appService.IsBusy = false;

                return false;
            }

            this.NoteAdded(this, new NoteChangedEventArgs(noteId));
            LogClient.Info($"{nameof(AddNoteAsync)} successful. {nameof(uniqueNoteTitle)}={uniqueNoteTitle}");
            this.appService.IsBusy = false;

            return true;
        }

        public void OnNotebookSelectionChanged(string notebookId, string notebookTitle)
        {
            this.selectedNotebookId = notebookId;
            this.NotebookSelectionChanged(this, new NotebookSelectionChangedEventArgs(notebookId, notebookTitle));
        }

        private async Task<string> GetUniqueNewNoteTitleAsync(string proposedTitle)
        {
            int counter = 1;

            string uniqueTitle = $"{proposedTitle} {counter.ToString()}";
            List<string> allNoteTitles = await this.importer.GetProvider().GetAllNoteTitlesAsync();
            List<string> similarNoteTitles = allNoteTitles.Where(t => t.StartsWith(proposedTitle)).OrderBy(t => t).Select(t => t).ToList();

            while (similarNoteTitles.Contains(uniqueTitle))
            {
                counter++;
                uniqueTitle = $"{proposedTitle} {counter.ToString()}";
            }

            return uniqueTitle;
        }

        public async Task<List<NoteViewModel>> GetNotesAsync(bool sortByModificationDate)
        {
            this.appService.IsBusy = true;

            List<INote> notes = null;

            try
            {
                switch (this.selectedNotebookId)
                {
                    case null:
                    case "":
                        notes = new List<INote>();
                        break;
                    case NotebookViewModel.AllNotesNotebookId:
                        notes = await this.importer.GetProvider().GetAllNotesAsync();
                        break;
                    case NotebookViewModel.UnfiledNotesNotebookId:
                        notes = await this.importer.GetProvider().GetUnfiledNotesAsync();
                        break;
                    default:
                        notes = await this.importer.GetProvider().GetNotesAsync(this.selectedNotebookId);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(GetNotesAsync)} failed. Exception: {ex.Message}");
            }

            if (notes == null || notes.Count.Equals(0))
            {
                LogClient.Error($"{nameof(notes)} is null or empty");
                this.appService.IsBusy = false;

                return new List<NoteViewModel>();
            }

            var noteViewModels = new List<NoteViewModel>();

            foreach (Note note in notes)
            {
                noteViewModels.Add(new NoteViewModel(note.Id, note.Title, note.ModificationDate));
            }

            this.appService.IsBusy = false;

            if (sortByModificationDate)
            {
                return noteViewModels.OrderBy(n => n.ModificationDate).ToList();
            }

            return noteViewModels.OrderBy(n => n.Title).ToList();
        }

        public async Task<bool> DeleteNoteAsync(NoteViewModel note)
        {
            if (note == null)
            {
                LogClient.Error($"{nameof(note)} is null");
                return false;
            }

            if (string.IsNullOrEmpty(note.Id))
            {
                LogClient.Error($"{nameof(note.Id)} is null");
                return false;
            }

            try
            {
                await this.importer.GetProvider().DeleteNoteAsync(note.Id);
            }
            catch (Exception ex)
            {
                LogClient.Error($"{nameof(DeleteNoteAsync)} failed. Exception: {ex.Message}");
                return false;
            }

            this.NoteDeleted(this, new NoteChangedEventArgs(note.Id));

            return true;
        }

        public async Task<NotesCount> GetNotesCountAsync()
        {
            var noteCount = new NotesCount();

            // First, get all the notes
            List<INote> notes = await this.importer.GetProvider().GetAllNotesAsync();

            await Task.Run(() =>
            {
                foreach (INote note in notes)
                {
                    // All notes
                    noteCount.AllNotesCount++;

                    // Today
                    if (DateUtils.CountDays(new DateTime(note.ModificationDate), DateTime.Now) == 0)
                    {
                        noteCount.TodayNotesCount++;
                    }

                    // Yesterday
                    if (DateUtils.CountDays(new DateTime(note.ModificationDate), DateTime.Now.AddDays(-1)) == 0)
                    {
                        noteCount.YesterdayNotesCount++;
                    }

                    // This week
                    if (DateUtils.CountDays(new DateTime(note.ModificationDate), DateTime.Now) <= (int)DateTime.Now.DayOfWeek)
                    {
                        noteCount.ThisWeekNotesCount++;
                    }

                    // Marked
                    if (note.Flagged == 1)
                    {
                        noteCount.MarkedNotesCount++;
                    }
                }
            });

            return noteCount;
        }
    }
}
