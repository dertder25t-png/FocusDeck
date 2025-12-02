import { useEffect, useState, createContext, useContext, type ReactNode } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { getOrRefreshAuthToken, refreshAuthToken } from '../lib/utils'
import * as ToastPrimitives from '@radix-ui/react-toast'
import { Toast, ToastTitle, ToastDescription, ToastViewport } from '../components/Toast'

// Extend notification client contract for React usage
interface NotificationContextType {
  connection: HubConnection | null
  notifications: Notification[]
  removeNotification: (id: string) => void
}

interface Notification {
  id: string
  title: string
  message: string
  severity: 'info' | 'success' | 'warning' | 'error'
}

const SignalRContext = createContext<NotificationContextType | null>(null)

export function useSignalR() {
  return useContext(SignalRContext)
}

export function SignalRProvider({ children }: { children: ReactNode }) {
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [notifications, setNotifications] = useState<Notification[]>([])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        // Initial check to see if we have a token or can get one
        // We use catch to return null so we don't crash init
        const token = await getOrRefreshAuthToken().catch(() => null)

        if (!token && !cancelled) {
             console.warn('SignalR init skipped (no auth token)')
             // We could optionally redirect to login here, but the ProtectedRoute should handle that.
             // For now, we just don't connect SignalR.
             return
        }

        if (cancelled) return

        const newConnection = new HubConnectionBuilder()
          .withUrl('/hubs/notifications', {
            accessTokenFactory: async () => {
              try {
                // Always try to get a fresh valid token for the connection
                return await getOrRefreshAuthToken() || ''
              } catch {
                return ''
              }
            }
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build()

        if (!cancelled) setConnection(newConnection)
      } catch (err) {
        console.warn('SignalR init failed', err)
      }
    })()
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (connection) {
      const onReceiveNotification = (title: string, message: string, severity: string) => {
        console.log('Notification received:', title, message)
        const id = Math.random().toString(36).substring(7)
        setNotifications(prev => [...prev, {
          id,
          title,
          message,
          severity: (severity as any) || 'info'
        }])

        // Auto dismiss after 5s
        setTimeout(() => {
          setNotifications(prev => prev.filter(n => n.id !== id))
        }, 5000)
      }

      connection.on('ReceiveNotification', onReceiveNotification)

      const startConnection = async () => {
        try {
          await connection.start()
          console.log('SignalR Connected')
        } catch (err: any) {
          console.error('SignalR Connection Error: ', err)
          // If the error suggests authentication failure (401), try to refresh and retry once
          if (err.toString().includes('401') || (err.statusCode === 401)) {
             console.log('SignalR 401 detected, attempting auth refresh...')
             const newToken = await refreshAuthToken()
             if (newToken) {
                 try {
                     await connection.start()
                     console.log('SignalR Reconnected after auth refresh')
                 } catch (retryErr) {
                     console.error('SignalR Retry failed:', retryErr)
                 }
             }
          }
        }
      }

      startConnection()

      return () => {
        connection.off('ReceiveNotification', onReceiveNotification)
        connection.stop()
      }
    }
  }, [connection])

  const removeNotification = (id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id))
  }

  return (
    <SignalRContext.Provider value={{ connection, notifications, removeNotification }}>
      {children}

      {/* Render Notifications */}
      <ToastPrimitives.Provider>
        {notifications.map(n => (
          <Toast key={n.id} open={true} onOpenChange={() => removeNotification(n.id)} variant={n.severity === 'info' ? 'default' : n.severity as any}>
            <div className="grid gap-1">
              <ToastTitle>{n.title}</ToastTitle>
              <ToastDescription>{n.message}</ToastDescription>
            </div>
          </Toast>
        ))}
        <ToastViewport />
      </ToastPrimitives.Provider>
    </SignalRContext.Provider>
  )
}
