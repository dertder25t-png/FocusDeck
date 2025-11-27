import { useState, useEffect } from 'react';
import { apiFetch } from '../lib/utils';
import { Card, CardContent } from '../components/Card';
import { Badge } from '../components/Badge';
import { EmptyState } from '../components/States';

interface BriefingItem {
    title: string;
    time: string;
    isUrgent: boolean;
}

interface MorningBriefing {
    greeting: string;
    horizonColor: string;
    horizonStatus: string;
    urgencyScore: number;
    upNext: BriefingItem[];
}

export function MorningBriefingPage() {
    const [briefing, setBriefing] = useState<MorningBriefing | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const offset = new Date().getTimezoneOffset(); // Returns minutes, positive for West
        apiFetch(`/v1/ambient/briefing?offset=${offset}`)
            .then(res => {
                if (res.ok) return res.json();
                throw new Error('Failed to fetch briefing');
            })
            .then(data => setBriefing(data))
            .catch(err => console.error(err))
            .finally(() => setIsLoading(false));
    }, []);

    if (isLoading) {
        return <div className="p-8 text-gray-400">Loading horizon...</div>;
    }

    if (!briefing) {
        return <EmptyState title="No briefing available" description="Could not connect to the ambient service." />;
    }

    // Dynamic theme based on Horizon Status
    const getThemeColor = () => {
        switch (briefing.horizonStatus) {
            case 'Critical': return 'border-red-500 shadow-red-500/20';
            case 'Busy': return 'border-orange-500 shadow-orange-500/20';
            case 'Calm': return 'border-blue-500 shadow-blue-500/20';
            default: return 'border-gray-800';
        }
    };

    const getBgGradient = () => {
        switch (briefing.horizonStatus) {
            case 'Critical': return 'bg-gradient-to-br from-red-950/30 to-surface-100';
            case 'Busy': return 'bg-gradient-to-br from-orange-950/30 to-surface-100';
            case 'Calm': return 'bg-gradient-to-br from-blue-950/30 to-surface-100';
            default: return 'bg-surface-100';
        }
    };

    return (
        <div className="max-w-4xl mx-auto py-8">
            <div className={`rounded-3xl border-2 p-8 ${getThemeColor()} ${getBgGradient()} transition-all duration-700`}>
                <div className="flex items-start justify-between mb-8">
                    <div>
                        <h1 className="text-4xl font-light tracking-tight text-white mb-2">{briefing.greeting}</h1>
                        <p className="text-gray-400">Here is your horizon for today.</p>
                    </div>
                    <div className="text-right">
                        <div className="text-3xl font-bold" style={{ color: briefing.horizonColor }}>
                            {Math.round(briefing.urgencyScore)}
                        </div>
                        <div className="text-xs uppercase tracking-widest text-gray-500 mt-1">Urgency Score</div>
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    {/* Visual Status */}
                    <div className="space-y-4">
                        <h3 className="text-sm uppercase tracking-wider text-gray-500 font-semibold">Horizon Status</h3>
                        <div className="flex items-center gap-4">
                            <div className="w-4 h-4 rounded-full shadow-[0_0_10px_currentColor]" style={{ color: briefing.horizonColor, backgroundColor: briefing.horizonColor }} />
                            <span className="text-2xl font-medium text-white">{briefing.horizonStatus}</span>
                        </div>
                        <p className="text-sm text-gray-400 leading-relaxed">
                            {briefing.horizonStatus === 'Critical' && "You have immediate deadlines. Focus on high-priority tasks."}
                            {briefing.horizonStatus === 'Busy' && "Your schedule is tightening. Plan your breaks carefully."}
                            {briefing.horizonStatus === 'Calm' && "Clear skies ahead. A good time for deep work or learning."}
                        </p>
                    </div>

                    {/* Up Next List */}
                    <div className="space-y-4">
                        <h3 className="text-sm uppercase tracking-wider text-gray-500 font-semibold">Up Next</h3>
                        {briefing.upNext.length === 0 ? (
                            <div className="text-gray-500 italic">Nothing immediate on the radar.</div>
                        ) : (
                            <div className="space-y-3">
                                {briefing.upNext.map((item, i) => (
                                    <div key={i} className={`flex items-center gap-4 p-3 rounded-lg border ${item.isUrgent ? 'bg-surface-200 border-red-500/50' : 'bg-surface-200/50 border-gray-800'}`}>
                                        <div className="text-sm font-mono text-gray-400 min-w-[60px]">{item.time}</div>
                                        <div className="font-medium text-white">{item.title}</div>
                                        {item.isUrgent && <Badge variant="danger">Urgent</Badge>}
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
