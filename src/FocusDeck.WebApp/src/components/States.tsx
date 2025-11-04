import { type ReactNode } from 'react'
import { Button } from './Button'

interface EmptyStateProps {
  icon?: ReactNode
  title: string
  description?: string
  action?: {
    label: string
    onClick: () => void
  }
}

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      {icon && <div className="mb-4 text-4xl opacity-50">{icon}</div>}
      <h3 className="text-lg font-semibold text-gray-200">{title}</h3>
      {description && <p className="mt-2 text-sm text-gray-400 max-w-sm">{description}</p>}
      {action && (
        <Button onClick={action.onClick} className="mt-6">
          {action.label}
        </Button>
      )}
    </div>
  )
}

interface ErrorStateProps {
  title?: string
  message: string
  action?: {
    label: string
    onClick: () => void
  }
}

export function ErrorState({ 
  title = 'Something went wrong', 
  message, 
  action 
}: ErrorStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      <div className="mb-4 text-4xl">⚠️</div>
      <h3 className="text-lg font-semibold text-red-400">{title}</h3>
      <p className="mt-2 text-sm text-gray-400 max-w-sm">{message}</p>
      {action && (
        <Button onClick={action.onClick} variant="secondary" className="mt-6">
          {action.label}
        </Button>
      )}
    </div>
  )
}
