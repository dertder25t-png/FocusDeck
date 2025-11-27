namespace FocusDeck.Domain.Entities;

public enum ProjectResourceType
{
    Url = 0,
    FilePath = 1,
    AppId = 2,
    RepoSlug = 3,
    NoteId = 4,
    CanvasCourseId = 5
}

public enum ProjectResourceStatus
{
    Active = 0,
    Archived = 1
}
