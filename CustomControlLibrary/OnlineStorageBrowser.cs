using Microsoft.Win32;
using StorageLib.CloudStorage.Api;
using StorageLib.CloudStorage.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static StorageLib.CloudStorage.Implementation.Resource;

namespace CustomControlLibrary
{
    [TemplatePart(Name = "Part_DataGrid", Type = typeof(DataGrid))]
    [TemplatePart(Name = "Part_TreeView", Type = typeof(TreeView))]
    public class OnlineStorageBrowser : Control
    {
        #region Icon
        public double IconSize
        {
            get { return (double)GetValue(IconSizeProperty); }
            set { SetValue(IconSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register("IconSize", typeof(double), typeof(OnlineStorageBrowser), new PropertyMetadata(20.0));
        #endregion

        #region StorageExplorer
        public IStorage Storage
        {
            get { return (IStorage)GetValue(StorageExplorerProperty); }
            set { SetValue(StorageExplorerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StorageExplorer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StorageExplorerProperty =
            DependencyProperty.Register("StorageExplorer", typeof(IStorage), typeof(OnlineStorageBrowser), new PropertyMetadata(null, OnProviderChanged));

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

        static OnlineStorageBrowser()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OnlineStorageBrowser), new FrameworkPropertyMetadata(typeof(OnlineStorageBrowser)));
        }

        public OnlineStorageBrowser()
        {
            Unloaded += OnlineStorageBrowserOnUnloaded;


            //InputBindings.Add(new KeyBinding(ApplicationCommands.Paste, Key.V, ModifierKeys.Control));
            //CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, CanEx) );
            CommandManager.AddPreviewCanExecuteHandler(this, OnPreviewCanExecuted);
            CommandManager.AddPreviewExecutedHandler(this, OnPreviewExecuted);
        }


        private void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            
        }

        private void OnPreviewCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
        }

        private void OnlineStorageBrowserOnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnlineStorageBrowserOnUnloaded;

            FinalizeUI();
            Storage = null;
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
                    CommandManager.RemovePreviewCanExecuteHandler(_elementTreeView, OnTreeViewPreviewCanExecuted);
                    CommandManager.RemovePreviewExecutedHandler(_elementTreeView, OnTreeViewPreviewExecuted);
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
                CommandManager.AddPreviewCanExecuteHandler(_elementTreeView, OnTreeViewPreviewCanExecuted);
                CommandManager.AddPreviewExecutedHandler(_elementTreeView, OnTreeViewPreviewExecuted);
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
                    _elementDataGrid.SelectionChanged -= ElementDataGridOnSelectionChanged;
                    _elementDataGrid.InputBindings.Clear();
                    CommandManager.RemovePreviewExecutedHandler(_elementDataGrid, OnDataGridPreviewExecuted);
                    CommandManager.RemovePreviewCanExecuteHandler(_elementDataGrid, OnDataGridPreviewCanExecuted);
                    _elementDataGrid = null;
                }

                if (value == null)
                {
                    return;
                }
                _elementDataGrid = value;
                _elementDataGrid.SelectionChanged += ElementDataGridOnSelectionChanged;
                _elementDataGrid.InputBindings.Add(new KeyBinding(ApplicationCommands.Cut, Key.X, ModifierKeys.Control));
                _elementDataGrid.InputBindings.Add(new KeyBinding(ApplicationCommands.Paste, Key.V, ModifierKeys.Control));
                _elementDataGrid.InputBindings.Add(new KeyBinding(ApplicationCommands.Open, Key.O, ModifierKeys.Control));
                CommandManager.AddPreviewCanExecuteHandler(_elementDataGrid, OnDataGridPreviewCanExecuted);
                CommandManager.AddPreviewExecutedHandler(_elementDataGrid, OnDataGridPreviewExecuted);
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
                    canExecuted: (resource, parameter) => {
                        return Storage!=null && ResourceHelper.IsAlive(resource) && resource.Parent != null
                        && Storage.IsOperationSupported(resource.IsFolder ? Operations.DeleteFolder : Operations.DeleteFile);
                    },
                    execute: (resource, parameter) =>
                    {
                        if (MessageBox.Show($"Deleting resource: {resource.Name}?", "Delete resource", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            Storage?.Delete(resource);
                        }
                    }
                ));

            _commandsHelper.RegisterCommand(ApplicationCommands.Copy,
                new CommandDescription(
                    canExecuted: (resource, parameter) => { return Storage != null && ResourceHelper.IsAlive(resource) 
                        && Storage.IsOperationSupported(resource.IsFolder? Operations.CopyFolder: Operations.CopyFile)
                        && resource.Parent != null; },
                    execute: (resource, parameter) => { _commandsHelper.PutBuffer(resource); }
                ));

            _commandsHelper.RegisterCommand(ApplicationCommands.Cut,
                new CommandDescription(
                    canExecuted: (resource, parameter) =>
                    {
                        return Storage != null && ResourceHelper.IsAlive(resource)
                        && Storage.IsOperationSupported(resource.IsFolder ? Operations.CutFolder : Operations.CutFile)
                        && resource.Parent != null;
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
                canExecuted:  (resource, parameter)=>{ 
                    return ResourceHelper.IsAliveFolder(resource); 
                },
                execute: async (resource, parameter)=>{
                    //1. get file
                    //2. send to content to the storage
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
        }

        private void OnTreeViewPreviewCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if(!_commandsHelper.IsRegisteredCommand(e.Command))
            {
                return;
            }
            var resource = GetInputResource(sender, e.Parameter as string);
            e.CanExecute = _commandsHelper.CanExecuted(e.Command, resource, e.Parameter);
            e.Handled = true;
        }

        private void OnTreeViewPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!_commandsHelper.IsRegisteredCommand(e.Command))
            {
                return;
            }

            var resource = GetInputResource(sender, e.Parameter as string);
            _commandsHelper.Execute(e.Command, resource, e.Parameter);
            e.Handled = true;
        }

        private void OnDataGridPreviewCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!_commandsHelper.IsRegisteredCommand(e.Command))
            {
                return;
            }

            var resource = GetInputResource(sender, e.Parameter as string);
            e.CanExecute = _commandsHelper.CanExecuted(e.Command, resource, e.Parameter );
            e.Handled = true;
        }

        private void OnDataGridPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!_commandsHelper.IsRegisteredCommand(e.Command))
            {
                return;
            }

            var resource = GetInputResource(sender, e.Parameter as string);
            _commandsHelper.Execute(e.Command, resource, e.Parameter );
            e.Handled = true;
        }

        private IResource GetInputResource(object sender, string parameter)
        {
            var treeview = sender as TreeView;
            if (treeview != null && (parameter == null || parameter == "tree"))
            {
                return treeview.SelectedItem as IResource;
            }
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                if (parameter == null || parameter == "outCell")
                {
                    var selected = dataGrid.SelectedItem as IResource;
                    return selected != null ? selected.Parent : _elementTreeView.SelectedItem as IResource;
                }
                return dataGrid.SelectedItem as IResource;
            }

            return null;
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

        private void ElementDataGridOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 0)
            //{
            //    return;
            //}

            //if (e.AddedItems[0] is FolderResourceViewModel folder)
            //{
            //    folder.IsSelected = true;
            //}
            //else if (e.AddedItems[0] is FileResourceViewModel file)
            //{
            //    file.IsSelected = true;
            //}
        }

        private void AwareIsAnyResourceSelected()
        {
            var hasSelectedResource = _elementTreeView.SelectedItem != null;
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
            if(Storage.Resources.Count == 0)
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
        }

        private void ReleaseStorage(IStorage storage)
        {
            storage.Resources.CollectionChanged -= OnStorageResourcesChanged;
            if (_elementTreeView.SelectedItem is FolderResourceViewModel folder)
            {
                folder.IsSelected = false;
            }
        }

        private class CommandDescription
        {
            public CommandDescription(Func<IResource, object, bool> canExecuted, Action<IResource, object> execute)
            {
                CanExecuted = canExecuted;
                Execute = execute;
            }

            public Func<IResource, object, bool> CanExecuted { get;  }
            public Action<IResource, object> Execute { get; }
        }

        private class CommandsHelper
        {
            private IResource _buffer;

            private Dictionary<ICommand, CommandDescription> _commands = new();
            public void RegisterCommand(ICommand command, CommandDescription description)
            {
                _commands[command] = description;
            }
            
            public void UnregisterCommand(ICommand command)
            {
                _commands.Remove(command);
            }
            
            public bool CanExecuted (ICommand command, IResource resource, object parameter)
            {
                return _commands.ContainsKey(command) && _commands[command].CanExecuted.Invoke(resource, parameter);
            } 
            
            public void Execute (ICommand command, IResource resource, object parameter)
            {
                _commands[command].Execute.Invoke(resource, parameter);
            }

            public bool IsRegisteredCommand(ICommand command) => _commands.ContainsKey(command);

            internal void ClearBuffer()
            {
                if(_buffer is IResourceViewModel view)
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

    }
}
