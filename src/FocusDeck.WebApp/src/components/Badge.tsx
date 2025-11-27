import { type HTMLAttributes, forwardRef } from 'react'
import { cn } from '../lib/utils'

export interface BadgeProps extends HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info' | 'secondary'
  size?: 'sm' | 'md' | 'lg'
}

const Badge = forwardRef<HTMLDivElement, BadgeProps>(
  ({ className, variant = 'default', size = 'md', ...props }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          'inline-flex items-center rounded-full font-semibold',
          {
            'px-2 py-0.5 text-xs': size === 'sm',
            'px-2.5 py-0.5 text-xs': size === 'md',
            'px-3 py-1 text-sm': size === 'lg',
          },
          {
            'bg-gray-800 text-gray-300': variant === 'default' || variant === 'secondary',
            'bg-green-500/10 text-green-400 border border-green-500/20': variant === 'success',
            'bg-yellow-500/10 text-yellow-400 border border-yellow-500/20': variant === 'warning',
            'bg-red-500/10 text-red-400 border border-red-500/20': variant === 'danger',
            'bg-blue-500/10 text-blue-400 border border-blue-500/20': variant === 'info',
          },
          className
        )}
        {...props}
      />
    )
  }
)

Badge.displayName = 'Badge'

export { Badge }
