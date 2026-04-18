using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using SuperpowersRevit.Models;
using SuperpowersRevit.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace SuperpowersRevit.UI
{
    public partial class DrofusDataWindow : Window
    {
        private readonly DrofusService  _service;
        private readonly Document       _doc;
        private readonly DrofusViewModel _vm = new();

        // Full lists (unfiltered) — filters are applied over these
        private List<DrofusRoom> _allRooms = [];
        private List<DrofusItem> _allItems = [];

        public DrofusDataWindow(DrofusService service, Document doc)
        {
            _service    = service;
            _doc        = doc;
            DataContext = _vm;

            InitializeComponent();

            // Kick off async load after the window is rendered
            Loaded += async (_, _) => await LoadProjectsAsync();
        }

        // ── Project loading ───────────────────────────────────────────────────

        private async Task LoadProjectsAsync()
        {
            SetStatus("Loading projects…");
            try
            {
                List<DrofusProject> projects = await _service.GetProjectsAsync();
                ProjectCombo.ItemsSource = projects;

                if (projects.Count > 0)
                    ProjectCombo.SelectedIndex = 0; // triggers SelectionChanged → LoadProjectDataAsync

                SetStatus(projects.Count == 0 ? "No projects found." : $"{projects.Count} project(s) available.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading projects: {ex.Message}");
            }
        }

        private async void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem is DrofusProject project)
                await LoadProjectDataAsync(project.Id);
        }

        private async void RefreshProjects_Click(object sender, RoutedEventArgs e) =>
            await LoadProjectsAsync();

        private async Task LoadProjectDataAsync(string projectId)
        {
            SetStatus("Loading rooms and items…");
            _allRooms = [];
            _allItems = [];
            _vm.Rooms.Clear();
            _vm.Items.Clear();

            try
            {
                // Fetch rooms and items in parallel
                Task<List<DrofusRoom>> roomsTask  = _service.GetRoomsAsync(projectId);
                Task<List<DrofusItem>> itemsTask  = _service.GetItemsAsync(projectId);
                await Task.WhenAll(roomsTask, itemsTask);

                _allRooms = roomsTask.Result;
                _allItems = itemsTask.Result;

                ApplyRoomFilter();
                ApplyItemFilter();

                SetStatus($"{_allRooms.Count} room(s) · {_allItems.Count} item(s)");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading project data: {ex.Message}");
            }
        }

        // ── Search / filter ───────────────────────────────────────────────────

        private void RoomSearch_TextChanged(object sender, TextChangedEventArgs e) =>
            ApplyRoomFilter();

        private void ItemSearch_TextChanged(object sender, TextChangedEventArgs e) =>
            ApplyItemFilter();

        private void ApplyRoomFilter()
        {
            string q = RoomSearchBox.Text.Trim();
            _vm.Rooms.Clear();

            foreach (DrofusRoom r in _allRooms)
            {
                if (string.IsNullOrEmpty(q) ||
                    r.Number.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.Function.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    _vm.Rooms.Add(r);
                }
            }

            RoomCountText.Text = $"{_vm.Rooms.Count} / {_allRooms.Count}";
        }

        private void ApplyItemFilter()
        {
            string q = ItemSearchBox.Text.Trim();
            _vm.Items.Clear();

            foreach (DrofusItem i in _allItems)
            {
                if (string.IsNullOrEmpty(q) ||
                    i.Number.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    i.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    _vm.Items.Add(i);
                }
            }

            ItemCountText.Text = $"{_vm.Items.Count} / {_allItems.Count}";
        }

        // ── Write to Revit ────────────────────────────────────────────────────

        private void WriteToRevit_Click(object sender, RoutedEventArgs e)
        {
            if (_allRooms.Count == 0)
            {
                MessageBox.Show(
                    "No Drofus rooms are loaded.\nSelect a project first.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Index Drofus rooms by number for O(1) lookup
            Dictionary<string, DrofusRoom> index = _allRooms.ToDictionary(
                r => r.Number, r => r,
                StringComparer.OrdinalIgnoreCase);

            int matched   = 0;
            int unmatched = 0;

            try
            {
                using var t = new Transaction(_doc, "Write Drofus Data to Revit Rooms");
                t.Start();

                IEnumerable<Room> revitRooms = new FilteredElementCollector(_doc)
                    .OfClass(typeof(SpatialElement))
                    .Cast<SpatialElement>()
                    .OfType<Room>()
                    .Where(r => r.Area > 0);

                foreach (Room revitRoom in revitRooms)
                {
                    if (!index.TryGetValue(revitRoom.Number, out DrofusRoom? drofusRoom))
                    {
                        unmatched++;
                        continue;
                    }

                    // Write Drofus function description to the Comments parameter
                    TrySetStringParam(revitRoom,
                        BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                        $"Drofus function: {drofusRoom.Function}");

                    // Attempt to map any extra Drofus attributes to identically-named parameters
                    foreach ((string key, string value) in drofusRoom.Attributes)
                    {
                        Parameter? p = revitRoom.LookupParameter(key);
                        if (p is not null && !p.IsReadOnly && p.StorageType == StorageType.String)
                            p.Set(value);
                    }

                    matched++;
                }

                t.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Write failed:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                $"Updated {matched} room(s).\n{unmatched} room(s) had no matching Drofus number.",
                "Write Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private static void TrySetStringParam(Element el, BuiltInParameter bip, string value)
        {
            Parameter? p = el.get_Parameter(bip);
            if (p is not null && !p.IsReadOnly && p.StorageType == StorageType.String)
                p.Set(value);
        }

        private void SetStatus(string msg) =>
            Dispatcher.Invoke(() => StatusText.Text = msg);
    }

    // ── ViewModel ─────────────────────────────────────────────────────────────

    internal sealed class DrofusViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DrofusRoom> Rooms { get; } = [];
        public ObservableCollection<DrofusItem> Items { get; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
