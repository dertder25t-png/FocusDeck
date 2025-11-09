import { useEffect, useMemo, useState } from 'react'
import { apiFetch } from '../lib/utils'

interface TenantDto {
  id: string
  name: string
  slug: string
  createdAt: string
  memberCount: number
  userRole: string
}

export function TenantsPage() {
  const [tenants, setTenants] = useState<TenantDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')

  const sortedTenants = useMemo(() =>
    [...tenants].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()),
  [tenants])

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const response = await apiFetch('/v1/tenants')
        const body = await response.json().catch(() => null)
        if (!response.ok || !Array.isArray(body)) {
          throw new Error((body as any)?.message || 'Failed to load tenants')
        }
        setTenants(body as TenantDto[])
      } catch (err: any) {
        setError(err?.message || 'Unable to load tenants')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  const resetForm = () => {
    setName('')
    setSlug('')
    setCreating(false)
  }

  const createTenant = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim() || !slug.trim()) {
      setError('Name and slug are required')
      return
    }

    setError(null)
    try {
      const response = await apiFetch('/v1/tenants', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: name.trim(), slug: slug.trim().toLowerCase() }),
      })
      const body = await response.json().catch(() => null)
      if (!response.ok || !body) {
        throw new Error((body as any)?.message || 'Failed to create tenant')
      }
      const created = body as TenantDto
      setTenants(prev => [...prev, created])
      resetForm()
    } catch (err: any) {
      setError(err?.message || 'Unable to create tenant')
    }
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-white">Tenants</h1>
          <p className="text-sm text-gray-400">Manage your workspaces and invite collaborators.</p>
        </div>
        <button
          onClick={() => setCreating(prev => !prev)}
          className="rounded bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
        >
          {creating ? 'Cancel' : 'New tenant'}
        </button>
      </div>

      {creating && (
        <form onSubmit={createTenant} className="rounded-lg border border-gray-800 bg-surface-100 p-4 space-y-3">
          <div>
            <label className="block text-sm text-gray-300">Name</label>
            <input
              className="mt-1 w-full rounded border border-gray-700 bg-gray-900 p-2 text-white focus:outline-none focus:ring-2 focus:ring-primary"
              value={name}
              onChange={e => setName(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm text-gray-300">Slug</label>
            <input
              className="mt-1 w-full rounded border border-gray-700 bg-gray-900 p-2 text-white focus:outline-none focus:ring-2 focus:ring-primary"
              value={slug}
              onChange={e => setSlug(e.target.value)}
              placeholder="my-team"
              required
              pattern="[a-z0-9-]+"
            />
            <p className="mt-1 text-xs text-gray-500">Lowercase letters, numbers, and hyphens only.</p>
          </div>
          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={resetForm}
              className="rounded bg-gray-800 px-3 py-2 text-sm text-gray-200 hover:bg-gray-700"
            >
              Clear
            </button>
            <button type="submit" className="rounded bg-primary px-3 py-2 text-sm font-medium text-white hover:bg-primary/90">
              Create tenant
            </button>
          </div>
        </form>
      )}

      {error && <div className="rounded border border-red-600 bg-red-950/60 p-3 text-sm text-red-200">{error}</div>}

      {loading ? (
        <div className="flex items-center justify-center rounded-lg border border-gray-800 bg-surface-100 p-8 text-gray-400">
          Loading tenants…
        </div>
      ) : sortedTenants.length === 0 ? (
        <div className="rounded-lg border border-dashed border-gray-700 bg-surface-100 p-8 text-center text-gray-400">
          You do not belong to any tenants yet. Create one to get started.
        </div>
      ) : (
        <div className="grid gap-4">
          {sortedTenants.map(tenant => (
            <div key={tenant.id} className="rounded-lg border border-gray-800 bg-surface-100 p-4">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <h2 className="text-lg font-semibold text-white">{tenant.name}</h2>
                  <p className="text-sm text-gray-400">/{tenant.slug}</p>
                </div>
                <span className="rounded bg-gray-800 px-2 py-1 text-xs text-gray-300">{tenant.memberCount} members</span>
              </div>
              <div className="mt-3 text-sm text-gray-400">Your role: {tenant.userRole}</div>
              <div className="mt-2 text-xs text-gray-500">
                Created {new Date(tenant.createdAt).toLocaleString()}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
