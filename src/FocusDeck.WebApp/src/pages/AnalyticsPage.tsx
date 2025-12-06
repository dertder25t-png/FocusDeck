import { useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '../components/Card'
import { Button } from '../components/Button'
import { Badge } from '../components/Badge'
import { useAnalytics } from '../hooks/useAnalytics'
import { analytics } from '../services/api'

type TimeRange = 7 | 14 | 30

interface OverviewData {
  focusMinutes: number
  distractionsPerHour: number
  currentStreak: number
  lecturesProcessed: number
  avgCoverage: number
  suggestionsAccepted: number
}

interface ChartDataPoint {
  date: string
  minutes?: number
  count?: number
  processed?: number
}

export function AnalyticsPage() {
  const [range, setRange] = useState<TimeRange>(30)
  const [selectedCourse, setSelectedCourse] = useState<string | null>(null)

  const {
    overview,
    focus: focusData,
    lectures: lecturesData,
    suggestions: suggestionsData,
    courses,
    isLoading: loading
  } = useAnalytics(range, selectedCourse);

  const exportData = async (format: 'json' | 'csv') => {
    try {
      const data = await analytics.exportData(format, range, selectedCourse);
      
      if (format === 'json') {
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
        downloadBlob(blob, `analytics_${Date.now()}.json`)
      } else {
        downloadBlob(data as Blob, `analytics_${Date.now()}.csv`)
      }
    } catch (err) {
      console.error('Failed to export:', err)
    }
  }

  const downloadBlob = (blob: Blob, filename: string) => {
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }

  const formatMinutes = (mins: number) => {
    const hours = Math.floor(mins / 60)
    const minutes = Math.floor(mins % 60)
    return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`
  }

  const getCoverageColor = (coverage: number) => {
    if (coverage >= 80) return 'success'
    if (coverage >= 50) return 'warning'
    return 'danger'
  }

  const maxFocus = Math.max(...focusData.map((d: ChartDataPoint) => d.minutes || 0), 1)
  const maxLectures = Math.max(...lecturesData.map((d: ChartDataPoint) => d.count || 0), 1)
  const maxSuggestions = Math.max(...suggestionsData.map((d: ChartDataPoint) => d.count || 0), 1)

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-400">Loading analytics...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Analytics</h1>
          <p className="text-sm text-gray-400 mt-1">
            Track your productivity insights and trends
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" onClick={() => exportData('json')}>
            Export JSON
          </Button>
          <Button variant="secondary" size="sm" onClick={() => exportData('csv')}>
            Export CSV
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2">
          <span className="text-sm text-gray-400">Range:</span>
          <div className="flex gap-1">
            {[7, 14, 30].map(days => (
              <button
                key={days}
                onClick={() => setRange(days as TimeRange)}
                className={`px-3 py-1 text-sm rounded-lg transition-colors ${
                  range === days
                    ? 'bg-primary text-white'
                    : 'bg-gray-800 text-gray-300 hover:bg-gray-700'
                }`}
              >
                {days} days
              </button>
            ))}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-gray-400">Course:</span>
          <select
            value={selectedCourse || ''}
            onChange={(e) => setSelectedCourse(e.target.value || null)}
            className="px-3 py-1 text-sm bg-gray-800 text-gray-300 rounded-lg border border-gray-700 focus:outline-none focus:ring-2 focus:ring-primary"
          >
            <option value="">All Courses</option>
            {courses.map((course: any) => (
              <option key={course.id} value={course.id}>
                {course.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-6 gap-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Focus Time</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">
              {overview ? formatMinutes(overview.focusMinutes) : '0m'}
            </div>
            <p className="text-xs text-gray-500 mt-1">total</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Distractions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">
              {(overview?.distractionsPerHour || 0).toFixed(1)}
            </div>
            <p className="text-xs text-gray-500 mt-1">per hour</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Current Streak</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">
              {overview?.currentStreak || 0}
            </div>
            <p className="text-xs text-gray-500 mt-1">days</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Lectures</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">
              {overview?.lecturesProcessed || 0}
            </div>
            <p className="text-xs text-gray-500 mt-1">processed</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Avg Coverage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-2">
              <div className="text-3xl font-semibold">
                {overview?.avgCoverage || 0}%
              </div>
              {overview && (
                <Badge variant={getCoverageColor(overview.avgCoverage)}>
                  {overview.avgCoverage >= 80 ? 'Great' : overview.avgCoverage >= 50 ? 'Good' : 'Low'}
                </Badge>
              )}
            </div>
            <p className="text-xs text-gray-500 mt-1">note quality</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Suggestions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">
              {overview?.suggestionsAccepted || 0}
            </div>
            <p className="text-xs text-gray-500 mt-1">accepted</p>
          </CardContent>
        </Card>
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Focus Minutes Bar Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Focus Minutes</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64 flex items-end gap-1">
              {focusData.length === 0 ? (
                <div className="flex-1 flex items-center justify-center text-gray-400">
                  No focus session data
                </div>
              ) : (
                focusData.map((point: ChartDataPoint, i: number) => (
                  <div key={i} className="flex-1 flex flex-col items-center gap-1">
                    <div
                      className="w-full bg-primary rounded-t transition-all hover:bg-primary/80"
                      style={{ height: `${((point.minutes || 0) / maxFocus) * 100}%` }}
                      title={`${point.date}: ${point.minutes || 0} min`}
                    />
                    {i % Math.ceil(focusData.length / 5) === 0 && (
                      <span className="text-xs text-gray-500 rotate-45 origin-top-left mt-2">
                        {new Date(point.date).getDate()}
                      </span>
                    )}
                  </div>
                ))
              )}
            </div>
          </CardContent>
        </Card>

        {/* Lectures Timeline */}
        <Card>
          <CardHeader>
            <CardTitle>Lectures Processed</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64 relative">
              {lecturesData.length === 0 ? (
                <div className="flex items-center justify-center h-full text-gray-400">
                  No lecture data
                </div>
              ) : (
                <svg className="w-full h-full">
                  <polyline
                    points={lecturesData
                      .map((point: ChartDataPoint, i: number) => {
                        const x = (i / (lecturesData.length - 1)) * 100
                        const y = 100 - ((point.processed || 0) / maxLectures) * 80
                        return `${x},${y}`
                      })
                      .join(' ')}
                    fill="none"
                    stroke="#512BD4"
                    strokeWidth="2"
                    vectorEffect="non-scaling-stroke"
                  />
                  {lecturesData.map((point: ChartDataPoint, i: number) => {
                    const x = (i / (lecturesData.length - 1)) * 100
                    const y = 100 - ((point.processed || 0) / maxLectures) * 80
                    return (
                      <circle
                        key={i}
                        cx={`${x}%`}
                        cy={`${y}%`}
                        r="4"
                        fill="#512BD4"
                      >
                        <title>{`${point.date}: ${point.processed || 0} lectures`}</title>
                      </circle>
                    )
                  })}
                </svg>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Suggestions Accepted Bar Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Suggestions Accepted</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64 flex items-end gap-1">
              {suggestionsData.length === 0 ? (
                <div className="flex-1 flex items-center justify-center text-gray-400">
                  No suggestion data
                </div>
              ) : (
                suggestionsData.map((point: ChartDataPoint, i: number) => (
                  <div key={i} className="flex-1 flex flex-col items-center gap-1">
                    <div
                      className="w-full bg-success rounded-t transition-all hover:bg-success/80"
                      style={{ height: `${((point.count || 0) / maxSuggestions) * 100}%` }}
                      title={`${point.date}: ${point.count || 0} suggestions`}
                    />
                    {i % Math.ceil(suggestionsData.length / 5) === 0 && (
                      <span className="text-xs text-gray-500 rotate-45 origin-top-left mt-2">
                        {new Date(point.date).getDate()}
                      </span>
                    )}
                  </div>
                ))
              )}
            </div>
          </CardContent>
        </Card>

        {/* Coverage Gauge */}
        <Card>
          <CardHeader>
            <CardTitle>Average Coverage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-64 flex flex-col items-center justify-center gap-4">
              <div className="relative w-48 h-48">
                <svg className="w-full h-full transform -rotate-90">
                  <circle
                    cx="96"
                    cy="96"
                    r="80"
                    fill="none"
                    stroke="#1F2937"
                    strokeWidth="16"
                  />
                  <circle
                    cx="96"
                    cy="96"
                    r="80"
                    fill="none"
                    stroke={overview && overview.avgCoverage >= 80 ? '#10B981' : overview && overview.avgCoverage >= 50 ? '#F59E0B' : '#EF4444'}
                    strokeWidth="16"
                    strokeDasharray={`${((overview?.avgCoverage || 0) / 100) * 502.4} 502.4`}
                    strokeLinecap="round"
                  />
                </svg>
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="text-center">
                    <div className="text-4xl font-semibold">{overview?.avgCoverage || 0}%</div>
                    <div className="text-xs text-gray-500 mt-1">quality</div>
                  </div>
                </div>
              </div>
              <Badge variant={overview ? getCoverageColor(overview.avgCoverage) : 'default'} size="lg">
                {overview && overview.avgCoverage >= 80 ? 'Excellent Coverage' : overview && overview.avgCoverage >= 50 ? 'Good Coverage' : 'Needs Improvement'}
              </Badge>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
