import React from 'react';
import { KanbanApp } from '../apps/KanbanApp';

export function KanbanPage() {
  // Since KanbanApp is the robust implementation with real data hooks,
  // we just wrap it here for the standalone page route.
  return (
    <div className="h-full w-full p-4">
      <KanbanApp />
    </div>
  );
}
