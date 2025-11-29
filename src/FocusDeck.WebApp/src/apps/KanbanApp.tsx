import React, { useState } from 'react';
import { DndContext, DragOverlay, closestCorners, KeyboardSensor, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import type { DragStartEvent, DragOverEvent, DragEndEvent } from '@dnd-kit/core';
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

// --- Types ---
type LaneId = 'focus' | 'social' | 'health' | 'admin';

interface Task {
  id: string;
  title: string;
  time?: string;
}

interface LaneData {
  id: LaneId;
  title: string;
  color: string;
  tasks: Task[];
}

// --- Mock Data ---
const INITIAL_LANES: LaneData[] = [
  {
    id: 'focus',
    title: 'Focus',
    color: 'bg-blue-500',
    tasks: [
      { id: 't1', title: 'Deep Work: Coding', time: '09:00 AM' },
      { id: 't2', title: 'Review PRs', time: '11:00 AM' }
    ]
  },
  {
    id: 'social',
    title: 'Social',
    color: 'bg-pink-500',
    tasks: [
      { id: 't3', title: 'Lunch with Team', time: '12:30 PM' }
    ]
  },
  {
    id: 'health',
    title: 'Health',
    color: 'bg-green-500',
    tasks: [
      { id: 't4', title: 'Morning Run', time: '07:00 AM' },
      { id: 't5', title: 'Meditation', time: '08:00 PM' }
    ]
  },
  {
    id: 'admin',
    title: 'Admin',
    color: 'bg-gray-500',
    tasks: [
      { id: 't6', title: 'Pay Bills', time: '06:00 PM' }
    ]
  }
];

// --- Components ---

const SortableTask = ({ task }: { task: Task }) => {
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
    >
      {/* Timeline Line */}
      <div className="absolute left-[5px] top-3 bottom-[-16px] w-0.5 bg-gray-200 dark:bg-gray-700 group-last:hidden"></div>
      {/* Timeline Dot */}
      <div className="absolute left-0 top-3 w-3 h-3 rounded-full bg-blue-500 border-2 border-white dark:border-gray-900 z-10 shadow-sm"></div>

      <div className="bg-white dark:bg-gray-800 p-3 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 hover:shadow-md transition-shadow">
        <div className="flex justify-between items-start mb-1">
             <span className="text-xs font-mono text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded">{task.time || 'Anytime'}</span>
        </div>
        <div className="font-medium text-gray-800 dark:text-gray-200">{task.title}</div>
      </div>
    </div>
  );
};

const Lane = ({ lane }: { lane: LaneData }) => {
  const { setNodeRef } = useDroppable({ id: lane.id });

  return (
    <div ref={setNodeRef} className="flex-1 min-w-[280px] flex flex-col h-full bg-gray-50 dark:bg-gray-900/50 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div className={`p-3 border-b border-gray-200 dark:border-gray-700 flex items-center gap-2 bg-white dark:bg-gray-800/50`}>
        <div className={`w-3 h-3 rounded-full ${lane.color}`}></div>
        <h3 className="font-bold text-gray-700 dark:text-gray-200">{lane.title}</h3>
        <span className="ml-auto text-xs font-medium bg-gray-200 dark:bg-gray-800 px-2 py-0.5 rounded-full text-gray-600 dark:text-gray-400">{lane.tasks.length}</span>
      </div>
      
      {/* Task List */}
      <div className="flex-1 p-4 overflow-y-auto">
        <SortableContext items={lane.tasks.map(t => t.id)} strategy={verticalListSortingStrategy}>
          {lane.tasks.map(task => (
            <SortableTask key={task.id} task={task} />
          ))}
        </SortableContext>
        {lane.tasks.length === 0 && (
            <div className="text-center py-8 text-gray-400 dark:text-gray-600 text-sm italic">No tasks yet</div>
        )}
      </div>
    </div>
  );
};

// Helper for Droppable container
import { useDroppable } from '@dnd-kit/core';

export const KanbanApp: React.FC = () => {
  const [lanes, setLanes] = useState<LaneData[]>(INITIAL_LANES);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<LaneId>('focus'); // For Mobile View

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const findLane = (id: string) => {
    if (lanes.find(l => l.id === id)) return lanes.find(l => l.id === id);
    return lanes.find(l => l.tasks.some(t => t.id === id));
  };

  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(event.active.id as string);
  };

  const handleDragOver = (event: DragOverEvent) => {
    const { active, over } = event;
    if (!over) return;

    const activeId = active.id;
    const overId = over.id;

    const activeLane = findLane(activeId as string);
    const overLane = findLane(overId as string);

    if (!activeLane || !overLane || activeLane === overLane) return;

    setLanes(prev => {
      const activeItems = activeLane.tasks;
      const overItems = overLane.tasks;
      const activeIndex = activeItems.findIndex(t => t.id === activeId);
      const overIndex = overItems.findIndex(t => t.id === overId);

      let newIndex;
      if (overId in lanes.map(l => l.id)) {
        newIndex = overItems.length + 1;
      } else {
        const isBelowOverItem = over && active.rect.current.translated && active.rect.current.translated.top > over.rect.top + over.rect.height;
        const modifier = isBelowOverItem ? 1 : 0;
        newIndex = overIndex >= 0 ? overIndex + modifier : overItems.length + 1;
      }

      return prev.map(l => {
        if (l.id === activeLane.id) {
          return { ...l, tasks: l.tasks.filter(t => t.id !== activeId) };
        } else if (l.id === overLane.id) {
          return {
            ...l,
            tasks: [
              ...l.tasks.slice(0, newIndex),
              activeItems[activeIndex],
              ...l.tasks.slice(newIndex, l.tasks.length)
            ]
          };
        }
        return l;
      });
    });
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    const activeId = active.id;
    const overId = over?.id;

    if (!overId) {
      setActiveId(null);
      return;
    }

    const activeLane = findLane(activeId as string);
    const overLane = findLane(overId as string);

    if (activeLane && overLane && activeLane === overLane) {
      const activeIndex = activeLane.tasks.findIndex(t => t.id === activeId);
      const overIndex = overLane.tasks.findIndex(t => t.id === overId);

      if (activeIndex !== overIndex) {
        setLanes(prev => prev.map(l => {
          if (l.id === activeLane.id) {
            return { ...l, tasks: arrayMove(l.tasks, activeIndex, overIndex) };
          }
          return l;
        }));
      }
    }

    setActiveId(null);
  };

  return (
    <div className="h-full flex flex-col bg-white dark:bg-gray-900 transition-colors duration-300">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center shrink-0">
        <h2 className="text-xl font-bold text-gray-800 dark:text-white">Daily Lanes</h2>
        <div className="flex gap-2">
          <button className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"><i className="fa-solid fa-filter"></i></button>
          <button className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"><i className="fa-solid fa-plus"></i></button>
        </div>
      </div>

      {/* Mobile Tabs */}
      <div className="md:hidden flex border-b border-gray-200 dark:border-gray-700 overflow-x-auto no-scrollbar shrink-0">
        {lanes.map(lane => (
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
        onDragOver={handleDragOver} 
        onDragEnd={handleDragEnd}
      >
        <div className="flex-1 overflow-hidden p-4">
          {/* Desktop Grid */}
          <div className="hidden md:flex gap-4 h-full overflow-x-auto pb-2">
            {lanes.map(lane => (
              <Lane key={lane.id} lane={lane} />
            ))}
          </div>

          {/* Mobile View (Active Tab Only) */}
          <div className="md:hidden h-full">
            {lanes.filter(l => l.id === activeTab).map(lane => (
              <Lane key={lane.id} lane={lane} />
            ))}
          </div>
        </div>

        <DragOverlay>
          {activeId ? (
            <div className="bg-white dark:bg-gray-800 p-3 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 opacity-90 rotate-2 cursor-grabbing">
               <div className="text-xs text-gray-500 dark:text-gray-400 font-mono mb-1">...</div>
               <div className="font-medium text-gray-800 dark:text-gray-200">Moving Task...</div>
            </div>
          ) : null}
        </DragOverlay>
      </DndContext>
    </div>
  );
};
