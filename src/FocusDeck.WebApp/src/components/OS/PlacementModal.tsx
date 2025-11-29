import React from 'react';
import { useWindowManager } from '../../contexts/WindowManagerContext';

export const PlacementModal: React.FC = () => {
  const { showPlacementModal, confirmPlacement, setShowPlacementModal } = useWindowManager();

  if (!showPlacementModal) return null;

  return (
    <div id="placement-modal" className="absolute inset-0 z-[60] flex items-center justify-center p-4 backdrop-blur-sm bg-paper/80">
        <div className="bg-white border-2 border-ink rounded-xl shadow-hard p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-4 text-center text-ink">Where should this open?</h3>
            <div className="grid grid-cols-3 gap-3">
                <button onClick={() => confirmPlacement('left')} className="flex flex-col items-center p-3 border rounded hover:bg-gray-50 hover:border-black transition-all">
                    <div className="flex gap-1 w-12 h-8 mb-2 border border-gray-300 rounded p-0.5"><div className="w-1/2 h-full bg-blue-500 rounded-sm"></div><div className="w-1/2 h-full bg-gray-200 rounded-sm"></div></div>
                    <span className="text-xs font-bold text-ink">Replace Left</span>
                </button>
                <button onClick={() => confirmPlacement('right')} className="flex flex-col items-center p-3 border rounded hover:bg-gray-50 hover:border-black transition-all">
                    <div className="flex gap-1 w-12 h-8 mb-2 border border-gray-300 rounded p-0.5"><div className="w-1/2 h-full bg-gray-200 rounded-sm"></div><div className="w-1/2 h-full bg-blue-500 rounded-sm"></div></div>
                    <span className="text-xs font-bold text-ink">Replace Right</span>
                </button>
                    <button onClick={() => confirmPlacement('over')} className="flex flex-col items-center p-3 border rounded hover:bg-gray-50 hover:border-black transition-all">
                    <div className="w-12 h-8 mb-2 border border-gray-300 rounded bg-blue-500 p-0.5 relative"><div className="absolute -bottom-1 -right-1 w-4 h-4 bg-white border border-gray-300 rounded-full flex items-center justify-center text-[8px] text-gray-500">NEW</div></div>
                    <span className="text-xs font-bold text-ink">Open Over</span>
                </button>
            </div>
            <button onClick={() => setShowPlacementModal(false)} className="w-full mt-4 text-xs font-bold text-gray-500 hover:text-black">Cancel</button>
        </div>
    </div>
  );
};
