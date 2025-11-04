import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '../components/Card'
import { Badge } from '../components/Badge'
import { EmptyState } from '../components/States'

interface Job {
  id: string
  status: string
  jobType: string
  method: string
  args: string[]
  startedAt?: string
  scheduledAt?: string
  succeededAt?: string
  failedAt?: string
  duration?: number
  exceptionMessage?: string
}

export function JobsPage() {
  const [jobs] = useState<Job[]>([])
  const [stats] = useState<any>(null)
  const [filter, setFilter] = useState<string>('all')

  useEffect(() => {
    // TODO: Fetch jobs from API
    // fetch('/v1/jobs?status=' + filter)
    //   .then(res => res.json())
    //   .then(data => setJobs(data.jobs))
    
    // TODO: Fetch stats from API
    // fetch('/v1/jobs/stats')
    //   .then(res => res.json())
    //   .then(data => setStats(data))
  }, [filter])

  const getStatusBadgeVariant = (status: string) => {
    switch (status.toLowerCase()) {
      case 'succeeded':
        return 'success'
      case 'processing':
        return 'info'
      case 'scheduled':
        return 'warning'
      case 'failed':
        return 'danger'
      default:
        return 'default'
    }
  }

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A'
    return new Date(dateString).toLocaleString()
  }

  const formatDuration = (ms?: number) => {
    if (!ms) return 'N/A'
    const seconds = Math.floor(ms / 1000)
    if (seconds < 60) return `${seconds}s`
    const minutes = Math.floor(seconds / 60)
    const remainingSeconds = seconds % 60
    return `${minutes}m ${remainingSeconds}s`
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Background Jobs</h1>
        <p className="text-sm text-gray-400 mt-1">
          Monitor and manage background job processing
        </p>
      </div>

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-4">
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Enqueued</div>
              <div className="text-2xl font-semibold">{stats.enqueued || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Scheduled</div>
              <div className="text-2xl font-semibold">{stats.scheduled || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Processing</div>
              <div className="text-2xl font-semibold text-blue-400">{stats.processing || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Succeeded</div>
              <div className="text-2xl font-semibold text-green-400">{stats.succeeded || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Failed</div>
              <div className="text-2xl font-semibold text-red-400">{stats.failed || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Deleted</div>
              <div className="text-2xl font-semibold">{stats.deleted || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Recurring</div>
              <div className="text-2xl font-semibold">{stats.recurring || 0}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-gray-400 text-xs mb-1">Servers</div>
              <div className="text-2xl font-semibold">{stats.servers || 0}</div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-2">
        <button
          onClick={() => setFilter('all')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            filter === 'all'
              ? 'bg-primary text-white'
              : 'bg-surface-100 text-gray-300 hover:bg-gray-800/50'
          }`}
        >
          All
        </button>
        <button
          onClick={() => setFilter('processing')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            filter === 'processing'
              ? 'bg-primary text-white'
              : 'bg-surface-100 text-gray-300 hover:bg-gray-800/50'
          }`}
        >
          Processing
        </button>
        <button
          onClick={() => setFilter('scheduled')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            filter === 'scheduled'
              ? 'bg-primary text-white'
              : 'bg-surface-100 text-gray-300 hover:bg-gray-800/50'
          }`}
        >
          Scheduled
        </button>
        <button
          onClick={() => setFilter('succeeded')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            filter === 'succeeded'
              ? 'bg-primary text-white'
              : 'bg-surface-100 text-gray-300 hover:bg-gray-800/50'
          }`}
        >
          Succeeded
        </button>
        <button
          onClick={() => setFilter('failed')}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            filter === 'failed'
              ? 'bg-primary text-white'
              : 'bg-surface-100 text-gray-300 hover:bg-gray-800/50'
          }`}
        >
          Failed
        </button>
      </div>

      {/* Jobs List */}
      {jobs.length === 0 ? (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              icon="⚙️"
              title="No jobs found"
              description="Background jobs will appear here when they are scheduled or running"
            />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>Jobs</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {jobs.map((job) => (
                <div
                  key={job.id}
                  className="p-4 bg-surface-100 rounded-md border border-gray-800 hover:border-gray-700 transition-colors"
                >
                  <div className="flex items-start justify-between mb-2">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="font-medium">{job.jobType}</span>
                        <span className="text-gray-500">→</span>
                        <span className="text-sm text-gray-400">{job.method}</span>
                        <Badge variant={getStatusBadgeVariant(job.status) as any}>
                          {job.status}
                        </Badge>
                      </div>
                      {job.args && job.args.length > 0 && (
                        <div className="text-xs text-gray-500 font-mono">
                          Args: {job.args.join(', ')}
                        </div>
                      )}
                    </div>
                    <span className="text-xs text-gray-500">
                      {formatDate(job.startedAt || job.scheduledAt || job.succeededAt || job.failedAt)}
                    </span>
                  </div>
                  
                  {job.duration && (
                    <div className="text-xs text-gray-500">
                      Duration: {formatDuration(job.duration)}
                    </div>
                  )}
                  
                  {job.exceptionMessage && (
                    <div className="mt-2 p-2 bg-red-500/10 border border-red-500/20 rounded text-xs text-red-400">
                      <div className="font-semibold mb-1">Error:</div>
                      <div className="font-mono">{job.exceptionMessage}</div>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
