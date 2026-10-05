using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Seal.Models;
using Seal.Services;

namespace Seal;

public partial class TasksWindow : Window
{
    private readonly TaskCatalog tasks;

    public TasksWindow(TaskCatalog tasks)
    {
        InitializeComponent();
        this.tasks = tasks;
        tasks.Changed += TasksChanged;
        Closed += (_, _) => tasks.Changed -= TasksChanged;
        RefreshTasks(Guid.Empty);
    }

    private void TasksChanged(object? sender, EventArgs e) => RefreshTasks((TaskList.SelectedItem as FocusTask)?.Id);

    private void RefreshTasks(Guid? selectedId)
    {
        TaskList.ItemsSource = tasks.ActiveTasks();
        TaskList.SelectedItem = tasks.ActiveTasks().FirstOrDefault(task => task.Id == selectedId)
            ?? tasks.ActiveTasks().First();
    }

    private void SelectTask(object sender, SelectionChangedEventArgs e)
    {
        if (TaskList.SelectedItem is not FocusTask task)
        {
            return;
        }

        TaskNameInput.Text = task.Name;
        RenameButton.IsEnabled = task.Id != Guid.Empty;
        RemoveButton.IsEnabled = tasks.CanRemove(task.Id);
    }

    private void AddTask(object sender, RoutedEventArgs e) => Execute(() =>
    {
        var task = tasks.Add(TaskNameInput.Text);
        RefreshTasks(task.Id);
    });

    private void RenameTask(object sender, RoutedEventArgs e) => Execute(() =>
    {
        if (TaskList.SelectedItem is FocusTask task)
        {
            tasks.Rename(task.Id, TaskNameInput.Text);
            RefreshTasks(task.Id);
        }
    });

    private void RemoveTask(object sender, RoutedEventArgs e) => Execute(() =>
    {
        if (TaskList.SelectedItem is FocusTask task)
        {
            tasks.Remove(task.Id);
        }
    });

    private void Execute(Action action)
    {
        try
        {
            action();
            FeedbackLabel.Text = "Tasks saved.";
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or JsonException)
        {
            FeedbackLabel.Text = error is ArgumentException ? error.Message : "Unable to save tasks.";
        }
    }

    private void DragWindow(object sender, MouseButtonEventArgs e) => WindowDrag.Handle(this, e);
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
