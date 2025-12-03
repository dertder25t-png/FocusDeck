import { createContext, useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { apiFetch } from '../lib/utils'
import type { PrivacySetting } from '../types/privacy'

interface PrivacySettingsContextValue {
  settings: PrivacySetting[]
  loading: boolean
  refresh: () => Promise<void>
  updateSetting: (contextType: string, isEnabled: boolean) => Promise<PrivacySetting>
  isEnabled: (contextType: string) => boolean
}

export const PrivacySettingsContext = createContext<PrivacySettingsContextValue | null>(null)

export function PrivacySettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<PrivacySetting[]>([])
  const [loading, setLoading] = useState(true)

  const refresh = useCallback(async () => {
    // Only fetch if authenticated (hacky check but prevents loop on login page)
    if (window.location.pathname.includes('/login') || window.location.pathname.includes('/register')) {
        setLoading(false);
        return;
    }

    setLoading(true)
    try {
      const response = await apiFetch('/v1/privacy/consent')
      if (!response.ok) {
        throw new Error('Failed to load privacy consent dashboard')
      }

      const payload = (await response.json()) as PrivacySetting[]
      setSettings(payload)
    } catch (error) {
      // Silently ignore auth errors - these are handled by apiFetch/logout
      if (error instanceof Error && (
        error.message === 'Not authenticated' || 
        error.message === 'Session expired' ||
        error.message === 'Refresh failed'
      )) {
        return
      }
      console.warn('Unable to refresh privacy settings (likely offline or unauthenticated)', error)
    } finally {
      setLoading(false)
    }
  }, [])

  const updateSetting = useCallback(async (contextType: string, isEnabled: boolean) => {
    const response = await apiFetch('/v1/privacy/consent', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contextType, isEnabled }),
    })

    if (!response.ok) {
      const payload = await response.text()
      throw new Error(payload || 'Unable to update privacy setting')
    }

    const updated = (await response.json()) as PrivacySetting
    setSettings((prev) =>
      prev.map((setting) =>
        setting.contextType === updated.contextType ? updated : setting
      )
    )

    return updated
  }, [])

  const isEnabled = useCallback(
    (contextType: string) => settings.some((setting) => setting.contextType === contextType && setting.isEnabled),
    [settings],
  )

  useEffect(() => {
    refresh()
  }, [refresh])

  const value = useMemo(
    () => ({
      settings,
      loading,
      refresh,
      updateSetting,
      isEnabled,
    }),
    [settings, loading, refresh, updateSetting, isEnabled],
  )

  return (
    <PrivacySettingsContext.Provider value={value}>
      {children}
    </PrivacySettingsContext.Provider>
  )
}
