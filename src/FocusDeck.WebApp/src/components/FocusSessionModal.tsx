import { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from './Dialog';
import { Button } from './Button';
import { Input } from './Input';
import { useFocus } from '../contexts/FocusContext';

interface FocusSessionModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function FocusSessionModal({ open, onOpenChange }: FocusSessionModalProps) {
  const [duration, setDuration] = useState(25);
  const [task, setTask] = useState('');
  const { startSession } = useFocus();

  const handleStart = () => {
    startSession(duration, task);
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Start Focus Session</DialogTitle>
          <DialogDescription>
            Set your duration and intent.
          </DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid grid-cols-4 items-center gap-4">
            <label htmlFor="duration" className="text-right text-sm font-medium">
              Duration
            </label>
            <Input
              id="duration"
              type="number"
              value={duration}
              onChange={(e) => setDuration(Number(e.target.value))}
              className="col-span-3"
            />
          </div>
          <div className="grid grid-cols-4 items-center gap-4">
            <label htmlFor="task" className="text-right text-sm font-medium">
              Task
            </label>
            <Input
              id="task"
              value={task}
              onChange={(e) => setTask(e.target.value)}
              placeholder="What are you working on?"
              className="col-span-3"
            />
          </div>
        </div>
        <DialogFooter>
          <Button onClick={handleStart}>Start Focus</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
