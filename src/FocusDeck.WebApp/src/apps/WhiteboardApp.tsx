
import React, { useState, useRef } from 'react';
import { Stage, Layer, Line, Rect, Circle } from 'react-konva';
import { Eraser, Pen, Square, Circle as CircleIcon, Trash2 } from 'lucide-react';

type Tool = 'pen' | 'eraser' | 'rect' | 'circle';

interface Shape {
  id: string;
  tool: Tool;
  points?: number[];
  x?: number;
  y?: number;
  width?: number;
  height?: number;
  radius?: number;
  text?: string;
  color?: string;
}

export const WhiteboardApp: React.FC = () => {
  const [tool, setTool] = useState<Tool>('pen');
  const [color, setColor] = useState('#000000');
  const [lines, setLines] = useState<Shape[]>([]);
  const isDrawing = useRef(false);

  const handleMouseDown = (e: any) => {
    isDrawing.current = true;
    const pos = e.target.getStage().getPointerPosition();

    if (tool === 'pen' || tool === 'eraser') {
      setLines([...lines, {
          id: Date.now().toString(),
          tool,
          points: [pos.x, pos.y],
          color: tool === 'eraser' ? '#ffffff' : color
      }]);
    } else if (tool === 'rect') {
        setLines([...lines, {
            id: Date.now().toString(),
            tool: 'rect',
            x: pos.x,
            y: pos.y,
            width: 0,
            height: 0,
            color
        }]);
    } else if (tool === 'circle') {
         setLines([...lines, {
            id: Date.now().toString(),
            tool: 'circle',
            x: pos.x,
            y: pos.y,
            radius: 0,
            color
        }]);
    }
  };

  const handleMouseMove = (e: any) => {
    if (!isDrawing.current) return;
    const stage = e.target.getStage();
    const point = stage.getPointerPosition();
    let lastLine = lines[lines.length - 1];

    if (tool === 'pen' || tool === 'eraser') {
      // Add point
      lastLine.points = lastLine.points!.concat([point.x, point.y]);
    } else if (tool === 'rect') {
        lastLine.width = point.x - lastLine.x!;
        lastLine.height = point.y - lastLine.y!;
    } else if (tool === 'circle') {
        const dx = point.x - lastLine.x!;
        const dy = point.y - lastLine.y!;
        lastLine.radius = Math.sqrt(dx*dx + dy*dy);
    }

    // Replace last element
    lines.splice(lines.length - 1, 1, lastLine);
    setLines(lines.concat());
  };

  const handleMouseUp = () => {
    isDrawing.current = false;
  };

  const clearCanvas = () => {
      if(confirm('Clear whiteboard?')) setLines([]);
  }

  return (
    <div className="flex flex-col h-full bg-white dark:bg-gray-900 relative">
      {/* Toolbar */}
      <div className="absolute top-4 left-1/2 transform -translate-x-1/2 bg-white dark:bg-gray-800 shadow-lg rounded-full px-4 py-2 flex items-center gap-4 z-10 border border-gray-200 dark:border-gray-700">
        <button onClick={() => setTool('pen')} className={`p-2 rounded-full transition-colors ${tool === 'pen' ? 'bg-blue-100 text-blue-600 dark:bg-blue-900/30' : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700'}`}>
          <Pen size={20} />
        </button>
        <button onClick={() => setTool('eraser')} className={`p-2 rounded-full transition-colors ${tool === 'eraser' ? 'bg-blue-100 text-blue-600 dark:bg-blue-900/30' : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700'}`}>
          <Eraser size={20} />
        </button>
        <div className="w-px h-6 bg-gray-300 dark:bg-gray-600"></div>
        <button onClick={() => setTool('rect')} className={`p-2 rounded-full transition-colors ${tool === 'rect' ? 'bg-blue-100 text-blue-600 dark:bg-blue-900/30' : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700'}`}>
          <Square size={20} />
        </button>
        <button onClick={() => setTool('circle')} className={`p-2 rounded-full transition-colors ${tool === 'circle' ? 'bg-blue-100 text-blue-600 dark:bg-blue-900/30' : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700'}`}>
          <CircleIcon size={20} />
        </button>
        <div className="w-px h-6 bg-gray-300 dark:bg-gray-600"></div>
        <input
            type="color"
            value={color}
            onChange={(e) => setColor(e.target.value)}
            className="w-8 h-8 rounded-full border-none cursor-pointer bg-transparent"
        />
        <div className="w-px h-6 bg-gray-300 dark:bg-gray-600"></div>
        <button onClick={clearCanvas} className="p-2 rounded-full text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20">
          <Trash2 size={20} />
        </button>
      </div>

      <Stage
        width={window.innerWidth}
        height={window.innerHeight}
        onMouseDown={handleMouseDown}
        onMousemove={handleMouseMove}
        onMouseup={handleMouseUp}
        className="bg-dot-pattern"
        style={{ cursor: tool === 'pen' ? 'crosshair' : 'default' }}
      >
        <Layer>
          {lines.map((line, i) => {
             if (line.tool === 'pen' || line.tool === 'eraser') {
                 return (
                    <Line
                        key={i}
                        points={line.points}
                        stroke={line.color}
                        strokeWidth={line.tool === 'eraser' ? 20 : 5}
                        tension={0.5}
                        lineCap="round"
                        lineJoin="round"
                        globalCompositeOperation={
                            line.tool === 'eraser' ? 'destination-out' : 'source-over'
                        }
                    />
                 );
             } else if (line.tool === 'rect') {
                 return (
                    <Rect
                        key={i}
                        x={line.x}
                        y={line.y}
                        width={line.width}
                        height={line.height}
                        stroke={line.color}
                        strokeWidth={4}
                    />
                 );
             } else if (line.tool === 'circle') {
                 return (
                     <Circle
                        key={i}
                        x={line.x}
                        y={line.y}
                        radius={line.radius}
                        stroke={line.color}
                        strokeWidth={4}
                     />
                 )
             }
             return null;
          })}
        </Layer>
      </Stage>
    </div>
  );
};
