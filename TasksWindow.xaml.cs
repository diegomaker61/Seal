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
    private bool isRefreshing;
    private FocusTask? newTask;

    public TasksWindow(TaskCatalog tasks)
    {
        InitializeComponent();
        this.tasks = tasks;
        tasks.Changed += TasksChanged;
        Closed += (_, _) => tasks.Changed -= TasksChanged;
        TargetInput.ItemsSource = Enumerable.Range(0, 11).ToList();
        RefreshTasks(tasks.ActiveTasks().First().Id);
    }

    private void TasksChanged(object? sender, EventArgs e) => RefreshTasks((TaskList.SelectedItem as FocusTask)?.Id);

    private void RefreshTasks(Guid? selectedId)
    {
        isRefreshing = true;
        var items = tasks.ActiveTasks().ToList();
        if (newTask is not null) items.Add(newTask);
        TaskList.ItemsSource = items;
        TaskList.SelectedItem = items.FirstOrDefault(task => task.Id == selectedId) ?? items.FirstOrDefault();
        isRefreshing = false;
        LoadSelectedTask();
    }

    private void SelectTask(object sender, SelectionChangedEventArgs e)
    {
        if (!isRefreshing && tasks is not null)
        {
            if (newTask is not null && e.RemovedItems.OfType<FocusTask>().Any(task => task.Id == newTask.Id))
            {
                newTask = newTask with { Name = TaskNameInput.Text, TargetMinutes = ReadTargetMinutes() };
            }

            LoadSelectedTask();
        }
    }

    private void LoadSelectedTask()
    {
        var selected = TaskList.SelectedItem as FocusTask;
        var task = selected?.Id == newTask?.Id ? newTask : selected;
        TaskNameInput.Text = task?.Name ?? "";
        TaskNameInput.IsReadOnly = task?.Id == Guid.Empty;
        TargetInput.SelectedItem = Math.Clamp((int)Math.Ceiling((task?.TargetMinutes ?? 0) / 60.0), 0, 10);
        AddButton.Content = "Save";
        AddButton.IsEnabled = task is not null;
        AddButton.ToolTip = "Save task changes";
        NewButton.IsEnabled = newTask is not null || tasks.ActiveTasks().Count < TaskCatalog.MaximumActiveTasks;
        RemoveButton.IsEnabled = task is not null && (task.Id == newTask?.Id || tasks.CanRemove(task.Id));
    }

    private void NewTask(object sender, RoutedEventArgs e)
    {
        if (newTask is null)
        {
            if (tasks.ActiveTasks().Count >= TaskCatalog.MaximumActiveTasks) return;
            newTask = new FocusTask(Guid.NewGuid(), "", ColorHex:
                TaskColorPalette.Next(tasks.ActiveTasks().Select(task => task.ColorHex)));
        }

        RefreshTasks(newTask.Id);
        FeedbackLabel.Text = "Enter a name and an optional daily goal, then click Save.";
        TaskNameInput.Focus();
    }

    private void SaveTask(object sender, RoutedEventArgs e) => Execute(() =>
    {
        if (TaskList.SelectedItem is not FocusTask selected) return;
        if (selected.Id == newTask?.Id)
        {
            var saved = tasks.Add(TaskNameInput.Text, ReadTargetMinutes());
            newTask = null;
            RefreshTasks(saved.Id);
        }
        else
        {
            tasks.Update(selected.Id, TaskNameInput.Text, ReadTargetMinutes());
            RefreshTasks(selected.Id);
        }
    });

    private int? ReadTargetMinutes()
    {
        if (TargetInput.SelectedItem is not int hours || hours is < 0 or > 10)
        {
            throw new ArgumentException("Choose a daily goal between 0h and 10h.");
        }

        return hours == 0 ? null : hours * 60;
    }

    private void RemoveTask(object sender, RoutedEventArgs e) => Execute(() =>
    {
        if (TaskList.SelectedItem is FocusTask task)
        {
            if (task.Id == newTask?.Id)
            {
                newTask = null;
                RefreshTasks(null);
            }
            else
            {
                tasks.Remove(task.Id);
            }
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
