namespace CodeCafe.Domain.Notes.Enums;

public enum NotebookItemAddViolation
{
    ParentNotFound,
    ParentNotFolder,
    NoRoomForChild,
    SlugConflict,
}
