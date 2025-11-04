# FocusDeck Web Application

Modern React + Vite + TypeScript + Tailwind CSS SPA for FocusDeck.

## Development

### Prerequisites

- Node.js 20+ and npm
- .NET 9 SDK (for running the API server)

### Running Locally

1. **Start the API Server** (in a separate terminal):
   ```bash
   cd ../FocusDeck.Server
   dotnet run
   ```
   The API will be available at `http://localhost:5000`

2. **Start the Vite Dev Server**:
   ```bash
   npm install
   npm run dev
   ```
   The dev server will start at `http://localhost:5173`

The Vite dev server is configured to proxy API requests (`/v1/*`) and WebSocket connections (`/hubs/*`) to the ASP.NET Core server running on port 5000.

### Building for Production

```bash
npm run build
```

This creates an optimized production build in the `dist/` directory. The ASP.NET Core server will automatically serve these files from `/app/*` when running in production mode.

## Architecture

### Stack

- **React 18**: UI framework
- **TypeScript**: Type-safe development
- **Vite**: Build tool and dev server
- **Tailwind CSS v4**: Utility-first styling
- **Design System**: Custom tokens based on FocusDeck brand
  - Primary: `#512BD4` (purple)
  - Surface: `#0F0F10` (dark)
  - 8-point spacing grid
  - 12px border radius

### Project Structure

```
src/
├── App.tsx           # Main app component with shell layout
├── main.tsx          # Application entry point
├── index.css         # Global styles with Tailwind
└── assets/          # Static assets
```

## Integration with ASP.NET Core

### Production Mode

The ASP.NET Core server serves the SPA from `/app/*` with:
- Fallback routing for client-side navigation
- Security headers (CSP, X-Frame-Options, etc.)
- Long-term caching for static assets
- No-cache for index.html

### API Integration

All API endpoints are under `/v1/*` and require JWT authentication. The Vite dev server proxies these requests to the backend during development.
