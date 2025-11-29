import React, { useState } from 'react';
import { WIDGET_REGISTRY, type WidgetDefinition, type WidgetType } from '../components/OS/Widgets/WidgetRegistry';

const DEFAULT_WIDGETS: WidgetDefinition[] = [
  { id: 'w1', type: 'weather', title: 'Weather', w: 1, h: 1 },
  { id: 'w2', type: 'focus-timer', title: 'Focus Timer', w: 2, h: 1 },
  { id: 'w3', type: 'tasks', title: 'Tasks', w: 1, h: 2 },
  { id: 'w4', type: 'calendar', title: 'Calendar', w: 1, h: 2 },
  { id: 'w5', type: 'spotify', title: 'Spotify', w: 2, h: 1 },
  { id: 'w6', type: 'habits', title: 'Habits', w: 2, h: 1 },
];

export const DashboardApp: React.FC = () => {
    const [widgets, setWidgets] = useState<WidgetDefinition[]>(DEFAULT_WIDGETS);
    const [showWidgetPicker, setShowWidgetPicker] = useState(false);

    const addWidget = (type: WidgetType) => {
        const def = WIDGET_REGISTRY[type];
        const newWidget: WidgetDefinition = {
            id: Date.now().toString(),
            type,
            title: def.title,
            w: def.defaultW,
            h: def.defaultH
        };
        setWidgets([...widgets, newWidget]);
        setShowWidgetPicker(false);
    };

    const removeWidget = (id: string) => {
        setWidgets(widgets.filter(w => w.id !== id));
    };

    return (
        <div className="flex-1 overflow-y-auto p-4 md:p-8 bg-paper dark:bg-gray-900 h-full relative">
            <div className="max-w-6xl mx-auto">
                <div className="flex justify-between items-center mb-6">
                    <h1 className="text-3xl font-display font-bold text-ink">Welcome Back.</h1>
                    <button onClick={() => setShowWidgetPicker(true)} className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-bold hover:opacity-80 transition-opacity">
                        <i className="fa-solid fa-plus mr-2"></i> Add Widget
                    </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-4 gap-4 auto-rows-[180px]">
                    {widgets.map(widget => {
                        const Component = WIDGET_REGISTRY[widget.type].component;
                        return (
                            <div 
                                key={widget.id} 
                                className={`bg-surface border-2 border-ink rounded-xl shadow-hard overflow-hidden relative group transition-all hover:shadow-lg`}
                                style={{ gridColumn: `span ${widget.w}`, gridRow: `span ${widget.h}` }}
                            >
                                <div className="absolute top-2 right-2 z-20 opacity-0 group-hover:opacity-100 transition-opacity">
                                    <button onClick={() => removeWidget(widget.id)} className="w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center shadow-sm hover:bg-red-600">
                                        <i className="fa-solid fa-xmark text-xs"></i>
                                    </button>
                                </div>
                                <Component />
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Widget Picker Modal */}
            {showWidgetPicker && (
                <div className="absolute inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-8">
                    <div className="bg-surface border-2 border-ink rounded-xl shadow-hard w-full max-w-3xl max-h-[80vh] flex flex-col">
                        <div className="p-4 border-b border-border flex justify-between items-center">
                            <h2 className="text-xl font-bold text-ink">Add Widget</h2>
                            <button onClick={() => setShowWidgetPicker(false)} className="text-gray-500 hover:text-ink"><i className="fa-solid fa-xmark text-xl"></i></button>
                        </div>
                        <div className="p-6 overflow-y-auto grid grid-cols-2 md:grid-cols-3 gap-4">
                            {Object.entries(WIDGET_REGISTRY).map(([type, def]) => (
                                <button 
                                    key={type} 
                                    onClick={() => addWidget(type as WidgetType)}
                                    className="flex flex-col items-center p-4 border border-border rounded-lg hover:bg-subtle hover:border-accent-blue transition-all group"
                                >
                                    <div className="w-12 h-12 bg-subtle rounded-lg mb-3 flex items-center justify-center group-hover:bg-blue-100 dark:group-hover:bg-blue-900/30">
                                        <i className="fa-solid fa-puzzle-piece text-gray-400 group-hover:text-blue-500 text-xl"></i>
                                    </div>
                                    <span className="font-bold text-sm text-ink">{def.title}</span>
                                    <span className="text-xs text-gray-500 mt-1">{def.defaultW}x{def.defaultH}</span>
                                </button>
                            ))}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};
