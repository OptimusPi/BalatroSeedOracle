using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace Oracle.Views.Modals
{
    public partial class WordListsModal : UserControl
    {
        private TextBox? _fileNameBox;
        private TextBox? _textEditor;
        private TextBlock? _statusText;
        private ListBox? _fileList;
        private TextBlock? _directoryPath;
        private Button? _openButton;
        private Button? _deleteButton;
        private StackPanel? _editorPanel;
        private StackPanel? _browsePanel;
        private Button? _editorTab;
        private Button? _browseTab;
        private Polygon? _tabTriangle;
        
        private string _wordListsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "WordLists");
        private string? _currentFile;
        private bool _hasUnsavedChanges = false;
        
        public WordListsModal()
        {
            InitializeComponent();
            EnsureDirectoryExists();
            _ = LoadFileListAsync();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _fileNameBox = this.FindControl<TextBox>("FileNameBox");
            _textEditor = this.FindControl<TextBox>("TextEditor");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _fileList = this.FindControl<ListBox>("FileList");
            _directoryPath = this.FindControl<TextBlock>("DirectoryPath");
            _openButton = this.FindControl<Button>("OpenButton");
            _deleteButton = this.FindControl<Button>("DeleteButton");
            _editorPanel = this.FindControl<StackPanel>("EditorPanel");
            _browsePanel = this.FindControl<StackPanel>("BrowsePanel");
            _editorTab = this.FindControl<Button>("EditorTab");
            _browseTab = this.FindControl<Button>("BrowseTab");
            _tabTriangle = this.FindControl<Polygon>("TabTriangle");
            
            if (_directoryPath != null)
                _directoryPath.Text = _wordListsPath;
                
            if (_textEditor != null)
            {
                _textEditor.TextChanged += (s, e) => _hasUnsavedChanges = true;
            }
        }
        
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_wordListsPath))
            {
                Directory.CreateDirectory(_wordListsPath);
            }
        }
        
        private async Task LoadFileListAsync()
        {
            await Task.Run(() =>
            {
                var files = Directory.GetFiles(_wordListsPath, "*.txt")
                    .Select(f => new FileItem
                    {
                        Name = System.IO.Path.GetFileName(f),
                        Path = f,
                        Size = FormatFileSize(new FileInfo(f).Length)
                    })
                    .OrderBy(f => f.Name)
                    .ToList();
                    
                Dispatcher.UIThread.Post(() =>
                {
                    if (_fileList != null)
                        _fileList.ItemsSource = files;
                });
            });
        }
        
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
        
        private void OnEditorTabClick(object? sender, RoutedEventArgs e)
        {
            ShowEditorPanel();
        }
        
        private void OnBrowseTabClick(object? sender, RoutedEventArgs e)
        {
            ShowBrowsePanel();
        }
        
        private void ShowEditorPanel()
        {
            if (_editorPanel != null) _editorPanel.IsVisible = true;
            if (_browsePanel != null) _browsePanel.IsVisible = false;
            if (_editorTab != null) _editorTab.Classes.Add("active");
            if (_browseTab != null) _browseTab.Classes.Remove("active");
        }
        
        private void ShowBrowsePanel()
        {
            if (_editorPanel != null) _editorPanel.IsVisible = false;
            if (_browsePanel != null) _browsePanel.IsVisible = true;
            if (_editorTab != null) _editorTab.Classes.Remove("active");
            if (_browseTab != null) _browseTab.Classes.Add("active");
            _ = LoadFileListAsync();
        }
        
        private async void OnNewClick(object? sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = await ConfirmUnsavedChanges();
                if (!result) return;
            }
            
            if (_fileNameBox != null) _fileNameBox.Text = "";
            if (_textEditor != null) _textEditor.Text = "";
            _currentFile = null;
            _hasUnsavedChanges = false;
            UpdateStatus("New file created", Brushes.LightBlue);
        }
        
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (_fileNameBox == null || _textEditor == null) return;
            
            var fileName = _fileNameBox.Text?.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                UpdateStatus("Please enter a filename", Brushes.Orange);
                return;
            }
            
            if (!fileName.EndsWith(".txt"))
                fileName += ".txt";
                
            var filePath = System.IO.Path.Combine(_wordListsPath, fileName);
            
            try
            {
                await File.WriteAllTextAsync(filePath, _textEditor.Text ?? "");
                _currentFile = filePath;
                _hasUnsavedChanges = false;
                UpdateStatus($"Saved: {fileName}", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Save failed: {ex.Message}", Brushes.Red);
            }
        }
        
        private async void OnLoadClick(object? sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = await ConfirmUnsavedChanges();
                if (!result) return;
            }
            
            ShowBrowsePanel();
        }
        
        private void OnFileSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_fileList?.SelectedItem is FileItem file)
            {
                if (_openButton != null) _openButton.IsEnabled = true;
                if (_deleteButton != null) _deleteButton.IsEnabled = true;
            }
            else
            {
                if (_openButton != null) _openButton.IsEnabled = false;
                if (_deleteButton != null) _deleteButton.IsEnabled = false;
            }
        }
        
        private async void OnOpenClick(object? sender, RoutedEventArgs e)
        {
            if (_fileList?.SelectedItem is FileItem file)
            {
                await LoadFile(file.Path);
                ShowEditorPanel();
            }
        }
        
        private async Task LoadFile(string filePath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                if (_textEditor != null) _textEditor.Text = content;
                if (_fileNameBox != null) _fileNameBox.Text = System.IO.Path.GetFileName(filePath);
                _currentFile = filePath;
                _hasUnsavedChanges = false;
                UpdateStatus($"Loaded: {System.IO.Path.GetFileName(filePath)}", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Load failed: {ex.Message}", Brushes.Red);
            }
        }
        
        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (_fileList?.SelectedItem is FileItem file)
            {
                var confirm = await ConfirmDelete(file.Name);
                if (confirm)
                {
                    try
                    {
                        File.Delete(file.Path);
                        UpdateStatus($"Deleted: {file.Name}", Brushes.Orange);
                        await LoadFileListAsync();
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Delete failed: {ex.Message}", Brushes.Red);
                    }
                }
            }
        }
        
        private async void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            await LoadFileListAsync();
            UpdateStatus("File list refreshed", Brushes.LightBlue);
        }
        
        private void UpdateStatus(string message, IBrush color)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
                _statusText.Foreground = color;
            }
        }
        
        private Task<bool> ConfirmUnsavedChanges()
        {
            // For now, just return true. In a real app, show a confirmation dialog
            return Task.FromResult(true);
        }
        
        private Task<bool> ConfirmDelete(string fileName)
        {
            // For now, just return true. In a real app, show a confirmation dialog
            return Task.FromResult(true);
        }
        
        private class FileItem
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public string Size { get; set; } = "";
        }
    }
}