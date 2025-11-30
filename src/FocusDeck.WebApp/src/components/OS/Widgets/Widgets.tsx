import React, { useState, useEffect } from 'react';

// --- Types ---

// --- Widgets ---

export const FocusTimerWidget: React.FC = () => {
  const [timeLeft, setTimeLeft] = useState(25 * 60);
  const [isActive, setIsActive] = useState(false);
  const totalTime = 25 * 60;

  useEffect(() => {
    let interval: any = null;
    if (isActive && timeLeft > 0) {
      interval = setInterval(() => {
        setTimeLeft(timeLeft - 1);
      }, 1000);
    } else if (timeLeft === 0) {
      setIsActive(false);
    }
    return () => clearInterval(interval);
  }, [isActive, timeLeft]);

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const progress = ((totalTime - timeLeft) / totalTime) * 100;
  const radius = 60;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - (progress / 100) * circumference;

  return (
    <div className="flex flex-col items-center justify-center h-full bg-ink text-white p-4 relative overflow-hidden">
      <div className="relative flex items-center justify-center">
         {/* Ring Background */}
         <svg className="w-40 h-40 transform -rotate-90">
             <circle
                 cx="80" cy="80" r={radius}
                 fill="transparent"
                 stroke="currentColor"
                 strokeWidth="6"
                 className="text-gray-700"
             />
             <circle
                 cx="80" cy="80" r={radius}
                 fill="transparent"
                 stroke="currentColor"
                 strokeWidth="6"
                 strokeDasharray={circumference}
                 strokeDashoffset={strokeDashoffset}
                 strokeLinecap="round"
                 className="text-white transition-all duration-1000 ease-linear"
             />
         </svg>
         <div className="absolute inset-0 flex items-center justify-center">
            <div className="text-3xl font-mono font-bold">{formatTime(timeLeft)}</div>
         </div>
      </div>

      <div className="flex gap-4 mt-4">
        <button onClick={() => setIsActive(!isActive)} className="w-10 h-10 rounded-full bg-white text-ink flex items-center justify-center hover:bg-gray-200 transition-colors shadow-lg">
          <i className={`fa-solid ${isActive ? 'fa-pause' : 'fa-play'}`}></i>
        </button>
        <button onClick={() => { setIsActive(false); setTimeLeft(25 * 60); }} className="w-10 h-10 rounded-full bg-white/20 text-white flex items-center justify-center hover:bg-white/30 transition-colors">
          <i className="fa-solid fa-rotate-right"></i>
        </button>
      </div>
      <div className="absolute bottom-2 right-2 text-[10px] opacity-50">3/4 Sessions</div>
    </div>
  );
};

export const TaskListWidget: React.FC = () => {
  const [tasks, setTasks] = useState([
    { id: 1, text: 'Review PR #102', done: false },
    { id: 2, text: 'Email Client', done: true },
    { id: 3, text: 'Update Docs', done: false },
  ]);

  return (
    <div className="flex flex-col h-full bg-surface p-4">
      <h3 className="text-xs font-bold uppercase tracking-wider text-gray-500 mb-3">My Day</h3>
      <div className="flex-1 overflow-y-auto space-y-2">
        {tasks.map(task => (
          <div key={task.id} className="flex items-center gap-2 group cursor-pointer" onClick={() => setTasks(tasks.map(t => t.id === task.id ? { ...t, done: !t.done } : t))}>
            <div className={`w-4 h-4 rounded border flex items-center justify-center transition-colors ${task.done ? 'bg-accent-green border-accent-green text-white' : 'border-gray-300 dark:border-gray-600'}`}>
              {task.done && <i className="fa-solid fa-check text-[10px]"></i>}
            </div>
            <span className={`text-sm ${task.done ? 'line-through text-gray-400' : 'text-ink'}`}>{task.text}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export const CalendarWidget: React.FC = () => {
  return (
    <div className="flex flex-col h-full bg-surface p-4">
      <h3 className="text-xs font-bold uppercase tracking-wider text-gray-500 mb-3">Schedule</h3>
      <div className="space-y-4 relative">
        <div className="absolute left-[5px] top-2 bottom-2 w-0.5 bg-gray-200 dark:bg-gray-700"></div>
        {[
          { time: '10:00', title: 'Team Standup', color: 'bg-blue-500' },
          { time: '13:00', title: 'Design Review', color: 'bg-purple-500' },
          { time: '15:30', title: 'Focus Time', color: 'bg-green-500' }
        ].map((event, i) => (
          <div key={i} className="relative pl-4">
            <div className={`absolute left-0 top-1.5 w-3 h-3 rounded-full border-2 border-surface ${event.color}`}></div>
            <div className="text-xs font-mono text-gray-400">{event.time}</div>
            <div className="text-sm font-medium text-ink">{event.title}</div>
          </div>
        ))}
      </div>
    </div>
  );
};

export const WeatherWidget: React.FC = () => {
  return (
    <div className="flex flex-col items-center justify-center h-full bg-gradient-to-br from-blue-400 to-blue-600 text-white p-4">
      <i className="fa-solid fa-cloud-sun text-4xl mb-2 animate-pulse"></i>
      <div className="text-2xl font-bold">72°F</div>
      <div className="text-xs opacity-80">Partly Cloudy</div>
      <div className="text-xs font-mono mt-2 opacity-60">10:42 AM</div>
    </div>
  );
};

export const SpotifyWidget: React.FC = () => {
  const [isPlaying, setIsPlaying] = useState(false);

  return (
    <div className="flex items-center h-full bg-gradient-to-br from-gray-900 to-black text-white p-0 overflow-hidden relative group">
      <div className="relative h-full w-24 shrink-0">
         <img src="https://picsum.photos/200" alt="Album Art" className="w-full h-full object-cover opacity-80 group-hover:opacity-60 transition-opacity" />
         <div className="absolute inset-0 bg-black/20 group-hover:bg-transparent transition-colors"></div>
      </div>

      <div className="flex-1 p-4 z-10 flex flex-col justify-center">
        <div className="flex items-center gap-2 mb-1">
             <i className="fa-brands fa-spotify text-green-500 text-lg"></i>
             <span className="text-xs text-green-500 font-bold uppercase tracking-wide">Now Playing</span>
        </div>
        <div className="font-bold text-lg truncate leading-tight">Lo-Fi Beats to Study To</div>
        <div className="text-xs text-gray-400 truncate mb-3">Lofi Girl • 2024 Mix</div>

        {/* Progress Bar */}
        <div className="w-full h-1 bg-gray-700 rounded-full mb-3 overflow-hidden">
            <div className="h-full bg-white w-1/3 rounded-full"></div>
        </div>

        {/* Controls */}
        <div className="flex items-center gap-6 text-xl">
          <button className="text-gray-400 hover:text-white transition-colors"><i className="fa-solid fa-backward-step"></i></button>
          <button
            onClick={() => setIsPlaying(!isPlaying)}
            className="w-8 h-8 rounded-full bg-white text-black flex items-center justify-center hover:scale-110 transition-transform"
          >
            <i className={`fa-solid ${isPlaying ? 'fa-pause' : 'fa-play'} text-xs`}></i>
          </button>
          <button className="text-gray-400 hover:text-white transition-colors"><i className="fa-solid fa-forward-step"></i></button>
        </div>
      </div>

      {/* Background Blur Effect */}
      <div className="absolute inset-0 bg-gradient-to-r from-black via-black/80 to-transparent pointer-events-none"></div>
    </div>
  );
};

export const HabitTrackerWidget: React.FC = () => {
  const [habits, setHabits] = useState([
    { id: 1, icon: 'fa-glass-water', done: false },
    { id: 2, icon: 'fa-book', done: true },
    { id: 3, icon: 'fa-person-running', done: false },
    { id: 4, icon: 'fa-moon', done: false },
  ]);

  return (
    <div className="flex flex-col h-full bg-surface p-4 justify-center">
      <h3 className="text-xs font-bold uppercase tracking-wider text-gray-500 mb-3">Daily Habits</h3>
      <div className="flex justify-between px-2">
        {habits.map(habit => (
          <button 
            key={habit.id}
            onClick={() => setHabits(habits.map(h => h.id === habit.id ? { ...h, done: !h.done } : h))}
            className={`w-10 h-10 rounded-full flex items-center justify-center transition-all ${habit.done ? 'bg-green-500 text-white scale-110 shadow-md' : 'bg-subtle text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700'}`}
          >
            <i className={`fa-solid ${habit.icon}`}></i>
          </button>
        ))}
      </div>
    </div>
  );
};

export const QuickNoteWidget: React.FC = () => {
  return (
    <div className="flex flex-col h-full bg-yellow-100 dark:bg-yellow-900/20 p-4 relative">
      <div className="absolute top-0 right-0 w-0 h-0 border-t-[20px] border-r-[20px] border-t-white dark:border-t-gray-900 border-r-transparent shadow-sm"></div>
      <textarea className="w-full h-full bg-transparent resize-none outline-none text-sm text-gray-800 dark:text-yellow-100 placeholder-yellow-700/50" placeholder="Scratchpad..."></textarea>
    </div>
  );
};

export const CourseProgressWidget: React.FC = () => {
  return (
    <div className="flex flex-col h-full bg-surface p-4 justify-center space-y-3">
      <div>
        <div className="flex justify-between text-xs mb-1">
          <span className="font-bold text-ink">CS101</span>
          <span className="text-gray-500">45%</span>
        </div>
        <div className="h-1.5 bg-subtle rounded-full overflow-hidden">
          <div className="h-full bg-blue-500 w-[45%]"></div>
        </div>
      </div>
      <div>
        <div className="flex justify-between text-xs mb-1">
          <span className="font-bold text-ink">MATH202</span>
          <span className="text-gray-500">70%</span>
        </div>
        <div className="h-1.5 bg-subtle rounded-full overflow-hidden">
          <div className="h-full bg-purple-500 w-[70%]"></div>
        </div>
      </div>
    </div>
  );
};

export const QuoteWidget: React.FC = () => {
  return (
    <div className="flex flex-col items-center justify-center h-full bg-surface p-6 text-center border-l-4 border-accent-yellow">
      <p className="font-serif italic text-ink text-lg mb-2">"The only way to do great work is to love what you do."</p>
      <p className="text-xs font-bold text-gray-500 uppercase tracking-widest">— Steve Jobs</p>
    </div>
  );
};

export const RecentFilesWidget: React.FC = () => {
  return (
    <div className="flex flex-col h-full bg-surface p-4">
      <h3 className="text-xs font-bold uppercase tracking-wider text-gray-500 mb-3">Recent</h3>
      <div className="space-y-2">
        <div className="flex items-center gap-2 text-sm text-ink hover:text-blue-500 cursor-pointer transition-colors">
          <i className="fa-solid fa-note-sticky text-yellow-500"></i> Q4_Strategy
        </div>
        <div className="flex items-center gap-2 text-sm text-ink hover:text-blue-500 cursor-pointer transition-colors">
          <i className="fa-solid fa-layer-group text-blue-500"></i> Study Set 4
        </div>
        <div className="flex items-center gap-2 text-sm text-ink hover:text-blue-500 cursor-pointer transition-colors">
          <i className="fa-solid fa-list-check text-teal-500"></i> Launch Plan
        </div>
      </div>
    </div>
  );
};
