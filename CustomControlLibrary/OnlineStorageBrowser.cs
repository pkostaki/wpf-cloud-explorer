using Microsoft.Win32;
using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static StorageLib.CloudStorage.Implementation.Resource;

namespace CustomControlLibrary
{
    [TemplatePart(Name = "Part_DataGrid", Type = typeof(DataGrid))]
    [TemplatePart(Name = "Part_TreeView", Type = typeof(TreeView))]
    public class OnlineStorageBrowser : Control
    {
        #region Icon size
        /// <summary>
        /// Gets and sets icons size.
        /// </summary>
        public double IconSize
        {
            get { return (double)GetValue(IconSizeProperty); }
            set { SetValue(IconSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register("IconSize", typeof(double), typeof(OnlineStorageBrowser), new PropertyMetadata(20.0));
        #endregion

        #region Storage
        /// <summary>
        /// Gets and sets storage.
        /// </summary>
        public IStorage Storage
        {
            get { return (IStorage)GetValue(StorageProperty); }
            set { SetValue(StorageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StorageExplorer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StorageProperty =
            DependencyProperty.Register(nameof(Storage), typeof(IStorage), typeof(OnlineStorageBrowser), new PropertyMetadata(null, OnProviderChanged));

        private static void OnProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as OnlineStorageBrowser;
            if (e.OldValue is IStorage previusStorage)
            {
                instance.ReleaseStorage(previusStorage);
            }

            if (e.NewValue is IStorage storage)
            {
                instance.SetupStorage(storage);
            }
        }


        #endregion

        #region Status
        // Using a DependencyProperty as the backing store for Status.  This enables animation, styling, binding, etc...
        internal static readonly DependencyPropertyKey StatusPropertyKey =
            DependencyProperty.RegisterReadOnly("Status", typeof(string), typeof(OnlineStorageBrowser), new PropertyMetadata("Disconnected"));

        public static readonly DependencyProperty StatusProperty = StatusPropertyKey.DependencyProperty;

        /// <summary>
        /// Status of connected storage.
        /// </summary>
        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
        }




        #endregion




        static OnlineStorageBrowser()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OnlineStorageBrowser), new FrameworkPropertyMetadata(typeof(OnlineStorageBrowser)));
        }

        public OnlineStorageBrowser()
        {
            Unloaded += OnlineStorageBrowserOnUnloaded;
        }

        private void OnlineStorageBrowserOnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnlineStorageBrowserOnUnloaded;

            FinalizeUI();
            Storage = null;
            _commandsHelper.ClearBuffer();
        }

        public override void OnApplyTemplate()
        {
            FinalizeUI();
            InitializeUI();

            base.OnApplyTemplate();
            Focus();
        }

        #region UI
        private bool _uiInited;
        private TreeView _elementTreeView;
        private TreeView ElementTreeView
        {
            get => _elementTreeView;
            set
            {
                if (_elementTreeView != null)
                {
                    _elementTreeView.InputBindings.Clear();
                    CommandManager.RemovePreviewCanExecuteHandler(_elementTreeView, PreviewCanExecuted);
                    CommandManager.RemovePreviewExecutedHandler(_elementTreeView, PreviewExecuted);
                    _elementTreeView.SelectedItemChanged -= ElementTreeViewSelectedItemChanged;
                    _elementTreeView = null;
                }
                if (value == null)
                {
                    return;
                }
                _elementTreeView = value;
                _elementTreeView.SelectedItemChanged += ElementTreeViewSelectedItemChanged;

                _elementTreeView.InputBindings.Add(new KeyBinding(ApplicationCommands.Paste, Key.V, ModifierKeys.Control));
                _elementTreeView.InputBindings.Add(new KeyBinding(ApplicationCommands.Cut, Key.X, ModifierKeys.Control));
                _elementTreeView.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.C, ModifierKeys.Control));
                _elementTreeView.InputBindings.Add(new KeyBinding(ApplicationCommands.Open, Key.O, ModifierKeys.Control));
                _elementTreeView.InputBindings.Add(new KeyBinding(ApplicationCommands.Delete, Key.Delete, ModifierKeys.None));
                CommandManager.AddPreviewCanExecuteHandler(_elementTreeView, PreviewCanExecuted);
                CommandManager.AddPreviewExecutedHandler(_elementTreeView, PreviewExecuted);
            }
        }

        private DataGrid _elementDataGrid;
        private DataGrid ElementDataGrid
        {
            get => _elementDataGrid;
            set
            {
                if (_elementDataGrid != null)
                {
                    _elementDataGrid.InputBindings.Clear();
                    CommandManager.RemovePreviewCanExecuteHandler(_elementDataGrid, PreviewCanExecuted);
                    CommandManager.RemovePreviewExecutedHandler(_elementDataGrid, PreviewExecuted);

                    _elementDataGrid.PreviewMouseDoubleClick -= ElementDataGridOnMouseDoubleClick;
                    _elementDataGrid = null;
                }

                if (value == null)
                {
                    return;
                }
                _elementDataGrid = value;
                _elementDataGrid.InputBindings.Add(new KeyBinding(ApplicationCommands.Cut, Key.X, ModifierKeys.Control));
                _elementDataGrid.InputBindings.Add(new KeyBinding(ApplicationCommands.Paste, Key.V, ModifierKeys.Control));
                _elementDataGrid.InputBindings.Add(new KeyBinding(ApplicationCommands.Open, Key.O, ModifierKeys.Control));
                CommandManager.AddPreviewCanExecuteHandler(_elementDataGrid, PreviewCanExecuted);
                CommandManager.AddPreviewExecutedHandler(_elementDataGrid, PreviewExecuted);

                _elementDataGrid.PreviewMouseDoubleClick += ElementDataGridOnMouseDoubleClick;
            }
        }

        private void ElementDataGridOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var parent = e.OriginalSource as DependencyObject;
            DataGridCell clickedDataCell = null;
            do
            {
                if (parent == null)
                {
                    return;
                }

                if (parent is DataGridCell cell)
                {
                    clickedDataCell = cell;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            } while (parent != null);

            if (clickedDataCell != null)
            {
                if (clickedDataCell.DataContext is FolderResourceViewModel folderResource)
                {
                    folderResource.IsSelected = true;
                }
                else if (clickedDataCell.DataContext is FileResourceViewModel fileResource)
                {
                    Process.Start(new ProcessStartInfo { FileName = fileResource.WebLink, UseShellExecute = true });
                }
                e.Handled = true;
            }
        }

        private void FinalizeUI()
        {
            UnregisterCommands();
            ElementTreeView = null;
            ElementDataGrid = null;

            _uiInited = false;
        }

        private void InitializeUI()
        {
            ElementTreeView = GetTemplateChild("Part_TreeView") as TreeView;
            ElementDataGrid = GetTemplateChild("Part_DataGrid") as DataGrid;
            RegisterCommands();
            _uiInited = true;
        }
        #endregion

        #region Commands

        private readonly CommandsHelper _commandsHelper = new();

        private void RegisterCommands()
        {
            _commandsHelper.RegisterCommand(ApplicationCommands.Delete,
                new CommandDescription(
                    canExecuted: (resource, parameter) =>
                    {
                        return Storage != null && ResourceHelper.HasAliveParent(resource)
                        && Storage.IsOperationSupported(resource.IsFolder ? Operations.DeleteFolder : Operations.DeleteFile);
                    },
                    execute: (resource, parameter) =>
                    {
                        if (MessageBox.Show($"Deleting resource: {resource.Name}?",
                            "Deletion",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            Storage?.Delete(resource);
                        }
                    }
                ));

            _commandsHelper.RegisterCommand(ApplicationCommands.Copy,
                new CommandDescription(
                    canExecuted: (resource, parameter) =>
                    {
                        return Storage != null && ResourceHelper.HasAliveParent(resource) &&
                        Storage.IsOperationSupported(resource.IsFolder ? Operations.CopyFolder : Operations.CopyFile);
                    },
                    execute: (resource, parameter) => { _commandsHelper.PutBuffer(resource); }
                ));

            _commandsHelper.RegisterCommand(ApplicationCommands.Cut,
                new CommandDescription(
                    canExecuted: (resource, parameter) =>
                    {
                        return Storage != null && ResourceHelper.HasAliveParent(resource) &&
                        Storage.IsOperationSupported(resource.IsFolder ? Operations.CutFolder : Operations.CutFile);
                    },
                    execute: (resource, parameter) =>
                    {
                        _commandsHelper.PutBuffer(resource, true);
                    }
                ));

            Func<IResourceViewModel, IResource, IResource> GetPasteCommandTarget = (source, resource) =>
              {
                  if (!ResourceHelper.IsAlive(source) || !ResourceHelper.IsAlive(resource))
                  {
                      return null;
                  }

                  if (resource.IsFolder)
                  {
                      return resource;
                  }

                  if (!ResourceHelper.IsAliveFolder(resource.Parent))
                  {
                      return null;
                  }

                  IResource destinition = null;
                  if (resource.Parent.Id != source.Parent.Id)
                  {
                      destinition = resource.Parent;
                  }
                  else
                  {
                      destinition = source.IsCutted ? null : resource.Parent;
                  }

                  return destinition;
              };

            _commandsHelper.RegisterCommand(ApplicationCommands.Paste,
                new CommandDescription(
                    canExecuted: (resource, parameter) =>
                    {
                        IResourceViewModel source = _commandsHelper.GetBuffer() as IResourceViewModel;
                        return Storage != null && GetPasteCommandTarget(source, resource) != null;
                    },

                    execute: (resource, parameter) =>
                    {
                        IResourceViewModel source = _commandsHelper.GetBuffer() as IResourceViewModel;
                        var target = GetPasteCommandTarget(source, resource);
                        if (source.IsCutted)
                        {
                            Storage?.Move(source, target);
                        }
                        else
                        {
                            Storage?.Copy(source, target);
                        }
                        _commandsHelper.ClearBuffer();
                    })
                );

            _commandsHelper.RegisterCommand(ApplicationCommands.Open, new CommandDescription(
                canExecuted: (resource, parameter) =>
                {
                    return ResourceHelper.IsAliveFolder(resource) && Storage.IsOperationSupported(Operations.UploadFile);
                },
                execute: async (resource, parameter) =>
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    if (openFileDialog.ShowDialog() == true)
                    {
                        using var stream = openFileDialog.OpenFile();
                        // Todo find resonable way to get mime type for file
                        await Storage.Upload(Path.GetFileName(openFileDialog.FileName), resource, stream, "");
                    }
                }
            ));
        }

        private void UnregisterCommands()
        {
            _commandsHelper.UnregisterCommand(ApplicationCommands.Open);
            _commandsHelper.UnregisterCommand(ApplicationCommands.Copy);
            _commandsHelper.UnregisterCommand(ApplicationCommands.Delete);
            _commandsHelper.UnregisterCommand(ApplicationCommands.Paste);
            _commandsHelper.UnregisterCommand(ApplicationCommands.Cut);
        }


        private void PreviewCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!_commandsHelper.IsRegisteredCommand(e.Command))
            {
                return;
            }

            var resource = GetInputResource(sender, e.Command, e.Parameter as string);
            e.CanExecute = _commandsHelper.CanExecuted(e.Command, resource, e.Parameter);
            e.Handled = true;
        }

        private void PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!_commandsHelper.IsRegisteredCommand(e.Command))
            {
                return;
            }

            var resource = GetInputResource(sender, e.Command, e.Parameter as string);
            _commandsHelper.Execute(e.Command, resource, e.Parameter);
            e.Handled = true;
        }

        private IResource GetInputResource(object sender, ICommand command, string parameter)
        {
            if (sender is TreeView treeview && (parameter == null || parameter == "tree"))
            {
                return treeview.SelectedItem as IResource;
            }

            if (sender is not DataGrid dataGrid)
            {
                return null;
            }

            var selected = dataGrid.SelectedItem as IResource;
            // Null means that command activated through keyboard.
            // Cell means that command activated not directly at resource view, but at area of datagrid.
            if (parameter == null || parameter == "outCell")
            {
                if (command == ApplicationCommands.Open || command == ApplicationCommands.Paste)
                {
                    if (selected == null)
                    {
                        return _elementTreeView.SelectedItem as IResource;
                    }

                    return ResourceHelper.IsAliveFolder(selected) ? selected :
                         ResourceHelper.HasAliveParentFolder(selected) ? selected.Parent : null;
                }

                return selected;
            }

            return selected;
        }
        #endregion

        private void ElementTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not Resource resource)
            {
                return;
            }

            resource.Load();
        }

        private void AwareIsAnyResourceSelected()
        {
            var hasSelectedResource = _elementTreeView?.SelectedItem != null;
            if (hasSelectedResource)
            {
                return;
            }

            if (Storage.Resources.Count == 0)
            {
                Storage.Resources.CollectionChanged -= OnStorageResourcesChanged;
                Storage.Resources.CollectionChanged += OnStorageResourcesChanged;
                return;
            }

            TrySelectRootResource();
        }

        private void OnStorageResourcesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Storage.Resources.Count == 0)
            {
                return;
            }

            Storage.Resources.CollectionChanged -= OnStorageResourcesChanged;

            TrySelectRootResource();
        }

        private void TrySelectRootResource()
        {
            var root = Storage.Resources.Count > 0 ? Storage.Resources[0] : null;
            if (root is FolderResourceViewModel folder)
            {
                folder.IsSelected = true;
            }
        }

        private void SetupStorage(IStorage storage)
        {
            AwareIsAnyResourceSelected();
            SetValue(StatusPropertyKey, $"Connected: {storage.StorageName}");
        }

        private void ReleaseStorage(IStorage storage)
        {
            storage.Resources.CollectionChanged -= OnStorageResourcesChanged;
            if (_elementTreeView?.SelectedItem is IResourceViewModel folder)
            {
                folder.IsSelected = false;
            }
            if (_elementDataGrid?.SelectedItem is FolderResourceViewModel resource)
            {
                resource.IsSelected = false;
            }

            SetValue(StatusPropertyKey, "Disconnected");
        }

        #region Commands helpers
        private class CommandDescription
        {
            public CommandDescription(Func<IResource, object, bool> canExecuted, Action<IResource, object> execute)
            {
                CanExecuted = canExecuted;
                Execute = execute;
            }

            public Func<IResource, object, bool> CanExecuted { get; }
            public Action<IResource, object> Execute { get; }
        }

        private class CommandsHelper
        {
            private IResource _buffer;

            private readonly Dictionary<ICommand, CommandDescription> _commands = new();
            public void RegisterCommand(ICommand command, CommandDescription description)
            {
                _commands[command] = description;
            }

            public void UnregisterCommand(ICommand command)
            {
                _commands.Remove(command);
            }

            public bool CanExecuted(ICommand command, IResource resource, object parameter)
            {
                return _commands.ContainsKey(command) && _commands[command].CanExecuted.Invoke(resource, parameter);
            }

            public void Execute(ICommand command, IResource resource, object parameter)
            {
                _commands[command].Execute.Invoke(resource, parameter);
            }

            public bool IsRegisteredCommand(ICommand command) => _commands.ContainsKey(command);

            internal void ClearBuffer()
            {
                if (_buffer is IResourceViewModel view)
                {
                    view.IsCutted = false;
                }

                _buffer = null;
            }

            public void PutBuffer(IResource resource)
            {
                ClearBuffer();
                _buffer = resource;
            }

            public void PutBuffer(IResource resource, bool cut)
            {
                ClearBuffer();
                _buffer = resource;
                (_buffer as IResourceViewModel).IsCutted = cut;
            }

            public IResource GetBuffer()
            {
                return _buffer;
            }
        }

        #endregion

    }
}
