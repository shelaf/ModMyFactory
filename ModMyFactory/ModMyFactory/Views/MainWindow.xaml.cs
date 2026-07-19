using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ModMyFactory.Models;
using ModMyFactory.ViewModels;

namespace ModMyFactory.Views
{
    partial class MainWindow
    {
        const int DefaultWidth = 800, DefaultHeight = 600;

        const double AutoScrollThreshold = 25.0;
        const double AutoScrollMaxSpeed = 25.0;

        Point dragStartPoint;
        bool dragging;
        bool modsListBoxDeselectionOmitted;
        bool modpacksListBoxDeselectionOmitted;

        DispatcherTimer dropTimer;
        string[] droppedFiles = null;

        DispatcherTimer autoScrollTimer;
        double autoScrollSpeed;
        ScrollViewer autoScrollViewer;

        public MainWindow()
            : base(App.Instance.Settings.MainWindowInfo, DefaultWidth, DefaultHeight)
        {
            InitializeComponent();

            dragging = false;
            Closing += ClosingHandler;

            dropTimer = new DispatcherTimer(DispatcherPriority.Input);
            dropTimer.Interval = TimeSpan.FromMilliseconds(1);
            dropTimer.Tick += DropTimerCallback;

            autoScrollTimer = new DispatcherTimer(DispatcherPriority.Input);
            autoScrollTimer.Interval = TimeSpan.FromMilliseconds(30);
            autoScrollTimer.Tick += AutoScrollTimerCallback;
        }

        private void ClosingHandler(object sender, CancelEventArgs e)
        {
            App.Instance.Settings.MainWindowInfo = CreateInfo();
            App.Instance.Settings.Save();
        }

        void CanExecuteCommandDefault(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        void Close(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        private Point GetMousePos()
        {
            return PointToScreen(Mouse.GetPosition(this));
        }

        private static ListBoxItem GetItem(ListBox listBox, Func<ListBoxItem, Point> positionSelector)
        {
            int itemCount = ((ICollectionView)listBox.ItemsSource).Cast<object>().Count();
            for (int i = 0; i < itemCount; i++)
            {
                ListBoxItem item = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(i);
                Rect itemBounds = VisualTreeHelper.GetDescendantBounds(item);
                Point relativePosition = positionSelector.Invoke(item);

                if (itemBounds.Contains(relativePosition))
                    return item;
            }

            return null;
        }

        private void ModpackListBoxDropHandler(object sender, DragEventArgs e)
        {
            StopAutoScroll();

            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            ListBoxItem item = GetItem(listBox, e.GetPosition);
            if (item != null)
            {
                if (e.Data.GetDataPresent(typeof(List<Mod>)))
                {
                    var mods = (List<Mod>)e.Data.GetData(typeof(List<Mod>));
                    Modpack parent = (Modpack)listBox.ItemContainerGenerator.ItemFromContainer(item);
                    if (parent.IsLocked) return;

                    foreach (Mod mod in mods)
                    {
                        if (parent.Contains(mod.Name, mod.FactorioVersion, out var @ref))
                        {
                            if (@ref.Mod != mod)
                            {
                                @ref.RemoveFromParentCommand.Execute();
                                var reference = new ModReference(mod, parent);
                                parent.Mods.Add(reference);
                            }
                        }
                        else
                        {
                            var reference = new ModReference(mod, parent);
                            parent.Mods.Add(reference);
                        }
                    }

                    ModpackTemplateList.Instance.Update(MainViewModel.Instance.Modpacks);
                    ModpackTemplateList.Instance.Save();
                }
                else if (e.Data.GetDataPresent(typeof(List<Modpack>)))
                {
                    var modpacks = (List<Modpack>)e.Data.GetData(typeof(List<Modpack>));
                    Modpack parent = (Modpack)listBox.ItemContainerGenerator.ItemFromContainer(item);
                    if (parent.IsLocked) return;

                    foreach (Modpack modpack in modpacks)
                    {
                        if (modpack != parent && !parent.Contains(modpack) && !modpack.Contains(parent, true))
                        {
                            var reference = new ModpackReference(modpack, parent);
                            reference.ParentView = parent.ModsView;
                            parent.Mods.Add(reference);
                        }
                    }

                    ModpackTemplateList.Instance.Update(MainViewModel.Instance.Modpacks);
                    ModpackTemplateList.Instance.Save();
                }
            }
            else
            {
                if (e.Data.GetDataPresent(typeof(List<Mod>)))
                {
                    var model = (MainViewModel)ViewModel;
                    var mods = (List<Mod>)e.Data.GetData(typeof(List<Mod>));
                    model.CreateNewModpack(mods);
                }
            }
        }

        private static ScrollViewer GetScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer scrollViewer)
                return scrollViewer;

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                ScrollViewer result = GetScrollViewer(child);
                if (result != null) return result;
            }

            return null;
        }

        private void AutoScrollTimerCallback(object sender, EventArgs e)
        {
            if (autoScrollViewer == null || autoScrollSpeed == 0.0)
            {
                StopAutoScroll();
                return;
            }

            autoScrollViewer.ScrollToVerticalOffset(autoScrollViewer.VerticalOffset + autoScrollSpeed);
        }

        private void StopAutoScroll()
        {
            autoScrollTimer.Stop();
            autoScrollSpeed = 0.0;
            autoScrollViewer = null;
        }

        private void UpdateAutoScroll(ListBox listBox, Point position)
        {
            ScrollViewer scrollViewer = autoScrollViewer;
            if (scrollViewer == null || autoScrollTimer.IsEnabled == false)
                scrollViewer = GetScrollViewer(listBox);

            if (scrollViewer == null)
            {
                StopAutoScroll();
                return;
            }

            double height = listBox.ActualHeight;
            double speed = 0.0;

            if (position.Y >= 0.0 && position.Y < AutoScrollThreshold)
            {
                double factor = (AutoScrollThreshold - position.Y) / AutoScrollThreshold;
                speed = -AutoScrollMaxSpeed * factor;
            }
            else if (position.Y > height - AutoScrollThreshold && position.Y <= height)
            {
                double factor = (position.Y - (height - AutoScrollThreshold)) / AutoScrollThreshold;
                speed = AutoScrollMaxSpeed * factor;
            }

            if (speed == 0.0)
            {
                StopAutoScroll();
            }
            else
            {
                autoScrollViewer = scrollViewer;
                autoScrollSpeed = speed;
                if (!autoScrollTimer.IsEnabled)
                    autoScrollTimer.Start();
            }
        }

        private void ModpackListBoxDragLeaveHandler(object sender, DragEventArgs e)
        {
            StopAutoScroll();
        }

        private void ModpackListBoxDragOverHandler(object sender, DragEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(typeof(List<Modpack>)))
            {
                ListBoxItem item = GetItem(listBox, e.GetPosition);
                if (item != null) e.Effects = DragDropEffects.Link;
            }
            else if (e.Data.GetDataPresent(typeof(List<Mod>)))
            {
                ListBoxItem item = GetItem(listBox, e.GetPosition);
                e.Effects = (item == null) ? DragDropEffects.Copy : DragDropEffects.Link;
            }

            UpdateAutoScroll(listBox, e.GetPosition(listBox));

            e.Handled = true;
        }

        private void ModpackListBoxPreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            if (e.ChangedButton != MouseButton.Left)
                return;

            ListBoxItem item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                dragStartPoint = e.GetPosition(null);
                dragging = true;

                if (item.IsSelected)
                    modpacksListBoxDeselectionOmitted = true;
            }
            else
            {
                dragging = false;
                listBox.SelectedItems.Clear();
            }
        }

        private void ModpackListBoxMouseMoveHandler(object sender, MouseEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            Vector dragDistance = e.GetPosition(null) - dragStartPoint;
            if (e.LeftButton == MouseButtonState.Pressed && dragging &&
                (Math.Abs(dragDistance.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(dragDistance.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var modpacks = new List<Modpack>(listBox.SelectedItems.Cast<Modpack>());
                DragDrop.DoDragDrop(listBox, modpacks, DragDropEffects.Link);
                dragging = false;
                modpacksListBoxDeselectionOmitted = false;
            }
        }

        private void ModpackListBoxPreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (modpacksListBoxDeselectionOmitted)
            {
                ListBox listBox = sender as ListBox;
                if (listBox == null) return;

                ListBoxItem item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
                if (item != null && item.IsSelected)
                {
                    Modpack selectedModpack = (Modpack)listBox.ItemContainerGenerator.ItemFromContainer(item);
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        listBox.SelectedItems.Remove(selectedModpack);
                    }
                    else
                    {
                        for (int i = listBox.SelectedItems.Count - 1; i >= 0; i--)
                        {
                            Modpack modpack = (Modpack)listBox.SelectedItems[i];
                            if (modpack != selectedModpack)
                                listBox.SelectedItems.Remove(modpack);
                        }
                    }
                }

                modpacksListBoxDeselectionOmitted = false;
            }
        }

        private void ModsListBoxPreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            if (e.ChangedButton != MouseButton.Left)
                return;

            ListBoxItem item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                dragStartPoint = e.GetPosition(null);
                dragging = true;

                if (item.IsSelected)
                    modsListBoxDeselectionOmitted = true;
            }
            else
            {
                dragging = false;
                listBox.SelectedItems.Clear();
            }
        }

        private void ModsListBoxPreviewMouseMoveHandler(object sender, MouseEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            Vector dragDistance = e.GetPosition(null) - dragStartPoint;
            if (e.LeftButton == MouseButtonState.Pressed && dragging &&
                (Math.Abs(dragDistance.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(dragDistance.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var mods = new List<Mod>();
                mods.AddRange(listBox.SelectedItems.Cast<Mod>());
                DragDrop.DoDragDrop(listBox, mods, DragDropEffects.Link | DragDropEffects.Copy);
                dragging = false;
                modsListBoxDeselectionOmitted = false;
            }
        }

        private void ModsListBoxPreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (modsListBoxDeselectionOmitted)
            {
                ListBox listBox = sender as ListBox;
                if (listBox == null) return;

                ListBoxItem item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
                if (item != null && item.IsSelected)
                {
                    Mod selectedMod = (Mod)listBox.ItemContainerGenerator.ItemFromContainer(item);
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        listBox.SelectedItems.Remove(selectedMod);
                    }
                    else
                    {
                        for (int i = listBox.SelectedItems.Count - 1; i >= 0; i--)
                        {
                            Mod mod = (Mod)listBox.SelectedItems[i];
                            if (mod != selectedMod)
                                listBox.SelectedItems.Remove(mod);
                        }
                    }
                }

                modsListBoxDeselectionOmitted = false;
            }
        }

        private void ModpackListBoxMouseDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            ListBox listBox = sender as ListBox;
            if (listBox == null) return;

            // Ignore double clicks on interactive controls (buttons, checkboxes, rename box, expander toggle)
            // so they don't unintentionally toggle the contents.
            DependencyObject current = e.OriginalSource as DependencyObject;
            while (current != null && !(current is ListBoxItem))
            {
                if (current is ButtonBase || current is CheckBox || current is TextBoxBase)
                    return;

                current = VisualTreeHelper.GetParent(current);
            }

            ListBoxItem item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item == null) return;

            Modpack modpack = listBox.ItemContainerGenerator.ItemFromContainer(item) as Modpack;
            if (modpack == null) return;

            modpack.ContentsExpanded = !modpack.ContentsExpanded;
        }

        private void RenameTextBoxLostFocusHandler(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.Visibility = Visibility.Collapsed;
        }

        private void RenameTextBoxVisibilityChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if ((bool)e.NewValue)
            {
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void ModsListBoxDragOverHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void ModsListBoxDropHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                droppedFiles = ((string[])e.Data.GetData(DataFormats.FileDrop));
                dropTimer.Start();
            }
        }

        private async void DropTimerCallback(object sender, EventArgs e)
        {
            dropTimer.Stop();

            if (droppedFiles != null)
            {
                await MainViewModel.Instance.AddModsFromFiles(droppedFiles, true);
                droppedFiles = null;
            }
        }
    }
}
