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
        private readonly DrofusService _service;
        private readonly Document _doc;
        private readonly DrofusViewModel _vm = new();

        private List<DrofusRoom> _allRooms = [];
        private List<DrofusItem> _allItems = [];

        public DrofusDataWindow(DrofusService service, Document doc)
        {
            _service = service;
            _doc = doc;
            InitializeComponent();
            DataContext = _vm;
            Loaded += async (_, _) => await LoadProjectsAsync();
        }

        // ── Projects ────────────────────────────────────────────────────────

        private async Task LoadProjectsAsync()
        {
            SetStatus("Loading projects…");
            try
            {
                var projects = await _service.GetProjectsAsync();
                ProjectCombo.ItemsSource = projects;
                if (projects.Count > 0)
                    ProjectCombo.SelectedIndex = 0;
                SetStatus($"{projects.Count} project(s) loaded.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private async void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem is not DrofusProject project) return;
            await LoadProjectDataAsync(project.Id);
        }

        private async void RefreshProjects_Click(object sender, RoutedEventArgs e) =>
            await LoadProjectsAsync();

        private async Task LoadProjectDataAsync(string projectId)
        {
            SetStatus("Loading rooms and items…");
            try
            {
                var roomTask = _service.GetRoomsAsync(projectId);
                var itemTask = _service.GetItemsAsync(projectId);

                await Task.WhenAll(roomTask, itemTask);

                _allRooms = roomTask.Result;
                _allItems = itemTask.Result;

                ApplyRoomFilter();
                ApplyItemFilter();

                SetStatus($"{_allRooms.Count} room(s) · {_allItems.Count} item(s).");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        // ── Search ──────────────────────────────────────────────────────────

        private void RoomSearch_TextChanged(object sender, TextChangedEventArgs e) =>
            ApplyRoomFilter();

        private void ItemSearch_TextChanged(object sender, TextChangedEventArgs e) =>
            ApplyItemFilter();

        private void ApplyRoomFilter()
        {
            string q = RoomSearchBox.Text.Trim().ToLowerInvariant();
            _vm.Rooms.Clear();
            foreach (var r in _allRooms.Where(r =>
                string.IsNullOrEmpty(q) ||
                r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Number.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Function.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                _vm.Rooms.Add(r);
            }
        }

        private void ApplyItemFilter()
        {
            string q = ItemSearchBox.Text.Trim().ToLowerInvariant();
            _vm.Items.Clear();
            foreach (var i in _allItems.Where(i =>
                string.IsNullOrEmpty(q) ||
                i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Number.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Category.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                _vm.Items.Add(i);
            }
        }

        // ── Write to Revit ───────────────────────────────────────────────────

        private void WriteToRevit_Click(object sender, RoutedEventArgs e)
        {
            if (_allRooms.Count == 0)
            {
                MessageBox.Show("No Drofus rooms loaded. Select a project first.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int matched = 0;
            int unmatched = 0;

            using var t = new Transaction(_doc, "Write Drofus Data to Rooms");
            t.Start();

            // Index Drofus rooms by number for fast lookup
            var drofusIndex = _allRooms.ToDictionary(
                r => r.Number, r => r,
                StringComparer.OrdinalIgnoreCase);

            var revitRooms = new FilteredElementCollector(_doc)
                .OfClass(typeof(SpatialElement))
                .Cast<SpatialElement>()
                .OfType<Room>()
                .Where(r => r.Area > 0);

            foreach (Room revitRoom in revitRooms)
            {
                if (!drofusIndex.TryGetValue(revitRoom.Number, out DrofusRoom? drofusRoom))
                {
                    unmatched++;
                    continue;
                }

                // Write Drofus function to room Comments parameter (safe fallback)
                TrySetParam(revitRoom, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                    $"Drofus: {drofusRoom.Function}");

                // Write any custom Drofus attributes that match shared/project parameters by name
                foreach (var (key, value) in drofusRoom.Attributes)
                {
                    Parameter? p = revitRoom.LookupParameter(key);
                    if (p is not null && !p.IsReadOnly && p.StorageType == StorageType.String)
                        p.Set(value);
                }

                matched++;
            }

            t.Commit();

            MessageBox.Show(
                $"Matched and updated {matched} room(s).\n{unmatched} room(s) had no matching Drofus number.",
                "Write Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void TrySetParam(Element el, BuiltInParameter bip, string value)
        {
            Parameter? p = el.get_Parameter(bip);
            if (p is not null && !p.IsReadOnly)
                p.Set(value);
        }

        private void SetStatus(string msg) =>
            Dispatcher.Invoke(() => StatusText.Text = msg);
    }

    internal class DrofusViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DrofusRoom> Rooms { get; } = [];
        public ObservableCollection<DrofusItem> Items { get; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
