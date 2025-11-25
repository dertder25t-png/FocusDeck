import { useState } from 'react';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Card, CardContent } from '../components/Card';

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
    todo: ['Task 1', 'Task 2'],
    inProgress: ['Task 3'],
    done: ['Task 4'],
  });

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  function handleDragEnd(event) {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const activeContainer = active.data.current.sortable.containerId;
      const overContainer = over.data.current.sortable.containerId;

      if (activeContainer === overContainer) {
        setTasks((prev) => ({
          ...prev,
          [activeContainer]: arrayMove(prev[activeContainer], active.data.current.sortable.index, over.data.current.sortable.index),
        }));
      } else {
        const activeItems = tasks[activeContainer];
        const overItems = tasks[overContainer];
        const activeIndex = active.data.current.sortable.index;
        const overIndex = over.data.current.sortable.index;

        const [removed] = activeItems.splice(activeIndex, 1);
        overItems.splice(overIndex, 0, removed);

        setTasks((prev) => ({
          ...prev,
          [activeContainer]: [...activeItems],
          [overContainer]: [...overItems],
        }));
      }
    }
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
      <div className="flex gap-4">
        {Object.keys(tasks).map((columnId) => (
          <div key={columnId} className="w-1/3 bg-surface-100 p-4 rounded-lg">
            <h3 className="font-semibold mb-4 capitalize">{columnId}</h3>
            <SortableContext items={tasks[columnId]} strategy={verticalListSortingStrategy}>
              <div className="space-y-2">
                {tasks[columnId].map((task) => (
                  <SortableItem key={task} id={task}>
                    <Card>
                      <CardContent className="p-2">{task}</CardContent>
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
