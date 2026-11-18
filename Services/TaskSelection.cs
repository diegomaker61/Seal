namespace Seal.Services;

public sealed class TaskSelection
{
    private Guid? selectedId;
    public bool IsLocked { get; set; }
    public Guid? SelectedId => selectedId;
    public event EventHandler? Changed;

    public void Select(Guid id)
    {
        if (selectedId == id || IsLocked) return;
        selectedId = id;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
