import React, { useState } from 'react';
import { DndContext, DragOverlay, closestCorners, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import type { DragStartEvent, DragEndEvent } from '@dnd-kit/core';
import { SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useDroppable } from '@dnd-kit/core';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../components/Dialog';
import { useTasks, useCreateTask, useUpdateTask, useDeleteTask } from '../hooks/useTasks';
import type { TodoItem } from '../types';
import { Plus, Clock } from 'lucide-react';
import { format } from 'date-fns';

// --- Components ---

const SortableTask = ({ task, onClick }: { task: TodoItem; onClick: () => void }) => {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id: task.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div 
      ref={setNodeRef} 
      style={style} 
      {...attributes} 
      {...listeners}
      className="relative pl-6 mb-4 group cursor-grab active:cursor-grabbing"
      onClick={onClick}
    >
      {/* Timeline Line */}
      <div className="absolute left-[5px] top-3 bottom-[-16px] w-0.5 bg-gray-200 dark:bg-gray-700 group-last:hidden"></div>
      {/* Timeline Dot */}
      <div className={`absolute left-0 top-3 w-3 h-3 rounded-full border-2 border-white dark:border-gray-900 z-10 shadow-sm ${task.isCompleted ? 'bg-green-500' : 'bg-blue-500'}`}></div>

      <div className="bg-white dark:bg-gray-800 p-3 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 hover:shadow-md transition-shadow">
        <div className="flex justify-between items-start mb-1">
             {task.dueDate && (
                 <span className="text-xs font-mono text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded flex items-center gap-1">
                     <Clock size={10} />
                     {format(new Date(task.dueDate), 'h:mm a')}
                 </span>
             )}
        </div>
        <div className={`font-medium text-gray-800 dark:text-gray-200 ${task.isCompleted ? 'line-through text-gray-500' : ''}`}>{task.title}</div>
        {task.tags && task.tags.length > 0 && (
            <div className="flex gap-1 mt-2 flex-wrap">
                {task.tags.filter(t => !['Focus', 'Social', 'Health', 'Admin'].includes(t)).map(tag => (
                    <span key={tag} className="text-[10px] px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-600 dark:bg-blue-900/30 dark:text-blue-300 border border-blue-100 dark:border-blue-800">
                        #{tag}
                    </span>
                ))}
            </div>
        )}
      </div>
    </div>
  );
};

const Lane = ({ id, title, color, tasks, onAddTask, onTaskClick }: { id: string, title: string, color: string, tasks: TodoItem[], onAddTask: (laneId: string) => void, onTaskClick: (task: TodoItem) => void }) => {
  const { setNodeRef } = useDroppable({ id });

  return (
    <div ref={setNodeRef} className="flex-1 min-w-[280px] flex flex-col h-full bg-gray-50 dark:bg-gray-900/50 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div className={`p-3 border-b border-gray-200 dark:border-gray-700 flex items-center gap-2 bg-white dark:bg-gray-800/50`}>
        <div className={`w-3 h-3 rounded-full ${color}`}></div>
        <h3 className="font-bold text-gray-700 dark:text-gray-200">{title}</h3>
        <span className="ml-auto text-xs font-medium bg-gray-200 dark:bg-gray-800 px-2 py-0.5 rounded-full text-gray-600 dark:text-gray-400">{tasks.length}</span>
      </div>
      
      {/* Task List */}
      <div className="flex-1 p-4 overflow-y-auto">
        <SortableContext items={tasks.map(t => t.id)} strategy={verticalListSortingStrategy}>
          {tasks.map(task => (
            <SortableTask key={task.id} task={task} onClick={() => onTaskClick(task)} />
          ))}
        </SortableContext>
        {tasks.length === 0 && (
            <div className="text-center py-8 text-gray-400 dark:text-gray-600 text-sm italic">No tasks yet</div>
        )}
      </div>

      {/* Footer Add Button */}
      <div className="p-2 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800/50">
          <button
            onClick={() => onAddTask(id)}
            className="w-full py-2 rounded-lg border border-dashed border-gray-300 dark:border-gray-600 text-gray-500 hover:text-blue-600 hover:border-blue-400 dark:text-gray-400 dark:hover:text-blue-400 transition-colors flex items-center justify-center gap-2 text-sm font-medium"
          >
              <Plus size={14} /> Add Task
          </button>
      </div>
    </div>
  );
};

export const KanbanApp: React.FC = () => {
  const { data: allTasks = [], isLoading } = useTasks();
  const createTaskMutation = useCreateTask();
  const updateTaskMutation = useUpdateTask();
  const deleteTaskMutation = useDeleteTask();

  const [activeId, setActiveId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<string>('Focus'); // For Mobile View

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Partial<TodoItem> | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }), // Prevent accidental drags
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const LANES = [
      { id: 'Focus', title: 'Focus', color: 'bg-blue-500' },
      { id: 'Social', title: 'Social', color: 'bg-pink-500' },
      { id: 'Health', title: 'Health', color: 'bg-green-500' },
      { id: 'Admin', title: 'Admin', color: 'bg-gray-500' },
  ];

  const getTasksForLane = (laneId: string) => {
      // Filter tasks that have the laneId as a tag
      return allTasks.filter((t: TodoItem) => t.tags && t.tags.includes(laneId));
  };

  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(event.active.id as string);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    const activeTaskId = active.id as string;
    const overId = over?.id as string;

    if (!overId) {
      setActiveId(null);
      return;
    }

    // Find the lane we dropped into
    // It could be the lane container itself (overId exists in LANES)
    // or a task within that lane.
    let targetLaneId = LANES.find(l => l.id === overId)?.id;

    if (!targetLaneId) {
        // If dropped over a task, find that task's lane
        const overTask = allTasks.find((t: TodoItem) => t.id === overId);
        if (overTask) {
             // Find which lane this task belongs to (first matching tag)
             targetLaneId = LANES.find(l => overTask.tags?.includes(l.id))?.id;
        }
    }

    if (targetLaneId) {
        const task = allTasks.find((t: TodoItem) => t.id === activeTaskId);
        if (task) {
            // Remove old lane tags and add new lane tag
            const oldLaneTags = LANES.map(l => l.id);
            const newTags = (task.tags || []).filter((t: string) => !oldLaneTags.includes(t));
            newTags.push(targetLaneId);

            if (!task.tags?.includes(targetLaneId)) { // Only update if changed
                updateTaskMutation.mutate({
                    id: task.id,
                    task: { ...task, tags: newTags }
                });
            }
        }
    }

    setActiveId(null);
  };

  const handleAddTask = (laneId: string) => {
      const newTask = {
          title: 'New Task',
          description: '',
          tags: [laneId],
          dueDate: new Date().toISOString(),
          isCompleted: false
      };
      setEditingTask(newTask);
      setIsModalOpen(true);
  };

  const handleTaskClick = (task: TodoItem) => {
      setEditingTask({ ...task });
      setIsModalOpen(true);
  };

  const handleSaveTask = async () => {
      if (!editingTask) return;

      if (editingTask.id) {
          // Update
          await updateTaskMutation.mutateAsync({ id: editingTask.id, task: editingTask });
      } else {
          // Create
          await createTaskMutation.mutateAsync(editingTask);
      }
      setIsModalOpen(false);
      setEditingTask(null);
  };

  const handleDeleteTask = async () => {
      if (editingTask?.id) {
          if (confirm('Are you sure you want to delete this task?')) {
              await deleteTaskMutation.mutateAsync(editingTask.id);
              setIsModalOpen(false);
              setEditingTask(null);
          }
      }
  };

  if (isLoading) return <div className="p-8">Loading tasks...</div>;

  return (
    <div className="h-full flex flex-col bg-white dark:bg-gray-900 transition-colors duration-300">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center shrink-0">
        <h2 className="text-xl font-bold text-gray-800 dark:text-white">Daily Lanes</h2>
        <div className="flex gap-2">
          {/* <button className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"><i className="fa-solid fa-filter"></i></button> */}
        </div>
      </div>

      {/* Mobile Tabs */}
      <div className="md:hidden flex border-b border-gray-200 dark:border-gray-700 overflow-x-auto no-scrollbar shrink-0">
        {LANES.map(lane => (
          <button
            key={lane.id}
            onClick={() => setActiveTab(lane.id)}
            className={`flex-1 py-3 px-4 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${activeTab === lane.id ? `border-${lane.color.replace('bg-', '')} text-gray-900 dark:text-white` : 'border-transparent text-gray-500 dark:text-gray-400'}`}
          >
            {lane.title}
          </button>
        ))}
      </div>

      {/* Content Area */}
      <DndContext 
        sensors={sensors} 
        collisionDetection={closestCorners} 
        onDragStart={handleDragStart} 
        onDragEnd={handleDragEnd}
      >
        <div className="flex-1 overflow-hidden p-4">
          {/* Desktop Grid */}
          <div className="hidden md:flex gap-4 h-full overflow-x-auto pb-2">
            {LANES.map(lane => (
              <Lane
                key={lane.id}
                {...lane}
                tasks={getTasksForLane(lane.id)}
                onAddTask={handleAddTask}
                onTaskClick={handleTaskClick}
              />
            ))}
          </div>

          {/* Mobile View (Active Tab Only) */}
          <div className="md:hidden h-full">
            {LANES.filter(l => l.id === activeTab).map(lane => (
               <Lane
                 key={lane.id}
                 {...lane}
                 tasks={getTasksForLane(lane.id)}
                 onAddTask={handleAddTask}
                 onTaskClick={handleTaskClick}
               />
            ))}
          </div>
        </div>

        <DragOverlay>
          {activeId ? (
             <div className="bg-white dark:bg-gray-800 p-3 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 opacity-90 rotate-2 cursor-grabbing w-[280px]">
               <div className="font-medium text-gray-800 dark:text-gray-200">Moving Task...</div>
             </div>
          ) : null}
        </DragOverlay>
      </DndContext>

      {/* Task Detail Modal */}
      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-[500px]">
            <DialogHeader>
                <DialogTitle>{editingTask?.id ? 'Edit Task' : 'New Task'}</DialogTitle>
            </DialogHeader>
            <div className="grid gap-4 py-4">
                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium">Title</label>
                    <input
                        className="w-full px-3 py-2 border rounded-md dark:bg-gray-800 dark:border-gray-700"
                        value={editingTask?.title || ''}
                        onChange={e => setEditingTask(prev => ({ ...prev, title: e.target.value }))}
                        placeholder="Task title"
                    />
                </div>
                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium">Description</label>
                    <textarea
                        className="w-full px-3 py-2 border rounded-md dark:bg-gray-800 dark:border-gray-700 min-h-[100px]"
                        value={editingTask?.description || ''}
                        onChange={e => setEditingTask(prev => ({ ...prev, description: e.target.value }))}
                        placeholder="Description..."
                    />
                </div>
                <div className="grid grid-cols-2 gap-4">
                     <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium">Due Date</label>
                        <input
                            type="datetime-local"
                            className="w-full px-3 py-2 border rounded-md dark:bg-gray-800 dark:border-gray-700"
                            value={editingTask?.dueDate ? new Date(editingTask.dueDate).toISOString().slice(0, 16) : ''}
                            onChange={e => setEditingTask(prev => ({ ...prev, dueDate: new Date(e.target.value).toISOString() }))}
                        />
                     </div>
                     <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium">Status</label>
                        <div className="flex items-center gap-2 mt-2">
                            <input
                                type="checkbox"
                                className="w-4 h-4"
                                checked={editingTask?.isCompleted || false}
                                onChange={e => setEditingTask(prev => ({ ...prev, isCompleted: e.target.checked }))}
                            />
                            <span className="text-sm">Completed</span>
                        </div>
                     </div>
                </div>
                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium">Tags (comma separated)</label>
                     <input
                        className="w-full px-3 py-2 border rounded-md dark:bg-gray-800 dark:border-gray-700"
                        value={editingTask?.tags?.join(', ') || ''}
                        onChange={e => setEditingTask(prev => ({ ...prev, tags: e.target.value.split(',').map(t => t.trim()).filter(Boolean) }))}
                        placeholder="e.g. Focus, Urgent"
                    />
                </div>
            </div>
            <DialogFooter className="gap-2 sm:gap-0">
                {editingTask?.id && (
                    <button
                        onClick={handleDeleteTask}
                        className="mr-auto text-red-500 hover:text-red-700 text-sm font-medium px-4 py-2"
                    >
                        Delete
                    </button>
                )}
                <button
                    onClick={() => setIsModalOpen(false)}
                    className="px-4 py-2 border rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                >
                    Cancel
                </button>
                <button
                    onClick={handleSaveTask}
                    className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                    Save
                </button>
            </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
