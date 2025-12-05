import React, { useState, useEffect } from 'react';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { apiFetch } from '../../../services/api';

interface SpotifyState {
    track: string;
    artist: string;
    isPlaying: boolean;
    imageUrl?: string;
}

interface SpotifyPlaylist {
    id: string;
    name: string;
    imageUrl?: string;
}

export const SpotifyChip: React.FC = () => {
    const [state, setState] = useState<SpotifyState | null>(null);
    const [playlists, setPlaylists] = useState<SpotifyPlaylist[]>([]);
    const [loading, setLoading] = useState(false);

    const fetchState = async () => {
        try {
            const res = await apiFetch('/v1/integrations/spotify/player');
            if (res.ok) {
                const data = await res.json();
                setState(data);
            } else {
                setState(null);
            }
        } catch {
            setState(null);
        }
    };

    const togglePlayback = async (e: React.MouseEvent) => {
        e.stopPropagation();
        if (!state) return;
        const action = state.isPlaying ? 'pause' : 'play';
        await apiFetch(`/v1/integrations/spotify/${action}`, { method: 'POST' });
        setTimeout(fetchState, 500);
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
        setTimeout(fetchState, 1000);
    };

    useEffect(() => {
        fetchState();
        const interval = setInterval(fetchState, 10000); // Poll every 10s for chip
        return () => clearInterval(interval);
    }, []);

    return (
        <DropdownMenu.Root onOpenChange={(open) => {
            if (open) {
                fetchState(); // Refresh immediately on open
                loadPlaylists();
            }
        }}>
            <DropdownMenu.Trigger asChild>
                <div className="smart-chip hidden md:flex items-center gap-2 px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-bold cursor-pointer border border-green-200 hover:bg-green-200 transition-colors">
                    <i className={`fa-brands fa-spotify ${state?.isPlaying ? 'animate-spin-slow' : ''}`}></i>
                    <span className="truncate max-w-[100px]">{state ? state.track : 'Spotify'}</span>
                </div>
            </DropdownMenu.Trigger>

            <DropdownMenu.Portal>
                <DropdownMenu.Content
                    className="z-[100] min-w-[220px] bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-2 text-ink dark:text-white"
                    sideOffset={5}
                >
                    {state ? (
                        <div className="flex items-center gap-3 mb-2 p-2 border-b border-gray-100 dark:border-gray-700">
                            {state.imageUrl && <img src={state.imageUrl} alt="Art" className="w-10 h-10 rounded object-cover" />}
                            <div className="flex-1 min-w-0">
                                <div className="text-sm font-bold truncate">{state.track}</div>
                                <div className="text-xs text-gray-500 truncate">{state.artist}</div>
                            </div>
                            <button onClick={togglePlayback} className="w-8 h-8 rounded-full bg-green-500 text-white flex items-center justify-center hover:scale-105 transition-transform">
                                <i className={`fa-solid ${state.isPlaying ? 'fa-pause' : 'fa-play'} text-xs`}></i>
                            </button>
                        </div>
                    ) : (
                        <div className="text-xs text-gray-500 p-2 text-center">Not Playing</div>
                    )}

                    <DropdownMenu.Label className="text-xs font-bold text-gray-400 px-2 py-1 uppercase tracking-wider">
                        Playlists
                    </DropdownMenu.Label>

                    <div className="max-h-[200px] overflow-y-auto space-y-1 custom-scrollbar">
                        {loading && <div className="text-xs text-center py-2 text-gray-400">Loading...</div>}
                        {playlists.map(p => (
                            <DropdownMenu.Item
                                key={p.id}
                                className="flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-green-50 dark:hover:bg-gray-700 cursor-pointer outline-none"
                                onClick={() => playPlaylist(p.id)}
                            >
                                <i className="fa-solid fa-music text-gray-400 text-xs"></i>
                                <span className="truncate">{p.name}</span>
                            </DropdownMenu.Item>
                        ))}
                    </div>
                </DropdownMenu.Content>
            </DropdownMenu.Portal>
        </DropdownMenu.Root>
    );
};
