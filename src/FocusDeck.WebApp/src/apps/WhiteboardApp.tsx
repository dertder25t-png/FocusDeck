import React, { useState, useRef } from 'react';
import { Stage, Layer, Line, Rect, Circle } from 'react-konva';
import { cn } from '../lib/utils';
import Konva from 'konva';

type Tool = 'pen' | 'eraser' | 'rect' | 'circle';

interface LineShape {
  tool: 'pen' | 'eraser';
  points: number[];
  color: string;
  width: number;
}

interface RectShape {
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
  strokeWidth: number;
}

interface CircleShape {
    x: number;
    y: number;
    radius: number;
    color: string;
    strokeWidth: number;
}

// Simple color palette
const COLORS = ['#000000', '#EF4444', '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6'];

export const WhiteboardApp: React.FC = () => {
  const [tool, setTool] = useState<Tool>('pen');
  const [color, setColor] = useState('#000000');
  const [lineWidth, setLineWidth] = useState(5);

  const [lines, setLines] = useState<LineShape[]>([]);
  const [rects, setRects] = useState<RectShape[]>([]);
  const [circles, setCircles] = useState<CircleShape[]>([]);

  // Undo/Redo Stacks could be added here, but starting basic

  const isDrawing = useRef(false);
  const startPos = useRef<{x: number, y: number} | null>(null);

  const handleMouseDown = (e: Konva.KonvaEventObject<MouseEvent>) => {
    isDrawing.current = true;
    const pos = e.target.getStage()?.getPointerPosition();
    if (!pos) return;

    if (tool === 'pen' || tool === 'eraser') {
        setLines([...lines, { tool, points: [pos.x, pos.y], color, width: lineWidth }]);
    } else {
        startPos.current = pos;
        // For shapes, we might create a placeholder that gets updated on move
        // But simpler for now: just track start point and draw on drag (preview?)
        // Let's implement preview drawing logic
        if (tool === 'rect') {
             setRects([...rects, { x: pos.x, y: pos.y, width: 0, height: 0, color, strokeWidth: lineWidth }]);
        } else if (tool === 'circle') {
             setCircles([...circles, { x: pos.x, y: pos.y, radius: 0, color, strokeWidth: lineWidth }]);
        }
    }
  };

  const handleMouseMove = (e: Konva.KonvaEventObject<MouseEvent>) => {
    if (!isDrawing.current) return;
    const stage = e.target.getStage();
    const point = stage?.getPointerPosition();
    if (!point) return;

    if (tool === 'pen' || tool === 'eraser') {
      const lastLine = lines[lines.length - 1];
      // append points
      lastLine.points = lastLine.points.concat([point.x, point.y]);
      // replace last
      lines.splice(lines.length - 1, 1, lastLine);
      setLines(lines.concat());
    } else if (tool === 'rect' && startPos.current) {
        const lastRect = rects[rects.length - 1];
        const newWidth = point.x - startPos.current.x;
        const newHeight = point.y - startPos.current.y;

        lastRect.width = newWidth;
        lastRect.height = newHeight;

        rects.splice(rects.length - 1, 1, lastRect);
        setRects(rects.concat());
    } else if (tool === 'circle' && startPos.current) {
        const lastCircle = circles[circles.length - 1];
        const dx = point.x - startPos.current.x;
        const dy = point.y - startPos.current.y;
        const radius = Math.sqrt(dx * dx + dy * dy);

        lastCircle.radius = radius;
        circles.splice(circles.length - 1, 1, lastCircle);
        setCircles(circles.concat());
    }
  };

  const handleMouseUp = () => {
    isDrawing.current = false;
    startPos.current = null;
  };

  const clearCanvas = () => {
      setLines([]);
      setRects([]);
      setCircles([]);
  };

  return (
    <div className="h-full bg-white relative flex flex-col overflow-hidden">
      {/* Toolbar */}
      <div className="absolute top-4 left-4 z-10 flex flex-col gap-2 bg-white/90 backdrop-blur border border-ink/20 shadow-hard rounded-lg p-2">
        <div className="flex flex-col gap-1 border-b border-gray-200 pb-2">
            <button
                onClick={() => setTool('pen')}
                className={cn("w-8 h-8 rounded flex items-center justify-center hover:bg-gray-100", tool === 'pen' && "bg-accent-blue text-white hover:bg-blue-600")}
                title="Pen"
            >
                <i className="fa-solid fa-pen"></i>
            </button>
            <button
                onClick={() => setTool('eraser')}
                className={cn("w-8 h-8 rounded flex items-center justify-center hover:bg-gray-100", tool === 'eraser' && "bg-accent-red text-white hover:bg-red-600")}
                title="Eraser"
            >
                <i className="fa-solid fa-eraser"></i>
            </button>
            <button
                onClick={() => setTool('rect')}
                className={cn("w-8 h-8 rounded flex items-center justify-center hover:bg-gray-100", tool === 'rect' && "bg-accent-purple text-white hover:bg-purple-600")}
                title="Rectangle"
            >
                <i className="fa-regular fa-square"></i>
            </button>
            <button
                onClick={() => setTool('circle')}
                className={cn("w-8 h-8 rounded flex items-center justify-center hover:bg-gray-100", tool === 'circle' && "bg-accent-yellow text-white hover:bg-yellow-600")}
                title="Circle"
            >
                <i className="fa-regular fa-circle"></i>
            </button>
        </div>

        <div className="grid grid-cols-2 gap-1 pt-1 border-b border-gray-200 pb-2">
            {COLORS.map(c => (
                <button
                    key={c}
                    onClick={() => setColor(c)}
                    className={cn("w-4 h-4 rounded-full border border-gray-300", color === c && "ring-2 ring-offset-1 ring-black")}
                    style={{ backgroundColor: c }}
                />
            ))}
        </div>

        <div className="flex flex-col gap-1 pt-1 items-center">
            <input
                type="range"
                min="1" max="20"
                value={lineWidth}
                onChange={(e) => setLineWidth(parseInt(e.target.value))}
                className="w-8 h-20 writing-mode-vertical"
                style={{ writingMode: 'vertical-lr', direction: 'rtl' }}
            />
            <span className="text-[10px] text-gray-500 font-bold">{lineWidth}px</span>
        </div>

        <button onClick={clearCanvas} className="mt-2 text-xs font-bold text-red-500 hover:text-red-700">Clear</button>
      </div>

      <div className="flex-1 cursor-crosshair">
        <Stage
            width={window.innerWidth}
            height={window.innerHeight}
            onMouseDown={handleMouseDown}
            onMousemove={handleMouseMove}
            onMouseup={handleMouseUp}
            className="bg-grid-pattern" // You might need a CSS class for grid background
        >
          <Layer>
            {/* Shapes */}
            {rects.map((rect, i) => (
                <Rect
                    key={i}
                    x={rect.x}
                    y={rect.y}
                    width={rect.width}
                    height={rect.height}
                    stroke={rect.color}
                    strokeWidth={rect.strokeWidth}
                />
            ))}
             {circles.map((circ, i) => (
                <Circle
                    key={i}
                    x={circ.x}
                    y={circ.y}
                    radius={circ.radius}
                    stroke={circ.color}
                    strokeWidth={circ.strokeWidth}
                />
            ))}
            {/* Lines */}
            {lines.map((line, i) => (
              <Line
                key={i}
                points={line.points}
                stroke={line.tool === 'eraser' ? '#ffffff' : line.color}
                strokeWidth={line.width}
                tension={0.5}
                lineCap="round"
                globalCompositeOperation={
                  line.tool === 'eraser' ? 'destination-out' : 'source-over'
                }
              />
            ))}
          </Layer>
        </Stage>
      </div>
    </div>
  );
};
