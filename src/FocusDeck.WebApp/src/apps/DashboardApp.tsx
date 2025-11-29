import React from 'react';

export const DashboardApp: React.FC = () => {
    return (
        <div className="flex-1 overflow-y-auto p-4 md:p-8 bg-paper h-full">
            <div className="max-w-5xl mx-auto">
                <h1 className="text-3xl font-display font-bold mb-6">Welcome Back.</h1>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                    <div className="bg-white border-2 border-black rounded-xl p-6 shadow-hard flex flex-col justify-between h-48">
                        <div className="flex justify-between items-start">
                            <i className="fa-solid fa-cloud-sun text-4xl text-accent-yellow"></i>
                            <span className="font-mono text-xs">NYC, USA</span>
                        </div>
                        <div><div className="text-4xl font-bold">72°F</div><div className="text-gray-500 text-sm">Partly Cloudy</div></div>
                    </div>
                    <div className="md:col-span-2 bg-black text-white rounded-xl p-6 shadow-hard flex items-center justify-between">
                        <div><div className="text-xs font-bold text-gray-400 uppercase">Focus Timer</div><div className="text-5xl font-mono font-bold">24:59</div></div>
                        <button className="w-12 h-12 rounded-full bg-white text-black flex items-center justify-center hover:scale-110 transition-transform"><i className="fa-solid fa-pause"></i></button>
                    </div>
                </div>
            </div>
        </div>
    );
};
