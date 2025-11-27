import { useCallback, useEffect, useState } from 'react'
import { apiFetch } from '../lib/utils'

export interface CurrentTenant {
  id: string
  name: string
  slug: string
  userRole: string
  memberCount: number
}

export function useCurrentTenant() {
  const [tenant, setTenant] = useState<CurrentTenant | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await apiFetch('/v1/tenants/current')
      const body = await res.json().catch(() => null)
      if (!res.ok || !body) {
        throw new Error((body as any)?.message || 'Unable to load current tenant')
      }
      setTenant(body as CurrentTenant)
    } catch (err: any) {
      setTenant(null)
      setError(err?.message || 'Unable to resolve tenant')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  return { tenant, loading, error, refresh }
}
