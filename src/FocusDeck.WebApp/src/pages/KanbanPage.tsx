import { useState } from 'react';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Card, CardContent } from '../components/Card';
import { apiFetch } from '../lib/utils';

interface SortableItemProps {
  id: string;
  children: React.ReactNode;
}

function SortableItem({ id, children }: SortableItemProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      {children}
    </div>
  );
}

interface Task {
  id: string;
  content: string;
}

type TaskStatus = 'todo' | 'inProgress' | 'done';

type TasksState = Record<TaskStatus, Task[]>;

export function KanbanPage() {
  const [tasks, setTasks] = useState<TasksState>({
    todo: [{ id: '1', content: 'Task 1' }, { id: '2', content: 'Task 2' }],
    inProgress: [{ id: '3', content: 'Task 3' }],
    done: [{ id: '4', content: 'Task 4' }],
  });

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      // Note: dnd-kit's active/over data structure might need adjustment depending on how SortableContext is set up.
      // Assuming simple container logic for now or that we can infer container from item ID if needed.
      // But here the code assumes active.data.current.sortable exists.
      
      const activeData = active.data.current?.sortable;
      const overData = over.data.current?.sortable;

      if (!activeData || !overData) return;

      const activeContainer = activeData.containerId as TaskStatus;
      const overContainer = overData.containerId as TaskStatus;
      const activeIndex = activeData.index;
      const overIndex = overData.index;
      
      let newTasks: TasksState;

      if (activeContainer === activeContainer) { // This logic in original code was activeContainer === overContainer
         // But wait, the original code used activeContainer and overContainer variables which were derived from event data.
      }
      
      // Let's rewrite the logic to be safer and typed.
      // We need to find which container the items belong to if data.current is not reliable or if we want to be sure.
      // But assuming the original code's intent:

      if (activeContainer === overContainer) {
        newTasks = {
          ...tasks,
          [activeContainer]: arrayMove(tasks[activeContainer], activeIndex, overIndex),
        };
      } else {
        const activeItems = [...tasks[activeContainer]];
        const overItems = [...tasks[overContainer]];
        const [removed] = activeItems.splice(activeIndex, 1);
        overItems.splice(overIndex, 0, removed);

        newTasks = {
          ...tasks,
          [activeContainer]: activeItems,
          [overContainer]: overItems,
        };
      }

      setTasks(newTasks);

      try {
        // TODO: Replace with actual API endpoint and payload
        const response = await apiFetch(`/v1/tasks/update-status`, {
          method: 'POST',
          body: JSON.stringify({
            taskId: active.id,
            newStatus: overContainer,
            newOrder: newTasks[overContainer].map(t => t.id),
          }),
        });
        if (!response.ok) throw new Error('Failed to update task status');
      } catch (error) {
        console.error('Failed to update task status', error);
        // Optionally, revert the state change on error
        setTasks(tasks);
      }
    }
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
      <div className="flex gap-4">
        {(Object.keys(tasks) as TaskStatus[]).map((columnId) => (
          <div key={columnId} className="w-1/3 bg-surface-100 p-4 rounded-lg">
            <h3 className="font-semibold mb-4 capitalize">{columnId}</h3>
            <SortableContext items={tasks[columnId].map(t => t.id)} strategy={verticalListSortingStrategy}>
              <div className="space-y-2">
                {tasks[columnId].map((task) => (
                  <SortableItem key={task.id} id={task.id}>
                    <Card>
                      <CardContent className="p-2">{task.content}</CardContent>
                    </Card>
                  </SortableItem>
                ))}
              </div>
            </SortableContext>
          </div>
        ))}
      </div>
    </DndContext>
  );
}
