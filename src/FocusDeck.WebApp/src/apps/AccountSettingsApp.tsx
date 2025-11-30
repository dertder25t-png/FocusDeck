import React, { useEffect, useState } from 'react'
import { apiFetch } from '../lib/utils'

interface UserSettings {
  googleApiKey?: string | null
  canvasApiToken?: string | null
  homeAssistantUrl?: string | null
  homeAssistantToken?: string | null
  openAiKey?: string | null
  anthropicKey?: string | null
  updatedAt?: string | null
}

export const AccountSettingsApp: React.FC = () => {
  const [settings, setSettings] = useState<UserSettings>({})
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  async function loadSettings() {
    setLoading(true)
    setError(null)
    try {
      const res = await apiFetch('/v1/user/settings')
      if (!res.ok) throw new Error('Failed to load settings')
      const data = await res.json()
      setSettings(data || {})
    } catch (e: any) {
      setError(e.message || 'Load failed')
    } finally {
      setLoading(false)
    }
  }

  async function saveSettings(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      const res = await apiFetch('/v1/user/settings', {
        method: 'POST',
        body: JSON.stringify(settings)
      })
      if (!res.ok) throw new Error('Save failed')
      const data = await res.json()
      setSettings(data)
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    } catch (e: any) {
      setError(e.message || 'Save failed')
    } finally {
      setSaving(false)
    }
  }

  useEffect(() => { loadSettings() }, [])

  const bind = (key: keyof UserSettings) => ({
    value: settings[key] || '',
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setSettings(s => ({ ...s, [key]: e.target.value }))
  })

  return (
    <div className="h-full flex flex-col bg-white dark:bg-gray-900 text-ink dark:text-gray-100">
      <div className="px-4 py-3 border-b-2 border-ink dark:border-gray-700 bg-subtle dark:bg-gray-800 flex items-center justify-between">
        <h2 className="font-display text-lg font-bold flex items-center gap-2"><i className="fa-solid fa-gears"></i> Account Settings</h2>
        {loading && <span className="text-xs flex items-center gap-2"><i className="fa-solid fa-circle-notch fa-spin"></i> Loading...</span>}
        {saved && !saving && <span className="text-xs text-green-600 font-semibold flex items-center gap-1"><i className="fa-solid fa-circle-check"></i> Saved</span>}
      </div>
      <form onSubmit={saveSettings} className="flex-1 overflow-y-auto p-4 space-y-6 bg-paper dark:bg-gray-900">
        {error && <div className="border-2 border-red-600 bg-red-100 text-red-800 rounded-lg p-3 text-sm flex items-start gap-3 shadow-sm"><i className="fa-solid fa-triangle-exclamation mt-0.5"></i><div className="flex-1">{error}</div><button type="button" onClick={() => setError(null)} className="text-red-700 hover:text-red-900"><i className="fa-solid fa-xmark"></i></button></div>}
        <section className="space-y-4">
          <h3 className="text-sm font-bold tracking-wide uppercase">AI Providers</h3>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-xs font-semibold">OpenAI API Key</label>
              <input type="text" placeholder="sk-..." className="px-3 py-2 rounded-lg border-2 border-ink bg-white dark:bg-gray-950 font-mono text-xs" {...bind('openAiKey')} />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-xs font-semibold">Anthropic API Key</label>
              <input type="text" placeholder="anth-..." className="px-3 py-2 rounded-lg border-2 border-ink bg-white dark:bg-gray-950 font-mono text-xs" {...bind('anthropicKey')} />
            </div>
          </div>
        </section>
        <section className="space-y-4">
          <h3 className="text-sm font-bold tracking-wide uppercase">Learning Platforms</h3>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label className="text-xs font-semibold">Canvas API Token</label>
              <input type="text" placeholder="canvas token" className="px-3 py-2 rounded-lg border-2 border-ink bg-white dark:bg-gray-950 font-mono text-xs" {...bind('canvasApiToken')} />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-xs font-semibold">Google API Key</label>
              <input type="text" placeholder="AIza..." className="px-3 py-2 rounded-lg border-2 border-ink bg-white dark:bg-gray-950 font-mono text-xs" {...bind('googleApiKey')} />
            </div>
          </div>
        </section>
        <section className="space-y-4">
          <h3 className="text-sm font-bold tracking-wide uppercase">Home Automation</h3>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-2 md:col-span-1">
              <label className="text-xs font-semibold">Home Assistant URL</label>
              <input type="text" placeholder="https://homeassistant.local" className="px-3 py-2 rounded-lg border-2 border-ink bg-white dark:bg-gray-950 font-mono text-xs" {...bind('homeAssistantUrl')} />
            </div>
            <div className="flex flex-col gap-2 md:col-span-1">
              <label className="text-xs font-semibold">Home Assistant Token</label>
              <input type="text" placeholder="long-lived token" className="px-3 py-2 rounded-lg border-2 border-ink bg-white dark:bg-gray-950 font-mono text-xs" {...bind('homeAssistantToken')} />
            </div>
          </div>
        </section>
        <div className="pt-2 flex items-center justify-between">
          <div className="text-xs text-gray-500">Last updated: {settings.updatedAt ? new Date(settings.updatedAt).toLocaleString() : 'Never'}</div>
          <button disabled={saving} type="submit" className="px-6 py-2 rounded-lg bg-ink text-white font-bold text-sm tracking-wide flex items-center gap-2 disabled:opacity-50">
            {saving && <i className="fa-solid fa-circle-notch fa-spin"></i>}
            {saving ? 'Saving…' : 'Save Settings'}
          </button>
        </div>
      </form>
    </div>
  )
}
