import { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from './Dialog';
import { Button } from './Button';
import { Input } from './Input';

interface FocusSessionModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function FocusSessionModal({ open, onOpenChange }: FocusSessionModalProps) {
  const [duration, setDuration] = useState(25);
  const [task, setTask] = useState('');

  const handleStart = () => {
    console.log(`Starting focus session for ${duration} minutes on "${task}"`);
    // Ideally call a context/hook to start timer
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
