using Seal.Models;

namespace Seal.Services;

public sealed class TaskCatalog
{
    public const int MaximumActiveTasks = 20;

    private readonly ITaskRepository repository;
    private List<FocusTask> tasks;

    public TaskCatalog(ITaskRepository repository)
    {
        this.repository = repository;
        tasks = repository.ReadAll().ToList();

        if (!tasks.Any(task => task.Id == Guid.Empty))
        {
            var updated = tasks.Append(new FocusTask(Guid.Empty, "default")).ToList();
            repository.SaveAll(updated);
            tasks = updated;
        }
        var assigned = tasks.Where(task => !task.IsDeleted && TaskColorPalette.Contains(task.ColorHex))
            .Select(task => task.ColorHex).ToList();
        var updatedColors = tasks.ToList();

        foreach (var task in tasks.OrderBy(task => task.IsDeleted))
        {
            if (TaskColorPalette.Contains(task.ColorHex))
            {
                continue;
            }

            var color = TaskColorPalette.Next(assigned);
            updatedColors[updatedColors.FindIndex(item => item.Id == task.Id)] = task with { ColorHex = color };
            if (!task.IsDeleted)
            {
                assigned.Add(color);
            }
        }

        if (!tasks.SequenceEqual(updatedColors))
        {
            repository.SaveAll(updatedColors);
            tasks = updatedColors;
        }
    }

    public event EventHandler? Changed;

    public IReadOnlyList<FocusTask> ReadAll() => tasks.ToList();

    public IReadOnlyList<FocusTask> ActiveTasks() => tasks
        .Where(task => !task.IsDeleted)
        .OrderBy(task => task.Id == Guid.Empty ? 0 : 1)
        .ThenBy(task => task.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public FocusTask Add(string name, int? targetMinutes = null)
    {
        if (ActiveTasks().Count >= MaximumActiveTasks)
        {
            throw new ArgumentException($"You can have up to {MaximumActiveTasks} active tasks. Remove a task before adding another.");
        }

        ValidateTarget(targetMinutes);
        var color = TaskColorPalette.Next(ActiveTasks().Select(task => task.ColorHex));
        var task = new FocusTask(Guid.NewGuid(), ValidateName(name), ColorHex: color, TargetMinutes: targetMinutes);
        Commit(tasks.Append(task).ToList());
        return task;
    }

    public void Update(Guid id, string name, int? targetMinutes)
    {
        RequireEditable(id, allowDefault: true);
        var current = tasks.Single(task => task.Id == id);
        if (id == Guid.Empty && name.Trim() != current.Name)
        {
            throw new ArgumentException("The default task cannot be renamed.");
        }

        var validated = ValidateName(name, id);
        ValidateTarget(targetMinutes);
        Commit(tasks.Select(task => task.Id == id
            ? task with { Name = validated, TargetMinutes = targetMinutes }
            : task).ToList());
    }

    public void SetTarget(Guid id, int? targetMinutes)
    {
        RequireEditable(id, allowDefault: true);
        ValidateTarget(targetMinutes);
        Commit(tasks.Select(task => task.Id == id ? task with { TargetMinutes = targetMinutes } : task).ToList());
    }

    private static void ValidateTarget(int? minutes)
    {
        if (minutes.HasValue && (minutes.Value is < 60 or > 600 || minutes.Value % 60 != 0))
        {
            throw new ArgumentException("Choose a daily goal between 1h and 10h.");
        }
    }

    public void Rename(Guid id, string name)
    {
        RequireEditable(id);
        var validated = ValidateName(name, id);
        Commit(tasks.Select(task => task.Id == id ? task with { Name = validated } : task).ToList());
    }

    public void Remove(Guid id)
    {
        RequireEditable(id, allowDefault: true);

        if (!CanRemove(id))
        {
            throw new ArgumentException("Keep at least one active task. Add another task before removing this one.");
        }

        Commit(tasks.Select(task => task.Id == id ? task with { IsDeleted = true } : task).ToList());
    }

    public bool CanRemove(Guid id) => tasks.Any(task => task.Id == id && !task.IsDeleted)
        && tasks.Count(task => !task.IsDeleted) > 1;

    private string ValidateName(string name, Guid? excludedId = null)
    {
        var trimmed = name.Trim();

        if (trimmed.Length is 0 or > 15)
        {
            throw new ArgumentException("Enter a task name between 1 and 15 characters.");
        }

        if (tasks.Any(task => !task.IsDeleted && task.Id != excludedId &&
            string.Equals(task.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("A task with this name already exists.");
        }

        return trimmed;
    }

    private void RequireEditable(Guid id, bool allowDefault = false)
    {
        if (id == Guid.Empty && !allowDefault)
        {
            throw new ArgumentException("The default task cannot be renamed.");
        }

        if (!tasks.Any(task => task.Id == id && !task.IsDeleted))
        {
            throw new ArgumentException("Select an existing task.");
        }
    }

    private void Commit(List<FocusTask> updated)
    {
        repository.SaveAll(updated);
        tasks = updated;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
