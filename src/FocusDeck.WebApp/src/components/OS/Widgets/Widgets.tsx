import React, { useState, useEffect } from 'react';
import { useDashboard } from '../../../hooks/useDashboard';
import { useFocus } from '../../../contexts/FocusContext';
import { apiFetch } from '../../../services/api';

// --- Types ---

// --- Widgets ---

export const FocusTimerWidget: React.FC = () => {
  const { isActive, isPaused, timeLeft, totalTime, startSession, pauseSession, resumeSession, stopSession, formatTime } = useFocus();

  const handleToggle = () => {
      if (!isActive) {
          startSession(25, "Quick Focus");
      } else if (isPaused) {
          resumeSession();
      } else {
          pauseSession();
      }
  };

  const progress = totalTime > 0 ? ((totalTime - timeLeft) / totalTime) * 100 : 0;
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
        <button onClick={handleToggle} className="w-10 h-10 rounded-full bg-white text-ink flex items-center justify-center hover:bg-gray-200 transition-colors shadow-lg">
          <i className={`fa-solid ${isActive && !isPaused ? 'fa-pause' : 'fa-play'}`}></i>
        </button>
        <button onClick={stopSession} className="w-10 h-10 rounded-full bg-white/20 text-white flex items-center justify-center hover:bg-white/30 transition-colors">
          <i className="fa-solid fa-rotate-right"></i>
        </button>
      </div>
      <div className="absolute bottom-2 right-2 text-[10px] opacity-50">3/4 Sessions</div>
    </div>
  );
};

export const TaskListWidget: React.FC = () => {
  const { data } = useDashboard();
  // Map API data to widget format
  const tasks = (data?.tasks || []).map(t => ({
    id: t.id,
    text: t.title,
    done: t.isCompleted
  }));

  return (
    <div className="flex flex-col h-full bg-surface p-4">
      <h3 className="text-xs font-bold uppercase tracking-wider text-gray-500 mb-3">My Day</h3>
      <div className="flex-1 overflow-y-auto space-y-2">
        {tasks.length === 0 && <div className="text-xs text-gray-400 italic">No tasks for today</div>}
        {tasks.map(task => (
          <div key={task.id} className="flex items-center gap-2 group cursor-pointer">
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
  const { data } = useDashboard();

  const events = (data?.events || []).map(e => ({
    time: new Date(e.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    title: e.title,
    color: e.color || 'bg-blue-500'
  }));

  return (
    <div className="flex flex-col h-full bg-surface p-4">
      <h3 className="text-xs font-bold uppercase tracking-wider text-gray-500 mb-3">Schedule</h3>
      <div className="space-y-4 relative">
        <div className="absolute left-[5px] top-2 bottom-2 w-0.5 bg-gray-200 dark:bg-gray-700"></div>
        {events.length === 0 && <div className="text-xs text-gray-400 italic pl-4">No events scheduled</div>}
        {events.map((event, i) => (
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

interface SpotifyState {
    track: string;
    artist: string;
    album: string;
    isPlaying: boolean;
    progressMs: number;
    durationMs: number;
    uri: string;
    imageUrl?: string;
}

interface SpotifyPlaylist {
    id: string;
    name: string;
    imageUrl?: string;
    uri?: string;
}

export const SpotifyWidget: React.FC = () => {
  const [playerState, setPlayerState] = useState<SpotifyState | null>(null);
  const [playlists, setPlaylists] = useState<SpotifyPlaylist[]>([]);
  const [showPlaylists, setShowPlaylists] = useState(false);
  const [loading, setLoading] = useState(false);

  const fetchState = async () => {
      try {
          const res = await apiFetch('/v1/integrations/spotify/player');
          if (res.ok) {
              const data = await res.json();
              setPlayerState(data);
          } else {
              setPlayerState(null);
          }
      } catch {
          setPlayerState(null);
      }
  };

  const togglePlayback = async () => {
      if (!playerState) return;
      const action = playerState.isPlaying ? 'pause' : 'play';
      await apiFetch(`/v1/integrations/spotify/${action}`, { method: 'POST' });
      setTimeout(fetchState, 500); // refresh after delay
  };

  const loadPlaylists = async () => {
      setLoading(true);
      try {
          const res = await apiFetch('/v1/integrations/spotify/playlists');
          if (res.ok) {
              const data = await res.json();
              setPlaylists(data);
          }
      } catch (e) {
          console.error(e);
      } finally {
          setLoading(false);
      }
  };

  const playPlaylist = async (id: string) => {
      await apiFetch(`/v1/integrations/spotify/playlists/${id}/play`, { method: 'POST' });
      setShowPlaylists(false);
      setTimeout(fetchState, 1000);
  };

  useEffect(() => {
      fetchState();
      const interval = setInterval(fetchState, 5000); // Poll every 5s
      return () => clearInterval(interval);
  }, []);

  const progress = playerState && playerState.durationMs > 0
      ? (playerState.progressMs / playerState.durationMs) * 100
      : 0;

  if (showPlaylists) {
      return (
          <div className="flex flex-col h-full bg-gray-900 text-white p-4 relative overflow-hidden">
              <div className="flex justify-between items-center mb-2">
                  <h3 className="font-bold text-sm">Select Playlist</h3>
                  <button onClick={() => setShowPlaylists(false)} className="text-gray-400 hover:text-white">
                      <i className="fa-solid fa-xmark"></i>
                  </button>
              </div>
              <div className="flex-1 overflow-y-auto space-y-2 pr-1 custom-scrollbar">
                  {loading && <div className="text-xs text-gray-500 text-center py-4">Loading playlists...</div>}
                  {playlists.map(p => (
                      <button
                          key={p.id}
                          onClick={() => playPlaylist(p.id)}
                          className="w-full flex items-center gap-3 p-2 hover:bg-white/10 rounded-md transition-colors text-left group"
                      >
                          <div className="w-10 h-10 bg-gray-800 rounded overflow-hidden shrink-0">
                              {p.imageUrl ? (
                                  <img src={p.imageUrl} alt={p.name} className="w-full h-full object-cover" />
                              ) : (
                                  <div className="w-full h-full flex items-center justify-center text-gray-600">
                                      <i className="fa-solid fa-music"></i>
                                  </div>
                              )}
                          </div>
                          <span className="text-sm font-medium truncate flex-1">{p.name}</span>
                          <i className="fa-solid fa-play opacity-0 group-hover:opacity-100 text-green-500"></i>
                      </button>
                  ))}
              </div>
          </div>
      );
  }

  return (
    <div className="flex items-center h-full bg-gradient-to-br from-gray-900 to-black text-white p-0 overflow-hidden relative group">
      <div className="relative h-full w-24 shrink-0">
         {playerState?.imageUrl ? (
             <img src={playerState.imageUrl} alt="Album Art" className="w-full h-full object-cover opacity-80 group-hover:opacity-60 transition-opacity" />
         ) : (
             <div className="w-full h-full bg-gray-800 flex items-center justify-center">
                 <i className="fa-brands fa-spotify text-4xl text-gray-600"></i>
             </div>
         )}
         <div className="absolute inset-0 bg-black/20 group-hover:bg-transparent transition-colors"></div>
      </div>

      <div className="flex-1 p-4 z-10 flex flex-col justify-center min-w-0">
        <div className="flex items-center justify-between mb-1">
             <div className="flex items-center gap-2">
                 <i className="fa-brands fa-spotify text-green-500 text-lg"></i>
                 <span className="text-xs text-green-500 font-bold uppercase tracking-wide">
                     {playerState ? 'Now Playing' : 'Spotify'}
                 </span>
             </div>
             <button
                onClick={() => { setShowPlaylists(true); loadPlaylists(); }}
                className="text-xs bg-white/10 hover:bg-white/20 px-2 py-1 rounded transition-colors"
                title="Open Library"
             >
                 <i className="fa-solid fa-list-ul"></i>
             </button>
        </div>

        <div className="font-bold text-lg truncate leading-tight">
            {playerState ? playerState.track : 'Not Connected / Idle'}
        </div>
        <div className="text-xs text-gray-400 truncate mb-3">
            {playerState ? `${playerState.artist} • ${playerState.album}` : 'Connect in Settings'}
        </div>

        {/* Progress Bar */}
        <div className="w-full h-1 bg-gray-700 rounded-full mb-3 overflow-hidden">
            <div className="h-full bg-white rounded-full transition-all duration-1000 ease-linear" style={{ width: `${progress}%` }}></div>
        </div>

        {/* Controls */}
        <div className="flex items-center gap-6 text-xl">
          <button className="text-gray-400 hover:text-white transition-colors"><i className="fa-solid fa-backward-step"></i></button>
          <button
            onClick={togglePlayback}
            disabled={!playerState}
            className={`w-8 h-8 rounded-full bg-white text-black flex items-center justify-center hover:scale-110 transition-transform ${!playerState ? 'opacity-50 cursor-not-allowed' : ''}`}
          >
            <i className={`fa-solid ${playerState?.isPlaying ? 'fa-pause' : 'fa-play'} text-xs`}></i>
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
