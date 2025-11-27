import { useState } from 'react';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Card, CardContent } from '../components/Card';
import { apiFetch } from '../lib/api';

function SortableItem({ id, children }) {
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

export function KanbanPage() {
  const [tasks, setTasks] = useState({
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

  async function handleDragEnd(event) {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const activeContainer = active.data.current.sortable.containerId;
      const overContainer = over.data.current.sortable.containerId;
      const activeIndex = active.data.current.sortable.index;
      const overIndex = over.data.current.sortable.index;
      let newTasks;

      if (activeContainer === overContainer) {
        newTasks = {
          ...tasks,
          [activeContainer]: arrayMove(tasks[activeContainer], activeIndex, overIndex),
        };
      } else {
        const activeItems = Array.from(tasks[activeContainer]);
        const overItems = Array.from(tasks[overContainer]);
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
        await apiFetch(`/v1/tasks/update-status`, {
          method: 'POST',
          body: JSON.stringify({
            taskId: active.id,
            newStatus: overContainer,
            newOrder: newTasks[overContainer].map(t => t.id),
          }),
        });
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
        {Object.keys(tasks).map((columnId) => (
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
