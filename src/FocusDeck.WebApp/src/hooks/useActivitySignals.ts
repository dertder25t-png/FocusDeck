import { useEffect, useRef, useCallback } from 'react'
import { apiFetch } from '../lib/utils'
import { usePrivacySettings } from './usePrivacySettings'

export function useActivitySignals() {
  const { settings } = usePrivacySettings()
  const allowedRef = useRef(new Set<string>())

  useEffect(() => {
    allowedRef.current = new Set(
      settings.filter((setting) => setting.isEnabled).map((setting) => setting.contextType),
    )
  }, [settings])

  const sendSignal = useCallback(
    async (signalType: string, signalValue: string, metadata?: Record<string, unknown>) => {
      if (!allowedRef.current.has(signalType)) {
        return
      }

      const payload: Record<string, unknown> = {
        signalType,
        signalValue,
        sourceApp: 'FocusDeck.WebApp',
        capturedAtUtc: new Date().toISOString(),
      }

      if (metadata) {
        payload.metadataJson = JSON.stringify(metadata)
      }

      try {
        const response = await apiFetch('/v1/activity/signals', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })

        if (!response.ok) {
          const body = await response.text()
          console.error('Activity signal rejected', signalType, body)
        }
      } catch (error) {
        console.error('Failed to send activity signal', signalType, error)
      }
    },
    [],
  )

  useEffect(() => {
    if (typeof window === 'undefined') {
      return
    }

    const typingTimestamps: number[] = []
    let mouseDistance = 0
    let lastMouse: { x: number; y: number } | null = null

    const pruneTyping = () => {
      const now = Date.now()
      const windowMs = 15_000
      const cutoff = now - windowMs
      const recent = typingTimestamps.filter((ts) => ts >= cutoff)
      typingTimestamps.splice(0, typingTimestamps.length, ...recent)
      return { count: recent.length, windowMs }
    }

    const recordKey = () => typingTimestamps.push(Date.now())

    const recordMouse = (event: MouseEvent) => {
      if (lastMouse) {
        const dx = event.clientX - lastMouse.x
        const dy = event.clientY - lastMouse.y
        mouseDistance += Math.hypot(dx, dy)
      }
      lastMouse = { x: event.clientX, y: event.clientY }
    }

    const sendActiveWindow = () => {
      const metadata = { path: window.location.pathname, url: window.location.href }
      sendSignal('ActiveWindowTitle', document.title || 'FocusDeck WebApp', metadata)
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        sendActiveWindow()
      }
    }

    const intervalId = window.setInterval(() => {
      const { count, windowMs } = pruneTyping()
      if (count > 0) {
        sendSignal('TypingVelocity', `keystrokes=${count};windowMs=${windowMs}`, {
          window: window.location.pathname,
        })
      }

      if (mouseDistance > 0) {
        sendSignal('MouseEntropy', `distance=${mouseDistance.toFixed(1)}`, {
          window: window.location.pathname,
        })
        mouseDistance = 0
      }

      sendActiveWindow()
    }, 15_000)

    window.addEventListener('keydown', recordKey)
    window.addEventListener('mousemove', recordMouse)
    window.addEventListener('focus', sendActiveWindow)
    document.addEventListener('visibilitychange', handleVisibilityChange)

    sendActiveWindow()

    return () => {
      window.clearInterval(intervalId)
      window.removeEventListener('keydown', recordKey)
      window.removeEventListener('mousemove', recordMouse)
      window.removeEventListener('focus', sendActiveWindow)
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [sendSignal])
}
