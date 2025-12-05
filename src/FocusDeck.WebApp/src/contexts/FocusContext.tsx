import React, { createContext, useContext, useState, useEffect, useRef } from 'react';

interface FocusContextType {
  isActive: boolean;
  isPaused: boolean;
  timeLeft: number;
  totalTime: number;
  task: string | null;
  startSession: (durationMinutes: number, task?: string) => void;
  pauseSession: () => void;
  resumeSession: () => void;
  stopSession: () => void;
  formatTime: (seconds: number) => string;
}

const FocusContext = createContext<FocusContextType | undefined>(undefined);

export const FocusProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isActive, setIsActive] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [timeLeft, setTimeLeft] = useState(25 * 60);
  const [totalTime, setTotalTime] = useState(25 * 60);
  const [task, setTask] = useState<string | null>(null);

  const timerRef = useRef<NodeJS.Timeout | null>(null);

  const startSession = (durationMinutes: number, taskTitle?: string) => {
    const seconds = durationMinutes * 60;
    setTotalTime(seconds);
    setTimeLeft(seconds);
    setTask(taskTitle || null);
    setIsActive(true);
    setIsPaused(false);
  };

  const pauseSession = () => {
    setIsPaused(true);
  };

  const resumeSession = () => {
    setIsPaused(false);
  };

  const stopSession = () => {
    setIsActive(false);
    setIsPaused(false);
    setTimeLeft(totalTime);
  };

  useEffect(() => {
    if (isActive && !isPaused && timeLeft > 0) {
      timerRef.current = setInterval(() => {
        setTimeLeft((prev) => {
          if (prev <= 1) {
            clearInterval(timerRef.current!);
            setIsActive(false);
            // Play sound or notification here
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } else {
      if (timerRef.current) {
        clearInterval(timerRef.current);
      }
    }

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [isActive, isPaused]); // Removed timeLeft dependency to prevent re-setup every second, rely on state updater

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  return (
    <FocusContext.Provider value={{
      isActive, isPaused, timeLeft, totalTime, task,
      startSession, pauseSession, resumeSession, stopSession,
      formatTime
    }}>
      {children}
    </FocusContext.Provider>
  );
};

export const useFocus = () => {
  const context = useContext(FocusContext);
  if (!context) throw new Error('useFocus must be used within a FocusProvider');
  return context;
};
