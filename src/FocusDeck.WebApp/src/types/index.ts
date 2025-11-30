
export interface Note {
  id: string;
  title: string;
  content: string;
  type: number; // 0=QuickNote, 1=AcademicPaper
  tags: string[];
  color: string;
  isPinned: boolean;
  createdDate: string;
  lastModified?: string;
  bookmarks: NoteBookmark[];
  sources: AcademicSource[];
  citationStyle?: string;
  courseId?: string;
  eventId?: string;
  tenantId: string;
}

export interface NoteBookmark {
  id: string;
  name: string;
  position: number;
  length: number;
  color: string;
  createdDate: string;
}

export interface AcademicSource {
    id: string;
    noteId: string;
    title: string;
    authors: string[];
    year: number;
    url?: string;
    citationKey: string;
}

export interface TodoItem {
    id: string;
    title: string;
    description: string;
    priority: number;
    isCompleted: boolean;
    dueDate?: string;
    completedDate?: string;
    source: string;
    canvasAssignmentId?: string;
    canvasCourseId?: string;
    tags: string[];
    estimatedMinutes: number;
    actualMinutes: number;
    showReminder: boolean;
    repeat: string;
}

export interface CreateNoteDto {
    title: string;
    content: string;
    type: number;
    tags?: string[];
    color?: string;
    isPinned?: boolean;
}

export interface UpdateNoteDto {
    id: string;
    title: string;
    content: string;
    type: number;
    tags?: string[];
    color?: string;
    isPinned?: boolean;
}
